using global::Android.App;
using global::Android.Content;
using global::Android.OS;
using AndroidX.Core.App;
using FoodStreet.Mobile.Services;
using Microsoft.Maui.Devices.Sensors;
using Application = global::Android.App.Application;

namespace FoodStreet.Mobile.Platforms.Android
{
    [Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
    public class GpsForegroundService : Service
    {
        private const int NOTIFICATION_ID = 10001;
        private const string CHANNEL_ID = "gps_tracking_channel";
        
        private NativeGpsTrackingService? _gpsService;
        private CancellationTokenSource? _cts;

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            CreateNotificationChannel();
            var notification = CreateNotification();
            
            // Start the foreground service with the notification and type (Required for Android 14+ / API 34+)
            if (OperatingSystem.IsAndroidVersionAtLeast(34))
            {
                StartForeground(NOTIFICATION_ID, notification, global::Android.Content.PM.ForegroundService.TypeLocation);
            }
            else
            {
                StartForeground(NOTIFICATION_ID, notification);
            }

            // Run the actual tracking
            StartTracking();

            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            StopTracking();
            base.OnDestroy();
        }

        private void CreateNotificationChannel()
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                var channel = new NotificationChannel(
                    CHANNEL_ID,
                    "GPS Tracking",
                    NotificationImportance.Low) // Low importance so it doesn't make a sound
                {
                    Description = "Used to keep GPS tracking active in the background for FoodStreet narration"
                };

                var notificationManager = (NotificationManager)GetSystemService(NotificationService)!;
                notificationManager.CreateNotificationChannel(channel);
            }
        }

        private Notification CreateNotification()
        {
#pragma warning disable CS8602
            var intent = new Intent(this, typeof(MainActivity));
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Immutable);
            var appContext = Application.Context;
            var appInfo = appContext.ApplicationInfo;
            var icon = appInfo.Icon;
            var notificationBuilder = new NotificationCompat.Builder(appContext, CHANNEL_ID)
                .SetContentTitle("FoodStreet")
                .SetContentText("Đang theo dõi vị trí để phát thuyết minh...")
                .SetSmallIcon(icon)
                .SetOngoing(true)
                .SetContentIntent(pendingIntent);
            return notificationBuilder.Build()!;
#pragma warning restore CS8602
        }

        private void StartTracking()
        {
            var services = IPlatformApplication.Current?.Services;
            if (services == null) return;

            var gpsService = services.GetService<FoodStreet.Client.Services.IGpsTrackingService>() as NativeGpsTrackingService;
            if (gpsService == null) return;

            _gpsService = gpsService;
            _cts = new CancellationTokenSource();

            // FIX Bug 4: call RunTrackingLoopAsync directly instead of StartTrackingAsync
            // to avoid the recursive StartForegroundService → StartTracking → StartTrackingAsync loop
            _ = Task.Run(async () =>
            {
                try
                {
                    await _gpsService.RunTrackingLoopAsync(_cts.Token);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ForegroundService] GPS loop error: {ex.Message}");
                }
            }, _cts.Token);
        }

        private void StopTracking()
        {
            _cts?.Cancel();
            _gpsService?.StopTrackingAsync();
            _cts?.Dispose();
        }
    }
}
