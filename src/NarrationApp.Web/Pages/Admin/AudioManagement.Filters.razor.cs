using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Pages.Admin;

public partial class AudioManagement
{
    private IReadOnlyList<string> CategoryOptions => _pois
        .Select(poi => poi.CategoryName)
        .Where(category => !string.IsNullOrWhiteSpace(category))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(category => category, StringComparer.CurrentCultureIgnoreCase)
        .Cast<string>()
        .ToArray();

    private IReadOnlyList<AdminPoiDto> FilteredPois => _pois
        .Where(MatchesSearch)
        .Where(MatchesStatus)
        .Where(MatchesSource)
        .Where(MatchesCategory)
        .OrderByDescending(poi => poi.Id == _selectedPoi?.Id)
        .ThenBy(poi => poi.Name, StringComparer.CurrentCultureIgnoreCase)
        .ToArray();

    private int FilteredFailedPoiCount =>
        FilteredPois.Count(poi => GetFailedLanguageCodes(GetAudioItems(poi.Id)).Count > 0);

    private void ResetFilters()
    {
        _searchTerm = string.Empty;
        _statusFilter = "all";
        _sourceFilter = "all";
        _categoryFilter = "all";
    }

    private bool MatchesSearch(AdminPoiDto poi)
    {
        if (string.IsNullOrWhiteSpace(_searchTerm))
        {
            return true;
        }

        var search = _searchTerm.Trim();
        return ContainsIgnoreCase(poi.Name, search)
            || ContainsIgnoreCase(poi.Slug, search)
            || ContainsIgnoreCase(poi.CategoryName, search)
            || ContainsIgnoreCase(poi.OwnerEmail, search);
    }

    private bool MatchesStatus(AdminPoiDto poi)
    {
        if (_statusFilter == "all")
        {
            return true;
        }

        var status = GetOverallAudioStatus(GetAudioItems(poi.Id));
        return _statusFilter switch
        {
            "ready" => status == AudioStatus.Ready,
            "processing" => status is AudioStatus.Requested or AudioStatus.Generating,
            "failed" => status == AudioStatus.Failed,
            "missing" => status is null,
            _ => true
        };
    }

    private bool MatchesSource(AdminPoiDto poi)
    {
        if (_sourceFilter == "all")
        {
            return true;
        }

        var source = GetVietnameseSource(GetAudioItems(poi.Id));
        return _sourceFilter switch
        {
            "recorded" => source?.SourceType == AudioSourceType.Recorded,
            "tts" => source?.SourceType == AudioSourceType.Tts,
            "missing" => source is null,
            _ => true
        };
    }

    private bool MatchesCategory(AdminPoiDto poi) =>
        _categoryFilter == "all"
        || string.Equals(poi.CategoryName, _categoryFilter, StringComparison.OrdinalIgnoreCase);

    private static bool ContainsIgnoreCase(string? source, string value) =>
        !string.IsNullOrWhiteSpace(source)
        && source.Contains(value, StringComparison.OrdinalIgnoreCase);
}
