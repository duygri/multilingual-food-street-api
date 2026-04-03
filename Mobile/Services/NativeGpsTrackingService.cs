using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FoodStreet.Client.Services;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;

namespace FoodStreet.Mobile.Services
{
    public class NativeGpsTrackingService : IGpsTrackingService
    {
        private readonly HttpClient _httpClient;
        private readonly ITtsService _ttsService;
        private CancellationTokenSource? _cts;
        
        // State
        public bool IsTracking { get; private set; }
        public double? CurrentLatitude { get; private set; }
        public double? CurrentLongitude { get; private set; }
        public double? Accuracy { get; private set; }
        public string? LastError { get; private set; }
        public string SessionId { get; private set; }

        // Anti-spam
        private readonly Dictionary<int, DateTime> _triggeredPois = new();
        private readonly TimeSpan _cooldownDuration = TimeSpan.FromMinutes(5);
        private readonly HashSet<int> _currentGeofencePois = new();

        public event Action<double, double>? OnPositionUpdated;
        public event Action<List<NearbyPoiDto>>? OnGeofenceEntered;
        public event Action<List<int>>? OnGeofenceExited;
        public event Action<string>? OnError;

        public NativeGpsTrackingService(HttpClient httpClient, ITtsService ttsService)
        {
            _httpClient = httpClient;
            _ttsService = ttsService;
            SessionId = Guid.NewGuid().ToString();
        }

        public async Task StartTrackingAsync()
        {
            if (IsTracking) return;

            var permission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (permission != PermissionStatus.Granted)
            {
                permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (permission != PermissionStatus.Granted)
                {
                    LastError = "Location permission denied";
                    OnError?.Invoke(LastError);
                    return;
                }
            }

            // Step 2: Location Always (Background)
            var alwaysPermission = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
            if (alwaysPermission != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.LocationAlways>();
            }

            // Step 3: Notifications (Required for Foreground Service feedback on API 33+)
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                var notificationPermission = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (notificationPermission != PermissionStatus.Granted)
                {
                    await Permissions.RequestAsync<Permissions.PostNotifications>();
                }
            }

            IsTracking = true;
            LastError = null;
            _cts = new CancellationTokenSource();

#if ANDROID
            // On Android: delegate to ForegroundService which calls RunTrackingLoopAsync directly
            var context = Platform.AppContext;
            var intent = new global::Android.Content.Intent(context, typeof(FoodStreet.Mobile.Platforms.Android.GpsForegroundService));
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
#else
            _ = RunTrackingLoopAsync(_cts.Token);
#endif
        }

        public Task StopTrackingAsync()
        {
            if (!IsTracking) return Task.CompletedTask;
            IsTracking = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

#if ANDROID
            var context = Platform.AppContext;
            var intent = new global::Android.Content.Intent(context, typeof(FoodStreet.Mobile.Platforms.Android.GpsForegroundService));
            context.StopService(intent);
#endif

            return Task.CompletedTask;
        }

        public async Task GetCurrentPositionAsync()
        {
            await GetLocationAndUpdateAsync(CancellationToken.None);
        }

        /// <summary>
        /// Called by GpsForegroundService on Android to run the loop directly,
        /// avoiding recursive StartTrackingAsync call.
        /// </summary>
        public async Task RunTrackingLoopAsync(CancellationToken token)
        {
            await TrackingLoopAsync(token);
        }

        private async Task TrackingLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await GetLocationAndUpdateAsync(token);
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                LastError = ex.Message;
                IsTracking = false;
                OnError?.Invoke(LastError);
            }
        }

        private async Task GetLocationAndUpdateAsync(CancellationToken token)
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(5));
                var location = await Geolocation.Default.GetLocationAsync(request, token);

                if (location != null)
                {
                    CurrentLatitude = location.Latitude;
                    CurrentLongitude = location.Longitude;
                    Accuracy = location.Accuracy;

                    // Execute on Main Thread for UI
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnPositionUpdated?.Invoke(location.Latitude, location.Longitude);
                    });

                    await SendLocationToServerAsync(location.Latitude, location.Longitude, location.Accuracy ?? 0, location.Speed ?? 0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NativeGPS] Error: {ex.Message}");
            }
        }

        private DateTime _lastUpdateSent = DateTime.MinValue;

        private async Task SendLocationToServerAsync(double latitude, double longitude, double accuracy, double speed)
        {
            if ((DateTime.UtcNow - _lastUpdateSent).TotalSeconds < 3)
            {
                return;
            }
            _lastUpdateSent = DateTime.UtcNow;

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/maps/gps/update", new
                {
                    sessionId = SessionId,
                    latitude,
                    longitude,
                    accuracy,
                    speed
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GpsUpdateResponse>();
                    if (result?.EnteredPois != null)
                    {
                        var newlyEntered = new List<NearbyPoiDto>();
                        var now = DateTime.UtcNow;

                        foreach (var poi in result.EnteredPois)
                        {
                            if (_triggeredPois.TryGetValue(poi.Id, out var lastTriggered))
                            {
                                if (now - lastTriggered < _cooldownDuration)
                                    continue;
                            }

                            newlyEntered.Add(poi);
                            _triggeredPois[poi.Id] = now;
                        }

                        if (newlyEntered.Count > 0)
                        {
                            // Trigger TTS narration natively
                            foreach (var poi in newlyEntered)
                            {
                                if (!string.IsNullOrWhiteSpace(poi.Description))
                                {
                                    _ = _ttsService.PlayTextAsync(BuildArrivalText(poi), poi.LanguageCode);
                                }
                                else
                                {
                                    _ = _ttsService.PlayTextAsync(BuildArrivalText(poi), poi.LanguageCode);
                                }
                            }

                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                OnGeofenceEntered?.Invoke(newlyEntered);
                            });
                        }

                        // Detect EXIT: POI was in previous set but not in current
                        var currentIds = result.EnteredPois.Select(p => p.Id).ToHashSet();
                        var exitedIds = _currentGeofencePois
                            .Where(id => !currentIds.Contains(id))
                            .ToList();

                        if (exitedIds.Count > 0)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                OnGeofenceExited?.Invoke(exitedIds);
                            });
                        }

                        _currentGeofencePois.Clear();
                        foreach (var poi in result.EnteredPois)
                        {
                            _currentGeofencePois.Add(poi.Id);
                        }

                        var expiredKeys = _triggeredPois
                            .Where(kv => now - kv.Value > TimeSpan.FromMinutes(30))
                            .Select(kv => kv.Key)
                            .ToList();
                        foreach (var key in expiredKeys)
                        {
                            _triggeredPois.Remove(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NativeGPS] API error: {ex.Message}");
            }
        }

        // FIX Bug 3: return new ValueTask(StopTrackingAsync()) so the Task is properly awaited
        public ValueTask DisposeAsync()
        {
            return new ValueTask(StopTrackingAsync());
        }
        // ── Client-side Haversine distance (meters) ──────────────
        // Equivalent to turf.distance() from the reference diagram.
        // Enables offline geofence checks without server round-trip.
        public static double HaversineMeters(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6_371_000; // Earth radius in meters
            var dLat = ToRad(lat2 - lat1);
            var dLng = ToRad(lng2 - lng1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                  * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static string BuildArrivalText(NearbyPoiDto poi)
        {
            var intro = poi.LanguageCode?.ToLowerInvariant() switch
            {
                var code when code != null && code.StartsWith("en") => $"You have arrived at {poi.Name}",
                var code when code != null && code.StartsWith("ja") => $"{poi.Name} に到着しました",
                var code when code != null && code.StartsWith("ko") => $"{poi.Name}에 도착했습니다",
                var code when code != null && code.StartsWith("zh") => $"您已到达 {poi.Name}",
                _ => $"Bạn vừa đến {poi.Name}"
            };

            return string.IsNullOrWhiteSpace(poi.Description)
                ? intro
                : $"{intro}. {poi.Description}";
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;
    }
}
