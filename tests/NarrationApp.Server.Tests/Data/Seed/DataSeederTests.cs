using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Server.Data.Seed;
using NarrationApp.Server.Services;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Analytics;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Data.Seed;

public sealed class DataSeederTests
{
    [Fact]
    public async Task SeedAsync_populates_the_expected_baseline_records()
    {
        await using var dbContext = CreateDbContext();
        var sut = new DataSeeder(dbContext, NullLogger<DataSeeder>.Instance);

        await sut.SeedAsync();

        Assert.Equal(3, await dbContext.Roles.CountAsync());
        Assert.Equal(13, await dbContext.AppUsers.CountAsync());
        Assert.Equal(12, await dbContext.Pois.CountAsync());
        Assert.Equal(12, await dbContext.Geofences.CountAsync());
        Assert.Equal(12, await dbContext.PoiTranslations.CountAsync());
        Assert.Equal(5, await dbContext.ManagedLanguages.CountAsync());
        Assert.Equal(1, await dbContext.Tours.CountAsync());
        Assert.Equal(6, await dbContext.TourStops.CountAsync());
        Assert.Equal(3, await dbContext.QrCodes.CountAsync());
        Assert.Equal(121, await dbContext.VisitEvents.CountAsync(item => item.DeviceId.StartsWith("seed-admin-map-")));
        Assert.Equal(12, await dbContext.Pois.Select(poi => poi.OwnerId).Distinct().CountAsync());

        var busTour = await dbContext.Tours
            .Include(tour => tour.Stops)
            .SingleAsync(tour => tour.Title == "Tuyến xe buýt Khánh Hội - Vĩnh Hội - Xóm Chiếu");
        Assert.Equal(TourStatus.Published, busTour.Status);
        Assert.Equal([1, 2, 3, 4, 5, 6], busTour.Stops.OrderBy(stop => stop.Sequence).Select(stop => stop.Sequence));

        var seededQrCodes = await dbContext.QrCodes
            .Where(qr => qr.TargetType == "tour")
            .OrderBy(qr => qr.Code)
            .ToListAsync();
        Assert.Equal(["BUS-KHANH-HOI-TOUR", "BUS-VINH-HOI-TOUR", "BUS-XOM-CHIEU-TOUR"], seededQrCodes.Select(qr => qr.Code));
        Assert.All(seededQrCodes, qr => Assert.Equal(busTour.Id, qr.TargetId));

        var admin = await dbContext.AppUsers.SingleAsync(user => user.Email == AppConstants.DefaultAdminEmail);
        var owner = await dbContext.AppUsers.SingleAsync(user => user.Email == AppConstants.DefaultOwnerEmail);

        Assert.True(BCrypt.Net.BCrypt.Verify(AppConstants.DefaultAdminPassword, admin.PasswordHash));
        Assert.True(BCrypt.Net.BCrypt.Verify(AppConstants.DefaultOwnerPassword, owner.PasswordHash));
        Assert.Equal(AppConstants.DefaultLanguage, admin.PreferredLanguage);
        Assert.Equal(AppConstants.DefaultLanguage, owner.PreferredLanguage);
        Assert.NotEqual(default, admin.CreatedAtUtc);
        Assert.NotEqual(default, owner.CreatedAtUtc);

        var analyticsService = new AnalyticsService(dbContext);
        var heatmap = await analyticsService.GetHeatmapAsync(new HeatmapQueryDto
        {
            UseTimeDecay = false,
            ApplyGaussianSmoothing = false
        });
        var movementFlows = await analyticsService.GetMovementFlowsAsync(new MovementFlowQueryDto
        {
            MinimumUniqueSessions = 3
        });

        Assert.NotEmpty(heatmap);
        Assert.NotEmpty(movementFlows);
        Assert.All(movementFlows, flow => Assert.True(flow.UniqueSessions >= 3));
    }

    [Fact]
    public async Task SeedAsync_is_idempotent_when_called_multiple_times()
    {
        await using var dbContext = CreateDbContext();
        var sut = new DataSeeder(dbContext, NullLogger<DataSeeder>.Instance);

        await sut.SeedAsync();
        await sut.SeedAsync();

        Assert.Equal(3, await dbContext.Roles.CountAsync());
        Assert.Equal(13, await dbContext.AppUsers.CountAsync());
        Assert.Equal(12, await dbContext.Pois.CountAsync());
        Assert.Equal(12, await dbContext.Geofences.CountAsync());
        Assert.Equal(12, await dbContext.PoiTranslations.CountAsync());
        Assert.Equal(5, await dbContext.ManagedLanguages.CountAsync());
        Assert.Equal(1, await dbContext.Tours.CountAsync());
        Assert.Equal(6, await dbContext.TourStops.CountAsync());
        Assert.Equal(3, await dbContext.QrCodes.CountAsync());
        Assert.Equal(121, await dbContext.VisitEvents.CountAsync(item => item.DeviceId.StartsWith("seed-admin-map-")));
        Assert.Equal(12, await dbContext.Pois.Select(poi => poi.OwnerId).Distinct().CountAsync());
    }

    [Fact]
    public async Task SeedAsync_does_not_rewrite_blank_names_for_unrelated_existing_users()
    {
        await using var dbContext = CreateDbContext();
        dbContext.AppUsers.Add(new AppUser
        {
            Id = Guid.NewGuid(),
            FullName = string.Empty,
            Email = "legacy-user@narration.app",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Legacy@123"),
            PreferredLanguage = AppConstants.DefaultLanguage,
            CreatedAtUtc = DateTime.UtcNow,
            RoleId = Guid.Parse("0D84C6F8-7282-4D89-92A9-60B89B7B3A82"),
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var sut = new DataSeeder(dbContext, NullLogger<DataSeeder>.Instance);

        await sut.SeedAsync();

        var legacyUser = await dbContext.AppUsers.SingleAsync(user => user.Email == "legacy-user@narration.app");

        Assert.Equal(string.Empty, legacyUser.FullName);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }
}
