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
        [HttpGet("{foodId}")]
        public async Task<IActionResult> GetQrCode(int foodId, [FromQuery] int size = 300)
        {
            var food = await _context.Foods.FindAsync(foodId);
            if (food == null) return NotFound("POI not found");

            // Generate deep link URL - this URL will play audio when scanned
            var baseUrl = _configuration["App:BaseUrl"] ?? "https://vinhkhanh.app";
            var qrUrl = $"{baseUrl}/poi/{foodId}";

            // Generate QR code
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(size / 33); // pixels per module

            return File(qrCodeBytes, "image/png", $"qr-poi-{foodId}.png");
        }

        /// <summary>
        /// Generate QR code with custom label
        /// </summary>
        [HttpGet("{foodId}/labeled")]
        public async Task<IActionResult> GetLabeledQrCode(int foodId, [FromQuery] int size = 400)
        {
            var food = await _context.Foods.FindAsync(foodId);
            if (food == null) return NotFound("POI not found");

            var baseUrl = _configuration["App:BaseUrl"] ?? "https://vinhkhanh.app";
            var qrUrl = $"{baseUrl}/poi/{foodId}";

            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(size / 33);

            return File(qrCodeBytes, "image/png", $"qr-{food.Name.Replace(" ", "-")}.png");
        }

        /// <summary>
        /// Get all POIs with their QR code URLs
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllQrCodes()
        {
            var foods = await _context.Foods
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.Description,
                    f.Latitude,
                    f.Longitude,
                    QrCodeUrl = $"/api/qrcode/{f.Id}",
                    LabeledQrUrl = $"/api/qrcode/{f.Id}/labeled"
                })
                .ToListAsync();

            return Ok(foods);
        }

        /// <summary>
        /// Generate QR code for batch printing (multiple POIs)
        /// </summary>
        [HttpPost("batch")]
        [Authorize]
        public async Task<IActionResult> GenerateBatchQrCodes([FromBody] int[] foodIds)
        {
            if (foodIds == null || foodIds.Length == 0)
                return BadRequest("No POI IDs provided");

            var foods = await _context.Foods
                .Where(f => foodIds.Contains(f.Id))
                .ToListAsync();

            var result = foods.Select(f => new
            {
                f.Id,
                f.Name,
                QrCodeUrl = $"/api/qrcode/{f.Id}",
                DirectUrl = $"/poi/{f.Id}"
            }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// Generate printable sheet with multiple QR codes
        /// </summary>
        [HttpGet("print-sheet")]
        public async Task<IActionResult> GetPrintSheet([FromQuery] string? ids = null)
        {
            var foodQuery = _context.Foods.AsQueryable();
            
            if (!string.IsNullOrEmpty(ids))
            {
                var idList = ids.Split(',').Select(int.Parse).ToList();
                foodQuery = foodQuery.Where(f => idList.Contains(f.Id));
            }

            var foods = await foodQuery.Take(20).ToListAsync(); // Max 20 for performance

            var qrCodes = foods.Select(f => new
            {
                f.Id,
                f.Name,
                f.Description,
                Location = $"{f.Latitude:F6}, {f.Longitude:F6}",
                QrCodeUrl = $"/api/qrcode/{f.Id}?size=200"
            }).ToList();

            return Ok(qrCodes);
        }
    }
}
