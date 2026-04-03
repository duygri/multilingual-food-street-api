using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;
using FoodStreet.Server.Constants;
using FoodStreet.Server.Links;
using QRCoder;

namespace PROJECT_C_.Controllers
{
    [ApiController]
    [Route("api/content/qrcode")]
    [Route("api/qrcode")]
    [Route("api/qr")]
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

            var qrUrl = BuildPoiDeepLink(locationId);

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

            var qrUrl = BuildPoiDeepLink(locationId);

            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(size / 33);

            return File(qrCodeBytes, "image/png", $"qr-{location.Name.Replace(" ", "-")}.png");
        }

        /// <summary>
        /// Get QR metadata for a specific POI
        /// </summary>
        [HttpGet("{locationId}/meta")]
        public async Task<IActionResult> GetQrMetadata(int locationId)
        {
            var location = await _context.Locations.FindAsync(locationId);
            if (location == null) return NotFound("POI not found");

            return Ok(new
            {
                location.Id,
                location.Name,
                location.Description,
                DeepLink = BuildPoiDeepLink(locationId),
                QrCodeUrl = $"/api/qrcode/{locationId}",
                LabeledQrUrl = $"/api/qrcode/{locationId}/labeled",
                PreviewPage = $"/poi/{locationId}",
                RecommendedSource = PlaySources.QrScan
            });
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
                    l.Longitude
                })
                .ToListAsync();

            var result = locations.Select(l => new
            {
                l.Id,
                l.Name,
                l.Description,
                l.Latitude,
                l.Longitude,
                DeepLink = BuildPoiDeepLink(l.Id),
                QrCodeUrl = $"/api/qrcode/{l.Id}",
                LabeledQrUrl = $"/api/qrcode/{l.Id}/labeled"
            });

            return Ok(result);
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
                DeepLink = BuildPoiDeepLink(l.Id),
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
                DeepLink = BuildPoiDeepLink(l.Id),
                QrCodeUrl = $"/api/qrcode/{l.Id}?size=200"
            }).ToList();

            return Ok(qrCodes);
        }

        private string ResolveBaseUrl()
        {
            var configuredBaseUrl = _configuration["App:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
            {
                return configuredBaseUrl;
            }

            return $"{Request.Scheme}://{Request.Host}";
        }

        private string BuildPoiDeepLink(int locationId)
        {
            return PoiDeepLinkBuilder.Build(ResolveBaseUrl(), locationId, PlaySources.QrScan);
        }
    }
}
