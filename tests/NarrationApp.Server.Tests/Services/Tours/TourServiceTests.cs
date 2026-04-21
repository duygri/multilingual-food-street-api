using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Tour;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.Tours;

public sealed class TourServiceTests
{
    [Fact]
    public async Task CreateAsync_persists_tour_with_ordered_stops()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var sut = CreateSut(dbContext);
        var pois = await dbContext.Pois.OrderBy(item => item.Priority).Take(3).ToListAsync();

        var result = await sut.CreateAsync(new CreateTourRequest
        {
            Title = "Quận 4 buổi sáng",
            Description = "Tour thử nghiệm",
            EstimatedMinutes = 45,
            CoverImage = "https://example.com/tour.jpg",
            Stops =
            [
                new UpsertTourStopRequest { PoiId = pois[0].Id, Sequence = 1, RadiusMeters = AppConstants.DefaultTourStopRadiusMeters },
                new UpsertTourStopRequest { PoiId = pois[1].Id, Sequence = 2, RadiusMeters = AppConstants.DefaultTourStopRadiusMeters },
                new UpsertTourStopRequest { PoiId = pois[2].Id, Sequence = 3, RadiusMeters = AppConstants.DefaultTourStopRadiusMeters }
            ]
        });

        Assert.Equal(TourStatus.Draft, result.Status);
        Assert.Equal(3, result.Stops.Count);
        Assert.Equal(new[] { 1, 2, 3 }, result.Stops.Select(item => item.Sequence).ToArray());
        Assert.Equal(1, await dbContext.Tours.CountAsync());
        Assert.Equal(3, await dbContext.TourStops.CountAsync());
    }

    [Fact]
    public async Task StartAsync_creates_new_session_and_exposes_it_as_latest()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var tourist = await TestAppDbContextFactory.AddTouristAsync(dbContext, "tourist-session@narration.app");
        var sut = CreateSut(dbContext);
        var tour = await CreatePublishedTourAsync(sut, dbContext);

        var session = await sut.StartAsync(tour.Id, tourist.Id, "device-tour-1");
        var latest = await sut.GetLatestSessionAsync(tourist.Id);

        Assert.NotNull(latest);
        Assert.Equal(session.Id, latest!.Id);
        Assert.Equal(TourSessionStatus.InProgress, latest.Status);
        Assert.Equal(0, latest.CurrentStopSequence);
        Assert.Equal(tour.Id, latest.TourId);
        Assert.Equal(2, latest.TotalStops);
    }

    [Fact]
    public async Task ProgressAsync_advances_session_and_marks_it_completed_after_last_stop()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var tourist = await TestAppDbContextFactory.AddTouristAsync(dbContext, "tourist-progress@narration.app");
        var sut = CreateSut(dbContext);
        var tour = await CreatePublishedTourAsync(sut, dbContext);

        await sut.StartAsync(tour.Id, tourist.Id, "device-tour-2");
        var first = await sut.ProgressAsync(
            tour.Id,
            tourist.Id,
            new UpdateTourProgressRequest
            {
                PoiId = tour.Stops[0].PoiId,
                DeviceId = "device-tour-2"
            });
        var second = await sut.ProgressAsync(
            tour.Id,
            tourist.Id,
            new UpdateTourProgressRequest
            {
                PoiId = tour.Stops[1].PoiId,
                DeviceId = "device-tour-2"
            });

        Assert.Equal(TourSessionStatus.InProgress, first.Status);
        Assert.Equal(1, first.CurrentStopSequence);
        Assert.Equal(TourSessionStatus.Completed, second.Status);
        Assert.Equal(2, second.CurrentStopSequence);
        Assert.NotNull(second.CompletedAtUtc);
        Assert.Equal(2, await dbContext.VisitEvents.CountAsync(item => item.UserId == tourist.Id && item.EventType == EventType.TourProgress));
    }

    [Fact]
    public async Task ResumeAsync_reopens_the_latest_paused_session_for_the_requested_tour()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var tourist = await TestAppDbContextFactory.AddTouristAsync(dbContext, "tourist-resume@narration.app");
        var sut = CreateSut(dbContext);
        var firstTour = await CreatePublishedTourAsync(sut, dbContext);
        var secondTour = await CreatePublishedTourAsync(sut, dbContext, "Tour phụ");

        var firstSession = await sut.StartAsync(firstTour.Id, tourist.Id, "device-tour-3");
        await sut.StartAsync(secondTour.Id, tourist.Id, "device-tour-3");

        var resumed = await sut.ResumeAsync(firstTour.Id, tourist.Id);
        var sessions = await dbContext.TourSessions
            .AsNoTracking()
            .Where(item => item.UserId == tourist.Id)
            .OrderBy(item => item.Id)
            .ToListAsync();

        Assert.Equal(firstSession.Id, resumed.Id);
        Assert.Equal(TourSessionStatus.InProgress, resumed.Status);
        Assert.Contains(sessions, item => item.TourId == secondTour.Id && item.Status == TourSessionStatus.Paused);
    }

    [Fact]
    public async Task GetAsync_for_public_only_returns_published_tours()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var sut = CreateSut(dbContext);
        await CreateTourAsync(sut, dbContext, "Draft tour", TourStatus.Draft);
        await CreateTourAsync(sut, dbContext, "Published tour", TourStatus.Published);

        var publicTours = await sut.GetAsync(includeUnpublished: false);
        var adminTours = await sut.GetAsync(includeUnpublished: true);

        Assert.Single(publicTours);
        Assert.Equal("Published tour", publicTours[0].Title);
        Assert.Equal(2, adminTours.Count);
    }

    [Fact]
    public async Task UpdateAsync_when_publishing_a_tour_creates_notifications_for_active_tourists()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        await TestAppDbContextFactory.AddTouristAsync(dbContext, "tourist-a@narration.app");
        await TestAppDbContextFactory.AddTouristAsync(dbContext, "tourist-b@narration.app");
        var sut = CreateSut(dbContext);
        var draftTour = await CreateTourAsync(sut, dbContext, "Tour phát hành", TourStatus.Draft);

        await sut.UpdateAsync(
            draftTour.Id,
            new UpdateTourRequest
            {
                Title = draftTour.Title,
                Description = draftTour.Description,
                EstimatedMinutes = draftTour.EstimatedMinutes,
                CoverImage = draftTour.CoverImage,
                Status = TourStatus.Published,
                Stops = draftTour.Stops
                    .Select(stop => new UpsertTourStopRequest
                    {
                        PoiId = stop.PoiId,
                        Sequence = stop.Sequence,
                        RadiusMeters = stop.RadiusMeters
                    })
                    .ToArray()
            });

        Assert.Equal(2, await dbContext.Notifications.CountAsync(item => item.Type == NotificationType.TourPublished));
    }

    [Fact]
    public async Task DeleteAsync_removes_the_tour_with_its_stops_and_sessions()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var tourist = await TestAppDbContextFactory.AddTouristAsync(dbContext, "tourist-delete@narration.app");
        var sut = CreateSut(dbContext);
        var tour = await CreatePublishedTourAsync(sut, dbContext, "Tour xoa");

        await sut.StartAsync(tour.Id, tourist.Id, "device-tour-delete");
        await sut.DeleteAsync(tour.Id);

        Assert.Equal(0, await dbContext.Tours.CountAsync(item => item.Id == tour.Id));
        Assert.Equal(0, await dbContext.TourStops.CountAsync(item => item.TourId == tour.Id));
        Assert.Equal(0, await dbContext.TourSessions.CountAsync(item => item.TourId == tour.Id));
    }

    private static TourService CreateSut(Server.Data.AppDbContext dbContext)
    {
        var notifications = new NotificationService(dbContext, new NullNotificationBroadcaster());
        return new TourService(dbContext, notifications, new VisitEventService(dbContext));
    }

    private static async Task<TourDto> CreatePublishedTourAsync(TourService sut, Server.Data.AppDbContext dbContext, string title = "Tour mẫu")
    {
        return await CreateTourAsync(sut, dbContext, title, TourStatus.Published);
    }

    private static async Task<TourDto> CreateTourAsync(TourService sut, Server.Data.AppDbContext dbContext, string title, TourStatus status)
    {
        var pois = await dbContext.Pois
            .OrderByDescending(item => item.Priority)
            .Take(2)
            .ToListAsync();

        var created = await sut.CreateAsync(new CreateTourRequest
        {
            Title = title,
            Description = $"{title} description",
            EstimatedMinutes = 30,
            Stops =
            [
                new UpsertTourStopRequest { PoiId = pois[0].Id, Sequence = 1, RadiusMeters = AppConstants.DefaultTourStopRadiusMeters },
                new UpsertTourStopRequest { PoiId = pois[1].Id, Sequence = 2, RadiusMeters = AppConstants.DefaultTourStopRadiusMeters }
            ]
        });

        if (status == TourStatus.Published)
        {
            return await sut.UpdateAsync(
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

        return created;
    }
}
