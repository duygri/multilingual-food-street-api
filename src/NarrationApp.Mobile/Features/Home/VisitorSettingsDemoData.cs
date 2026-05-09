namespace NarrationApp.Mobile.Features.Home;

public static class VisitorSettingsDemoData
{
    public static IReadOnlyList<VisitorCachedAudioItem> CreateCachedAudioItems() =>
    [
        new("cache-poi-khanh-hoi-vi", "poi-khanh-hoi-bridge", "Cầu Khánh Hội", "vi", "Recorded", 6.4, "Cập nhật 12 phút trước"),
        new("cache-poi-khanh-hoi-en", "poi-khanh-hoi-bridge", "Cầu Khánh Hội", "en", "Google TTS", 4.1, "Cập nhật 12 phút trước"),
        new("cache-poi-ben-nha-rong-vi", "poi-ben-nha-rong", "Bến Nhà Rồng", "vi", "Recorded", 5.8, "Cập nhật 1 giờ trước"),
        new("cache-poi-ben-nha-rong-ja", "poi-ben-nha-rong", "Bến Nhà Rồng", "ja", "Google TTS", 4.6, "Cập nhật 1 giờ trước"),
        new("cache-poi-cho-ben-thanh-zh", "poi-cho-ben-thanh", "Chợ Bến Thành", "zh", "Google TTS", 4.9, "Cập nhật hôm nay"),
        new("cache-poi-pho-dem-fr", "poi-pho-dem-xom-chieu", "Phố đêm Xóm Chiếu", "fr", "Google TTS", 3.9, "Cập nhật hôm nay")
    ];

    public static IReadOnlyList<VisitorListeningHistoryDay> CreateListeningHistoryDays() =>
    [
        new(
            "Hôm nay",
            [
                new VisitorListeningHistoryEntry("history-1", "poi-khanh-hoi-bridge", "Cầu Khánh Hội", "Ven sông", "vi", "08:42", "3:12", 100),
                new VisitorListeningHistoryEntry("history-2", "poi-ben-nha-rong", "Bến Nhà Rồng", "Lịch sử", "en", "09:15", "2:40", 76),
                new VisitorListeningHistoryEntry("history-3", "poi-pho-dem-xom-chieu", "Phố đêm Xóm Chiếu", "Đêm", "fr", "11:30", "2:18", 42)
            ]),
        new(
            "Hôm qua",
            [
                new VisitorListeningHistoryEntry("history-4", "poi-cho-ben-thanh", "Chợ Bến Thành", "Ẩm thực", "zh", "17:22", "3:05", 100),
                new VisitorListeningHistoryEntry("history-5", "poi-tiem-banh-mi-co-lan", "Tiệm Bánh Mì Cô Lan", "Bánh mì", "vi", "18:04", "2:12", 63)
            ])
    ];
}
