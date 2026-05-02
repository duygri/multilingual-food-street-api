using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;
using System.Runtime.Versioning;

namespace NarrationApp.Mobile.Platforms.Android.Services;

public class VisitorBackgroundLocationForegroundService : Service
{
    public const string ActionStart = "NarrationApp.Mobile.Action.StartBackgroundLocation";
    public const string ActionStop = "NarrationApp.Mobile.Action.StopBackgroundLocation";
    public const string ExtraIntervalSeconds = "intervalSeconds";

    private const string NotificationChannelId = "visitor_background_location";
    private const int NotificationId = 49021;
    private CancellationTokenSource? _loopCancellation;
    private Task? _loopTask;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (string.Equals(intent?.Action, ActionStop, StringComparison.Ordinal))
        {
            StopTracking();
            StopForeground(StopForegroundFlags.Remove);
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        var intervalSeconds = Math.Max(5, intent?.GetIntExtra(ExtraIntervalSeconds, 12) ?? 12);
        EnsureNotificationChannel();
        StartForeground(NotificationId, BuildNotification(intervalSeconds));
        RestartTrackingLoop(intervalSeconds);
        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        StopTracking();
        base.OnDestroy();
    }

    private void RestartTrackingLoop(int intervalSeconds)
    {
        StopTracking();
        _loopCancellation = new CancellationTokenSource();
        _loopTask = RunTrackingLoopAsync(TimeSpan.FromSeconds(intervalSeconds), _loopCancellation.Token);
    }

    private void StopTracking()
    {
        _loopCancellation?.Cancel();
        _loopCancellation?.Dispose();
        _loopCancellation = null;
        _loopTask = null;
    }

    private async Task RunTrackingLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        try
        {
            await CaptureLocationAsync(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(interval, cancellationToken);
                await CaptureLocationAsync(cancellationToken);
            }
        }
        catch (System.OperationCanceledException)
        {
            // Expected when the service stops.
        }
    }

    private static async Task CaptureLocationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)),
                cancellationToken)
                ?? await Geolocation.Default.GetLastKnownLocationAsync();

            if (location is null)
            {
                return;
            }

            Preferences.Default.Set("visitor.background.lat", location.Latitude);
            Preferences.Default.Set("visitor.background.lng", location.Longitude);
            Preferences.Default.Set("visitor.background.updated_utc", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }
        catch (Exception)
        {
            // Best-effort capture only; foreground service must stay alive even if one sample fails.
        }
    }

    private Notification BuildNotification(int intervalSeconds)
    {
#pragma warning disable CA1416, CA1422
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            return BuildNotificationApi26(intervalSeconds);
        }

        return new Notification.Builder(this)
            .SetContentTitle("Food Street Visitor")
            .SetContentText($"Background tracking đang chạy • chu kỳ {intervalSeconds}s")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetOngoing(true)
            .Build();
#pragma warning restore CA1416, CA1422
    }

    [SupportedOSPlatform("android26.0")]
    private Notification BuildNotificationApi26(int intervalSeconds)
    {
        return new Notification.Builder(this, NotificationChannelId)
            .SetContentTitle("Food Street Visitor")
            .SetContentText($"Background tracking đang chạy • chu kỳ {intervalSeconds}s")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetOngoing(true)
            .Build();
    }

    private void EnsureNotificationChannel()
    {
#pragma warning disable CA1416
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        EnsureNotificationChannelApi26();
#pragma warning restore CA1416
    }

    [SupportedOSPlatform("android26.0")]
    private void EnsureNotificationChannelApi26()
    {
        var manager = (NotificationManager?)GetSystemService(NotificationService);
        if (manager?.GetNotificationChannel(NotificationChannelId) is not null)
        {
            return;
        }

        var channel = new NotificationChannel(
            NotificationChannelId,
            "Visitor background tracking",
            NotificationImportance.Low)
        {
            Description = "Giữ GPS visitor chạy nền cho geofence và nearby POI."
        };

        manager?.CreateNotificationChannel(channel);
    }
}
