using NarrationApp.Shared.DTOs.Languages;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Pages.Admin;

public partial class LanguageManagement
{
    private bool _isLoading = true;
    private bool _isFormOpen;
    private string? _errorMessage;
    private string? _statusMessage;
    private IReadOnlyList<ManagedLanguageDto> _languages = Array.Empty<ManagedLanguageDto>();
    private LanguageDraft _draft = new();
    private int ActiveLanguageCount => _languages.Count(item => item.IsActive);
    private int TranslationLanguageCount => _languages.Count(item => item.IsActive && item.Role == ManagedLanguageRole.TranslationAudio);
    private int TotalAudioCount => _languages.Sum(item => item.AudioCount);
    private string AverageCoveragePercent => _languages.Count == 0 ? "0%" : $"{Math.Round(_languages.Average(item => GetCoverageRatio(item)) * 100d)}%";

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private sealed class LanguageDraft
    {
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string NativeName { get; set; } = string.Empty;
        public string FlagCode { get; set; } = string.Empty;
    }
}
