using NarrationApp.Shared.Visitor;

namespace NarrationApp.Mobile.Features.Home;

public sealed partial class VisitorShellState
{
    public void ContinueFromWelcome()
    {
        CurrentStep = VisitorIntroStep.Language;
    }

    public void ApplyOnboardingStep(VisitorOnboardingStep step)
    {
        CurrentStep = step switch
        {
            VisitorOnboardingStep.Ready => VisitorIntroStep.Ready,
            VisitorOnboardingStep.Permissions => VisitorIntroStep.Permissions,
            VisitorOnboardingStep.Language => VisitorIntroStep.Language,
            _ => VisitorIntroStep.Welcome
        };
    }

    public void SelectLanguage(string languageCode)
    {
        if (!_languages.Any(language => language.Code == languageCode))
        {
            return;
        }

        SelectedLanguageCode = languageCode;
        InvalidatePoiViews();
    }

    public void AdvanceFromLanguageSelection()
    {
        CurrentStep = VisitorIntroStep.Permissions;
    }

    public void ChangeLanguage(string languageCode)
    {
        if (_languages.Any(language => language.Code == languageCode))
        {
            SelectedLanguageCode = languageCode;
            InvalidatePoiViews();
        }
    }

    public void ChangeAppLanguage(string languageCode)
    {
        if (VisitorUiTextCatalog.IsSupportedAppLanguage(languageCode))
        {
            SelectedAppLanguageCode = languageCode.Trim().ToLowerInvariant();
        }
    }

    public void CompletePermissions(bool granted)
    {
        LocationPermissionGranted = granted;
        if (granted)
        {
            CurrentStep = VisitorIntroStep.Ready;
        }
    }

    public void EnterReadyFromExternalEntry()
    {
        CurrentStep = VisitorIntroStep.Ready;
        ResetDiscoverFilters();
        ShowNotifications = false;

        if (SelectedPoi is not null)
        {
            ShowPoiSheet = true;
            ShowMiniPlayer = true;
        }
    }
}
