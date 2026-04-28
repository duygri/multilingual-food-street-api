using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Shared.DTOs.Tour;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class TourManagement
{
    private readonly TourStatus[] _tourStatuses = Enum.GetValues<TourStatus>();
    private bool _isLoading = true;
    private bool _isCreateMode;
    private bool _showEditor;
    private string? _errorMessage;
    private string? _statusMessage;
    private IReadOnlyList<TourDto> _tours = Array.Empty<TourDto>();
    private IReadOnlyList<PoiDto> _poiOptions = Array.Empty<PoiDto>();
    private TourDto? _selectedTour;
    private TourEditorModel _editor = TourEditorModel.CreateDefault();
    private int TotalParticipationEstimate => _tours.Sum(GetParticipationEstimate);
    private string AverageDurationLabel => _tours.Count == 0 ? "0 phút" : $"{Math.Round(_tours.Average(item => item.EstimatedMinutes))} phút";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _poiOptions = await TourPortalService.GetPoiOptionsAsync();
            _tours = await TourPortalService.GetToursAsync();
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
