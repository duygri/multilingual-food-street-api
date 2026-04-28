using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private string GetProfileDisplayName() =>
        VisitorProfilePresentationFormatter.GetDisplayName(_profileDraftName);

    private string GetProfileDisplayEmail() =>
        VisitorProfilePresentationFormatter.GetDisplayEmail(_profileDraftEmail);

    private string GetProfileModeLabel() =>
        VisitorProfilePresentationFormatter.GetModeLabel();

    private string GetProfileInitials() =>
        VisitorProfilePresentationFormatter.GetInitials(GetProfileDisplayName());
}
