using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.Enums;
using NarrationApp.SharedUI.Auth;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Services;

public sealed class AudioPortalServiceTests
{
    [Fact]
    public async Task UploadAsync_sends_multipart_form_data_and_returns_audio()
    {
        var handler = new InspectingAudioUploadHandler();
        var sessionStore = new TestAuthSessionStore
        {
            Session = new AuthSession
            {
                UserId = Guid.NewGuid(),
                Email = "owner@narration.app",
                Role = UserRole.PoiOwner,
                PreferredLanguage = "vi",
                Token = "jwt-token"
            }
        };

        var apiClient = new ApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:5001/")
        }, sessionStore);
        var sut = new AudioPortalService(apiClient);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake mp3 bytes"));
        var response = await sut.UploadAsync(new UploadAudioRequest
        {
            PoiId = 7,
            LanguageCode = "vi",
            FileName = "vinh-khanh.mp3"
        }, stream);

        Assert.Equal(7, response.PoiId);
        Assert.Equal("vi", handler.FormValues["languageCode"]);
        Assert.Equal("7", handler.FormValues["poiId"]);
        Assert.Contains("Bearer jwt-token", handler.AuthorizationHeader);
    }

    [Fact]
    public async Task UpdateAsync_and_DeleteAsync_send_authorized_requests()
    {
        var handler = new InspectingAudioMutationHandler();
        var sessionStore = new TestAuthSessionStore
        {
            Session = new AuthSession
            {
                UserId = Guid.NewGuid(),
                Email = "admin@narration.app",
                Role = UserRole.Admin,
                PreferredLanguage = "vi",
                Token = "jwt-token"
            }
        };

        var apiClient = new ApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:5001/")
        }, sessionStore);
        var sut = new AudioPortalService(apiClient);

        var updated = await sut.UpdateAsync(88, new UpdateAudioRequest
        {
            LanguageCode = "en",
            Provider = "restored-provider",
            StoragePath = "audio/restored.mp3",
            Url = "https://localhost:5001/api/audio/88/stream",
            Status = AudioStatus.Replaced,
            DurationSeconds = 47
        });

        await sut.DeleteAsync(88);

        Assert.Equal(HttpMethod.Put, handler.RequestMethods[0]);
        Assert.Equal(HttpMethod.Delete, handler.RequestMethods[1]);
        Assert.Contains("Bearer jwt-token", handler.AuthorizationHeaders[0]);
        Assert.Equal("restored-provider", updated.Provider);
        Assert.Contains("\"provider\":\"restored-provider\"", handler.SerializedBodies[0]);
    }

    private sealed class TestAuthSessionStore : IAuthSessionStore
    {
        public AuthSession? Session { get; set; }

        public ValueTask<AuthSession?> GetAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(Session);
        }

        public ValueTask SetAsync(AuthSession session, CancellationToken cancellationToken = default)
        {
            Session = session;
            return ValueTask.CompletedTask;
        }

        public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            Session = null;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class InspectingAudioUploadHandler : HttpMessageHandler
    {
        public Dictionary<string, string> FormValues { get; } = [];

        public string AuthorizationHeader { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AuthorizationHeader = request.Headers.Authorization?.ToString() ?? string.Empty;

            if (request.Content is MultipartFormDataContent form)
            {
                foreach (var part in form)
                {
                    var name = part.Headers.ContentDisposition?.Name?.Trim('"');
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    FormValues[name] = await part.ReadAsStringAsync(cancellationToken);
                }
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ApiResponse<AudioDto>
                {
                    Succeeded = true,
                    Message = "ok",
                    Data = new AudioDto
                    {
                        Id = 88,
                        PoiId = 7,
                        LanguageCode = "vi",
                        SourceType = AudioSourceType.Recorded,
                        Provider = "manual-upload",
                        StoragePath = "audio/vinh-khanh.mp3",
                        Url = "https://localhost:5001/api/audio/88/stream",
                        Status = AudioStatus.Ready,
                        DurationSeconds = 12,
                        GeneratedAtUtc = DateTime.UtcNow
                    }
                }, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
            };
        }
    }

    private sealed class InspectingAudioMutationHandler : HttpMessageHandler
    {
        public List<HttpMethod> RequestMethods { get; } = [];

        public List<string> AuthorizationHeaders { get; } = [];

        public List<string> SerializedBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestMethods.Add(request.Method);
            AuthorizationHeaders.Add(request.Headers.Authorization?.ToString() ?? string.Empty);

            if (request.Content is not null)
            {
                SerializedBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }

            if (request.Method == HttpMethod.Put)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new ApiResponse<AudioDto>
                    {
                        Succeeded = true,
                        Message = "ok",
                        Data = new AudioDto
                        {
                            Id = 88,
                            PoiId = 7,
                            LanguageCode = "en",
                            SourceType = AudioSourceType.Recorded,
                            Provider = "restored-provider",
                            StoragePath = "audio/restored.mp3",
                            Url = "https://localhost:5001/api/audio/88/stream",
                            Status = AudioStatus.Replaced,
                            DurationSeconds = 47,
                            GeneratedAtUtc = DateTime.UtcNow
                        }
                    }, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ApiResponse<object?>
                {
                    Succeeded = true,
                    Message = "deleted",
                    Data = null
                }, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
            };
        }
    }
}
