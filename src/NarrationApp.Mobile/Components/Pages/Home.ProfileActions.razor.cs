using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private Task SetProfileDraftNameAsync(string value)
    {
        _profileDraftName = value;
        return Task.CompletedTask;
    }

    private Task SetProfileDraftEmailAsync(string value)
    {
        _profileDraftEmail = value;
        return Task.CompletedTask;
    }

    private Task SaveProfileDraftAsync()
    {
        _profileErrorMessage = null;
        _profileStatusMessage = null;

        _profileDraftName = VisitorProfilePresentationFormatter.NormalizeDraftName(_profileDraftName);
        _profileDraftEmail = VisitorProfilePresentationFormatter.NormalizeDraftEmail(_profileDraftEmail);
        _profileStatusMessage = "Đã lưu hồ sơ cục bộ cho bản mobile demo.";
        return Task.CompletedTask;
    }

    private void SyncProfileDraftFromSession(bool force = false)
    {
        if (force || string.IsNullOrWhiteSpace(_profileDraftName))
        {
            _profileDraftName = VisitorProfilePresentationFormatter.GetDefaultProfileName();
        }

        if (force || string.IsNullOrWhiteSpace(_profileDraftEmail))
        {
            _profileDraftEmail = string.Empty;
        }

        if (force)
        {
            _profileStatusMessage = null;
            _profileErrorMessage = null;
        }
    }
}
