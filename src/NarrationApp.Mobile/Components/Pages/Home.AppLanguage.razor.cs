using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private void RestorePreferredLanguages(string preferredLanguageCode, string preferredAppLanguageCode)
    {
        if (!string.IsNullOrWhiteSpace(preferredLanguageCode))
        {
            _state.ChangeLanguage(preferredLanguageCode);
        }

        if (!string.IsNullOrWhiteSpace(preferredAppLanguageCode))
        {
            _state.ChangeAppLanguage(preferredAppLanguageCode);
        }
        else if (VisitorUiTextCatalog.IsSupportedAppLanguage(preferredLanguageCode))
        {
            _state.ChangeAppLanguage(preferredLanguageCode);
        }
    }
}
