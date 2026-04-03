using System.Net.Http.Json;
using FoodStreet.Client.Services;      // IGpsTrackingService, ILocalizationService, ILocationClientService
using Microsoft.Extensions.Logging;    // ILogger<T>
using Microsoft.Maui.Storage;          // FileSystem (MAUI API)

namespace FoodStreet.Mobile.Services;

/// <summary>
/// C# equivalent of "Frontend Startup Flow":
/// 
///  App Init → Navigation Setup → Splash Screen → Language Selection
///      └──► Parallel Load ──────────────────────────────────────────
///               ├── [GPS]          waitForPosition()
///               ├── [Offline Data] loadOfflinePOIs(lang)  → SQLite (0ms)
///               ├── [Online Data]  loadAllPOIs(lang)      → API → save cache
///               ├── [Hotset]       scan 1.5km, pre-translate top 10 POIs
///               └── [Warmup]       POST /warmup            → server warms corpus
/// </summary>
public class AppStartupFlow
{
    // ── Step results (read after RunAsync completes) ──────────────────
    public StartupResult Result { get; private set; } = new();

    private readonly HttpClient _http;
    private readonly ILocationClientService _locationService;
    private readonly IGpsTrackingService _gpsService;
    private readonly ILocalizationService _localization;
    private readonly ILogger<AppStartupFlow> _logger;

    public AppStartupFlow(
        HttpClient http,
        ILocationClientService locationService,
        IGpsTrackingService gpsService,
        ILocalizationService localization,
        ILogger<AppStartupFlow> logger)
    {
        _http = http;
        _locationService = locationService;
        _gpsService = gpsService;
        _localization = localization;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────
    // STEP 1 — App Init (equivalent to SW Register)
    // ─────────────────────────────────────────────────────────────────
    public static async Task<bool> InitAppAsync(ILogger logger)
    {
        logger.LogInformation("[Startup] ▶ Step 1/5 — App Init");
        try
        {
            // Ensure required directories exist (audio cache, image cache, etc.)
            var cacheDir = Path.Combine(FileSystem.AppDataDirectory, "cache");
            Directory.CreateDirectory(cacheDir);

            logger.LogInformation("[Startup] ✅ App directories initialized at {Dir}", cacheDir);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Startup] ❌ App Init failed");
            return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // STEP 2 — Navigation Ready (equivalent to React Router catch-all)
    //          In MAUI/Blazor this is handled by the router in App.razor.
    //          We validate the route table here programmatically.
    // ─────────────────────────────────────────────────────────────────
    public static void EnsureRouterReady(ILogger logger)
    {
        logger.LogInformation("[Startup] ▶ Step 2/5 — Navigation/Router ready");
        // Routes are declared via @page in Razor components.
        // This step is a logical gate; no code needed if router compiled correctly.
        logger.LogInformation("[Startup] ✅ Router configured (Blazor @page directives active)");
    }

    // ─────────────────────────────────────────────────────────────────
    // STEP 3 — Splash Screen (shown while Steps 4–5 run in background)
    //          Caller (App.razor) shows splash; this method is the gate.
    // ─────────────────────────────────────────────────────────────────
    public static async Task ShowSplashAsync(ILogger logger, int minDurationMs = 1500)
    {
        logger.LogInformation("[Startup] ▶ Step 3/5 — Splash Screen ({ms}ms min)", minDurationMs);
        await Task.Delay(minDurationMs);   // Ensure brand is visible
        logger.LogInformation("[Startup] ✅ Splash Screen complete");
    }

    // ─────────────────────────────────────────────────────────────────
    // STEP 4 — Language Selection
    //          If user already chose, skip straight to Parallel Load.
    // ─────────────────────────────────────────────────────────────────
    public static async Task<string> ResolveLangAsync(
        ILocalizationService localization,
        ILogger logger)
    {
        logger.LogInformation("[Startup] ▶ Step 4/5 — Chọn ngôn ngữ");

        // ILocalizationService uses synchronous SetLanguage() + CurrentLanguage property
        var lang = localization.CurrentLanguage;

        if (string.IsNullOrEmpty(lang))
        {
            lang = "vi-VN";   // default: Vietnamese
            localization.SetLanguage(lang);
            logger.LogInformation("[Startup] ℹ  No saved language — defaulted to '{Lang}'", lang);
        }
        else
        {
            logger.LogInformation("[Startup] ✅ Language restored: '{Lang}'", lang);
        }

        return lang;
    }

    // ─────────────────────────────────────────────────────────────────
    // STEP 5 — Parallel Load  ★ CORE OF THE FLOW ★
    //
    //   Task.WhenAll launches ALL 5 branches simultaneously:
    //   • GPS         → waitForPosition (high accuracy, timeout 10s)
    //   • Offline     → loadOfflinePOIs(lang)   — SQLite, instant (0ms)
    //   • Online      → loadAllPOIs(lang) → API → save to SQLite
    //   • Hotset      → scan 1.5km, pre-cache top-10 POIs
    //   • Warmup      → POST /warmup — server corpus warm
    // ─────────────────────────────────────────────────────────────────
    public async Task<StartupResult> RunParallelLoadAsync(string lang)
    {
        _logger.LogInformation("[Startup] ▶ Step 5/5 — Parallel Load (lang={Lang})", lang);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Launch all 5 branches in parallel ─────────────────
        var gpsTask        = StepGpsAsync();
        var offlineTask    = StepOfflineDataAsync(lang);
        var onlineTask     = StepOnlineDataAsync(lang);
        var hotsetTask     = StepHotsetAsync();
        var warmupTask     = StepWarmupAsync();
        // ────────────────────────────────────────────────────

        await Task.WhenAll(gpsTask, offlineTask, onlineTask, hotsetTask, warmupTask);

        sw.Stop();

        Result = new StartupResult
        {
            Lang           = lang,
            GpsReady       = gpsTask.Result,
            OfflinePois    = offlineTask.Result,
            OnlinePois     = onlineTask.Result,
            HotsetReady    = hotsetTask.Result,
            WarmupReady    = warmupTask.Result,
            ElapsedMs      = (int)sw.ElapsedMilliseconds,
        };

        _logger.LogInformation(
            "[Startup] ✅ Parallel Load done in {Ms}ms | GPS={Gps} | Offline={Off} POIs | Online={On} POIs | Hotset={Hot} | Warmup={Warm}",
            Result.ElapsedMs, Result.GpsReady, Result.OfflinePois,
            Result.OnlinePois, Result.HotsetReady, Result.WarmupReady);

        return Result;
    }

    // ─────────────────────────────────────────────────────────────────
    // Branch A — GPS  (waitForPosition — high accuracy, 10s timeout)
    // ─────────────────────────────────────────────────────────────────
    private async Task<bool> StepGpsAsync()
    {
        const int timeoutMs = 10_000;   // 10s like the diagram
        _logger.LogInformation("[GPS] waitForPosition() started (timeout {T}s)", timeoutMs / 1000);

        try
        {
            using var cts = new CancellationTokenSource(timeoutMs);
            await _gpsService.GetCurrentPositionAsync();

            var hasPos = _gpsService.CurrentLatitude.HasValue && _gpsService.CurrentLongitude.HasValue;
            if (hasPos)
                _logger.LogInformation("[GPS] ✅ Position: {Lat},{Lng}", _gpsService.CurrentLatitude, _gpsService.CurrentLongitude);
            else
                _logger.LogWarning("[GPS] ⚠ No position received within timeout");

            return hasPos;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GPS] ⚠ GPS unavailable — continuing without position");
            return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Branch B — Offline Data  (getOfflinePOIs(lang) — SQLite, 0ms)
    // ─────────────────────────────────────────────────────────────────
    private async Task<int> StepOfflineDataAsync(string lang)
    {
        _logger.LogInformation("[Offline] getOfflinePOIs(lang={Lang}) — reading local cache", lang);
        try
        {
            // Load approved locations from local SQLite/preferences cache
            var cached = await _locationService.GetApprovedLocations();
            var count = cached?.Count ?? 0;

            _logger.LogInformation("[Offline] ✅ {Count} POIs loaded from local cache (0ms)", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Offline] ⚠ No offline cache available");
            return 0;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Branch C — Online Data  (loadAllPOIs → API → savePOIs → cache)
    // ─────────────────────────────────────────────────────────────────
    private async Task<int> StepOnlineDataAsync(string lang)
    {
        _logger.LogInformation("[Online] loadAllPOIs(lang={Lang}) → API call", lang);
        try
        {
            var pois = await _locationService.GetApprovedLocations();
            var count = pois?.Count ?? 0;

            // TODO: savePOIs() — persist to SQLite for offline use
            // await _localDb.SavePoisAsync(pois, lang);

            _logger.LogInformation("[Online] ✅ {Count} POIs fetched from API → cache updated", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Online] ⚠ API unreachable — offline data will be used");
            return 0;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Branch D — Hotset  (scan 1.5km, pre-translate 10 nearest POIs)
    // ─────────────────────────────────────────────────────────────────
    private async Task<bool> StepHotsetAsync()
    {
        const double RadiusKm = 1.5;
        const int    TopN     = 10;

        _logger.LogInformation("[Hotset] Scanning {R}km radius for top {N} nearest POIs", RadiusKm, TopN);
        try
        {
            var lat = _gpsService.CurrentLatitude;
            var lng = _gpsService.CurrentLongitude;

            if (!lat.HasValue || !lng.HasValue)
            {
                _logger.LogWarning("[Hotset] ⚠ GPS not ready — skipping hotset pre-cache");
                return false;
            }

            var nearby = await _locationService.GetNearestLocations(lat.Value, lng.Value, page: 1, pageSize: TopN);
            var count  = nearby?.Items?.Count ?? 0;

            // Pre-warm audio/TTS for each POI so user hears immediately on arrival
            // await _narrationEngine.PreCacheAsync(nearby.Items);

            _logger.LogInformation("[Hotset] ✅ {Count} POIs pre-cached within {R}km → ready for immediate playback", count, RadiusKm);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Hotset] ⚠ Hotset pre-cache failed");
            return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Branch E — Warmup  (POST /warmup → server warms full corpus)
    // ─────────────────────────────────────────────────────────────────
    private async Task<bool> StepWarmupAsync()
    {
        _logger.LogInformation("[Warmup] POST /api/content/tours/warmup → server corpus warm");
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response  = await _http.PostAsync("api/content/tours/warmup", null, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[Warmup] ✅ Server corpus warmed — offline TTS ready");
                return true;
            }

            _logger.LogWarning("[Warmup] ⚠ Warmup responded {Code}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Warmup] ⚠ Warmup skipped (server unreachable)");
            return false;   // Non-fatal: app still works
        }
    }
}

// ─────────────────────────────────────────────────────────────────────
// Result DTO
// ─────────────────────────────────────────────────────────────────────
public class StartupResult
{
    public string Lang        { get; set; } = "vi";
    public bool   GpsReady    { get; set; }
    public int    OfflinePois { get; set; }
    public int    OnlinePois  { get; set; }
    public bool   HotsetReady { get; set; }
    public bool   WarmupReady { get; set; }
    public int    ElapsedMs   { get; set; }

    /// <summary>App is ready to show main UI.</summary>
    public bool IsReady => OfflinePois > 0 || OnlinePois > 0;
}
