using NarrationApp.Shared.DTOs.Auth;
using NarrationApp.SharedUI.Auth;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Owner;

public partial class Profile
{
    private async Task SaveProfileAsync()
    {
        if (_editor is null) return;
        _isSavingProfile = true;

        try
        {
            _profile = await OwnerProfileService.UpdateProfileAsync(_editor.ToRequest());
            _editor = OwnerProfileEditModel.FromProfile(_profile);
            await RefreshAuthSessionAsync(_profile);
            _statusMessage = "Đã cập nhật hồ sơ owner.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
        catch (Exception exception)
        {
            _statusMessage = exception.Message;
        }
        finally
        {
            _isSavingProfile = false;
        }
    }

    private async Task ChangePasswordAsync()
    {
        if (!string.Equals(_passwordEditor.NewPassword, _passwordEditor.ConfirmPassword, StringComparison.Ordinal))
        {
            _statusMessage = "Mật khẩu xác nhận không khớp.";
            return;
        }

        _isChangingPassword = true;

        try
        {
            await OwnerProfileService.ChangePasswordAsync(new ChangePasswordRequest
            {
                CurrentPassword = _passwordEditor.CurrentPassword,
                NewPassword = _passwordEditor.NewPassword
            });

            _passwordEditor = new PasswordEditModel();
            _statusMessage = "Đã đổi mật khẩu owner.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
        catch (Exception exception)
        {
            _statusMessage = exception.Message;
        }
        finally
        {
            _isChangingPassword = false;
        }
    }

    private async Task RefreshAuthSessionAsync(NarrationApp.Shared.DTOs.Owner.OwnerProfileDto profile)
    {
        var session = await AuthSessionStore.GetAsync();
        if (session is null) return;

        await AuthStateProvider.MarkUserAsAuthenticatedAsync(new AuthSession
        {
            UserId = profile.UserId,
            FullName = profile.FullName,
            Email = profile.Email,
            Role = session.Role,
            PreferredLanguage = profile.PreferredLanguage,
            Token = session.Token
        });
    }
}
