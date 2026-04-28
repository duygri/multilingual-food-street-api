using NarrationApp.Shared.DTOs.Geofence;
using NarrationApp.Shared.DTOs.Poi;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Owner;

public partial class PoiDetail
{
    private async Task SavePoiAsync()
    {
        if (_poi is null || _editor is null)
        {
            return;
        }

        _isSaving = true;

        try
        {
            var updated = await OwnerPortalService.UpdatePoiAsync(_poi.Id, _editor.ToRequest(_poi));
            _poi = updated;
            HydrateEditors(updated);
            await ReloadWorkspaceAsync();
            NotifyOwnerPortalChanged();
            _statusMessage = $"Đã lưu {_poi.Name}.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task SaveGeofenceAsync()
    {
        if (_poi is null || _geofenceEditor is null)
        {
            return;
        }

        _isSavingGeofence = true;

        try
        {
            var updated = await GeofencePortalService.UpdateAsync(_poi.Id, _geofenceEditor.ToRequest());
            _poi = WithUpdatedGeofence(_poi, updated);
            _geofenceEditor = GeofenceEditModel.FromGeofence(updated);
            await ReloadWorkspaceAsync();
            _statusMessage = "Đã cập nhật vùng kích hoạt.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
        finally
        {
            _isSavingGeofence = false;
        }
    }

    private void OpenDeleteDialog()
    {
        _isDeleteDialogOpen = true;
    }

    private void CloseDeleteDialog()
    {
        _isDeleteDialogOpen = false;
    }

    private async Task DeletePoiAsync()
    {
        if (_poi is null)
        {
            return;
        }

        try
        {
            await OwnerPortalService.DeletePoiAsync(_poi.Id);
            NotifyOwnerPortalChanged();
            NavigationManager.NavigateTo("/owner/pois");
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
            _isDeleteDialogOpen = false;
        }
    }

    private void NotifyOwnerPortalChanged()
    {
        if (ServiceProvider.GetService(typeof(OwnerPortalRefreshService)) is OwnerPortalRefreshService refreshService)
        {
            refreshService.NotifyChanged();
        }
    }

    private static PoiDto WithUpdatedGeofence(PoiDto poi, GeofenceDto geofence)
    {
        var geofences = poi.Geofences
            .Where(item => item.Id != geofence.Id)
            .Prepend(geofence)
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.Priority)
            .ToArray();

        return new PoiDto
        {
            Id = poi.Id,
            Name = poi.Name,
            Slug = poi.Slug,
            OwnerId = poi.OwnerId,
            Lat = poi.Lat,
            Lng = poi.Lng,
            Priority = poi.Priority,
            CategoryId = poi.CategoryId,
            CategoryName = poi.CategoryName,
            NarrationMode = poi.NarrationMode,
            Description = poi.Description,
            TtsScript = poi.TtsScript,
            MapLink = poi.MapLink,
            ImageUrl = poi.ImageUrl,
            Status = poi.Status,
            CreatedAtUtc = poi.CreatedAtUtc,
            Translations = poi.Translations,
            Geofences = geofences
        };
    }
}
