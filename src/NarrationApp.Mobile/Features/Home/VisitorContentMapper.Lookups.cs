using NarrationApp.Shared.DTOs.Category;
using NarrationApp.Shared.DTOs.Languages;
using NarrationApp.Shared.DTOs.Poi;

namespace NarrationApp.Mobile.Features.Home;

public static partial class VisitorContentMapper
{
    private static IReadOnlyList<VisitorLanguageOption> MapLanguages(IReadOnlyList<ManagedLanguageDto> languages)
    {
        if (languages.Count == 0)
        {
            return [];
        }

        return languages
            .Where(language => language.IsActive && !string.IsNullOrWhiteSpace(language.Code))
            .GroupBy(language => language.Code.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(language => VisitorLanguageCatalog.CreateOption(
                language.Code,
                language.DisplayName,
                language.NativeName,
                language.FlagCode))
            .ToArray();
    }

    private static IReadOnlyList<VisitorCategory> MapCategories(IReadOnlyList<CategoryDto> categories, IReadOnlyList<PoiDto> pois)
    {
        if (categories.Count > 0)
        {
            return categories
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(category => new VisitorCategory(
                    category.Slug,
                    category.Name,
                    ResolveCategoryIcon(category),
                    VisitorCategoryPresentationFormatter.GetCategoryTone(category.Slug, [], category.Name)))
                .ToList();
        }

        return pois
            .Where(poi => !string.IsNullOrWhiteSpace(poi.CategoryName))
            .GroupBy(poi => ResolveFallbackCategoryId(poi), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var firstPoi = group.First();
                return VisitorCategoryPresentationFormatter.CreateCategory(
                    group.Key,
                    firstPoi.CategoryName ?? firstPoi.Name);
            })
            .OrderBy(category => category.Label, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static string ResolveCategoryIcon(CategoryDto category)
    {
        var icon = category.Icon?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(icon) && icon.Length <= 3)
        {
            return icon;
        }

        return VisitorCategoryPresentationFormatter.GetCategoryIcon(
            category.Slug,
            [],
            $"{category.Name} {icon}".Trim());
    }
}
