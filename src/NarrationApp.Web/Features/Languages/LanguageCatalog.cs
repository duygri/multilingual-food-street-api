using System.Globalization;
using System.Text;

namespace NarrationApp.Web.Features.Languages;

public static class LanguageCatalog
{
    private static readonly IReadOnlyList<LanguageCatalogEntry> Entries =
    [
        new("vi", "Tiếng Việt", "Tiếng Việt", "VN", ["vietnamese", "tieng viet", "viet", "việt", "vietnam"]),
        new("en", "English", "English", "GB", ["english", "anh", "tieng anh", "tiếng anh", "uk", "british"]),
        new("fr", "French", "Français", "FR", ["french", "phap", "pháp", "tieng phap", "tiếng pháp", "francais", "français"]),
        new("de", "German", "Deutsch", "DE", ["german", "duc", "đức", "tieng duc", "tiếng đức", "deutsch"]),
        new("es", "Spanish", "Español", "ES", ["spanish", "tay ban nha", "tây ban nha", "espanol", "español"]),
        new("it", "Italian", "Italiano", "IT", ["italian", "y", "ý", "italiano"]),
        new("pt", "Portuguese", "Português", "PT", ["portuguese", "bo dao nha", "bồ đào nha", "portugues", "português"]),
        new("ru", "Russian", "Русский", "RU", ["russian", "nga", "tiếng nga", "tieng nga", "russkiy"]),
        new("ja", "Japanese", "日本語", "JP", ["japanese", "nhat", "nhật", "tieng nhat", "tiếng nhật", "nihongo"]),
        new("ko", "Korean", "한국어", "KR", ["korean", "han", "hàn", "tieng han", "tiếng hàn", "hangukeo"]),
        new("zh", "Chinese", "中文", "CN", ["chinese", "trung", "trung quoc", "trung quốc", "tieng trung", "tiếng trung", "zhongwen"]),
        new("th", "Thai", "ไทย", "TH", ["thai", "thai lan", "thái lan", "tieng thai", "tiếng thái"]),
        new("id", "Indonesian", "Bahasa Indonesia", "ID", ["indonesian", "indonesia", "nam duong", "nam dương"]),
        new("ms", "Malay", "Bahasa Melayu", "MY", ["malay", "malaysia", "mã lai", "ma lai"]),
        new("hi", "Hindi", "हिन्दी", "IN", ["hindi", "an do", "ấn độ", "india"]),
        new("ar", "Arabic", "العربية", "SA", ["arabic", "a rap", "ả rập", "arab"]),
        new("nl", "Dutch", "Nederlands", "NL", ["dutch", "ha lan", "hà lan", "nederlands"]),
        new("tr", "Turkish", "Türkçe", "TR", ["turkish", "tho nhi ky", "thổ nhĩ kỳ", "turkce", "türkçe"])
    ];

    public static IReadOnlyList<LanguageCatalogEntry> Search(string? query, int take = 8)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var normalizedQuery = Normalize(query);
        return Entries
            .Select(entry => new
            {
                Entry = entry,
                Score = Score(entry, normalizedQuery)
            })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .Select(item => item.Entry)
            .ToArray();
    }

    public static LanguageCatalogEntry? FindByCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return Entries.FirstOrDefault(entry => string.Equals(entry.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static LanguageCatalogEntry? FindByCodeOrFlag(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = Normalize(value);
        return Entries.FirstOrDefault(entry =>
            Normalize(entry.Code) == normalizedValue ||
            Normalize(entry.FlagCode) == normalizedValue);
    }

    private static int Score(LanguageCatalogEntry entry, string normalizedQuery)
    {
        if (Normalize(entry.Code) == normalizedQuery)
        {
            return 120;
        }

        if (Normalize(entry.FlagCode) == normalizedQuery)
        {
            return 110;
        }

        if (Normalize(entry.DisplayName).StartsWith(normalizedQuery, StringComparison.Ordinal))
        {
            return 100;
        }

        if (Normalize(entry.NativeName).StartsWith(normalizedQuery, StringComparison.Ordinal))
        {
            return 90;
        }

        if (entry.SearchTerms.Any(term => Normalize(term).StartsWith(normalizedQuery, StringComparison.Ordinal)))
        {
            return 80;
        }

        if (Normalize(entry.FlagCode).StartsWith(normalizedQuery, StringComparison.Ordinal))
        {
            return 75;
        }

        if (Normalize(entry.DisplayName).Contains(normalizedQuery, StringComparison.Ordinal))
        {
            return 70;
        }

        if (Normalize(entry.NativeName).Contains(normalizedQuery, StringComparison.Ordinal))
        {
            return 60;
        }

        if (Normalize(entry.FlagCode).Contains(normalizedQuery, StringComparison.Ordinal))
        {
            return 55;
        }

        return entry.SearchTerms.Any(term => Normalize(term).Contains(normalizedQuery, StringComparison.Ordinal)) ? 50 : 0;
    }

    private static string Normalize(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

public sealed record LanguageCatalogEntry(
    string Code,
    string DisplayName,
    string NativeName,
    string FlagCode,
    IReadOnlyList<string> SearchTerms);
