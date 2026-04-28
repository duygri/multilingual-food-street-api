using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class PoiManagement
{
    private const int PageSize = 5;
    private static readonly IReadOnlyList<PoiFilterTab> FilterTabs = Enum.GetValues<PoiFilterTab>();

    private bool _isLoading = true;
    private string? _errorMessage;
    private string? _statusMessage;
    private IReadOnlyList<AdminPoiDto> _pois = Array.Empty<AdminPoiDto>();
    private AdminPoiDto? _selectedPoi;
    private PoiFilterTab _activeFilter = PoiFilterTab.All;
    private string _searchText = string.Empty;

    private int CurrentPage { get; set; } = 1;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _pois = await AdminPortalService.GetPoisAsync();
            NormalizePageAndSelection();
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

    private sealed record PoiRow(int Number, AdminPoiDto Item);

    private enum PoiFilterTab
    {
        All,
        Published,
        Pending,
        Archived
    }
}
