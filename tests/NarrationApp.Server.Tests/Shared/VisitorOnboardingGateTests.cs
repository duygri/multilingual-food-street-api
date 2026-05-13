using NarrationApp.Shared.Visitor;

namespace NarrationApp.Server.Tests.Shared;

public sealed class VisitorOnboardingGateTests
{
    [Theory]
    [InlineData(false, false, false, VisitorOnboardingStep.Welcome)]
    [InlineData(true, false, false, VisitorOnboardingStep.Language)]
    [InlineData(true, true, false, VisitorOnboardingStep.Permissions)]
    [InlineData(true, true, true, VisitorOnboardingStep.Ready)]
    public void ResolveInitialStep_maps_persisted_onboarding_state_to_expected_intro_step(
        bool hasSeenWelcome,
        bool hasSelectedLanguage,
        bool hasGrantedLocation,
        VisitorOnboardingStep expectedStep)
    {
        var result = VisitorOnboardingGate.ResolveInitialStep(
            hasSeenWelcome,
            hasSelectedLanguage,
            hasGrantedLocation);

        Assert.Equal(expectedStep, result);
    }
}
