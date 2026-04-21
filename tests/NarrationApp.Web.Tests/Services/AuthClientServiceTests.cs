using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NarrationApp.Shared.DTOs.Auth;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.SharedUI.Auth;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Tests.Services;

public sealed class AuthClientServiceTests
{
    [Fact]
    public async Task RegisterOwnerAsync_posts_owner_application_without_authenticating_session()
    {
        var handler = new RegisterOwnerHandler();
        var store = new TestAuthSessionStore();
        var authStateProvider = new CustomAuthStateProvider(store);
        var apiClient = new ApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:5001/")
        }, store);
        var sut = new AuthClientService(apiClient, authStateProvider);

        var response = await sut.RegisterOwnerAsync(new RegisterOwnerRequest
        {
            FullName = "Owner Candidate",
            Email = "candidate-owner@narration.app",
            Password = "Owner@123"
        });

        Assert.Equal("/api/auth/register-owner", handler.RequestPath);
        Assert.Contains("\"fullName\":\"Owner Candidate\"", handler.SerializedBody);
        Assert.Equal("candidate-owner@narration.app", response.Email);
        Assert.Null(store.Session);
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

    private sealed class RegisterOwnerHandler : HttpMessageHandler
    {
        public string RequestPath { get; private set; } = string.Empty;

        public string SerializedBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestPath = request.RequestUri?.AbsolutePath ?? string.Empty;
            SerializedBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ApiResponse<OwnerRegistrationResponse>
                {
                    Succeeded = true,
                    Message = "submitted",
                    Data = new OwnerRegistrationResponse
                    {
                        UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                        Email = "candidate-owner@narration.app",
                        SubmittedAtUtc = DateTime.UtcNow
                    }
                }, options: new JsonSerializerOptions(JsonSerializerDefaults.Web))
            };
        }
    }
}
