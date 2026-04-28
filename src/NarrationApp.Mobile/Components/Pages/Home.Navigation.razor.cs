using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NarrationApp.Mobile.Components.Pages.Sections;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private void SwitchTabFromShell(VisitorTab tab)
    {
        _isQrModalOpen = false;
        _isSearchOverlayOpen = false;
        _isFullPlayerOpen = false;
        _discoverPoiDetailId = null;
        _tourDetailId = null;
        _state.CloseSettingsScreen();
        _state.SwitchTab(tab);
    }

    private void ToggleNotificationsPanel()
    {
        _isSearchOverlayOpen = false;
        _isFullPlayerOpen = false;

        if (_state.CurrentTab != VisitorTab.Map)
        {
            _state.SwitchTab(VisitorTab.Map);
        }

        _state.ToggleNotifications();
    }
}
