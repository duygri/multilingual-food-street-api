using Microsoft.AspNetCore.Components;
using NarrationApp.Shared.DTOs.Admin;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Pages.Admin;

public partial class PoiManagement
{
    private IReadOnlyList<AdminPoiDto> VisiblePois => FilteredPois
        .Skip((CurrentPage - 1) * PageSize)
        .Take(PageSize)
        .ToArray();

    private IReadOnlyList<PoiRow> VisibleRows => VisiblePois
        .Select((item, index) => new PoiRow(((CurrentPage - 1) * PageSize) + index + 1, item))
        .ToArray();

    private int PendingPoiCount => _pois.Count(item => item.Status == PoiStatus.PendingReview);
    private int PageCount => Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
    private IEnumerable<int> PageNumbers => Enumerable.Range(1, PageCount);

    private string VisibleRangeLabel => FilteredCount == 0
        ? "0-0 / 0 POI"
        : $"{((CurrentPage - 1) * PageSize) + 1}-{((CurrentPage - 1) * PageSize) + VisibleRows.Count} / {FilteredCount} POI";

    private void SetFilter(PoiFilterTab filter)
    {
        _activeFilter = filter;
        CurrentPage = 1;
        NormalizePageAndSelection();
    }

    private void HandleSearchChanged(ChangeEventArgs args)
    {
        _searchText = args.Value?.ToString()?.Trim() ?? string.Empty;
        CurrentPage = 1;
        NormalizePageAndSelection();
    }

    private void GoToPage(int page)
    {
        CurrentPage = Math.Clamp(page, 1, PageCount);
        NormalizePageAndSelection();
    }

    private void PreviousPage() => GoToPage(CurrentPage - 1);
    private void NextPage() => GoToPage(CurrentPage + 1);

    private void SelectPoi(AdminPoiDto poi)
    {
        _selectedPoi = poi;
        _statusMessage = null;
    }

    private void ClosePoiDetail() => _selectedPoi = null;

    private bool IsSelected(AdminPoiDto poi) => _selectedPoi?.Id == poi.Id;

    private void NormalizePageAndSelection(int? preferredPoiId = null)
    {
        CurrentPage = Math.Clamp(CurrentPage, 1, PageCount);
        var candidateId = preferredPoiId ?? _selectedPoi?.Id;
        _selectedPoi = candidateId is int poiId ? VisiblePois.FirstOrDefault(item => item.Id == poiId) : null;
    }
}
