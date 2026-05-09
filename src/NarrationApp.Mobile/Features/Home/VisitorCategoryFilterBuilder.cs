namespace NarrationApp.Mobile.Features.Home;

public static class VisitorCategoryFilterBuilder
{
    public static IReadOnlyList<VisitorCategory> Build(VisitorContentSnapshot snapshot)
    {
        var categories = new List<VisitorCategory>
        {
            new("all", "Tất cả", "🏷️", "is-history")
        };

        var liveCategories = (snapshot.Categories ?? [])
            .Where(category => !string.IsNullOrWhiteSpace(category.Id) && !string.Equals(category.Id, "all", StringComparison.OrdinalIgnoreCase))
            .GroupBy(category => category.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        if (liveCategories.Length > 0)
        {
            categories.AddRange(liveCategories);
            return categories;
        }

        categories.AddRange(
            snapshot.Pois
                .Where(poi => !string.IsNullOrWhiteSpace(poi.CategoryId))
                .GroupBy(poi => poi.CategoryId, StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var firstPoi = group.First();
                    return new VisitorCategory(
                        firstPoi.CategoryId,
                        string.IsNullOrWhiteSpace(firstPoi.CategoryLabel) ? firstPoi.District : firstPoi.CategoryLabel,
                        VisitorCategoryPresentationFormatter.GetPoiIcon(firstPoi, []),
                        VisitorCategoryPresentationFormatter.GetCategoryTone(firstPoi.CategoryId, [], firstPoi.CategoryLabel));
                })
                .OrderBy(category => category.Label, StringComparer.CurrentCultureIgnoreCase));

        return categories;
    }
}
