using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class TourManagement
{
    private async Task SaveTourAsync()
    {
        var validationMessage = ValidateEditor();
        if (validationMessage is not null)
        {
            _statusMessage = validationMessage;
            return;
        }

        try
        {
            if (_isCreateMode)
            {
                var created = await TourPortalService.CreateTourAsync(_editor.ToCreateRequest());
                _tours = [created, .. _tours];
                SelectTour(created);
                _statusMessage = $"Đã tạo tour mới: {created.Title}.";
            }
            else if (_selectedTour is not null)
            {
                var updated = await TourPortalService.UpdateTourAsync(_selectedTour.Id, _editor.ToUpdateRequest());
                _tours = _tours.Select(item => item.Id == updated.Id ? updated : item).ToArray();
                SelectTour(updated);
                _statusMessage = $"Đã cập nhật tour: {updated.Title}.";
            }
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
    }

    private async Task DeleteTourAsync()
    {
        if (_selectedTour is null) return;

        try
        {
            var deletedId = _selectedTour.Id;
            await TourPortalService.DeleteTourAsync(deletedId);
            _tours = _tours.Where(item => item.Id != deletedId).ToArray();
            _statusMessage = "Đã xóa tour khỏi hệ thống.";
            _selectedTour = null;
            _isCreateMode = false;
            _showEditor = false;
            _editor = TourEditorModel.CreateDefault();
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
    }

    private string? ValidateEditor()
    {
        if (string.IsNullOrWhiteSpace(_editor.Title)) return "Tên tour là bắt buộc.";
        if (string.IsNullOrWhiteSpace(_editor.Description)) return "Mô tả tour là bắt buộc.";
        if (_editor.EstimatedMinutes <= 0) return "Số phút dự kiến phải lớn hơn 0.";
        if (_editor.Stops.Count == 0) return "Tour phải có ít nhất một stop.";
        if (_editor.Stops.Any(item => item.PoiId <= 0)) return "Mỗi stop phải chọn một POI.";
        if (_editor.Stops.Select(item => item.PoiId).Distinct().Count() != _editor.Stops.Count) return "POI trong tour phải là duy nhất.";
        return _editor.Stops.Any(item => item.RadiusMeters <= 0) ? "Radius mỗi stop phải lớn hơn 0." : null;
    }
}
