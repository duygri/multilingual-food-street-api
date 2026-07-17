using System.Globalization;
using System.Text;

namespace NarrationApp.Mobile.Features.Home;

public static class VisitorCategoryPresentationFormatter
{
    private static readonly HashSet<string> SupportedIconNames = new(StringComparer.Ordinal)
    {
        "discover",
        "map",
        "journey",
        "user",
        "search",
        "language",
        "refresh",
        "location",
        "audio",
        "directions",
        "close",
        "notification",
        "history",
        "download",
        "settings"
    };

    public static string GetCategoryIcon(string categoryId, IReadOnlyList<VisitorCategory> categories, string? fallbackLabel = null)
    {
        var category = categories.FirstOrDefault(item => string.Equals(item.Id, categoryId, StringComparison.OrdinalIgnoreCase));
        var configuredIcon = NormalizeIconName(category?.MarkerLabel);
        if (configuredIcon is not null)
        {
            return configuredIcon;
        }

        var normalized = NormalizeCategoryKey(categoryId, fallbackLabel);
        return normalized switch
        {
            var value when value.Contains("lich-su") || value.Contains("history") || value.Contains("di-tich") || value.Contains("heritage") => "history",
            var value when value.Contains("song") || value.Contains("river") || value.Contains("bridge") || value.Contains("cau") => "map",
            var value when value.Contains("hai-san") || value.Contains("seafood") || value.Contains("shrimp") => "discover",
            var value when value.Contains("bun") || value.Contains("pho") || value.Contains("noodle") || value.Contains("food") || value.Contains("am-thuc") => "discover",
            var value when value.Contains("an-vat") || value.Contains("snack") || value.Contains("dem") => "discover",
            var value when value.Contains("uong") || value.Contains("drink") || value.Contains("coffee") || value.Contains("ca-phe") || value.Contains("tea") => "discover",
            "all" => "discover",
            _ => "location"
        };
    }

    public static string GetPoiIcon(VisitorPoi poi, IReadOnlyList<VisitorCategory> categories)
    {
        ArgumentNullException.ThrowIfNull(poi);
        return GetCategoryIcon(poi.CategoryId, categories, poi.CategoryLabel);
    }

    public static string GetCategoryTone(string categoryId, IReadOnlyList<VisitorCategory> categories, string? fallbackLabel = null)
    {
        var category = categories.FirstOrDefault(item => string.Equals(item.Id, categoryId, StringComparison.OrdinalIgnoreCase));
        if (category is not null && !string.IsNullOrWhiteSpace(category.ToneKey))
        {
            return category.ToneKey;
        }

        var normalized = NormalizeCategoryKey(categoryId, fallbackLabel);
        return normalized switch
        {
            var value when value.Contains("hai-san") || value.Contains("song") || value.Contains("river") || value.Contains("bridge") => "is-river",
            var value when value.Contains("bun") || value.Contains("pho") || value.Contains("food") || value.Contains("am-thuc") => "is-food",
            var value when value.Contains("an-vat") || value.Contains("snack") || value.Contains("dem") => "is-night",
            var value when value.Contains("uong") || value.Contains("drink") || value.Contains("coffee") || value.Contains("ca-phe") || value.Contains("tea") => "is-drink",
            _ => "is-history"
        };
    }

    public static string GetCategoryAccent(string categoryId, IReadOnlyList<VisitorCategory> categories, string? fallbackLabel = null)
    {
        return GetCategoryTone(categoryId, categories, fallbackLabel) switch
        {
            "is-food" => "#1ed6af",
            "is-river" => "#59b8ff",
            "is-night" => "#ff9b4d",
            "is-drink" => "#f6c453",
            _ => "#9dd0ff"
        };
    }

    public static VisitorCategory CreateCategory(string id, string label, string? markerLabel = null)
    {
        var icon = NormalizeIconName(markerLabel) ?? GetCategoryIcon(id, [], label);
        return new VisitorCategory(id, label, icon, GetCategoryTone(id, [], label));
    }

    private static string? NormalizeIconName(string? iconName)
    {
        var normalized = iconName?.Trim().ToLowerInvariant();
        return normalized is not null && SupportedIconNames.Contains(normalized) ? normalized : null;
    }

    private static string NormalizeCategoryKey(string categoryId, string? fallbackLabel)
    {
        var raw = string.Join(' ', [categoryId ?? string.Empty, fallbackLabel ?? string.Empty]).Trim();
        var builder = new StringBuilder();

        foreach (var character in raw.Normalize(NormalizationForm.FormD))
        {
            var normalized = CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark
                ? '\0'
                : char.ToLowerInvariant(character == 'đ' ? 'd' : character);

            if (normalized == '\0')
            {
                continue;
            }

            builder.Append(char.IsWhiteSpace(normalized) ? '-' : normalized);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
