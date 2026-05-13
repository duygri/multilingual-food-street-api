using NarrationApp.Shared.Visitor;

namespace NarrationApp.Mobile.Features.Home;

public sealed partial class VisitorShellState
{
    public void ApplyContent(VisitorContentSnapshot snapshot, bool isFallback = true, string? sourceLabel = null, string? syncMessage = null)
    {
        ApplyLanguages(snapshot.Languages);

        _categories.Clear();
        _categories.AddRange(VisitorCategoryFilterBuilder.Build(snapshot));

        _pois.Clear();
        _pois.AddRange(snapshot.Pois);
        InvalidatePoiViews();

        _tours.Clear();
        _tours.AddRange(snapshot.Tours);

        IsUsingFallbackData = isFallback;
        DataSourceLabel = sourceLabel ?? (isFallback ? "Demo fallback" : "Live API");
        SyncMessage = syncMessage ?? (isFallback ? "Đang dùng dữ liệu demo cục bộ." : "Đã đồng bộ dữ liệu từ máy chủ.");

        EnsureSelectedCategoryStillVisible();
        EnsureSelectedPoiStillVisible();
        EnsureSelectedTourStillVisible();
        EnsureActiveTourStillVisible();
        ApplyPendingQrNavigationTargetIfReady();
    }

    private void ApplyLanguages(IReadOnlyList<VisitorLanguageOption>? languages)
    {
        if (languages is null || languages.Count == 0)
        {
            return;
        }

        var normalizedLanguages = languages
            .Where(language => !string.IsNullOrWhiteSpace(language.Code))
            .GroupBy(language => language.Code.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(language => language with { Code = language.Code.Trim().ToLowerInvariant() })
            .ToArray();

        if (normalizedLanguages.Length == 0)
        {
            return;
        }

        _languages.Clear();
        _languages.AddRange(normalizedLanguages);

        if (_languages.Any(language => string.Equals(language.Code, SelectedLanguageCode, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        SelectedLanguageCode = _languages.FirstOrDefault(language => string.Equals(language.Code, "vi", StringComparison.OrdinalIgnoreCase))?.Code
            ?? _languages[0].Code;
    }
}
