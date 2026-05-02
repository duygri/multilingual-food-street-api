using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Services;

public sealed class DeviceVisitorLocationService : IVisitorLocationService
{
    public async Task<VisitorLocationSnapshot> GetCurrentAsync(bool requestPermission, CancellationToken cancellationToken = default)
    {
        var permissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (requestPermission && permissionStatus != PermissionStatus.Granted)
        {
            permissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        if (permissionStatus != PermissionStatus.Granted)
        {
            return VisitorLocationSnapshot.Disabled("Chưa cấp quyền vị trí");
        }

        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            var location = await Geolocation.Default.GetLocationAsync(request, cancellationToken);
            if (location is not null)
            {
                return new VisitorLocationSnapshot(
                    PermissionGranted: true,
                    IsLocationAvailable: true,
                    Latitude: location.Latitude,
                    Longitude: location.Longitude,
                    StatusLabel: VisitorLocationStatusFormatter.Build(new VisitorLocationSnapshot(
                        PermissionGranted: true,
                        IsLocationAvailable: true,
                        Latitude: location.Latitude,
                        Longitude: location.Longitude,
                        StatusLabel: string.Empty,
                        Source: VisitorLocationSource.Live)),
                    Source: VisitorLocationSource.Live);
            }

            location = await Geolocation.Default.GetLastKnownLocationAsync();

            if (location is null)
            {
                return new VisitorLocationSnapshot(
                    PermissionGranted: true,
                    IsLocationAvailable: false,
                    Latitude: null,
                    Longitude: null,
                    StatusLabel: "Đã bật quyền, chưa lấy được tọa độ",
                    Source: VisitorLocationSource.Unavailable);
            }

            return new VisitorLocationSnapshot(
                PermissionGranted: true,
                IsLocationAvailable: true,
                Latitude: location.Latitude,
                Longitude: location.Longitude,
                StatusLabel: VisitorLocationStatusFormatter.Build(new VisitorLocationSnapshot(
                    PermissionGranted: true,
                    IsLocationAvailable: true,
                    Latitude: location.Latitude,
                    Longitude: location.Longitude,
                    StatusLabel: string.Empty,
                    Source: VisitorLocationSource.LastKnown)),
                Source: VisitorLocationSource.LastKnown);
        }
        catch (FeatureNotEnabledException)
        {
            return new VisitorLocationSnapshot(
                PermissionGranted: true,
                IsLocationAvailable: false,
                Latitude: null,
                Longitude: null,
                StatusLabel: "GPS đang tắt trên thiết bị",
                Source: VisitorLocationSource.DeviceGpsOff);
        }
        catch (FeatureNotSupportedException)
        {
            return new VisitorLocationSnapshot(
                PermissionGranted: false,
                IsLocationAvailable: false,
                Latitude: null,
                Longitude: null,
                StatusLabel: "Thiết bị không hỗ trợ định vị",
                Source: VisitorLocationSource.Unsupported);
        }
        catch (PermissionException)
        {
            return new VisitorLocationSnapshot(
                PermissionGranted: false,
                IsLocationAvailable: false,
                Latitude: null,
                Longitude: null,
                StatusLabel: "Không truy cập được vị trí",
                Source: VisitorLocationSource.Error);
        }
    }
}
