using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodStreet.Server.Constants;
using FoodStreet.Server.Extensions;
using FoodStreet.Server.Hubs;
using FoodStreet.Server.Mapping;
using Microsoft.AspNetCore.SignalR;
using PROJECT_C_.Data;
using PROJECT_C_.DTOs;
using PROJECT_C_.Models;
using FoodStreet.Server.Services.Audio;

namespace PROJECT_C_.Controllers
{
    /// <summary>
    /// Localization — manages supported languages and UI string translations.
    /// Route prefix: api/localization
    /// </summary>
    [ApiController]
    [Route("api/localization")]
    public class LocalizationController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly FoodStreet.Server.Services.Audio.AudioTaskManager _audioTaskManager;
        private readonly PROJECT_C_.Services.Interfaces.IDistanceCalculator _distanceCalculator;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly GoogleTranslator _translator;

        public LocalizationController(
            AppDbContext db, 
            FoodStreet.Server.Services.Audio.AudioTaskManager audioTaskManager,
            PROJECT_C_.Services.Interfaces.IDistanceCalculator distanceCalculator,
            IHubContext<NotificationHub> hubContext,
            GoogleTranslator translator)
        {
            _db = db;
            _audioTaskManager = audioTaskManager;
            _distanceCalculator = distanceCalculator;
            _hubContext = hubContext;
            _translator = translator;
        }

        // Supported languages (to be driven by DB / config in production)
        private static readonly IReadOnlyList<SupportedLanguage> _supportedLanguages = new[]
        {
            new SupportedLanguage("vi-VN", "Tiếng Việt", "🇻🇳"),
            new SupportedLanguage("en-US", "English",    "🇺🇸"),
            new SupportedLanguage("zh-CN", "中文",        "🇨🇳"),
            new SupportedLanguage("ko-KR", "한국어",       "🇰🇷"),
            new SupportedLanguage("ja-JP", "日本語",       "🇯🇵"),
        };

        /// <summary>List all supported locale codes.</summary>
        [HttpGet("languages")]
        public IActionResult GetLanguages()
            => Ok(_supportedLanguages);

        /// <summary>
        /// 3-Tier Content Fallback for a specific POI.
        /// Tier 1: Requested language → Tier 2: English (is_fallback=true) → Tier 3: Vietnamese original
        /// Runtime Audio Decoration: audio_url gets ?v={mtime}&amp;l={lang} appended.
        /// </summary>
        [HttpGet("location/{locationId:int}")]
        public async Task<IActionResult> GetLocationContent(int locationId, [FromQuery] string lang = "vi-VN")
        {
            var poi = await _db.Locations
                .Include(l => l.Translations)
                .Include(l => l.AudioFiles)
                .FirstOrDefaultAsync(l => l.Id == locationId);

            if (poi == null) return NotFound(new { error = "POI not found" });

            var resolved = PoiContentResolver.Resolve(poi, lang);

            return Ok(new
            {
                locationId = poi.Id,
                name = resolved.Name,
                description = resolved.Description,
                ttsScript = resolved.TtsScript,
                audioUrl = resolved.AudioUrl,
                audioStatus = resolved.AudioStatus,
                languageCode = resolved.LanguageCode,
                tier = resolved.Tier,
                fallbackUsed = resolved.FallbackUsed,
                isFallback = resolved.IsFallback
            });
        }

        /// <summary>List all translations for a specific POI.</summary>
        [HttpGet("locations/{locationId:int}/translations")]
        [Authorize(Roles = AppRoles.AdminOrPoiOwner)]
        public async Task<IActionResult> GetLocationTranslations(int locationId, [FromQuery] string? lang = null)
        {
            var poi = await FindAccessibleLocationAsync(locationId);
            if (poi == null)
            {
                return NotFound(new { message = "Không tìm thấy POI hoặc bạn không có quyền truy cập." });
            }

            var translations = poi.Translations.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(lang))
            {
                var normalizedLang = PoiContentResolver.NormalizeLanguageCode(lang);
                translations = translations.Where(translation => translation.LanguageCode == normalizedLang);
            }

            var items = translations
                .OrderBy(translation => translation.LanguageCode)
                .Select(translation => new TranslationItemDto
                {
                    Id = translation.Id,
                    LocationId = poi.Id,
                    LocationName = poi.Name,
                    LanguageCode = translation.LanguageCode,
                    Name = translation.Name,
                    Description = translation.Description,
                    TtsScript = translation.TtsScript,
                    AudioUrl = translation.AudioUrl,
                    IsFallback = translation.IsFallback,
                    GeneratedAt = translation.GeneratedAt
                })
                .ToList();

            return Ok(items);
        }

        /// <summary>Create a translation for a POI (admin only).</summary>
        [HttpPost("translations")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> CreateTranslation([FromBody] UpsertTranslationRequest request)
        {
            var location = await _db.Locations
                .Include(poi => poi.Translations)
                .FirstOrDefaultAsync(poi => poi.Id == request.LocationId);

            if (location == null)
            {
                return NotFound(new { message = "POI không tồn tại." });
            }

            var normalizedLang = PoiContentResolver.NormalizeLanguageCode(request.LanguageCode);
            if (location.Translations.Any(translation => translation.LanguageCode == normalizedLang))
            {
                return Conflict(new { message = "POI này đã có bản dịch cho ngôn ngữ đã chọn." });
            }

            var translation = new LocationTranslation
            {
                LocationId = location.Id,
                LanguageCode = normalizedLang,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                TtsScript = string.IsNullOrWhiteSpace(request.TtsScript) ? request.Description?.Trim() : request.TtsScript.Trim(),
                GeneratedAt = DateTime.UtcNow,
                IsFallback = false
            };

            _db.LocationTranslations.Add(translation);
            await _db.SaveChangesAsync();

            await PublishTranslationUpdatedAsync(
                location,
                "updated",
                "Bản dịch mới",
                $"POI \"{location.Name}\" vừa được thêm bản dịch {normalizedLang}.",
                GetActorDisplayName("Admin"));

            return Ok(new TranslationItemDto
            {
                Id = translation.Id,
                LocationId = location.Id,
                LocationName = location.Name,
                LanguageCode = translation.LanguageCode,
                Name = translation.Name,
                Description = translation.Description,
                TtsScript = translation.TtsScript,
                AudioUrl = translation.AudioUrl,
                IsFallback = translation.IsFallback,
                GeneratedAt = translation.GeneratedAt
            });
        }

        /// <summary>Update a translation for a POI (admin only).</summary>
        [HttpPut("translations/{id:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> UpdateTranslation(int id, [FromBody] UpsertTranslationRequest request)
        {
            var translation = await _db.LocationTranslations
                .Include(item => item.Location)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (translation == null)
            {
                return NotFound(new { message = "Không tìm thấy bản dịch." });
            }

            var normalizedLang = PoiContentResolver.NormalizeLanguageCode(request.LanguageCode);
            var duplicateExists = await _db.LocationTranslations.AnyAsync(item =>
                item.Id != id
                && item.LocationId == translation.LocationId
                && item.LanguageCode == normalizedLang);

            if (duplicateExists)
            {
                return Conflict(new { message = "POI này đã có bản dịch cho ngôn ngữ đã chọn." });
            }

            translation.LanguageCode = normalizedLang;
            translation.Name = request.Name.Trim();
            translation.Description = request.Description?.Trim() ?? string.Empty;
            translation.TtsScript = string.IsNullOrWhiteSpace(request.TtsScript) ? request.Description?.Trim() : request.TtsScript.Trim();
            translation.GeneratedAt = DateTime.UtcNow;
            translation.IsFallback = false;

            await _db.SaveChangesAsync();

            await PublishTranslationUpdatedAsync(
                translation.Location,
                "updated",
                "Bản dịch đã cập nhật",
                $"POI \"{translation.Location.Name}\" vừa được cập nhật bản dịch {normalizedLang}.",
                GetActorDisplayName("Admin"));

            return Ok(new TranslationItemDto
            {
                Id = translation.Id,
                LocationId = translation.LocationId,
                LocationName = translation.Location.Name,
                LanguageCode = translation.LanguageCode,
                Name = translation.Name,
                Description = translation.Description,
                TtsScript = translation.TtsScript,
                AudioUrl = translation.AudioUrl,
                IsFallback = translation.IsFallback,
                GeneratedAt = translation.GeneratedAt
            });
        }

        /// <summary>Delete a translation (admin only).</summary>
        [HttpDelete("translations/{id:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> DeleteTranslation(int id)
        {
            var translation = await _db.LocationTranslations
                .Include(item => item.Location)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (translation == null)
            {
                return NotFound(new { message = "Không tìm thấy bản dịch." });
            }

            var location = translation.Location;
            var languageCode = translation.LanguageCode;

            _db.LocationTranslations.Remove(translation);
            await _db.SaveChangesAsync();

            await PublishTranslationUpdatedAsync(
                location,
                "deleted",
                "Bản dịch đã xóa",
                $"POI \"{location.Name}\" vừa bị xóa bản dịch {languageCode}.",
                GetActorDisplayName("Admin"));

            return NoContent();
        }

        /// <summary>List all translations for a specific menu item.</summary>
        [HttpGet("menu-items/{menuItemId:int}/translations")]
        [Authorize(Roles = AppRoles.AdminOrPoiOwner)]
        public async Task<IActionResult> GetMenuItemTranslations(int menuItemId, [FromQuery] string? lang = null)
        {
            var menuItem = await FindAccessibleMenuItemAsync(menuItemId, includeTranslations: true);
            if (menuItem == null)
            {
                return NotFound(new { message = "Không tìm thấy món trong menu hoặc bạn không có quyền truy cập." });
            }

            var translations = menuItem.Translations.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(lang))
            {
                var normalizedLang = PoiContentResolver.NormalizeLanguageCode(lang);
                translations = translations.Where(translation => translation.LanguageCode == normalizedLang);
            }

            var items = translations
                .OrderBy(translation => translation.LanguageCode)
                .Select(translation => MapMenuTranslation(translation, menuItem))
                .ToList();

            return Ok(items);
        }

        /// <summary>Create or update a translation for a menu item.</summary>
        [HttpPost("menu-translations")]
        [Authorize(Roles = AppRoles.AdminOrPoiOwner)]
        public async Task<IActionResult> CreateMenuTranslation([FromBody] UpsertMenuTranslationRequest request)
        {
            var menuItem = await FindAccessibleMenuItemAsync(request.PoiMenuItemId, includeTranslations: true);
            if (menuItem == null)
            {
                return NotFound(new { message = "Không tìm thấy món trong menu hoặc bạn không có quyền truy cập." });
            }

            var normalizedLang = PoiContentResolver.NormalizeLanguageCode(request.LanguageCode);
            if (menuItem.Translations.Any(translation => translation.LanguageCode == normalizedLang))
            {
                return Conflict(new { message = "Món này đã có bản dịch cho ngôn ngữ đã chọn." });
            }

            var translation = new PoiMenuItemTranslation
            {
                PoiMenuItemId = menuItem.Id,
                LanguageCode = normalizedLang,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                GeneratedAt = DateTime.UtcNow,
                IsFallback = false
            };

            _db.PoiMenuItemTranslations.Add(translation);
            await _db.SaveChangesAsync();

            await PublishMenuTranslationUpdatedAsync(
                menuItem,
                "updated",
                "Bản dịch menu mới",
                $"Món \"{menuItem.Name}\" vừa được thêm bản dịch {normalizedLang}.",
                GetActorDisplayName(User.IsAdminRole() ? "Admin" : "POI Owner"));

            return Ok(MapMenuTranslation(translation, menuItem));
        }

        /// <summary>Update an existing menu translation.</summary>
        [HttpPut("menu-translations/{id:int}")]
        [Authorize(Roles = AppRoles.AdminOrPoiOwner)]
        public async Task<IActionResult> UpdateMenuTranslation(int id, [FromBody] UpsertMenuTranslationRequest request)
        {
            var translation = await _db.PoiMenuItemTranslations
                .Include(item => item.PoiMenuItem)
                .ThenInclude(menuItem => menuItem.Location)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (translation == null)
            {
                return NotFound(new { message = "Không tìm thấy bản dịch menu." });
            }

            if (!await CanAccessMenuItemAsync(translation.PoiMenuItem))
            {
                return Forbid();
            }

            var normalizedLang = PoiContentResolver.NormalizeLanguageCode(request.LanguageCode);
            var duplicateExists = await _db.PoiMenuItemTranslations.AnyAsync(item =>
                item.Id != id
                && item.PoiMenuItemId == translation.PoiMenuItemId
                && item.LanguageCode == normalizedLang);

            if (duplicateExists)
            {
                return Conflict(new { message = "Món này đã có bản dịch cho ngôn ngữ đã chọn." });
            }

            translation.LanguageCode = normalizedLang;
            translation.Name = request.Name.Trim();
            translation.Description = request.Description?.Trim() ?? string.Empty;
            translation.GeneratedAt = DateTime.UtcNow;
            translation.IsFallback = false;

            await _db.SaveChangesAsync();

            await PublishMenuTranslationUpdatedAsync(
                translation.PoiMenuItem,
                "updated",
                "Bản dịch menu đã cập nhật",
                $"Món \"{translation.PoiMenuItem.Name}\" vừa được cập nhật bản dịch {normalizedLang}.",
                GetActorDisplayName(User.IsAdminRole() ? "Admin" : "POI Owner"));

            return Ok(MapMenuTranslation(translation, translation.PoiMenuItem));
        }

        /// <summary>Delete a menu translation.</summary>
        [HttpDelete("menu-translations/{id:int}")]
        [Authorize(Roles = AppRoles.AdminOrPoiOwner)]
        public async Task<IActionResult> DeleteMenuTranslation(int id)
        {
            var translation = await _db.PoiMenuItemTranslations
                .Include(item => item.PoiMenuItem)
                .ThenInclude(menuItem => menuItem.Location)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (translation == null)
            {
                return NotFound(new { message = "Không tìm thấy bản dịch menu." });
            }

            if (!await CanAccessMenuItemAsync(translation.PoiMenuItem))
            {
                return Forbid();
            }

            var menuItem = translation.PoiMenuItem;
            var languageCode = translation.LanguageCode;

            _db.PoiMenuItemTranslations.Remove(translation);
            await _db.SaveChangesAsync();

            await PublishMenuTranslationUpdatedAsync(
                menuItem,
                "deleted",
                "Bản dịch menu đã xóa",
                $"Món \"{menuItem.Name}\" vừa bị xóa bản dịch {languageCode}.",
                GetActorDisplayName(User.IsAdminRole() ? "Admin" : "POI Owner"));

            return NoContent();
        }

        /// <summary>Auto-translate a menu item into one or more languages.</summary>
        [HttpPost("menu-items/{menuItemId:int}/translate")]
        [Authorize(Roles = AppRoles.AdminOrPoiOwner)]
        public async Task<IActionResult> AutoTranslateMenuItem(int menuItemId, [FromBody] MenuTranslateRequest? request = null)
        {
            var menuItem = await FindAccessibleMenuItemAsync(menuItemId, includeTranslations: true);
            if (menuItem == null)
            {
                return NotFound(new { message = "Không tìm thấy món trong menu hoặc bạn không có quyền truy cập." });
            }

            var requestedLang = string.IsNullOrWhiteSpace(request?.Lang)
                ? null
                : PoiContentResolver.NormalizeLanguageCode(request!.Lang);

            var targetLanguages = ResolveMenuTargetLanguages(requestedLang);
            if (targetLanguages.Count == 0)
            {
                return BadRequest(new { message = "Không có ngôn ngữ hợp lệ để dịch menu." });
            }

            var translatedLanguages = new List<string>();
            var updatedLanguages = new List<string>();
            var now = DateTime.UtcNow;

            foreach (var lang in targetLanguages)
            {
                if (string.Equals(lang, "vi-VN", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var translatedName = await _translator.TranslateTextAsync(menuItem.Name, lang[..2].ToLowerInvariant());
                var translatedDescription = await _translator.TranslateTextAsync(menuItem.Description, lang[..2].ToLowerInvariant());

                var existing = menuItem.Translations.FirstOrDefault(translation =>
                    string.Equals(translation.LanguageCode, lang, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    menuItem.Translations.Add(new PoiMenuItemTranslation
                    {
                        LanguageCode = lang,
                        Name = translatedName,
                        Description = translatedDescription,
                        GeneratedAt = now,
                        IsFallback = string.Equals(lang, "en-US", StringComparison.OrdinalIgnoreCase)
                    });
                    translatedLanguages.Add(lang);
                }
                else
                {
                    existing.Name = translatedName;
                    existing.Description = translatedDescription;
                    existing.GeneratedAt = now;
                    existing.IsFallback = string.Equals(lang, "en-US", StringComparison.OrdinalIgnoreCase);
                    updatedLanguages.Add(lang);
                }
            }

            await _db.SaveChangesAsync();

            var actorName = GetActorDisplayName(User.IsAdminRole() ? "Admin" : "POI Owner");
            var affectedLanguages = translatedLanguages.Concat(updatedLanguages).Distinct().ToList();
            if (affectedLanguages.Count > 0)
            {
                await PublishMenuTranslationUpdatedAsync(
                    menuItem,
                    "translated",
                    "Menu đã được dịch tự động",
                    $"Món \"{menuItem.Name}\" vừa được dịch tự động cho {string.Join(", ", affectedLanguages)}.",
                    actorName);
            }

            return Ok(new
            {
                message = affectedLanguages.Count == 0
                    ? "Không có bản dịch mới nào cần tạo hoặc cập nhật."
                    : $"Đã xử lý bản dịch menu cho: {string.Join(", ", affectedLanguages)}.",
                createdLanguages = translatedLanguages,
                updatedLanguages
            });
        }

        /// <summary>Owner/Admin requests review for an existing menu translation.</summary>
        [HttpPost("menu-translations/{id:int}/request-review")]
        [Authorize(Roles = AppRoles.AdminOrPoiOwner)]
        public async Task<IActionResult> RequestMenuTranslationReview(int id, [FromBody] TranslationReviewRequest? request)
        {
            var translation = await _db.PoiMenuItemTranslations
                .Include(item => item.PoiMenuItem)
                .ThenInclude(menuItem => menuItem.Location)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (translation == null)
            {
                return NotFound(new { message = "Không tìm thấy bản dịch menu." });
            }

            if (!await CanAccessMenuItemAsync(translation.PoiMenuItem))
            {
                return Forbid();
            }

            var note = string.IsNullOrWhiteSpace(request?.Note)
                ? "Không có ghi chú thêm."
                : request!.Note.Trim();
            var actorName = GetActorDisplayName(User.IsAdminRole() ? "Admin" : "POI Owner");

            var notification = new Notification
            {
                TargetRole = AppRoles.Admin,
                Title = "Yêu cầu rà soát bản dịch menu",
                Message = $"Món \"{translation.PoiMenuItem.Name}\" cần rà soát bản dịch {translation.LanguageCode}. Ghi chú: {note}",
                Type = NotificationType.System,
                RelatedId = translation.PoiMenuItem.LocationId,
                SenderName = actorName
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            await _hubContext.Clients.Group(NotificationHubGroups.Role(AppRoles.Admin)).SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                Type = notification.Type.ToString(),
                notification.CreatedAt,
                notification.RelatedId,
                notification.SenderName
            });

            await PublishMenuTranslationUpdatedAsync(
                translation.PoiMenuItem,
                "review_requested",
                "Yêu cầu rà soát bản dịch menu",
                $"Món \"{translation.PoiMenuItem.Name}\" vừa gửi yêu cầu rà soát bản dịch {translation.LanguageCode}.",
                actorName);

            return Ok(new { message = "Đã gửi yêu cầu rà soát bản dịch menu tới admin." });
        }

        /// <summary>Queue translation/audio generation for a POI.</summary>
        [HttpPost("locations/{locationId:int}/translate")]
        [Authorize(Roles = AppRoles.AdminOrPoiOwner)]
        public async Task<IActionResult> QueueLocationTranslation(int locationId, [FromBody] LocationTranslateRequest? request = null)
        {
            var poi = await FindAccessibleLocationAsync(locationId);
            if (poi == null)
            {
                return NotFound(new { message = "Không tìm thấy POI hoặc bạn không có quyền truy cập." });
            }

            var requestedLang = string.IsNullOrWhiteSpace(request?.Lang)
                ? null
                : PoiContentResolver.NormalizeLanguageCode(request.Lang);

            if (requestedLang != null && poi.Translations.Any(translation => translation.LanguageCode == requestedLang))
            {
                return Ok(new
                {
                    message = $"POI đã có sẵn bản dịch {requestedLang}.",
                    status = PoiAudioStatuses.Normalize(poi.AudioStatus, poi.AudioFiles.Any()),
                    languageCode = requestedLang
                });
            }

            if (PoiAudioStatuses.IsQueuedOrRunning(poi.AudioStatus))
            {
                return Accepted(new
                {
                    message = "POI đang trong hàng đợi dịch / audio.",
                    status = poi.AudioStatus,
                    languageCode = requestedLang
                });
            }

            poi.AudioStatus = PoiAudioStatuses.Queued;
            var taskId = _audioTaskManager.EnqueueTask(poi.Id, poi.Name);
            await _db.SaveChangesAsync();

            await PublishTranslationUpdatedAsync(
                poi,
                "queued",
                "Đã xếp hàng dịch",
                $"POI \"{poi.Name}\" đã được đưa vào hàng đợi sinh bản dịch/audio{(requestedLang == null ? string.Empty : $" ({requestedLang})")}.",
                GetActorDisplayName(User.IsAdminRole() ? "Admin" : "POI Owner"));

            return Accepted(new
            {
                message = "Yêu cầu dịch đã được đưa vào hàng đợi.",
                taskId,
                status = poi.AudioStatus,
                languageCode = requestedLang
            });
        }

        /// <summary>Owner/Admin requests review for an existing translation.</summary>
        [HttpPost("translations/{id:int}/request-review")]
        [Authorize(Roles = AppRoles.AdminOrPoiOwner)]
        public async Task<IActionResult> RequestTranslationReview(int id, [FromBody] TranslationReviewRequest? request)
        {
            var translation = await _db.LocationTranslations
                .Include(item => item.Location)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (translation == null)
            {
                return NotFound(new { message = "Không tìm thấy bản dịch." });
            }

            if (!User.IsAdminRole())
            {
                var userId = User.GetUserId();
                if (string.IsNullOrWhiteSpace(userId) || translation.Location.OwnerId != userId)
                {
                    return Forbid();
                }
            }

            var note = string.IsNullOrWhiteSpace(request?.Note)
                ? "Không có ghi chú thêm."
                : request!.Note.Trim();
            var actorName = GetActorDisplayName(User.IsAdminRole() ? "Admin" : "POI Owner");

            var notification = new Notification
            {
                TargetRole = AppRoles.Admin,
                Title = "Yêu cầu rà soát bản dịch",
                Message = $"POI \"{translation.Location.Name}\" cần rà soát bản dịch {translation.LanguageCode}. Ghi chú: {note}",
                Type = NotificationType.System,
                RelatedId = translation.LocationId,
                SenderName = actorName
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            await _hubContext.Clients.Group(NotificationHubGroups.Role(AppRoles.Admin)).SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                Type = notification.Type.ToString(),
                notification.CreatedAt,
                notification.RelatedId,
                notification.SenderName
            });

            await PublishTranslationUpdatedAsync(
                translation.Location,
                "review_requested",
                "Yêu cầu rà soát bản dịch",
                $"POI \"{translation.Location.Name}\" vừa gửi yêu cầu rà soát bản dịch {translation.LanguageCode}.",
                actorName);

            return Ok(new { message = "Đã gửi yêu cầu rà soát bản dịch tới admin." });
        }

        // ====== PRD: LOCALIZATION MODULE ======

        public record HotsetRequest(double Latitude, double Longitude, double RadiusKm = 1.5, string Lang = "en-US");

        /// <summary>
        /// Hotset: Quét tìm các POI trong bán kính, nếu thiếu bản dịch (ngôn ngữ đích) thì đẩy vào AudioTaskManager.
        /// </summary>
        [HttpPost("prepare-hotset")]
        public async Task<IActionResult> PrepareHotset([FromBody] HotsetRequest req)
        {
            var allLocations = await _db.Locations.Include(l => l.Translations).ToListAsync();
            var nearbyPOIs = allLocations
                .Select(l => new 
                { 
                    Location = l, 
                    Distance = _distanceCalculator.Calculate(req.Latitude, req.Longitude, (double)l.Latitude, (double)l.Longitude) 
                })
                .Where(x => x.Distance <= req.RadiusKm)
                .OrderBy(x => x.Distance)
                .Take(10)
                .ToList();

            int queuedCount = 0;
            foreach (var item in nearbyPOIs)
            {
                var poi = item.Location;
                // Nếu chưa có ngôn ngữ yêu cầu (hotset target lang) thì enqueue
                if (!poi.Translations.Any(t => t.LanguageCode == req.Lang))
                {
                    if (!PoiAudioStatuses.IsQueuedOrRunning(poi.AudioStatus))
                    {
                        poi.AudioStatus = PoiAudioStatuses.Queued;
                        _audioTaskManager.EnqueueTask(poi.Id, poi.Name);
                        queuedCount++;
                    }
                }
            }
            if (queuedCount > 0)
                await _db.SaveChangesAsync();

            return Ok(new { message = $"Hotset prepared.", nearPoiCount = nearbyPOIs.Count, queuedItems = queuedCount });
        }

        public record OnDemandRequest(int LocationId, string Lang = "en-US");

        /// <summary>
        /// On-demand: Dịch khẩn cấp 1 POI khi user bước vào zone mà chưa có tiếng.
        /// </summary>
        [HttpPost("on-demand")]
        public async Task<IActionResult> OnDemand([FromBody] OnDemandRequest req)
        {
            var poi = await _db.Locations.Include(l => l.Translations).FirstOrDefaultAsync(l => l.Id == req.LocationId);
            if (poi == null) return NotFound("POI not found");

            if (poi.Translations.Any(t => t.LanguageCode == req.Lang))
            {
                return Ok(new { message = "Already translated", status = PoiAudioStatuses.Ready });
            }

            poi.AudioStatus = PoiAudioStatuses.Queued;
            var taskId = _audioTaskManager.EnqueueTask(poi.Id, poi.Name);
            await _db.SaveChangesAsync();

            return Ok(new { message = "On-demand task queued", taskId });
        }

        /// <summary>
        /// Warmup: Dịch ngầm toàn bộ DB để chuẩn bị offline pack.
        /// </summary>
        [HttpPost("warmup")]
        public async Task<IActionResult> Warmup()
        {
            var allLocations = await _db.Locations.Include(l => l.Translations).ToListAsync();
            int queuedCount = 0;

            foreach (var poi in allLocations)
            {
                // Kiểm tra sơ bộ nếu chưa đủ 4 ngoại ngữ
                int translationCount = poi.Translations.Count;
                if (translationCount < 4 && !PoiAudioStatuses.IsQueuedOrRunning(poi.AudioStatus))
                {
                    poi.AudioStatus = PoiAudioStatuses.Queued;
                    _audioTaskManager.EnqueueTask(poi.Id, poi.Name);
                    queuedCount++;
                }
            }
            if (queuedCount > 0)
                await _db.SaveChangesAsync();

            return Ok(new { message = "Warmup procedure initiated", totalPOIs = allLocations.Count, queuedItems = queuedCount });
        }

        private async Task<Location?> FindAccessibleLocationAsync(int locationId)
        {
            var poi = await _db.Locations
                .Include(location => location.Translations)
                .Include(location => location.AudioFiles)
                .FirstOrDefaultAsync(location => location.Id == locationId);

            if (poi == null)
            {
                return null;
            }

            if (User.IsAdminRole())
            {
                return poi;
            }

            if (User.IsPoiOwnerRole())
            {
                var userId = User.GetUserId();
                if (!string.IsNullOrWhiteSpace(userId) && poi.OwnerId == userId)
                {
                    return poi;
                }
            }

            return null;
        }

        private async Task<PoiMenuItem?> FindAccessibleMenuItemAsync(int menuItemId, bool includeTranslations = false)
        {
            IQueryable<PoiMenuItem> query = _db.PoiMenuItems
                .Include(menuItem => menuItem.Location);

            if (includeTranslations)
            {
                query = query.Include(menuItem => menuItem.Translations);
            }

            var menuItem = await query.FirstOrDefaultAsync(item => item.Id == menuItemId);
            if (menuItem == null)
            {
                return null;
            }

            return await CanAccessMenuItemAsync(menuItem) ? menuItem : null;
        }

        private Task<bool> CanAccessMenuItemAsync(PoiMenuItem menuItem)
        {
            if (User.IsAdminRole())
            {
                return Task.FromResult(true);
            }

            if (User.IsPoiOwnerRole())
            {
                var userId = User.GetUserId();
                return Task.FromResult(!string.IsNullOrWhiteSpace(userId) && menuItem.Location.OwnerId == userId);
            }

            return Task.FromResult(false);
        }

        private string GetActorDisplayName(string fallback)
        {
            return User.Claims.FirstOrDefault(claim =>
                       claim.Type == "name" || claim.Type == System.Security.Claims.ClaimTypes.Name)?.Value
                   ?? User.Identity?.Name
                   ?? fallback;
        }

        private async Task PublishTranslationUpdatedAsync(Location poi, string status, string title, string message, string? triggeredBy)
        {
            var payload = new RealtimeActivityMessage
            {
                EntityType = "poi",
                EntityId = poi.Id,
                Status = status,
                Title = title,
                Message = message,
                TriggeredBy = triggeredBy
            };

            await _hubContext.SendRealtimeToRoleAsync(AppRoles.Admin, NotificationHubEvents.TranslationUpdated, payload);
            await _hubContext.SendRealtimeToPoiAsync(poi.Id, NotificationHubEvents.TranslationUpdated, payload);

            if (!string.IsNullOrWhiteSpace(poi.OwnerId))
            {
                await _hubContext.SendRealtimeToUserAsync(poi.OwnerId, NotificationHubEvents.TranslationUpdated, payload);
            }
        }

        private async Task PublishMenuTranslationUpdatedAsync(PoiMenuItem menuItem, string status, string title, string message, string? triggeredBy)
        {
            var payload = new RealtimeActivityMessage
            {
                EntityType = "menu",
                EntityId = menuItem.Id,
                Status = status,
                Title = title,
                Message = message,
                TriggeredBy = triggeredBy
            };

            await _hubContext.SendRealtimeToRoleAsync(AppRoles.Admin, NotificationHubEvents.MenuUpdated, payload);
            await _hubContext.SendRealtimeToPoiAsync(menuItem.LocationId, NotificationHubEvents.MenuUpdated, payload);

            if (!string.IsNullOrWhiteSpace(menuItem.Location.OwnerId))
            {
                await _hubContext.SendRealtimeToUserAsync(menuItem.Location.OwnerId, NotificationHubEvents.MenuUpdated, payload);
            }
        }

        private static MenuTranslationItemDto MapMenuTranslation(PoiMenuItemTranslation translation, PoiMenuItem menuItem)
        {
            return new MenuTranslationItemDto
            {
                Id = translation.Id,
                PoiMenuItemId = menuItem.Id,
                LocationId = menuItem.LocationId,
                LocationName = menuItem.Location.Name,
                MenuItemName = menuItem.Name,
                LanguageCode = translation.LanguageCode,
                Name = translation.Name,
                Description = translation.Description,
                IsFallback = translation.IsFallback,
                GeneratedAt = translation.GeneratedAt
            };
        }

        private List<string> ResolveMenuTargetLanguages(string? requestedLang)
        {
            if (!string.IsNullOrWhiteSpace(requestedLang))
            {
                return [requestedLang];
            }

            return _supportedLanguages
                .Select(language => language.Code)
                .Where(code => !string.Equals(code, "vi-VN", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    public record SupportedLanguage(string Code, string Label, string Flag);
    public record LocationTranslateRequest(string? Lang = null);
    public record MenuTranslateRequest(string? Lang = null);
    public record TranslationReviewRequest(string? Note = null);

    public class TranslationItemDto
    {
        public int Id { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? TtsScript { get; set; }
        public string? AudioUrl { get; set; }
        public bool IsFallback { get; set; }
        public DateTime? GeneratedAt { get; set; }
    }

    public class UpsertTranslationRequest
    {
        public int LocationId { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? TtsScript { get; set; }
    }
}
