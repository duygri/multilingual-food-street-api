using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Owner;

public partial class Moderation
{
    private bool _isLoading = true;
    private string? _errorMessage;
    private OwnerModerationWorkspaceDto _workspace = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _workspace = await OwnerPortalService.GetModerationWorkspaceAsync();
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
