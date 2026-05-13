using NarrationApp.Shared.Visitor;

namespace NarrationApp.Mobile.Features.Home;

public sealed partial class VisitorShellState
{
    public void ApplyQrNavigationTarget(VisitorQrNavigationTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        EnterReadyFromExternalEntry();

        _pendingQrNavigationTarget = null;
        if (TryApplyQrNavigationTarget(target))
        {
            return;
        }

        if (target.Kind is VisitorQrTargetKind.Poi or VisitorQrTargetKind.Tour && !string.IsNullOrWhiteSpace(target.TargetId))
        {
            _pendingQrNavigationTarget = target;
        }

        SwitchTab(VisitorTab.Map);
    }

    public void SwitchTab(VisitorTab tab)
    {
        CurrentTab = tab;

        if (tab != VisitorTab.Settings)
        {
            CurrentSettingsScreen = VisitorSettingsScreen.Overview;
        }

        if (tab == VisitorTab.Tours && SelectedTour is null && _tours.Count > 0)
        {
            SelectedTourId = _tours[0].Id;
        }
    }

    public void OpenSettingsScreen(VisitorSettingsScreen screen)
    {
        CurrentTab = VisitorTab.Settings;
        CurrentSettingsScreen = screen;
    }

    public void CloseSettingsScreen()
    {
        CurrentSettingsScreen = VisitorSettingsScreen.Overview;
    }

    public void SelectCategory(string categoryId)
    {
        if (!_categories.Any(category => category.Id == categoryId))
        {
            return;
        }

        if (SelectedCategoryId == categoryId)
        {
            return;
        }

        SelectedCategoryId = categoryId;
        InvalidatePoiViews();
        EnsureSelectedPoiStillVisible();
    }

    public void SetSearchTerm(string searchTerm)
    {
        var trimmedSearchTerm = searchTerm.Trim();
        if (SearchTerm == trimmedSearchTerm)
        {
            return;
        }

        SearchTerm = trimmedSearchTerm;
        InvalidatePoiViews();
        EnsureSelectedPoiStillVisible();
    }

    public void OpenPoi(string poiId)
    {
        if (!_pois.Any(poi => poi.Id == poiId))
        {
            return;
        }

        if (string.Equals(_dismissedProximityPoiId, poiId, StringComparison.OrdinalIgnoreCase))
        {
            _dismissedProximityPoiId = null;
        }

        SelectedPoiId = poiId;
        ShowPoiSheet = true;
        ShowMiniPlayer = true;
        CurrentTab = VisitorTab.Map;
        CurrentSettingsScreen = VisitorSettingsScreen.Overview;
    }

    public void PreviewPoi(string poiId)
    {
        if (!_pois.Any(poi => poi.Id == poiId))
        {
            return;
        }

        SelectedPoiId = poiId;
        ShowPoiSheet = false;
        ShowMiniPlayer = true;
        CurrentSettingsScreen = VisitorSettingsScreen.Overview;
    }

    public void ClosePoiSheet()
    {
        if (ActiveProximity is not null
            && string.Equals(ActiveProximity.PoiId, SelectedPoiId, StringComparison.OrdinalIgnoreCase))
        {
            _dismissedProximityPoiId = ActiveProximity.PoiId;
        }

        ShowPoiSheet = false;
    }

    public void ToggleMiniPlayer()
    {
        ShowMiniPlayer = !ShowMiniPlayer;
    }

    private void EnsureSelectedPoiStillVisible()
    {
        var visiblePois = FilteredPois;
        if (visiblePois.Count == 0)
        {
            SelectedPoiId = null;
            ShowPoiSheet = false;
            ShowMiniPlayer = false;
            return;
        }

        if (SelectedPoiId is not null && visiblePois.Any(poi => poi.Id == SelectedPoiId))
        {
            return;
        }

        SelectedPoiId = null;
        ShowPoiSheet = false;
        ShowMiniPlayer = false;
    }

    private void ResetDiscoverFilters()
    {
        if (SelectedCategoryId == "all" && SearchTerm == string.Empty)
        {
            return;
        }

        SelectedCategoryId = "all";
        SearchTerm = string.Empty;
        InvalidatePoiViews();
    }

    private void InvalidatePoiViews()
    {
        _filteredPoisCache = null;
        _featuredPoisCache = null;
        _discoverPoisCache = null;
        _featuredDiscoverPoisCache = null;
    }

    private void EnsureSelectedCategoryStillVisible()
    {
        if (_categories.Any(category => string.Equals(category.Id, SelectedCategoryId, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        SelectedCategoryId = "all";
    }
}
