using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Pages.Admin;

public partial class PoiManagement
{
    private IReadOnlyList<AdminPoiDto> FilteredPois => _pois
        .Where(MatchesActiveFilter)
        .Where(MatchesSearch)
        .ToArray();

    private int FilteredCount => FilteredPois.Count;

    private bool MatchesActiveFilter(AdminPoiDto poi) => MatchesFilter(poi, _activeFilter);

    private bool MatchesSearch(AdminPoiDto poi)
    {
        if (string.IsNullOrWhiteSpace(_searchText))
        {
            return true;
        }

        var query = _searchText.Trim();
        return ContainsIgnoreCase(poi.Name, query)
            || ContainsIgnoreCase(poi.Slug, query)
            || ContainsIgnoreCase(poi.OwnerEmail, query)
            || ContainsIgnoreCase(poi.CategoryName, query);
    }

    private int GetFilterCount(PoiFilterTab filter) => _pois.Count(item => MatchesFilter(item, filter));

    private static bool MatchesFilter(AdminPoiDto poi, PoiFilterTab filter) => filter switch
    {
        PoiFilterTab.Published => poi.Status is PoiStatus.Published or PoiStatus.Updated,
        PoiFilterTab.Pending => poi.Status == PoiStatus.PendingReview,
        PoiFilterTab.Archived => poi.Status is PoiStatus.Archived or PoiStatus.Rejected,
        _ => true
    };

    private static bool ContainsIgnoreCase(string? source, string query) =>
        !string.IsNullOrWhiteSpace(source)
        && source.Contains(query, StringComparison.OrdinalIgnoreCase);
}
