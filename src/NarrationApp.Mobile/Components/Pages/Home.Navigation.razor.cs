using NarrationApp.Mobile.Components.Pages.Sections;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private void SwitchTabFromShell(VisitorTab tab)
    {
        PrepareForPrimaryTabSwitch();
        _state.SwitchTab(tab);
    }

    private void ToggleNotificationsPanel()
    {
        CloseSearchOverlaySurface();
        CloseFullPlayerSurface();

        if (_state.CurrentTab != VisitorTab.Map)
        {
            _state.SwitchTab(VisitorTab.Map);
        }

        _state.ToggleNotifications();
    }
}
