using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NarrationApp.Server.Data;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Auth;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Tour;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Integration;

public sealed class TouristTourSessionIntegrationTests
{
    [Fact]
    public async Task Registered_tourist_can_login_and_complete_tour_session_flow()
    {
        await using var factory = new TestWebApplicationFactory();
        await factory.SeedAsync();
        var publishedTour = await CreatePublishedTourAsync(factory);

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var email = $"tourist-{Guid.NewGuid():N}@narration.app";
        var password = "Tourist@123";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = password,
            PreferredLanguage = "en"
        });

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login-tourist", new LoginRequest
        {
            Email = email,
            Password = password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var authEnvelope = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        var auth = Assert.IsType<AuthResponse>(authEnvelope?.Data);
        Assert.Equal(UserRole.Tourist, auth.Role);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var meResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var toursResponse = await client.GetAsync("/api/tours");
        Assert.Equal(HttpStatusCode.OK, toursResponse.StatusCode);
        var toursEnvelope = await toursResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TourDto>>>();
        var tour = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<TourDto>>(toursEnvelope?.Data), item => item.Id == publishedTour.Id);
        Assert.Equal(TourStatus.Published, tour.Status);

        var latestBeforeStartResponse = await client.GetAsync("/api/tours/session/latest");
        Assert.Equal(HttpStatusCode.OK, latestBeforeStartResponse.StatusCode);
        var latestBeforeStart = await latestBeforeStartResponse.Content.ReadFromJsonAsync<ApiResponse<TourSessionDto?>>();
        Assert.Null(latestBeforeStart?.Data);

        var startResponse = await client.PostAsJsonAsync($"/api/tours/{tour.Id}/start", new { deviceId = "android-emulator-01" });
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        var started = Assert.IsType<TourSessionDto>((await startResponse.Content.ReadFromJsonAsync<ApiResponse<TourSessionDto>>())?.Data);
        Assert.Equal(TourSessionStatus.InProgress, started.Status);
        Assert.Equal(0, started.CurrentStopSequence);

        var firstProgressResponse = await client.PostAsJsonAsync(
            $"/api/tours/{tour.Id}/progress",
            new UpdateTourProgressRequest
            {
                PoiId = tour.Stops[0].PoiId,
                DeviceId = "android-emulator-01",
                Lat = 10.7609,
                Lng = 106.7054
            });

        Assert.Equal(HttpStatusCode.OK, firstProgressResponse.StatusCode);
        var firstProgress = Assert.IsType<TourSessionDto>((await firstProgressResponse.Content.ReadFromJsonAsync<ApiResponse<TourSessionDto>>())?.Data);
        Assert.Equal(TourSessionStatus.InProgress, firstProgress.Status);
        Assert.Equal(1, firstProgress.CurrentStopSequence);

        var latestAfterProgressResponse = await client.GetAsync("/api/tours/session/latest");
        Assert.Equal(HttpStatusCode.OK, latestAfterProgressResponse.StatusCode);
        var latestAfterProgress = Assert.IsType<TourSessionDto>((await latestAfterProgressResponse.Content.ReadFromJsonAsync<ApiResponse<TourSessionDto?>>())?.Data);
        Assert.Equal(firstProgress.Id, latestAfterProgress.Id);
        Assert.Equal(1, latestAfterProgress.CurrentStopSequence);

        var secondProgressResponse = await client.PostAsJsonAsync(
            $"/api/tours/{tour.Id}/progress",
            new UpdateTourProgressRequest
            {
                PoiId = tour.Stops[1].PoiId,
                DeviceId = "android-emulator-01",
                Lat = 10.7612,
                Lng = 106.7061
            });

        Assert.Equal(HttpStatusCode.OK, secondProgressResponse.StatusCode);
        var completed = Assert.IsType<TourSessionDto>((await secondProgressResponse.Content.ReadFromJsonAsync<ApiResponse<TourSessionDto>>())?.Data);
        Assert.Equal(TourSessionStatus.Completed, completed.Status);
        Assert.Equal(2, completed.CurrentStopSequence);
        Assert.NotNull(completed.CompletedAtUtc);
    }

    private static async Task<TourDto> CreatePublishedTourAsync(TestWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tourService = scope.ServiceProvider.GetRequiredService<ITourService>();
        var pois = await dbContext.Pois
            .OrderByDescending(item => item.Priority)
            .Take(2)
            .ToListAsync();

        var created = await tourService.CreateAsync(new CreateTourRequest
        {
            Title = "Integration Tour",
            Description = "Tour dùng cho integration test",
            EstimatedMinutes = 30,
            Stops =
            [
                new UpsertTourStopRequest { PoiId = pois[0].Id, Sequence = 1, RadiusMeters = AppConstants.DefaultTourStopRadiusMeters },
                new UpsertTourStopRequest { PoiId = pois[1].Id, Sequence = 2, RadiusMeters = AppConstants.DefaultTourStopRadiusMeters }
            ]
        });

        return await tourService.UpdateAsync(
            created.Id,
            new UpdateTourRequest
            {
                Title = created.Title,
                Description = created.Description,
                EstimatedMinutes = created.EstimatedMinutes,
                CoverImage = created.CoverImage,
                Status = TourStatus.Published,
                Stops = created.Stops
                    .Select(stop => new UpsertTourStopRequest
                    {
                        PoiId = stop.PoiId,
                        Sequence = stop.Sequence,
                        RadiusMeters = stop.RadiusMeters
                    })
                    .ToArray()
            });
    }
}
