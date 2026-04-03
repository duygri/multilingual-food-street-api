using PROJECT_C_.DTOs;
using PROJECT_C_.Models;

namespace FoodStreet.Server.Mapping;

public sealed record ResolvedMenuContent(
    string RequestedLanguageCode,
    string LanguageCode,
    int Tier,
    bool FallbackUsed,
    bool IsFallback,
    string Name,
    string Description);

public static class MenuContentResolver
{
    public static ResolvedMenuContent Resolve(PoiMenuItem menuItem, string? languageCode)
    {
        var requestedLanguage = PoiContentResolver.NormalizeLanguageCode(languageCode);
        var translations = menuItem.Translations ?? [];

        var exact = FindTranslation(translations, requestedLanguage);
        var english = requestedLanguage == "en-US" ? exact : FindTranslation(translations, "en-US");
        var resolved = exact ?? english;
        var tier = exact != null ? 1 : english != null ? 2 : 3;
        var fallbackUsed = tier > 1;

        return new ResolvedMenuContent(
            requestedLanguage,
            resolved?.LanguageCode ?? "vi-VN",
            tier,
            fallbackUsed,
            resolved?.IsFallback ?? false,
            resolved?.Name ?? menuItem.Name,
            resolved?.Description ?? menuItem.Description);
    }

    public static MenuItemDto MapResolved(PoiMenuItem menuItem, string? languageCode)
    {
        var resolved = Resolve(menuItem, languageCode);

        return new MenuItemDto
        {
            Id = menuItem.Id,
            LocationId = menuItem.LocationId,
            LocationName = menuItem.Location?.Name ?? string.Empty,
            Name = resolved.Name,
            Description = resolved.Description,
            Price = menuItem.Price,
            Currency = menuItem.Currency,
            ImageUrl = menuItem.ImageUrl,
            IsAvailable = menuItem.IsAvailable,
            SortOrder = menuItem.SortOrder,
            UpdatedAt = menuItem.UpdatedAt,
            TranslationCount = menuItem.Translations?.Count ?? 0,
            LanguageCode = resolved.LanguageCode,
            RequestedLanguageCode = resolved.RequestedLanguageCode,
            Tier = resolved.Tier,
            FallbackUsed = resolved.FallbackUsed,
            IsFallback = resolved.IsFallback
        };
    }

    private static PoiMenuItemTranslation? FindTranslation(IEnumerable<PoiMenuItemTranslation> translations, string languageCode)
    {
        return translations.FirstOrDefault(translation =>
            string.Equals(translation.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase));
    }
}
