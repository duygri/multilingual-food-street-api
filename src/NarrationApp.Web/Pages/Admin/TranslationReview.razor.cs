using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.DTOs.Languages;
using NarrationApp.Shared.DTOs.Translation;
using NarrationApp.Shared.Enums;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class TranslationReview
{
    private bool _isLoading = true;
    private string? _errorMessage;
    private string? _statusMessage;
    private bool _isReviewPanelOpen;
    private bool _isBulkTranslationPanelOpen;
    private string _bulkLanguageSearch = string.Empty;
    private string? _bulkTranslationLanguageInProgress;
    private string? _bulkTranslationStatusMessage;
    private string _selectedLanguage = "vi";
    private IReadOnlyList<ManagedLanguageDto> _languages = Array.Empty<ManagedLanguageDto>();
    private IReadOnlyList<AdminPoiDto> _pois = Array.Empty<AdminPoiDto>();
    private Dictionary<int, IReadOnlyList<TranslationDto>> _translationsByPoi = [];
    private Dictionary<int, IReadOnlyList<AudioDto>> _audioByPoi = [];
    private IReadOnlyList<TranslationDto> _translations = Array.Empty<TranslationDto>();
    private AdminPoiDto? _selectedPoi;
    private TranslationDto? _selectedTranslation;
    private TranslationEditorModel _editor = TranslationEditorModel.CreateDefault("vi");

    private AudioDto? SelectedAudio => _selectedPoi is null ? null : GetLatestAudio(_selectedPoi.Id, _selectedLanguage);
    private IReadOnlyList<ManagedLanguageDto> BulkTranslationLanguages => _languages.Where(item => item.Role == ManagedLanguageRole.TranslationAudio).ToArray();
    private bool IsBulkTranslationRunning => !string.IsNullOrWhiteSpace(_bulkTranslationLanguageInProgress);
    private IEnumerable<TranslationDto> AllTranslations => _translationsByPoi.Values.SelectMany(items => items);
    private int TotalTranslationCount => AllTranslations.Count();
    private int AutoTranslatedCount => AllTranslations.Count(item => item.WorkflowStatus == TranslationWorkflowStatus.AutoTranslated);
    private int ReviewedCount => AllTranslations.Count(item => item.WorkflowStatus == TranslationWorkflowStatus.Reviewed);
    private int PoiWithMultipleLanguagesCount => _pois.Count(poi => GetTranslations(poi.Id).Count >= 2);
    private string CoveragePercent => _pois.Count == 0 ? "0%" : $"{Math.Round((double)PoiWithMultipleLanguagesCount / _pois.Count * 100d)}%";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _languages = (await LanguagePortalService.GetAsync())
                .Where(item => item.IsActive)
                .OrderBy(item => item.Role)
                .ThenBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _pois = await AdminPortalService.GetPoisAsync();
            await LoadPoiRowsAsync();

            if (_pois.Count > 0)
            {
                SelectPoiLanguage(_pois[0], "vi", false);
            }
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

    private IReadOnlyList<TranslationDto> GetTranslations(int poiId) =>
        _translationsByPoi.TryGetValue(poiId, out var items) ? items : Array.Empty<TranslationDto>();

    private IReadOnlyList<AudioDto> GetAudioItems(int poiId) =>
        _audioByPoi.TryGetValue(poiId, out var items) ? items : Array.Empty<AudioDto>();

    private AudioDto? GetLatestAudio(int poiId, string languageCode) =>
        GetAudioItems(poiId)
            .Where(item => string.Equals(item.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
            .Where(item => item.Status is not AudioStatus.Deleted and not AudioStatus.Replaced)
            .OrderByDescending(item => item.GeneratedAtUtc ?? DateTime.MinValue)
            .ThenByDescending(item => item.Id)
            .FirstOrDefault();

    private async Task LoadPoiRowsAsync()
    {
        var rows = await Task.WhenAll(_pois.Select(async poi => new PoiRowState(
            poi.Id,
            await TranslationPortalService.GetByPoiAsync(poi.Id),
            await AudioPortalService.GetByPoiAsync(poi.Id))));

        _translationsByPoi = rows.ToDictionary(item => item.PoiId, item => item.Translations);
        _audioByPoi = rows.ToDictionary(item => item.PoiId, item => item.AudioItems);
    }

    private sealed record PoiRowState(int PoiId, IReadOnlyList<TranslationDto> Translations, IReadOnlyList<AudioDto> AudioItems);

    private sealed class TranslationEditorModel
    {
        public string LanguageCode { get; set; } = "vi";
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Story { get; set; } = string.Empty;
        public string Highlight { get; set; } = string.Empty;

        public static TranslationEditorModel CreateDefault(string languageCode) => new() { LanguageCode = languageCode };

        public static TranslationEditorModel FromDto(TranslationDto dto) => new()
        {
            LanguageCode = dto.LanguageCode,
            Title = dto.Title,
            Description = dto.Description,
            Story = dto.Story,
            Highlight = dto.Highlight
        };

        public CreateTranslationRequest ToRequest(int poiId) => new()
        {
            PoiId = poiId,
            LanguageCode = LanguageCode.Trim().ToLowerInvariant(),
            Title = Title.Trim(),
            Description = Description.Trim(),
            Story = Story.Trim(),
            Highlight = Highlight.Trim(),
            IsFallback = false
        };
    }
}
