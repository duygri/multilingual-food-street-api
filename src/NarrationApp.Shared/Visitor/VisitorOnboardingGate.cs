namespace NarrationApp.Shared.Visitor;

public enum VisitorOnboardingStep
{
    Welcome,
    Language,
    Permissions,
    Ready
}

public static class VisitorOnboardingGate
{
    public static VisitorOnboardingStep ResolveInitialStep(
        bool hasSeenWelcome,
        bool hasSelectedLanguage,
        bool hasGrantedLocation)
    {
        if (!hasSeenWelcome)
        {
            return VisitorOnboardingStep.Welcome;
        }

        if (!hasSelectedLanguage)
        {
            return VisitorOnboardingStep.Language;
        }

        return hasGrantedLocation
            ? VisitorOnboardingStep.Ready
            : VisitorOnboardingStep.Permissions;
    }
}
