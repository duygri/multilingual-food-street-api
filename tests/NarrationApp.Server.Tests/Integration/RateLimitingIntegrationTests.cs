using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Auth;

namespace NarrationApp.Server.Tests.Integration;

public sealed class RateLimitingIntegrationTests
{
    [Fact]
    public async Task Login_endpoint_returns_too_many_requests_after_auth_limit_is_exhausted()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.SeedAsync();

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var request = new LoginRequest
        {
            Email = AppConstants.DefaultAdminEmail,
            Password = AppConstants.DefaultAdminPassword
        };

        var responses = new List<HttpResponseMessage>();
        for (var attempt = 0; attempt < 7; attempt++)
        {
            responses.Add(await client.PostAsJsonAsync("/api/auth/login", request));
        }

        Assert.All(responses.Take(6), response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.Equal(HttpStatusCode.TooManyRequests, responses[6].StatusCode);
    }
}
