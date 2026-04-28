using System.Globalization;

namespace NarrationApp.Mobile.Features.Home;

public static class VisitorTourPresentationFormatter
{
    public static string GetHeroCopy(VisitorTourSession? activeSession)
    {
        if (activeSession is null)
        {
            return "Chọn tour ngắn, dễ đi bộ và sẵn audio để bắt đầu nhanh.";
        }

        if (activeSession.IsCompleted)
        {
            return $"Bạn vừa hoàn thành {activeSession.TourTitle}. Có thể chọn tour khác ngay.";
        }

        return $"Đang theo {activeSession.TourTitle} • {activeSession.CurrentStopSequence}/{activeSession.TotalStops} điểm đã xong.";
    }

    public static string GetActionLabel(VisitorTourSession? activeSession, string tourId)
    {
        if (activeSession?.TourId == tourId)
        {
            return activeSession.IsCompleted ? "Đi lại tour" : "Tiếp tục tour";
        }

        return "Bắt đầu tour";
    }

    public static string GetSelectedTourPrimaryActionLabel(VisitorTourCard? selectedTour, VisitorTourSession? activeSession) =>
        selectedTour is null ? "Bắt đầu tour" : GetActionLabel(activeSession, selectedTour.Id);

    public static string GetSelectedTourStatusBadge(VisitorTourCard? selectedTour, VisitorTourSession? activeSession)
    {
        if (selectedTour is null)
        {
            return "Sẵn sàng";
        }

        if (activeSession?.TourId != selectedTour.Id)
        {
            return "Chưa bắt đầu";
        }

        return activeSession.IsCompleted
            ? "Hoàn thành"
            : $"{activeSession.CurrentStopSequence}/{activeSession.TotalStops} điểm";
    }

    public static string GetActiveTourBannerText(VisitorTourSession? activeSession)
    {
        if (activeSession is null)
        {
            return "Chưa có tour đang theo.";
        }

        if (activeSession.IsCompleted)
        {
            return $"Đã xong {activeSession.TotalStops}/{activeSession.TotalStops} điểm. Bạn có thể quay lại tab Tours để chọn hành trình mới.";
        }

        return $"Đã xong {activeSession.CurrentStopSequence}/{activeSession.TotalStops} điểm • kế tiếp: {activeSession.NextPoiName}.";
    }

    public static string GetProgressBannerClass(VisitorTourSession? activeSession) =>
        activeSession?.IsCompleted == true ? "is-complete" : string.Empty;

    public static string GetHeroIcon(VisitorTourCard tour)
    {
        var text = $"{tour.Title} {tour.Description}".ToLowerInvariant();
        if (text.Contains("ẩm thực") || text.Contains("food"))
        {
            return "🍜";
        }

        if (text.Contains("đêm") || text.Contains("night"))
        {
            return "🌙";
        }

        return "🏛️";
    }

    public static string GetHeroTone(VisitorTourCard tour)
    {
        var text = $"{tour.Title} {tour.Description}".ToLowerInvariant();
        if (text.Contains("ẩm thực") || text.Contains("food"))
        {
            return "is-food";
        }

        if (text.Contains("đêm") || text.Contains("night"))
        {
            return "is-night";
        }

        return "is-history";
    }

    public static string GetRouteDistanceLabel(VisitorTourCard tour)
    {
        var distanceKm = Math.Max(0.8d, tour.StopPoiIds.Count * 0.24d);
        return $"{distanceKm:0.#}km";
    }

    public static string GetParticipationLabel(VisitorTourCard tour)
    {
        var count = tour.StopPoiIds.Count switch
        {
            <= 3 => 124,
            4 => 148,
            5 => 168,
            _ => 187
        };

        return count.ToString();
    }

    public static string GetProgressLabel(VisitorTourCard? selectedTour, VisitorTourSession? activeSession)
    {
        if (activeSession is null || activeSession.TourId != selectedTour?.Id)
        {
            return $"0/{selectedTour?.StopPoiIds.Count ?? 0} điểm";
        }

        return $"{activeSession.CurrentStopSequence}/{activeSession.TotalStops} điểm";
    }

    public static string GetProgressPercent(VisitorTourCard? selectedTour, VisitorTourSession? activeSession)
    {
        if (selectedTour is null || selectedTour.StopPoiIds.Count == 0)
        {
            return "0%";
        }

        var completed = activeSession?.TourId == selectedTour.Id
            ? activeSession.CurrentStopSequence
            : 0;

        var percent = Math.Clamp(completed * 100d / selectedTour.StopPoiIds.Count, 0d, 100d);
        return $"{percent.ToString("0.##", CultureInfo.InvariantCulture)}%";
    }

    public static string GetStopStateLabel(VisitorTourSession? activeSession, string tourId, string poiId, int stopIndex)
    {
        if (activeSession?.TourId != tourId)
        {
            return "Sẵn sàng";
        }

        if (activeSession.IsCompleted)
        {
            return "Đã hoàn thành";
        }

        if (stopIndex < activeSession.CurrentStopSequence)
        {
            return "Đã đi qua";
        }

        if (activeSession.NextPoiId == poiId)
        {
            return "Điểm kế tiếp";
        }

        return "Chờ tiếp tục";
    }

    public static string GetStopClass(VisitorTourSession? activeSession, string? selectedPoiId, string poiId)
    {
        if (activeSession?.NextPoiId == poiId && activeSession.IsCompleted == false)
        {
            return "is-next";
        }

        if (selectedPoiId == poiId)
        {
            return "is-current";
        }

        return string.Empty;
    }
}
