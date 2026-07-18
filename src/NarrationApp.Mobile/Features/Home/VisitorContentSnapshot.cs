namespace NarrationApp.Mobile.Features.Home;

public sealed record VisitorContentSnapshot
{
    public VisitorContentSnapshot(
        IReadOnlyList<VisitorPoi> Pois,
        IReadOnlyList<VisitorTourCard> Tours,
        IReadOnlyList<VisitorCategory>? Categories = null,
        IReadOnlyList<VisitorLanguageOption>? Languages = null)
    {
        this.Pois = Pois;
        this.Tours = Tours;
        this.Categories = Categories;
        this.Languages = Languages ?? [];
    }

    public IReadOnlyList<VisitorPoi> Pois { get; init; }

    public IReadOnlyList<VisitorTourCard> Tours { get; init; }

    public IReadOnlyList<VisitorCategory>? Categories { get; init; }

    public IReadOnlyList<VisitorLanguageOption> Languages { get; init; }

    public static VisitorContentSnapshot CreateDemo()
    {
        return new VisitorContentSnapshot(
            [
                new VisitorPoi(
                    "poi-khanh-hoi-bridge",
                    "Minh họa • Bưu điện Trung tâm",
                    "history",
                    "Di tích",
                    "TP.HCM",
                    "Di sản ven sông",
                    "Bản ghi minh họa không đại diện cho danh mục đang có trên máy chủ.",
                    "Bắt đầu tour ven sông",
                    18,
                    52,
                    180,
                    "3:12",
                    "Sẵn sàng",
                    10.7798,
                    106.6990,
                    9,
                    5,
                    ReadyAudioLanguageCodesRaw: ["vi", "en", "ja"]),
                new VisitorPoi(
                    "poi-cho-ben-thanh",
                    "Minh họa • Ký ức Chợ Lớn",
                    "food",
                    "Ẩm thực",
                    "TP.HCM",
                    "Ẩm thực đặc trưng",
                    "Bản ghi minh họa về ký ức thương mại và ẩm thực đô thị.",
                    "Nhiều món street food",
                    34,
                    64,
                    260,
                    "2:48",
                    "Đang phát gần đây",
                    10.7546,
                    106.6635,
                    10,
                    5,
                    ReadyAudioLanguageCodesRaw: ["vi", "en", "zh"]),
                new VisitorPoi(
                    "poi-tiem-banh-mi-co-lan",
                    "Minh họa • Bánh mì Thủ Đức",
                    "food",
                    "Bánh mì",
                    "TP.HCM",
                    "Điểm ăn sáng",
                    "Bản ghi minh họa về một hàng quán khu Đông, không phải địa điểm sản xuất.",
                    "Gợi ý ăn nhanh",
                    60,
                    30,
                    110,
                    "1:44",
                    "Tạo audio tự động",
                    10.8494,
                    106.7537,
                    7,
                    4,
                    ReadyAudioLanguageCodesRaw: ["vi"]),
                new VisitorPoi(
                    "poi-ben-nha-rong",
                    "Minh họa • Rừng ngập mặn Cần Giờ",
                    "river",
                    "Ven sông",
                    "TP.HCM",
                    "Dấu ấn lịch sử",
                    "Bản ghi minh họa về sinh thái cửa sông, không phải dữ liệu catalog live.",
                    "Ngắm sông Sài Gòn",
                    46,
                    18,
                    320,
                    "4:05",
                    "Có tour liên kết",
                    10.4114,
                    106.9547,
                    8,
                    5,
                    ReadyAudioLanguageCodesRaw: ["vi", "en", "fr"]),
                new VisitorPoi(
                    "poi-pho-dem-xom-chieu",
                    "Minh họa • Chợ đêm Sài Gòn",
                    "night",
                    "Đêm",
                    "TP.HCM",
                    "Khám phá buổi tối",
                    "Bản ghi minh họa cho trải nghiệm buổi tối, không khẳng định độ phủ thực tế.",
                    "Mở sau 18:00",
                    74,
                    70,
                    420,
                    "2:20",
                    "Gợi ý tối nay",
                    10.7597,
                    106.7008,
                    6,
                    4,
                    ReadyAudioLanguageCodesRaw: ["vi", "ko"])
            ],
            [
                new VisitorTourCard(
                    "tour-river",
                    "Minh họa • Những dòng sông thành phố",
                    "4 điểm dừng",
                    "35 phút",
                    "Dễ đi bộ",
                    "Câu chuyện thương cảng và cảnh sông.",
                    ["poi-khanh-hoi-bridge", "poi-ben-nha-rong", "poi-cho-ben-thanh", "poi-pho-dem-xom-chieu"]),
                new VisitorTourCard(
                    "tour-food",
                    "Minh họa • Vị Sài Gòn nhiều khu phố",
                    "5 điểm dừng",
                    "42 phút",
                    "Ngon và gần",
                    "Bánh mì, hủ tiếu, hải sản và các góc ăn đêm.",
                    ["poi-tiem-banh-mi-co-lan", "poi-cho-ben-thanh", "poi-ben-nha-rong", "poi-pho-dem-xom-chieu", "poi-khanh-hoi-bridge"]),
                new VisitorTourCard(
                    "tour-night",
                    "Minh họa • Thành phố sau hoàng hôn",
                    "3 điểm dừng",
                    "28 phút",
                    "Buổi tối",
                    "Phù hợp sau 18:00 với audio ngắn, nhịp nhanh.",
                    ["poi-pho-dem-xom-chieu", "poi-khanh-hoi-bridge", "poi-ben-nha-rong"])
            ],
            [
                new VisitorCategory("food", "Ẩm thực", "audio", "is-food"),
                new VisitorCategory("history", "Lịch sử", "history", "is-history"),
                new VisitorCategory("river", "Ven sông", "map", "is-river"),
                new VisitorCategory("night", "Đêm", "journey", "is-night")
            ],
            VisitorLanguageCatalog.Defaults);
    }
}

public sealed record VisitorContentResult(
    VisitorContentSnapshot Content,
    bool IsFallback,
    string SourceLabel,
    string Message,
    VisitorLocationSnapshot Location);
