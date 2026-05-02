using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Owner;

public partial class Profile
{
    private bool _isLoading = true;
    private bool _isSavingProfile;
    private bool _isChangingPassword;
    private string? _errorMessage;
    private string? _statusMessage;
    private OwnerProfileDto? _profile;
    private OwnerProfileEditModel? _editor;
    private PasswordEditModel _passwordEditor = new();
    protected override async Task OnInitializedAsync()
    {
        try
        {
            var profileTask = OwnerProfileService.GetProfileAsync();
            var languagesTask = LanguagePortalService.GetAsync();
            await Task.WhenAll(profileTask, languagesTask);
            _profile = profileTask.Result;
            _editor = OwnerProfileEditModel.FromProfile(_profile);
            PreferredLanguageOptions = BuildPreferredLanguageOptions(languagesTask.Result, _profile.PreferredLanguage);
        }
        catch (ApiException exception)
        {
            _errorMessage = exception.Message;
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }
}
