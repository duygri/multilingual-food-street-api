using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Tasks;
using FoodStreet.Server.Services.Audio;
using FoodStreet.Server.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using Microsoft.Extensions.Options;
using PROJECT_C_.Configuration;
using FoodStreet.Server.Constants;
using FoodStreet.Server.Services.GoogleCloud;
using System.IO;

namespace FoodStreet.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminAudioController : ControllerBase
    {
        private readonly AudioTaskManager _taskManager;
        private readonly GoogleCloudOptions _googleCloudOptions;
        private readonly IGoogleCloudAccessTokenProvider _accessTokenProvider;
        private readonly GoogleTranslator _translator;
        private readonly ITtsService _ttsService;

        public AdminAudioController(
            AudioTaskManager taskManager,
            IOptions<GoogleCloudOptions> googleCloudOptions,
            IGoogleCloudAccessTokenProvider accessTokenProvider,
            GoogleTranslator translator,
            ITtsService ttsService)
        {
            _taskManager = taskManager;
            _googleCloudOptions = googleCloudOptions.Value;
            _accessTokenProvider = accessTokenProvider;
            _translator = translator;
            _ttsService = ttsService;
        }

        // Tier 2 API: GET /api/adminaudio/tasks
        [HttpGet("tasks")]
        public IActionResult GetTasks()
        {
            return Ok(_taskManager.GetAllTasks());
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var tasks = _taskManager.GetAllTasks().ToList();
            string? projectId = _googleCloudOptions.ProjectId;

            try
            {
                projectId = await _accessTokenProvider.GetProjectIdAsync();
            }
            catch
            {
                // Status endpoint should stay resilient even when project resolution is incomplete.
            }

            var credentialPath = _accessTokenProvider.GetConfiguredCredentialPath();

            return Ok(new
            {
                Provider = "Google Cloud TTS",
                AuthMode = _accessTokenProvider.GetAuthMode(),
                ProjectId = projectId,
                HasServiceAccountJson = !string.IsNullOrWhiteSpace(credentialPath),
                CredentialFile = string.IsNullOrWhiteSpace(credentialPath) ? null : Path.GetFileName(credentialPath),
                LastAuthError = _accessTokenProvider.GetLastAuthError(),
                TotalTasks = tasks.Count,
                RunningTasks = tasks.Count(task => string.Equals(task.Status, PoiAudioStatuses.Running, StringComparison.OrdinalIgnoreCase)),
                ReadyTasks = tasks.Count(task => string.Equals(task.Status, PoiAudioStatuses.Ready, StringComparison.OrdinalIgnoreCase)),
                FailedTasks = tasks.Count(task => string.Equals(task.Status, PoiAudioStatuses.Failed, StringComparison.OrdinalIgnoreCase))
            });
        }

        [HttpPost("health-check")]
        public async Task<IActionResult> RunHealthCheck()
        {
            const string translateInput = "Xin chào Vĩnh Khánh";
            const string ttsInput = "Xin chào, đây là kiểm tra nhanh Google Cloud Text-to-Speech.";

            string? projectId = _googleCloudOptions.ProjectId;
            try
            {
                projectId = await _accessTokenProvider.GetProjectIdAsync();
            }
            catch
            {
                // Keep the endpoint resilient and still return probe details.
            }

            string translatedText;
            string? ttsUrl;
            string? translateError = null;
            string? ttsError = null;
            var useApiKey = !string.IsNullOrWhiteSpace(_googleCloudOptions.ApiKey);
            var authProbeSucceeded = useApiKey;
            string authProbeMessage;

            if (useApiKey)
            {
                authProbeMessage = "Đang chạy bằng API key.";
            }
            else
            {
                try
                {
                    var accessToken = await _accessTokenProvider.GetAccessTokenAsync();
                    authProbeSucceeded = !string.IsNullOrWhiteSpace(accessToken);
                    authProbeMessage = authProbeSucceeded
                        ? "Lấy access token thành công."
                        : _accessTokenProvider.GetLastAuthError() ?? "Không nhận được access token.";
                }
                catch (Exception ex)
                {
                    authProbeSucceeded = false;
                    authProbeMessage = ex.Message;
                }
            }

            if (authProbeSucceeded)
            {
                try
                {
                    translatedText = await _translator.TranslateTextAsync(translateInput, "en-US");
                }
                catch (Exception ex)
                {
                    translatedText = translateInput;
                    translateError = ex.Message;
                }
            }
            else
            {
                translatedText = translateInput;
                translateError = authProbeMessage;
            }

            if (authProbeSucceeded)
            {
                try
                {
                    ttsUrl = await _ttsService.TextToSpeechAsync(ttsInput, "vi-VN");
                }
                catch (Exception ex)
                {
                    ttsUrl = null;
                    ttsError = ex.Message;
                }
            }
            else
            {
                ttsUrl = null;
                ttsError = authProbeMessage;
            }

            var translateSucceeded =
                string.IsNullOrWhiteSpace(translateError) &&
                !string.IsNullOrWhiteSpace(translatedText) &&
                !string.Equals(translatedText.Trim(), translateInput, StringComparison.OrdinalIgnoreCase);

            var ttsSucceeded =
                string.IsNullOrWhiteSpace(ttsError) &&
                !string.IsNullOrWhiteSpace(ttsUrl);

            return Ok(new
            {
                CheckedAtUtc = DateTime.UtcNow,
                AuthMode = _accessTokenProvider.GetAuthMode(),
                ProjectId = projectId,
                AuthProbe = new
                {
                    Success = authProbeSucceeded,
                    Message = authProbeMessage,
                    LastAuthError = _accessTokenProvider.GetLastAuthError()
                },
                Translate = new
                {
                    Success = translateSucceeded,
                    Input = translateInput,
                    OutputPreview = translatedText.Length > 140 ? $"{translatedText[..137]}..." : translatedText,
                    Message = translateSucceeded
                        ? "Dịch thử nghiệm thành công."
                        : translateError ?? _accessTokenProvider.GetLastAuthError() ?? "Kết quả dịch không thay đổi, cần kiểm tra thêm credential/quota."
                },
                Tts = new
                {
                    Success = ttsSucceeded,
                    Input = ttsInput,
                    StaticUrl = ttsUrl,
                    Message = ttsSucceeded
                        ? "Sinh audio thử nghiệm thành công."
                        : ttsError ?? _accessTokenProvider.GetLastAuthError() ?? "Không nhận được URL audio từ Google TTS."
                }
            });
        }

        // Tier 2 API: GET /api/adminaudio/tasks/stream
        // SSE Real-time Endpoint
        [HttpGet("tasks/stream")]
        public async Task StreamTasks(CancellationToken cancellationToken)
        {
            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            // Initial dump
            var allTasks = _taskManager.GetAllTasks();
            var initData = JsonSerializer.Serialize(allTasks);
            await Response.WriteAsync($"data: {initData}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            // Setup event listener
            void TaskUpdateHandler(AudioTaskInfo info)
            {
                var payload = JsonSerializer.Serialize(new[] { info });
                var message = $"data: {payload}\n\n";
                // Fire and forget send
                _ = Response.WriteAsync(message, cancellationToken).ContinueWith(t => 
                {
                    if (!t.IsFaulted) _ = Response.Body.FlushAsync(cancellationToken);
                });
            }

            _taskManager.OnTaskChanged += TaskUpdateHandler;

            try
            {
                // Keep the connection open until client disconnects
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(5000, cancellationToken);
                    await Response.WriteAsync(": heartbeat\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                // Client disconnected
            }
            finally
            {
                _taskManager.OnTaskChanged -= TaskUpdateHandler;
            }
        }
    }
}
