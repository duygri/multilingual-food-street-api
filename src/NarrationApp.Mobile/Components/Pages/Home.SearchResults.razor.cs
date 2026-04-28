using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private IReadOnlyList<VisitorPoi> GetSearchPoiResults() =>
        VisitorSearchResultSelector.GetPoiResults(_state.FilteredPois, _state.SearchTerm);

    private IReadOnlyList<VisitorTourCard> GetSearchTourResults() =>
        VisitorSearchResultSelector.GetTourResults(_state.Tours, _state.Pois, _state.SelectedCategoryId, _state.SearchTerm);

    private int GetSearchResultCount() =>
        VisitorSearchResultSelector.GetResultCount(GetSearchPoiResults(), GetSearchTourResults());
}
