using Android.Content;
using Android.OS;
using Microsoft.Maui.ApplicationModel;
using NarrationApp.Mobile.Features.Home;
using System.Runtime.Versioning;

namespace NarrationApp.Mobile.Platforms.Android.Services;

public sealed class AndroidVisitorBackgroundLocationRuntime(Context context) : IVisitorBackgroundLocationRuntime
{
    public async Task<VisitorBackgroundTrackingStatus> ApplyAsync(
        VisitorBackgroundTrackingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.IsEnabled)
        {
            StopService();
            return new VisitorBackgroundTrackingStatus(
                IsSupported: true,
                IsRunning: false,
                HasBackgroundPermission: false,
                StatusLabel: "Background tracking đang tắt");
        }

        if (!request.HasForegroundPermission)
        {
            StopService();
            return new VisitorBackgroundTrackingStatus(
                IsSupported: true,
                IsRunning: false,
                HasBackgroundPermission: false,
                StatusLabel: "Cần bật quyền vị trí trước khi chạy nền");
        }

        var backgroundPermission = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        if (backgroundPermission != PermissionStatus.Granted)
        {
            backgroundPermission = await Permissions.RequestAsync<Permissions.LocationAlways>();
        }

        if (backgroundPermission != PermissionStatus.Granted)
        {
            StopService();
            return new VisitorBackgroundTrackingStatus(
                IsSupported: true,
                IsRunning: false,
                HasBackgroundPermission: false,
                StatusLabel: "Chưa cấp quyền định vị nền");
        }

        var intent = new Intent(context, typeof(VisitorBackgroundLocationForegroundService));
        intent.SetAction(VisitorBackgroundLocationForegroundService.ActionStart);
        intent.PutExtra(
            VisitorBackgroundLocationForegroundService.ExtraIntervalSeconds,
            ResolveIntervalSeconds(request.AccuracyMode));

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
#pragma warning disable CA1416
            StartForegroundServiceApi26(intent);
#pragma warning restore CA1416
        }
        else
        {
            context.StartService(intent);
        }

        return new VisitorBackgroundTrackingStatus(
            IsSupported: true,
            IsRunning: true,
            HasBackgroundPermission: true,
            StatusLabel: BuildRunningStatusLabel(request.AccuracyMode));
    }

    private void StopService()
    {
        var intent = new Intent(context, typeof(VisitorBackgroundLocationForegroundService));
        intent.SetAction(VisitorBackgroundLocationForegroundService.ActionStop);
        context.StartService(intent);
    }

    [SupportedOSPlatform("android26.0")]
    private void StartForegroundServiceApi26(Intent intent)
    {
        context.StartForegroundService(intent);
    }

    private static int ResolveIntervalSeconds(VisitorGpsAccuracyMode mode) =>
        mode switch
        {
            VisitorGpsAccuracyMode.High => 6,
            VisitorGpsAccuracyMode.BatterySaver => 20,
            _ => 12
        };

    private static string BuildRunningStatusLabel(VisitorGpsAccuracyMode mode) =>
        mode switch
        {
            VisitorGpsAccuracyMode.High => "Background tracking đang chạy • High accuracy",
            VisitorGpsAccuracyMode.BatterySaver => "Background tracking đang chạy • Battery saver",
            _ => "Background tracking đang chạy • Adaptive"
        };
}
