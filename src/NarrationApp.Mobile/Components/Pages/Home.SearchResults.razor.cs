using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private VisitorSearchResultsSummary GetSearchResultsSummary()
    {
        var poiResults = VisitorSearchResultSelector.GetPoiResults(_state.FilteredPois, _state.SearchTerm);
        var tourResults = VisitorSearchResultSelector.GetTourResults(
            _state.Tours,
            _state.Pois,
            _state.SelectedCategoryId,
            _state.SearchTerm);

        return new VisitorSearchResultsSummary(
            VisitorSearchResultSelector.GetResultCount(poiResults, tourResults),
            poiResults,
            tourResults);
    }
}
