using NarrationApp.Mobile.Features.Home;
using NarrationApp.Shared.DTOs.Tour;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class TouristTourSessionMapperTests
{
    [Fact]
    public void Map_UsesServerProgressToResolveNextStop()
    {
        var tour = new TouristTourCard(
            "tour-9",
            "Tour ven sông",
            "2 điểm dừng",
            "30 phút",
            "Nhanh",
            "Đi bộ quanh bờ sông.",
            ["poi-7", "poi-8"]);

        var session = TouristTourSessionMapper.Map(
            tour,
            new TourSessionDto
            {
                Id = 14,
                TourId = 9,
                UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Status = TourSessionStatus.InProgress,
                CurrentStopSequence = 1,
                TotalStops = 2,
                StartedAtUtc = new DateTime(2026, 4, 19, 8, 0, 0, DateTimeKind.Utc),
                UpdatedAtUtc = new DateTime(2026, 4, 19, 8, 10, 0, DateTimeKind.Utc)
            },
            [
                new TouristPoi("poi-7", "Bến Nhà Rồng", "river", "Ven sông", "Quận 4", "Live API", "desc", "highlight", 18, 52, 180, "3:12", "Sẵn sàng", 10.7609, 106.7054),
                new TouristPoi("poi-8", "Cầu Khánh Hội", "history", "Lịch sử", "Quận 4", "Live API", "desc", "highlight", 22, 46, 220, "2:44", "Sẵn sàng", 10.7680, 106.7068)
            ]);

        Assert.Equal("tour-9", session.TourId);
        Assert.Equal(1, session.CurrentStopSequence);
        Assert.Equal("poi-8", session.NextPoiId);
        Assert.Equal("Cầu Khánh Hội", session.NextPoiName);
        Assert.True(session.IsServerBacked);
        Assert.False(session.IsCompleted);
    }
}
