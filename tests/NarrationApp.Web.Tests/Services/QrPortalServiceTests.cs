using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.QR;
using NarrationApp.Shared.Enums;
using NarrationApp.SharedUI.Auth;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Services;

public sealed class QrPortalServiceTests
{
    [Fact]
    public async Task GetAsync_and_DeleteAsync_send_authorized_qr_requests()
    {
        var handler = new InspectingQrHandler();
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
        var sut = new QrPortalService(apiClient);

        var filtered = await sut.GetAsync("tour");
        await sut.DeleteAsync(9);

        Assert.Single(filtered);
        Assert.Equal("api/qr?targetType=tour", handler.RequestUris[0]);
        Assert.Equal(HttpMethod.Delete, handler.Methods[1]);
        Assert.Equal("api/qr/9", handler.RequestUris[1]);
        Assert.All(handler.AuthorizationHeaders, header => Assert.Contains("Bearer jwt-token", header));
    }

    [Fact]
    public async Task CreateAsync_posts_qr_request_and_returns_qr_code()
    {
        var handler = new InspectingQrHandler();
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
        var sut = new QrPortalService(apiClient);

        var created = await sut.CreateAsync(new CreateQrRequest
        {
            TargetType = "tour",
            TargetId = 7,
            LocationHint = "Cổng tour đêm"
        });

        Assert.Equal(HttpMethod.Post, handler.Methods[0]);
        Assert.Contains("\"targetType\":\"tour\"", handler.SerializedBodies[0]);
        Assert.Equal("QR-NEW-001", created.Code);
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

    private sealed class InspectingQrHandler : HttpMessageHandler
    {
        public List<HttpMethod> Methods { get; } = [];

        public List<string> RequestUris { get; } = [];

        public List<string> AuthorizationHeaders { get; } = [];

        public List<string> SerializedBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Methods.Add(request.Method);
            RequestUris.Add(request.RequestUri?.PathAndQuery.TrimStart('/') ?? string.Empty);
            AuthorizationHeaders.Add(request.Headers.Authorization?.ToString() ?? string.Empty);

            if (request.Content is not null)
            {
                SerializedBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }

            if (request.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new ApiResponse<IReadOnlyList<QrCodeDto>>
                    {
                        Succeeded = true,
                        Message = "ok",
                        Data =
                        [
                            new QrCodeDto
                            {
                                Id = 9,
                                Code = "QR-TOUR-009",
                                TargetType = "tour",
                                TargetId = 7,
                                LocationHint = "Tour booth"
                            }
                        ]
                    }, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
                };
            }

            if (request.Method == HttpMethod.Post)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new ApiResponse<QrCodeDto>
                    {
                        Succeeded = true,
                        Message = "ok",
                        Data = new QrCodeDto
                        {
                            Id = 10,
                            Code = "QR-NEW-001",
                            TargetType = "tour",
                            TargetId = 7,
                            LocationHint = "Cổng tour đêm"
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
