using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodStreet.Server.Mapping;
using PROJECT_C_.Data;
using PROJECT_C_.DTOs;
using PROJECT_C_.Models;

namespace PROJECT_C_.Controllers
{
    [ApiController]
    [Route("api/maps/gps")]
    public class GpsTrackingController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GpsTrackingController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Cập nhật vị trí GPS của user (ẩn danh qua SessionId)
        /// </summary>
        [HttpPost("update")]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
        {
            var location = new UserLocation
            {
                SessionId = request.SessionId,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Accuracy = request.Accuracy,
                Speed = request.Speed,
                RecordedAt = DateTime.UtcNow
            };

            _context.UserLocations.Add(location);
            await _context.SaveChangesAsync();

            // Check geofences và trả về POIs đang trong phạm vi
            var nearbyPois = await GetNearbyPoisInternal(request.Latitude, request.Longitude, 200, ResolveLanguage());
            var enteredPois = nearbyPois.Where(p => p.IsInGeofence).ToList();

            return Ok(new
            {
                recorded = true,
                enteredPois = enteredPois,
                nearbyCount = nearbyPois.Count
            });
        }

        /// <summary>
        /// Lấy các POI gần vị trí hiện tại
        /// </summary>
        [HttpGet("nearby")]
        public async Task<IActionResult> GetNearbyPois(
            [FromQuery] double lat,
            [FromQuery] double lng,
            [FromQuery] double radius = 500) // meters
        {
            var pois = await GetNearbyPoisInternal(lat, lng, radius, ResolveLanguage());
            return Ok(pois);
        }

        /// <summary>
        /// Kiểm tra user đang ở trong geofence nào
        /// </summary>
        [HttpGet("geofence-check")]
        public async Task<IActionResult> CheckGeofence(
            [FromQuery] double lat,
            [FromQuery] double lng)
        {
            var pois = await GetNearbyPoisInternal(lat, lng, 500, ResolveLanguage());
            var enteredPois = pois.Where(p => p.IsInGeofence).ToList();

            return Ok(new GeofenceCheckResponse
            {
                EnteredPois = enteredPois,
                CurrentLatitude = lat,
                CurrentLongitude = lng
            });
        }

        /// <summary>
        /// Internal: Lấy POIs gần và tính khoảng cách
        /// </summary>
        private string ResolveLanguage()
        {
            return PoiContentResolver.NormalizeLanguageCode(Request.Headers["Accept-Language"].ToString());
        }

        private async Task<List<NearbyPoiResponse>> GetNearbyPoisInternal(double lat, double lng, double radius, string languageCode)
        {
            var locations = await _context.Locations
                .Include(l => l.Translations)
                .Include(l => l.AudioFiles)
                .Where(l => l.IsApproved)
                .ToListAsync();

            var nearbyPois = locations
                .Select(l =>
                {
                    var resolved = PoiContentResolver.Resolve(l, languageCode);
                    var distance = CalculateHaversineDistance(lat, lng, l.Latitude, l.Longitude);

                    return new NearbyPoiResponse
                    {
                        Id = l.Id,
                        Name = resolved.Name,
                        Description = resolved.Description,
                        Latitude = l.Latitude,
                        Longitude = l.Longitude,
                        Radius = l.Radius,
                        ImageUrl = l.ImageUrl,
                        TtsScript = resolved.TtsScript,
                        HasAudio = resolved.HasAudio,
                        AudioUrl = resolved.AudioUrl,
                        AudioStatus = resolved.AudioStatus,
                        LanguageCode = resolved.LanguageCode,
                        Tier = resolved.Tier,
                        FallbackUsed = resolved.FallbackUsed,
                        IsFallback = resolved.IsFallback,
                        Priority = l.Priority,
                        Distance = distance,
                        IsInGeofence = distance <= l.Radius
                    };
                })
                .Where(p => p.Distance <= radius)
                .OrderBy(p => p.Distance)
                .ToList();

            return nearbyPois;
        }

        /// <summary>
        /// Haversine formula - tính khoảng cách giữa 2 điểm GPS (meters)
        /// </summary>
        private static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000; // Earth radius in meters

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;
    }
}
