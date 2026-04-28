using NarrationApp.Shared.DTOs.Owner;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Owner;

public partial class Pois
{
    private readonly PoiStatus[] _statusOptions = Enum.GetValues<PoiStatus>();
    private bool _isLoading = true;
    private string? _errorMessage;
    private string _searchTerm = string.Empty;
    private string _statusFilter = string.Empty;
    private OwnerPoisWorkspaceDto _workspace = new();
    private IReadOnlyList<OwnerPoisWorkspaceRowDto> FilteredRows => _workspace.Rows.Where(MatchesSearch).Where(MatchesStatus).ToArray();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _workspace = await OwnerPortalService.GetPoisWorkspaceAsync();
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
