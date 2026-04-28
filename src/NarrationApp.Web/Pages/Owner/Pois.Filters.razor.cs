using NarrationApp.Shared.DTOs.Owner;

namespace NarrationApp.Web.Pages.Owner;

public partial class Pois
{
    private bool MatchesSearch(OwnerPoisWorkspaceRowDto row)
    {
        if (string.IsNullOrWhiteSpace(_searchTerm)) return true;
        return row.PoiName.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase)
            || row.Slug.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase)
            || (row.CategoryName?.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private bool MatchesStatus(OwnerPoisWorkspaceRowDto row) =>
        string.IsNullOrWhiteSpace(_statusFilter)
        || string.Equals(row.Status.ToString(), _statusFilter, StringComparison.Ordinal);
}
