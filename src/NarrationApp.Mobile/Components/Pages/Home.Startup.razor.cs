using Microsoft.JSInterop;
using Microsoft.Maui.Storage;
using NarrationApp.Mobile.Features.Home;
using NarrationApp.Shared.Visitor;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private const string OnboardingWelcomeSeenKey = "visitor.onboarding.welcome-seen";
    private const string OnboardingLanguageSelectedKey = "visitor.onboarding.language-selected";
    private const string OnboardingLocationGrantedKey = "visitor.onboarding.location-granted";
    private const string PreferredLanguageCodeKey = "visitor.preferences.language-code";
    private const string PreferredAppLanguageCodeKey = "visitor.preferences.app-language-code";

    protected override void OnInitialized()
    {
        VisitorMobileDiagnostics.Log("Home", "OnInitialized start");
        RestoreOnboardingState();
        VisitorPendingDeepLinkStore.PendingChanged += HandlePendingDeepLinkChanged;
        VisitorMobileDiagnostics.Log("Home", $"OnInitialized end currentStep={_state.CurrentStep} currentTab={_state.CurrentTab}");
    }

    private void RestoreOnboardingState()
    {
        var hasSeenWelcome = Preferences.Default.Get(OnboardingWelcomeSeenKey, false);
        var hasSelectedLanguage = Preferences.Default.Get(OnboardingLanguageSelectedKey, false);
        var hasGrantedLocation = Preferences.Default.Get(OnboardingLocationGrantedKey, false);
        var preferredLanguageCode = Preferences.Default.Get(PreferredLanguageCodeKey, string.Empty);
        var preferredAppLanguageCode = Preferences.Default.Get(PreferredAppLanguageCodeKey, string.Empty);

        RestorePreferredLanguages(preferredLanguageCode, preferredAppLanguageCode);
        _cachePreloadStatusLabel = UiText.ReadyPreloadStatus();

        _state.ApplyOnboardingStep(VisitorOnboardingGate.ResolveInitialStep(
            hasSeenWelcome,
            hasSelectedLanguage,
            hasGrantedLocation));
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _mapBridge = DotNetObjectReference.Create(this);
            _audioBridge = DotNetObjectReference.Create(this);
            StartPresenceHeartbeatLoopIfNeeded();
            StartForegroundLocationLoopIfNeeded();
            QueueStartupWork();
        }

        if (_pendingSelectedPoiAudioPreparationRequested && _audioBridge is not null)
        {
            var autoPlay = _pendingSelectedPoiAutoPlay;
            _pendingSelectedPoiAudioPreparationRequested = false;
            _pendingSelectedPoiAutoPlay = false;
            await PrepareSelectedPoiAudioAsync(autoPlay, forceAutoPlay: autoPlay);
            StateHasChanged();
        }

        await RenderMapIfNeededAsync();
    }
}
