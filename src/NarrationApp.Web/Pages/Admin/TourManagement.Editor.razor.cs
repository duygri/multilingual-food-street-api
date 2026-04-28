using NarrationApp.Shared.DTOs.Tour;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Pages.Admin;

public partial class TourManagement
{
    private void BeginCreateTour()
    {
        _isCreateMode = true;
        _showEditor = true;
        _selectedTour = null;
        _statusMessage = null;
        _editor = TourEditorModel.CreateDefault();

        if (_poiOptions.Count > 0)
        {
            _editor.Stops[0].PoiId = _poiOptions[0].Id;
        }
    }

    private void SelectTour(TourDto tour)
    {
        _selectedTour = tour;
        _isCreateMode = false;
        _showEditor = true;
        _statusMessage = null;
        _editor = TourEditorModel.FromTour(tour);
    }

    private void CloseEditor() => _showEditor = false;

    private async Task QuickPublishAsync(TourDto tour)
    {
        _selectedTour = tour;
        _isCreateMode = false;
        _showEditor = true;
        _editor = TourEditorModel.FromTour(tour);
        _editor.Status = TourStatus.Published;
        await SaveTourAsync();
    }

    private void AddStop()
    {
        if (_poiOptions.Count == 0) return;

        var nextPoiId = _poiOptions.Select(item => item.Id)
            .Except(_editor.Stops.Select(item => item.PoiId))
            .DefaultIfEmpty(_poiOptions[0].Id)
            .First();

        _editor.Stops.Add(new TourStopEditorModel { PoiId = nextPoiId, RadiusMeters = 60 });
    }

    private void RemoveStop(int index)
    {
        if (_editor.Stops.Count > 1)
        {
            _editor.Stops.RemoveAt(index);
        }
    }

    private void UpdateStopPoi(int index, string? value)
    {
        if (int.TryParse(value, out var poiId))
        {
            _editor.Stops[index].PoiId = poiId;
        }
    }

    private void UpdateStopRadius(int index, string? value)
    {
        if (int.TryParse(value, out var radiusMeters))
        {
            _editor.Stops[index].RadiusMeters = radiusMeters;
        }
    }
}
