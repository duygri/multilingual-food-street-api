using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorTourPresentationFormatterTests
{
    [Fact]
    public void Hero_keywords_map_to_expected_icon_and_tone()
    {
        var foodTour = new VisitorTourCard(
            "tour-food",
            "Tour ẩm thực",
            "3 điểm dừng",
            "30 phút",
            "Dễ",
            "Đi food street về đêm",
            ["poi-1"]);
        var nightTour = new VisitorTourCard(
            "tour-night",
            "Saigon Night Walk",
            "4 điểm dừng",
            "45 phút",
            "Vừa",
            "Night route",
            ["poi-1"]);
        var cultureTour = new VisitorTourCard(
            "tour-culture",
            "Di sản cảng",
            "5 điểm dừng",
            "60 phút",
            "Vừa",
            "Hành trình lịch sử",
            ["poi-1"]);

        Assert.Equal("🍜", VisitorTourPresentationFormatter.GetHeroIcon(foodTour));
        Assert.Equal("is-food", VisitorTourPresentationFormatter.GetHeroTone(foodTour));
        Assert.Equal("🌙", VisitorTourPresentationFormatter.GetHeroIcon(nightTour));
        Assert.Equal("is-night", VisitorTourPresentationFormatter.GetHeroTone(nightTour));
        Assert.Equal("🏛️", VisitorTourPresentationFormatter.GetHeroIcon(cultureTour));
        Assert.Equal("is-history", VisitorTourPresentationFormatter.GetHeroTone(cultureTour));
    }

    [Fact]
    public void Progress_and_action_labels_follow_active_session_for_selected_tour()
    {
        var selectedTour = new VisitorTourCard(
            "tour-1",
            "Tour ven sông",
            "4 điểm dừng",
            "40 phút",
            "Dễ",
            "Dọc bờ sông",
            ["poi-1", "poi-2", "poi-3", "poi-4"]);
        var activeSession = new VisitorTourSession(
            TourId: "tour-1",
            TourTitle: "Tour ven sông",
            CurrentStopSequence: 2,
            TotalStops: 4,
            NextPoiId: "poi-3",
            NextPoiName: "Bến cảng",
            IsCompleted: false);

        Assert.Equal("Tiếp tục tour", VisitorTourPresentationFormatter.GetActionLabel(activeSession, "tour-1"));
        Assert.Equal("2/4 điểm", VisitorTourPresentationFormatter.GetSelectedTourStatusBadge(selectedTour, activeSession));
        Assert.Equal("2/4 điểm", VisitorTourPresentationFormatter.GetProgressLabel(selectedTour, activeSession));
        Assert.Equal("50%", VisitorTourPresentationFormatter.GetProgressPercent(selectedTour, activeSession));
    }

    [Fact]
    public void Progress_falls_back_when_session_is_missing_or_for_other_tour()
    {
        var selectedTour = new VisitorTourCard(
            "tour-1",
            "Tour ven sông",
            "4 điểm dừng",
            "40 phút",
            "Dễ",
            "Dọc bờ sông",
            ["poi-1", "poi-2", "poi-3", "poi-4"]);
        var otherSession = new VisitorTourSession(
            TourId: "tour-2",
            TourTitle: "Tour khác",
            CurrentStopSequence: 3,
            TotalStops: 5,
            NextPoiId: "poi-9",
            NextPoiName: "POI khác",
            IsCompleted: false);

        Assert.Equal("Bắt đầu tour", VisitorTourPresentationFormatter.GetSelectedTourPrimaryActionLabel(selectedTour, null));
        Assert.Equal("Chưa bắt đầu", VisitorTourPresentationFormatter.GetSelectedTourStatusBadge(selectedTour, otherSession));
        Assert.Equal("0/4 điểm", VisitorTourPresentationFormatter.GetProgressLabel(selectedTour, otherSession));
        Assert.Equal("0%", VisitorTourPresentationFormatter.GetProgressPercent(selectedTour, otherSession));
    }

    [Fact]
    public void Stop_labels_and_classes_follow_session_and_selected_poi()
    {
        var activeSession = new VisitorTourSession(
            TourId: "tour-1",
            TourTitle: "Tour ven sông",
            CurrentStopSequence: 1,
            TotalStops: 4,
            NextPoiId: "poi-2",
            NextPoiName: "Bến cảng",
            IsCompleted: false);

        Assert.Equal("Đã đi qua", VisitorTourPresentationFormatter.GetStopStateLabel(activeSession, "tour-1", "poi-1", 0));
        Assert.Equal("Điểm kế tiếp", VisitorTourPresentationFormatter.GetStopStateLabel(activeSession, "tour-1", "poi-2", 1));
        Assert.Equal("Chờ tiếp tục", VisitorTourPresentationFormatter.GetStopStateLabel(activeSession, "tour-1", "poi-3", 2));
        Assert.Equal("Sẵn sàng", VisitorTourPresentationFormatter.GetStopStateLabel(activeSession, "tour-2", "poi-1", 0));
        Assert.Equal("is-next", VisitorTourPresentationFormatter.GetStopClass(activeSession, null, "poi-2"));
        Assert.Equal("is-current", VisitorTourPresentationFormatter.GetStopClass(activeSession, "poi-9", "poi-9"));
        Assert.Equal(string.Empty, VisitorTourPresentationFormatter.GetStopClass(activeSession, null, "poi-4"));
    }
}
