using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Moderation;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class PoiManagement
{
    private async Task ApprovePoiAsync(AdminPoiDto poi, int moderationRequestId)
    {
        try
        {
            await AdminPortalService.ApproveModerationAsync(moderationRequestId, new ReviewModerationRequest
            {
                ReviewNote = "Approved from admin POI workspace."
            });

            ReplacePoi(CloneWithStatus(poi, PoiStatus.Published));
            _statusMessage = $"Đã duyệt POI {poi.Name}.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
        catch (Exception exception)
        {
            _statusMessage = exception.Message;
        }
    }

    private async Task RejectPoiAsync(AdminPoiDto poi, int moderationRequestId)
    {
        try
        {
            await AdminPortalService.RejectModerationAsync(moderationRequestId, new ReviewModerationRequest
            {
                ReviewNote = "Rejected from admin POI workspace."
            });

            ReplacePoi(CloneWithStatus(poi, PoiStatus.Rejected));
            _statusMessage = $"Đã từ chối POI {poi.Name}.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
        catch (Exception exception)
        {
            _statusMessage = exception.Message;
        }
    }

    private async Task DeletePoiAsync(AdminPoiDto poi)
    {
        try
        {
            await AdminPoiOperationsService.DeleteAsync(poi.Id);
            _pois = _pois.Where(item => item.Id != poi.Id).ToArray();
            NormalizePageAndSelection();
            _statusMessage = $"Đã xóa POI {poi.Name}.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
        catch (Exception exception)
        {
            _statusMessage = exception.Message;
        }
    }

    private void ReplacePoi(AdminPoiDto updatedPoi)
    {
        _pois = _pois.Select(item => item.Id == updatedPoi.Id ? updatedPoi : item).ToArray();
        NormalizePageAndSelection(updatedPoi.Id);
    }

    private static AdminPoiDto CloneWithStatus(AdminPoiDto poi, PoiStatus status) =>
        new()
        {
            Id = poi.Id,
            Name = poi.Name,
            Slug = poi.Slug,
            OwnerName = poi.OwnerName,
            OwnerEmail = poi.OwnerEmail,
            Lat = poi.Lat,
            Lng = poi.Lng,
            Priority = poi.Priority,
            CategoryId = poi.CategoryId,
            CategoryName = poi.CategoryName,
            Description = poi.Description,
            TtsScript = poi.TtsScript,
            Status = status,
            AudioAssetCount = poi.AudioAssetCount,
            TranslationCount = poi.TranslationCount,
            GeofenceCount = poi.GeofenceCount,
            PendingModerationId = null,
            CreatedAtUtc = poi.CreatedAtUtc
        };
}
