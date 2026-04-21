using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Services;

public sealed class DeviceTouristLocationService : ITouristLocationService
{
    public async Task<TouristLocationSnapshot> GetCurrentAsync(bool requestPermission, CancellationToken cancellationToken = default)
    {
        var permissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (requestPermission && permissionStatus != PermissionStatus.Granted)
        {
            permissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        if (permissionStatus != PermissionStatus.Granted)
        {
            return TouristLocationSnapshot.Disabled("Chưa cấp quyền vị trí");
        }

        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            var location = await Geolocation.Default.GetLocationAsync(request, cancellationToken)
                ?? await Geolocation.Default.GetLastKnownLocationAsync();

            if (location is null)
            {
                return new TouristLocationSnapshot(
                    PermissionGranted: true,
                    IsLocationAvailable: false,
                    Latitude: null,
                    Longitude: null,
                    StatusLabel: "Đã bật quyền, chưa lấy được tọa độ");
            }

            return new TouristLocationSnapshot(
                PermissionGranted: true,
                IsLocationAvailable: true,
                Latitude: location.Latitude,
                Longitude: location.Longitude,
                StatusLabel: $"Đã định vị {location.Latitude:F4}, {location.Longitude:F4}");
        }
        catch (FeatureNotEnabledException)
        {
            return new TouristLocationSnapshot(
                PermissionGranted: true,
                IsLocationAvailable: false,
                Latitude: null,
                Longitude: null,
                StatusLabel: "GPS đang tắt trên thiết bị");
        }
        catch (FeatureNotSupportedException)
        {
            return TouristLocationSnapshot.Disabled("Thiết bị không hỗ trợ định vị");
        }
        catch (PermissionException)
        {
            return TouristLocationSnapshot.Disabled("Không truy cập được vị trí");
        }
    }
}
