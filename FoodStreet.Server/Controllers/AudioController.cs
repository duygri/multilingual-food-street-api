using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;
using PROJECT_C_.DTOs;
using PROJECT_C_.Models;

namespace PROJECT_C_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AudioController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AudioController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        /// <summary>
        /// Get all audio files with optional filtering by location (POI)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<AudioFileDto>>> GetAllAudio([FromQuery] int? locationId = null)
        {
            var query = _context.AudioFiles.Include(a => a.Location).AsQueryable();
            
            if (locationId.HasValue)
                query = query.Where(a => a.LocationId == locationId);

            var audioFiles = await query.OrderByDescending(a => a.UploadedAt).ToListAsync();

            return audioFiles.Select(a => new AudioFileDto
            {
                Id = a.Id,
                FileName = a.FileName,
                OriginalName = a.OriginalName,
                ContentType = a.ContentType,
                Size = a.Size,
                DurationSeconds = a.DurationSeconds,
                LocationId = a.LocationId,
                LocationName = a.Location?.Name,
                UploadedAt = a.UploadedAt,
                Url = $"/api/audio/{a.Id}/stream"
            }).ToList();
        }

        /// <summary>
        /// Get audio file metadata by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<AudioFileDto>> GetAudio(int id)
        {
            var audio = await _context.AudioFiles.Include(a => a.Location).FirstOrDefaultAsync(a => a.Id == id);
            if (audio == null) return NotFound();

            return new AudioFileDto
            {
                Id = audio.Id,
                FileName = audio.FileName,
                OriginalName = audio.OriginalName,
                ContentType = audio.ContentType,
                Size = audio.Size,
                DurationSeconds = audio.DurationSeconds,
                LocationId = audio.LocationId,
                LocationName = audio.Location?.Name,
                UploadedAt = audio.UploadedAt,
                Url = $"/api/audio/{audio.Id}/stream"
            };
        }

        /// <summary>
        /// Stream audio file for playback
        /// </summary>
        [HttpGet("{id}/stream")]
        public async Task<IActionResult> StreamAudio(int id)
        {
            var audio = await _context.AudioFiles.FindAsync(id);
            if (audio == null) return NotFound();

            var filePath = Path.Combine(_env.ContentRootPath, "Uploads", "Audio", audio.FileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound("Audio file not found on disk");

            var stream = System.IO.File.OpenRead(filePath);
            return File(stream, audio.ContentType, enableRangeProcessing: true);
        }

        /// <summary>
        /// Upload new audio file, optionally assign to a location (POI)
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<AudioFileDto>> UploadAudio(
            IFormFile file, 
            [FromQuery] int? locationId = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            // Validate file type
            var allowedTypes = new[] { "audio/mpeg", "audio/wav", "audio/mp3", "audio/ogg" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest("Invalid file type. Allowed: MP3, WAV, OGG");

            // Validate location exists if provided
            if (locationId.HasValue)
            {
                var locationExists = await _context.Locations.AnyAsync(l => l.Id == locationId);
                if (!locationExists) return BadRequest("Location not found");
            }

            // Create upload directory if not exists
            var uploadDir = Path.Combine(_env.ContentRootPath, "Uploads", "Audio");
            Directory.CreateDirectory(uploadDir);

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Create database record
            var audioFile = new AudioFile
            {
                FileName = fileName,
                OriginalName = file.FileName,
                ContentType = file.ContentType,
                Size = file.Length,
                DurationSeconds = 0,
                LocationId = locationId,
                UploadedAt = DateTime.UtcNow
            };

            _context.AudioFiles.Add(audioFile);
            await _context.SaveChangesAsync();

            // Load location name for response
            string? locationName = null;
            if (locationId.HasValue)
            {
                locationName = await _context.Locations
                    .Where(l => l.Id == locationId)
                    .Select(l => l.Name)
                    .FirstOrDefaultAsync();
            }

            return CreatedAtAction(nameof(GetAudio), new { id = audioFile.Id }, new AudioFileDto
            {
                Id = audioFile.Id,
                FileName = audioFile.FileName,
                OriginalName = audioFile.OriginalName,
                ContentType = audioFile.ContentType,
                Size = audioFile.Size,
                DurationSeconds = audioFile.DurationSeconds,
                LocationId = audioFile.LocationId,
                LocationName = locationName,
                UploadedAt = audioFile.UploadedAt,
                Url = $"/api/audio/{audioFile.Id}/stream"
            });
        }

        /// <summary>
        /// Assign audio to a Location (POI)
        /// </summary>
        [HttpPut("{id}/assign")]
        [Authorize]
        public async Task<IActionResult> AssignToLocation(int id, [FromQuery] int? locationId)
        {
            var audio = await _context.AudioFiles.FindAsync(id);
            if (audio == null) return NotFound();

            if (locationId.HasValue)
            {
                var locationExists = await _context.Locations.AnyAsync(l => l.Id == locationId);
                if (!locationExists) return BadRequest("Location not found");
            }

            audio.LocationId = locationId;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Delete audio file
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteAudio(int id)
        {
            var audio = await _context.AudioFiles.FindAsync(id);
            if (audio == null) return NotFound();

            // Delete file from disk
            var filePath = Path.Combine(_env.ContentRootPath, "Uploads", "Audio", audio.FileName);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            // Delete database record
            _context.AudioFiles.Remove(audio);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Get audio stats
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetAudioStats()
        {
            var total = await _context.AudioFiles.CountAsync();
            var assigned = await _context.AudioFiles.CountAsync(a => a.LocationId != null);
            var unassigned = total - assigned;
            var totalSize = await _context.AudioFiles.SumAsync(a => a.Size);

            return new
            {
                TotalFiles = total,
                Assigned = assigned,
                Unassigned = unassigned,
                TotalSizeMB = Math.Round(totalSize / 1024.0 / 1024.0, 2)
            };
        }
    }
}
