using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.DTOs.QR;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.QR;

public sealed class QrServiceTests
{
    [Fact]
    public async Task CreateAsync_generates_unique_qr_code_for_target()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        var sut = new QrService(dbContext);

        var result = await sut.CreateAsync(new CreateQrRequest
        {
            TargetType = "poi",
            TargetId = poi.Id,
            LocationHint = "Front gate"
        });

        Assert.False(string.IsNullOrWhiteSpace(result.Code));
        Assert.Equal(poi.Id, result.TargetId);
    }

    [Fact]
    public async Task ScanAsync_records_visit_event_for_qr_scan()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        var sut = new QrService(dbContext);
        var qr = await sut.CreateAsync(new CreateQrRequest
        {
            TargetType = "poi",
            TargetId = poi.Id,
            LocationHint = "Bus stop"
        });

        var resolved = await sut.ScanAsync(qr.Code, "device-001");

        Assert.Equal(qr.Code, resolved.Code);
        Assert.Equal(1, await dbContext.VisitEvents.CountAsync(item => item.EventType == EventType.QrScan && item.PoiId == poi.Id));
    }

    [Fact]
    public async Task GetAsync_filters_qr_codes_by_target_type()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        var sut = new QrService(dbContext);

        await sut.CreateAsync(new CreateQrRequest
        {
            TargetType = "poi",
            TargetId = poi.Id,
            LocationHint = "Front gate"
        });

        await sut.CreateAsync(new CreateQrRequest
        {
            TargetType = "open_app",
            TargetId = 0,
            LocationHint = "App landing"
        });

        var filtered = await sut.GetAsync("open_app");

        Assert.Single(filtered);
        Assert.Equal("open_app", filtered[0].TargetType);
        Assert.Equal(0, filtered[0].TargetId);
    }

    [Fact]
    public async Task DeleteAsync_removes_qr_code_from_database()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.FirstAsync();
        var sut = new QrService(dbContext);
        var qr = await sut.CreateAsync(new CreateQrRequest
        {
            TargetType = "poi",
            TargetId = poi.Id,
            LocationHint = "Food street arch"
        });

        await sut.DeleteAsync(qr.Id);

        Assert.False(await dbContext.QrCodes.AnyAsync(item => item.Id == qr.Id));
    }

    [Fact]
    public async Task CreateAsync_rejects_unknown_target_type()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var sut = new QrService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateAsync(new CreateQrRequest
        {
            TargetType = "invalid_target",
            TargetId = 1
        }));
    }

    [Fact]
    public async Task CreateAsync_rejects_tour_target_type()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var sut = new QrService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateAsync(new CreateQrRequest
        {
            TargetType = "tour",
            TargetId = 7
        }));
    }
}
