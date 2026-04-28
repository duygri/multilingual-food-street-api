using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Web.Support;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Owner;

public partial class Dashboard
{
    private bool _isLoading = true;
    private string? _errorMessage;
    private string _ownerDisplayName = "owner@narration.app";
    private OwnerDashboardWorkspaceDto _workspace = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            _ownerDisplayName = RouteHelper.GetDisplayName(authState.User);
            _workspace = await OwnerPortalService.GetDashboardWorkspaceAsync();
        }
        catch (ApiException exception)
        {
            _errorMessage = exception.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }
}
