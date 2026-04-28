using Microsoft.AspNetCore.Components.Forms;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Languages;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class AudioManagement : IAsyncDisposable
{
    private const long MaxUploadBytes = 20_000_000;
    private static readonly TimeSpan AutoRefreshInterval = TimeSpan.FromSeconds(4);
    private static readonly VoiceProfileOption[] VoiceProfiles =
    [
        new("standard", "Standard (Tiêu chuẩn — tiết kiệm)"),
        new("wavenet", "WaveNet (Tự nhiên — cao cấp)"),
        new("neural2", "Neural2 (Tiên tiến nhất)")
    ];

    private bool _isLoading = true;
    private bool _isGenerateModalOpen;
    private bool _isUploadModalOpen;
    private bool _isGenerating;
    private bool _isUploading;
    private bool _isAutoRefreshing;
    private string? _errorMessage;
    private string? _statusMessage;
    private string _searchTerm = string.Empty;
    private string _statusFilter = "all";
    private string _sourceFilter = "all";
    private string _categoryFilter = "all";
    private string _selectedVoiceProfile = "standard";
    private HashSet<string> _selectedLanguages = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<ManagedLanguageDto> _languages = Array.Empty<ManagedLanguageDto>();
    private IReadOnlyList<AdminPoiDto> _pois = Array.Empty<AdminPoiDto>();
    private Dictionary<int, IReadOnlyList<AudioDto>> _audioByPoi = [];
    private Dictionary<int, IReadOnlyList<TranslationDto>> _translationsByPoi = [];
    private AdminPoiDto? _selectedPoi;
    private AdminPoiDto? _modalPoi;
    private AdminPoiDto? _uploadPoi;
    private int? _previewingAudioId;
    private IBrowserFile? _selectedUploadFile;
    private IAsyncDisposable? _refreshSubscription;

    private IReadOnlyList<AudioLanguageOption> ActiveLanguageOptions => _languages
        .Where(item => item.IsActive)
        .OrderBy(item => item.Role)
        .ThenBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
        .Select(item => new AudioLanguageOption(item.Code, item.FlagCode))
        .ToArray();

    private bool HasAnyPoi => _pois.Count > 0;

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardAsync();
        _refreshSubscription = AudioRefreshPump.Start(HandleAutoRefreshTickAsync, AutoRefreshInterval);
    }

    private async Task LoadDashboardAsync()
    {
        try
        {
            _pois = await AdminPortalService.GetPoisAsync();
            _languages = await LanguagePortalService.GetAsync();

            var audioTasks = _pois.Select(async poi => new { poi.Id, Items = await AudioPortalService.GetByPoiAsync(poi.Id) });
            var translationTasks = _pois.Select(async poi => new { poi.Id, Items = await TranslationPortalService.GetByPoiAsync(poi.Id) });

            _audioByPoi = (await Task.WhenAll(audioTasks)).ToDictionary(item => item.Id, item => item.Items);
            _translationsByPoi = (await Task.WhenAll(translationTasks)).ToDictionary(item => item.Id, item => item.Items);
            _selectedPoi = _pois.FirstOrDefault(poi => poi.Id == _selectedPoi?.Id) ?? _pois.FirstOrDefault();
            _errorMessage = null;
        }
        catch (ApiException exception)
        {
            _errorMessage = exception.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_refreshSubscription is not null)
        {
            await _refreshSubscription.DisposeAsync();
        }
    }

    private IReadOnlyList<AudioDto> GetAudioItems(int poiId) =>
        _audioByPoi.TryGetValue(poiId, out var items) ? items : Array.Empty<AudioDto>();

    private IReadOnlyList<TranslationDto> GetTranslations(int poiId) =>
        _translationsByPoi.TryGetValue(poiId, out var items) ? items : Array.Empty<TranslationDto>();

    private void SelectPoi(AdminPoiDto poi) => _selectedPoi = poi;

    private void ToggleInlinePreview(AudioDto audio)
    {
        _previewingAudioId = _previewingAudioId == audio.Id ? null : audio.Id;
    }

    private void AppendAudio(AudioDto audio)
    {
        _audioByPoi[audio.PoiId] = GetAudioItems(audio.PoiId)
            .Where(item => item.Id != audio.Id)
            .Append(audio)
            .OrderByDescending(item => item.GeneratedAtUtc ?? DateTime.MinValue)
            .ToArray();
    }

    private sealed record AudioLanguageOption(string Code, string Flag);
    private sealed record VoiceProfileOption(string Value, string Label);
}
