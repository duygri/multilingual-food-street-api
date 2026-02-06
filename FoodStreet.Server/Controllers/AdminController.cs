using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;
using PROJECT_C_.Models;

namespace PROJECT_C_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require Admin/User login
    [ApiExplorerSettings(IgnoreApi = true)] // TEMPORARILY HIDDEN FROM SWAGGER FOR DEBUG
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpPost("audio/upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAudio([FromForm] IFormFile file, [FromForm] int? foodId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            // Validate file type
            if (!file.ContentType.StartsWith("audio/"))
                return BadRequest("Only audio files are allowed.");

            // Prepare directory
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "audio");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Save metadata
            var audioFile = new AudioFile
            {
                FileName = uniqueFileName,
                OriginalName = file.FileName,
                ContentType = file.ContentType,
                Size = file.Length,
                DurationSeconds = 0, // Placeholder, usually requires FFmpeg or lib to parse
                FoodId = foodId
            };

            _context.AudioFiles.Add(audioFile);
            await _context.SaveChangesAsync();

            return Ok(new { audioFile.Id, audioFile.FileName });
        }

        [HttpGet("stats")]
        [AllowAnonymous] // Allow public access for now - dashboard needs this
        public async Task<IActionResult> GetStats()
        {
            var stats = new
            {
                TotalFoods = await _context.Foods.CountAsync(),
                TotalAudios = await _context.AudioFiles.CountAsync(),
                TotalUsers = await _context.Users.CountAsync()
            };
            return Ok(stats);
        }
    }
}
