using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Auth;

namespace NarrationApp.Server.Tests.Integration;

public sealed class RequestDiagnosticsIntegrationTests
{
    [Fact]
    public async Task Login_endpoint_echoes_correlation_id_header()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.SeedAsync();

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest
            {
                Email = AppConstants.DefaultAdminEmail,
                Password = AppConstants.DefaultAdminPassword
            })
        };

        request.Headers.Add(AppConstants.CorrelationIdHeaderName, "integration-corr-001");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(AppConstants.CorrelationIdHeaderName, out var correlationIds));
        Assert.Equal("integration-corr-001", Assert.Single(correlationIds));
    }
}
