using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;
using PROJECT_C_.DTOs;
using PROJECT_C_.Models;

namespace PROJECT_C_.Controllers
{
    [ApiController]
    [Route("api/gps")]
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
            var nearbyPois = await GetNearbyPoisInternal(request.Latitude, request.Longitude, 200);
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
            var pois = await GetNearbyPoisInternal(lat, lng, radius);
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
            var pois = await GetNearbyPoisInternal(lat, lng, 500);
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
        private async Task<List<NearbyPoiResponse>> GetNearbyPoisInternal(double lat, double lng, double radius)
        {
            var foods = await _context.Foods.ToListAsync();

            var nearbyPois = foods
                .Select(f => new NearbyPoiResponse
                {
                    Id = f.Id,
                    Name = f.Name,
                    Description = f.Description,
                    Latitude = f.Latitude,
                    Longitude = f.Longitude,
                    Radius = f.Radius,
                    ImageUrl = f.ImageUrl,
                    Distance = CalculateHaversineDistance(lat, lng, f.Latitude, f.Longitude),
                    IsInGeofence = CalculateHaversineDistance(lat, lng, f.Latitude, f.Longitude) <= f.Radius
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
