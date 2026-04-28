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
            _profile = await OwnerProfileService.GetProfileAsync();
            _editor = OwnerProfileEditModel.FromProfile(_profile);
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
