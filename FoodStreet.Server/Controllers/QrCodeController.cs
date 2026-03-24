using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace PROJECT_C_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QrCodeController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public QrCodeController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Generate QR code for a specific POI
        /// </summary>
        [HttpGet("{locationId}")]
        public async Task<IActionResult> GetQrCode(int locationId, [FromQuery] int size = 300)
        {
            var location = await _context.Locations.FindAsync(locationId);
            if (location == null) return NotFound("POI not found");

            // Generate deep link URL - this URL will play audio when scanned
            var baseUrl = _configuration["App:BaseUrl"] ?? "https://vinhkhanh.app";
            var qrUrl = $"{baseUrl}/poi/{locationId}";

            // Generate QR code
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(size / 33); // pixels per module

            return File(qrCodeBytes, "image/png", $"qr-poi-{locationId}.png");
        }

        /// <summary>
        /// Generate QR code with custom label
        /// </summary>
        [HttpGet("{locationId}/labeled")]
        public async Task<IActionResult> GetLabeledQrCode(int locationId, [FromQuery] int size = 400)
        {
            var location = await _context.Locations.FindAsync(locationId);
            if (location == null) return NotFound("POI not found");

            var baseUrl = _configuration["App:BaseUrl"] ?? "https://vinhkhanh.app";
            var qrUrl = $"{baseUrl}/poi/{locationId}";

            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(size / 33);

            return File(qrCodeBytes, "image/png", $"qr-{location.Name.Replace(" ", "-")}.png");
        }

        /// <summary>
        /// Get all POIs with their QR code URLs
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllQrCodes()
        {
            var locations = await _context.Locations
                .Select(l => new
                {
                    l.Id,
                    l.Name,
                    l.Description,
                    l.Latitude,
                    l.Longitude,
                    QrCodeUrl = $"/api/qrcode/{l.Id}",
                    LabeledQrUrl = $"/api/qrcode/{l.Id}/labeled"
                })
                .ToListAsync();

            return Ok(locations);
        }

        /// <summary>
        /// Generate QR code for batch printing (multiple POIs)
        /// </summary>
        [HttpPost("batch")]
        [Authorize]
        public async Task<IActionResult> GenerateBatchQrCodes([FromBody] int[] locationIds)
        {
            if (locationIds == null || locationIds.Length == 0)
                return BadRequest("No Location IDs provided");

            var locations = await _context.Locations
                .Where(l => locationIds.Contains(l.Id))
                .ToListAsync();

            var result = locations.Select(l => new
            {
                l.Id,
                l.Name,
                QrCodeUrl = $"/api/qrcode/{l.Id}",
                DirectUrl = $"/poi/{l.Id}"
            }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// Generate printable sheet with multiple QR codes
        /// </summary>
        [HttpGet("print-sheet")]
        public async Task<IActionResult> GetPrintSheet([FromQuery] string? ids = null)
        {
            var locationQuery = _context.Locations.AsQueryable();
            
            if (!string.IsNullOrEmpty(ids))
            {
                var idList = ids.Split(',').Select(int.Parse).ToList();
                locationQuery = locationQuery.Where(l => idList.Contains(l.Id));
            }

            var locations = await locationQuery.Take(20).ToListAsync();

            var qrCodes = locations.Select(l => new
            {
                l.Id,
                l.Name,
                l.Description,
                Location = $"{l.Latitude:F6}, {l.Longitude:F6}",
                QrCodeUrl = $"/api/qrcode/{l.Id}?size=200"
            }).ToList();

            return Ok(qrCodes);
        }
    }
}
