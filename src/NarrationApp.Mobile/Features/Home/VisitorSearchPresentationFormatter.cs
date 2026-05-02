namespace NarrationApp.Mobile.Features.Home;

public static class VisitorSearchPresentationFormatter
{
    public static string FormatResultCountLabel(int resultCount) =>
        resultCount switch
        {
            0 => "Chưa tìm thấy kết quả phù hợp",
            1 => "Tìm thấy 1 kết quả",
            _ => $"Tìm thấy {resultCount} kết quả"
        };

    public static string GetCategoryLabel(VisitorPoi poi) =>
        string.IsNullOrWhiteSpace(poi.CategoryLabel) ? poi.District : poi.CategoryLabel;

    public static string GetTourTone(VisitorTourCard tour)
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

    public static string GetTourIcon(VisitorTourCard tour)
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

    public static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
        {
            return text;
        }

        return $"{text[..maxLength].TrimEnd()}...";
    }
}
