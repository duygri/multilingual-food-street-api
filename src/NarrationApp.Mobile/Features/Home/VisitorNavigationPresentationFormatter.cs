namespace NarrationApp.Mobile.Features.Home;

public static class VisitorNavigationPresentationFormatter
{
    public static string GetHeaderTitle(VisitorTab currentTab) =>
        currentTab switch
        {
            VisitorTab.Map => "Bản đồ",
            VisitorTab.Discover => "Khám phá",
            VisitorTab.Tours => "Tour",
            _ => "Tôi"
        };

    public static string GetVisitorAppClass(VisitorTab currentTab) =>
        currentTab == VisitorTab.Map ? "visitor-app--map" : string.Empty;

    public static string GetVisitorFrameClass(VisitorTab currentTab) =>
        currentTab == VisitorTab.Map ? "visitor-frame--map" : string.Empty;

    public static string GetSelectionClass(bool isSelected) =>
        isSelected ? "is-selected" : string.Empty;

    public static string GetActiveClass(bool isActive) =>
        isActive ? "is-active" : string.Empty;

    public static string GetCategoryIcon(string categoryId) =>
        categoryId switch
        {
            "all" => "🏷️",
            "food" => "🍜",
            "river" => "🏛️",
            "night" => "🍢",
            _ => "☕"
        };

    public static string GetPoiCategoryLabel(VisitorPoi poi, IReadOnlyList<VisitorCategory> categories) =>
        string.IsNullOrWhiteSpace(poi.CategoryLabel)
            ? categories.FirstOrDefault(category => category.Id == poi.CategoryId)?.Label ?? poi.District
            : poi.CategoryLabel;

    public static string GetAutoAudioStatus(bool autoPlayEnabled, VisitorProximityMatch? activeProximity) =>
        !autoPlayEnabled
            ? "Audio tự động đang tắt"
            : activeProximity is null
                ? "Audio tự động sẵn sàng"
                : $"Đã vào vùng {activeProximity.PoiName}";

    public static string GetGeofenceToastMessage(
        bool autoPlayEnabled,
        VisitorProximityMatch? activeProximity,
        bool isAudioPlaying) =>
        !autoPlayEnabled
            ? "Bạn đã tắt auto-play. Chạm vào POI để nghe thủ công."
            : activeProximity is null
                ? "Sẵn sàng phát thuyết minh tự động."
                : isAudioPlaying
                ? $"Đang phát thuyết minh tự động • {activeProximity.DistanceMeters}m"
                : $"Sẵn sàng phát thuyết minh • {activeProximity.DistanceMeters}m";

    public static string? GetGeofenceQueueBadge(VisitorProximityMatch? queuedMatch) =>
        queuedMatch is null ? null : $"Chờ: {queuedMatch.PoiName}";
}
