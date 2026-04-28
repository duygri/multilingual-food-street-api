using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private string GetHeaderTitle() =>
        VisitorNavigationPresentationFormatter.GetHeaderTitle(_state.CurrentTab);

    private string GetVisitorAppClass() =>
        VisitorNavigationPresentationFormatter.GetVisitorAppClass(_state.CurrentTab);

    private string GetVisitorFrameClass() =>
        VisitorNavigationPresentationFormatter.GetVisitorFrameClass(_state.CurrentTab);

    private string GetLanguageTileClass(string languageCode) =>
        VisitorNavigationPresentationFormatter.GetSelectionClass(_state.SelectedLanguageCode == languageCode);

    private string GetLanguageChipClass(string languageCode) =>
        VisitorNavigationPresentationFormatter.GetSelectionClass(_state.SelectedLanguageCode == languageCode);

    private string GetCategoryChipClass(string categoryId) =>
        VisitorNavigationPresentationFormatter.GetSelectionClass(_state.SelectedCategoryId == categoryId);

    private string GetTabClass(VisitorTab tab) =>
        VisitorNavigationPresentationFormatter.GetActiveClass(_state.CurrentTab == tab);

    private string GetTourCardClass(string tourId) =>
        VisitorNavigationPresentationFormatter.GetSelectionClass(_state.SelectedTourId == tourId);

    private string GetPoiCardClass(string poiId) =>
        VisitorNavigationPresentationFormatter.GetSelectionClass(_state.SelectedPoiId == poiId);

    private string GetCategoryIcon(string categoryId) =>
        VisitorNavigationPresentationFormatter.GetCategoryIcon(categoryId);

    private string GetPoiCategoryLabel(VisitorPoi poi) =>
        VisitorNavigationPresentationFormatter.GetPoiCategoryLabel(poi, _state.Categories);

    private string GetAutoAudioStatus() =>
        VisitorNavigationPresentationFormatter.GetAutoAudioStatus(
            _state.AudioPreferences.AutoPlayEnabled,
            _state.ActiveProximity);

    private string GetGeofenceToastMessage() =>
        VisitorNavigationPresentationFormatter.GetGeofenceToastMessage(
            _state.AudioPreferences.AutoPlayEnabled,
            _state.ActiveProximity,
            _state.IsAudioPlaying);
}
