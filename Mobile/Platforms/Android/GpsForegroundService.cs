using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using FoodStreet.Mobile.Services;
using Microsoft.Maui.Devices.Sensors;
using Application = Android.App.Application;

namespace FoodStreet.Mobile.Platforms.Android
{
    [Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeLocation)]
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
            
            // Start the foreground service with the notification
            StartForeground(NOTIFICATION_ID, notification);

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
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
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
            var intent = new Intent(this, typeof(MainActivity));
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Immutable);

            var notificationBuilder = new NotificationCompat.Builder(this, CHANNEL_ID)
                .SetContentTitle("FoodStreet")
                .SetContentText("Đang theo dõi vị trí để phát thuyết minh...")
                .SetSmallIcon(Application.Context.ApplicationInfo!.Icon)
                .SetOngoing(true) // Sticky
                .SetContentIntent(pendingIntent);

            return notificationBuilder.Build();
        }

        private void StartTracking()
        {
            // Resolve the GPS service from DI container
            var services = IPlatformApplication.Current?.Services;
            if (services == null) return;
            
            var gpsService = services.GetService<FoodStreet.Client.Services.IGpsTrackingService>() as NativeGpsTrackingService;
            if (gpsService == null) return;

            _gpsService = gpsService;
            _cts = new CancellationTokenSource();

            // Run the loop asynchronously
            _ = Task.Run(async () =>
            {
                try 
                {
                    await _gpsService.StartTrackingAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error starting foreground GPS tracking: {ex.Message}");
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
