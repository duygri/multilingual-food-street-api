using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private string GetToursHeroCopy() =>
        VisitorTourPresentationFormatter.GetHeroCopy(_state.ActiveTourSession);

    private string GetTourActionLabel(string tourId) =>
        UiText.FormatTourActionLabel(_state.ActiveTourSession, tourId);

    private string GetSelectedTourPrimaryActionLabel() =>
        UiText.FormatSelectedTourActionLabel(_state.SelectedTour, _state.ActiveTourSession);

    private string GetSelectedTourStatusBadge() =>
        VisitorTourPresentationFormatter.GetSelectedTourStatusBadge(_state.SelectedTour, _state.ActiveTourSession);

    private string GetActiveTourBannerText() =>
        VisitorTourPresentationFormatter.GetActiveTourBannerText(_state.ActiveTourSession);

    private string GetTourProgressBannerClass() =>
        VisitorTourPresentationFormatter.GetProgressBannerClass(_state.ActiveTourSession);

    private string GetTourHeroIcon(VisitorTourCard tour) =>
        VisitorTourPresentationFormatter.GetHeroIcon(tour);

    private string GetTourHeroTone(VisitorTourCard tour) =>
        VisitorTourPresentationFormatter.GetHeroTone(tour);

    private string GetTourRouteDistanceLabel(VisitorTourCard tour) =>
        VisitorTourPresentationFormatter.GetRouteDistanceLabel(tour);

    private string GetTourParticipationLabel(VisitorTourCard tour) =>
        VisitorTourPresentationFormatter.GetParticipationLabel(tour);

    private string GetTourProgressLabel() =>
        UiText.FormatTourProgressLabel(_state.SelectedTour, _state.ActiveTourSession);

    private string GetTourProgressPercent() =>
        VisitorTourPresentationFormatter.GetProgressPercent(_state.SelectedTour, _state.ActiveTourSession);

    private string GetTourStopStateLabel(string tourId, string poiId, int stopIndex) =>
        UiText.FormatTourStopStateLabel(_state.ActiveTourSession, tourId, poiId, stopIndex);

    private string GetTourStopClass(string poiId) =>
        VisitorTourPresentationFormatter.GetStopClass(_state.ActiveTourSession, _state.SelectedPoi?.Id, poiId);
}
