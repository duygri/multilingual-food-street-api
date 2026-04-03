using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using FoodStreet.Server.Services.Interfaces;
using Microsoft.Extensions.Logging;
using FoodStreet.Server.Services.GoogleCloud;
using Microsoft.Extensions.Options;
using PROJECT_C_.Configuration;

namespace FoodStreet.Server.Services.Audio
{
    public class GoogleTtsService : ITtsService
    {
        private readonly HttpClient _httpClient;
        private readonly GoogleCloudOptions _options;
        private readonly IGoogleCloudAccessTokenProvider _accessTokenProvider;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<GoogleTtsService> _logger;
        private int _authModeLogged;

        public GoogleTtsService(
            HttpClient httpClient,
            IOptions<GoogleCloudOptions> options,
            IGoogleCloudAccessTokenProvider accessTokenProvider,
            IWebHostEnvironment env,
            ILogger<GoogleTtsService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _accessTokenProvider = accessTokenProvider;
            _env = env;
            _logger = logger;
        }

        public async Task<string?> TextToSpeechAsync(string text, string? language = "vi-VN")
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            try
            {
                LogAuthModeOnce();

                string voiceName = GetVoiceName(language);
                string langCode = GetLangCode(language);
                string fileName = BuildDeterministicFileName(text, langCode, voiceName);
                string folderPath = Path.Combine(_env.WebRootPath, "audio", "tts");
                string fullPath = Path.Combine(folderPath, fileName);
                string staticUrl = $"/audio/tts/{fileName}";

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                if (File.Exists(fullPath) && new FileInfo(fullPath).Length > 0)
                {
                    return staticUrl;
                }

                var url = !string.IsNullOrWhiteSpace(_options.ApiKey)
                    ? $"https://texttospeech.googleapis.com/v1/text:synthesize?key={_options.ApiKey}"
                    : "https://texttospeech.googleapis.com/v1/text:synthesize";

                var requestBody = new
                {
                    input = new { text = text },
                    voice = new { languageCode = langCode, name = voiceName },
                    audioConfig = new { audioEncoding = "MP3" }
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = JsonContent.Create(requestBody)
                };

                if (string.IsNullOrWhiteSpace(_options.ApiKey))
                {
                    var accessToken = await _accessTokenProvider.GetAccessTokenAsync();
                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                }

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Google TTS API Error: {response.StatusCode} - {errorJson}");
                    return null;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var document = JsonDocument.Parse(jsonResponse);
                
                string base64Audio = document.RootElement.GetProperty("audioContent").GetString() ?? string.Empty;
                if (string.IsNullOrEmpty(base64Audio)) return null;

                byte[] audioBytes = Convert.FromBase64String(base64Audio);

                await File.WriteAllBytesAsync(fullPath, audioBytes);

                return staticUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GoogleTtsService.");
                return null;
            }
        }

        private void LogAuthModeOnce()
        {
            if (Interlocked.Exchange(ref _authModeLogged, 1) != 0)
            {
                return;
            }

            _logger.LogInformation(
                "[GoogleTtsService] Runtime auth mode: {AuthMode}",
                _accessTokenProvider.GetAuthMode());
        }

        private string GetLangCode(string? lang)
        {
            return lang?.Trim().ToLowerInvariant() switch
            {
                "en" or "en-us" => "en-US",
                "zh" or "zh-cn" => "cmn-CN",
                "ja" or "ja-jp" => "ja-JP",
                "ko" or "ko-kr" => "ko-KR",
                "vi" or "vi-vn" => "vi-VN",
                _ => "vi-VN"
            };
        }

        private string GetVoiceName(string? lang)
        {
            return lang?.Trim().ToLowerInvariant() switch
            {
                "en" or "en-us" => "en-US-Neural2-F",
                "zh" or "zh-cn" => "cmn-CN-Wavenet-A",
                "ja" or "ja-jp" => "ja-JP-Neural2-B",
                "ko" or "ko-kr" => "ko-KR-Wavenet-A",
                "vi" or "vi-vn" => "vi-VN-Wavenet-A",
                _ => "vi-VN-Wavenet-A"
            };
        }

        private static string BuildDeterministicFileName(string text, string langCode, string voiceName)
        {
            var payload = $"{langCode}:{voiceName}:{text}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
            return $"tts_{Convert.ToHexString(hash).ToLowerInvariant()}.mp3";
        }
    }
}
