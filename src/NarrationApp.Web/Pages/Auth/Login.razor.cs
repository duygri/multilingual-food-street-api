using NarrationApp.Shared.DTOs.Auth;
using NarrationApp.SharedUI.Models;
using NarrationApp.Web.Support;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Auth;

public partial class Login
{
    private static readonly HeroAction RegisterAction = new() { Label = "Đăng ký owner", Href = "/auth/register", IsPrimary = false };
    private static readonly string[] HeroBadges = [".NET 8", "Blazor WASM", "Portal admin", "Owner phê duyệt", "PostgreSQL", "SignalR"];
    private readonly LoginFormModel _model = new();
    private bool _isSubmitting;
    private string? _errorMessage;
    private string? _statusMessage;

    protected override void OnInitialized()
    {
        if (NavigationManager.Uri.Contains("registered=owner-pending", StringComparison.OrdinalIgnoreCase))
        {
            _statusMessage = "Đã gửi yêu cầu đăng ký. Vui lòng chờ admin duyệt.";
        }
    }

    private async Task HandleSubmitAsync()
    {
        _isSubmitting = true;
        _errorMessage = null;
        try
        {
            var session = await AuthClientService.LoginAsync(new LoginRequest { Email = _model.Email, Password = _model.Password });
            NavigationManager.NavigateTo(RouteHelper.GetDefaultRoute(session.Role), replace: true);
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

    private sealed class LoginFormModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
