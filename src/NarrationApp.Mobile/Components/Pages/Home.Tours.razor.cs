using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NarrationApp.Mobile.Components.Pages.Sections;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private void OpenTourDetail(string tourId)
    {
        _isQrModalOpen = false;
        _isSearchOverlayOpen = false;
        _isFullPlayerOpen = false;
        _state.SelectTour(tourId);
        _tourDetailId = tourId;
        _discoverPoiDetailId = null;
    }

    private void CloseTourDetail()
    {
        _tourDetailId = null;
    }

    private IReadOnlyList<VisitorTourStopItem> GetSelectedTourStopItems()
    {
        if (_state.SelectedTour is null)
        {
            return [];
        }

        return _state.SelectedTour.StopPoiIds
            .Select((poiId, index) =>
            {
                var poi = _state.Pois.FirstOrDefault(item => item.Id == poiId);
                return new VisitorTourStopItem(
                    Sequence: index + 1,
                    PoiId: poiId,
                    PoiName: poi?.Name ?? $"POI {index + 1}",
                    Summary: poi?.Description ?? "Điểm dừng đang chờ nội dung chi tiết.",
                    StateLabel: GetTourStopStateLabel(_state.SelectedTour.Id, poiId, index),
                    DistanceLabel: poi is null ? "Đang cập nhật" : $"{poi.DistanceMeters}m",
                    IsCompleted: _state.ActiveTourSession is not null && index < _state.ActiveTourSession.CurrentStopSequence,
                    IsCurrent: string.Equals(_state.SelectedPoi?.Id, poiId, StringComparison.OrdinalIgnoreCase),
                    IsNext: string.Equals(_state.ActiveTourSession?.NextPoiId, poiId, StringComparison.OrdinalIgnoreCase));
            })
            .ToArray();
    }
}
