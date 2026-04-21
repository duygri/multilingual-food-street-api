using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.Pois;

public sealed class PoiServiceTests
{
    [Fact]
    public async Task GetNearbyAsync_returns_pois_sorted_by_distance()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var sut = new PoiService(dbContext);

        var result = await sut.GetNearbyAsync(new PoiNearRequest
        {
            Lat = 10.75986,
            Lng = 106.70188,
            RadiusMeters = 1200
        });

        Assert.NotEmpty(result);
        Assert.Equal("pho-am-thuc-vinh-khanh", result[0].Slug);
        Assert.True(result.Count >= 2);
    }

    [Fact]
    public async Task UpdateAsync_rejects_owner_attempting_to_edit_foreign_poi()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var foreignOwner = await TestAppDbContextFactory.AddOwnerAsync(dbContext, "owner2@narration.app");
        var poi = await dbContext.Pois.AsNoTracking().FirstAsync();
        var sut = new PoiService(dbContext);

        var action = async () => await sut.UpdateAsync(
            foreignOwner.Id,
            UserRole.PoiOwner,
            poi.Id,
            new UpdatePoiRequest
            {
                Name = poi.Name + " updated",
                Slug = poi.Slug,
                Lat = poi.Lat,
                Lng = poi.Lng,
                Priority = poi.Priority,
                NarrationMode = poi.NarrationMode,
                MapLink = poi.MapLink,
                ImageUrl = poi.ImageUrl,
                Status = PoiStatus.Published
            });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(action);
    }
}
