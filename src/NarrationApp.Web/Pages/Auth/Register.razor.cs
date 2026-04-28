using NarrationApp.Shared.DTOs.Auth;
using NarrationApp.SharedUI.Models;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Auth;

public partial class Register
{
    private static readonly HeroAction LoginAction = new() { Label = "Tôi đã có tài khoản", Href = "/auth/login", IsPrimary = false };
    private static readonly string[] HeroBadges = ["Cổng owner", "Hàng chờ duyệt", "Vận hành POI", "Admin phê duyệt"];
    private readonly RegisterFormModel _model = new();
    private bool _isSubmitting;
    private string? _errorMessage;

    private async Task HandleSubmitAsync()
    {
        _isSubmitting = true;
        _errorMessage = null;

        try
        {
            if (!string.Equals(_model.Password, _model.ConfirmPassword, StringComparison.Ordinal))
            {
                _errorMessage = "Mật khẩu nhập lại chưa khớp.";
                return;
            }

            await AuthClientService.RegisterOwnerAsync(new RegisterOwnerRequest
            {
                FullName = _model.FullName,
                Email = _model.Email,
                Password = _model.Password
            });

            NavigationManager.NavigateTo("/auth/login?registered=owner-pending", replace: true);
        }
        catch (ApiException exception)
        {
            _errorMessage = exception.Message;
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private sealed class RegisterFormModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
