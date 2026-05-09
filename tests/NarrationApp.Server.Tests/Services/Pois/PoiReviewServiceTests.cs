using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Services;
using NarrationApp.Server.Tests.Support;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Tests.Services.Pois;

public sealed class PoiReviewServiceTests
{
    [Fact]
    public async Task CreateAsync_stores_pending_review_and_summary_excludes_it_until_approved()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.AsNoTracking().FirstAsync(item => item.Status == PoiStatus.Published);
        var sut = new PoiReviewService(dbContext);

        var created = await sut.CreateAsync(poi.Id, new CreatePoiReviewRequest
        {
            DeviceId = "android-device-1",
            Rating = 5,
            Comment = "Rất thích thuyết minh ở điểm này."
        });

        var pendingSummary = await sut.GetSummaryAsync(poi.Id);

        Assert.Equal(PoiReviewStatus.Pending, created.Status);
        Assert.Equal(0, pendingSummary.ApprovedCount);
        Assert.Equal(0, pendingSummary.AverageRating);

        var approved = await sut.ReviewAsync(created.Id, approve: true, reviewNote: "ok");
        var approvedSummary = await sut.GetSummaryAsync(poi.Id);

        Assert.Equal(PoiReviewStatus.Approved, approved.Status);
        Assert.Equal(1, approvedSummary.ApprovedCount);
        Assert.Equal(5, approvedSummary.AverageRating);
    }

    [Fact]
    public async Task CreateAsync_rejects_invalid_rating()
    {
        await using var dbContext = await TestAppDbContextFactory.CreateSeededAsync();
        var poi = await dbContext.Pois.AsNoTracking().FirstAsync(item => item.Status == PoiStatus.Published);
        var sut = new PoiReviewService(dbContext);

        var action = async () => await sut.CreateAsync(poi.Id, new CreatePoiReviewRequest
        {
            DeviceId = "android-device-1",
            Rating = 6,
            Comment = "Rating không hợp lệ."
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Equal("Rating must be between 1 and 5.", ex.Message);
    }
}
