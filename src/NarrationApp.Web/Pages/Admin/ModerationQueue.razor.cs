using NarrationApp.Shared.DTOs.Moderation;

namespace NarrationApp.Web.Pages.Admin;

public partial class ModerationQueue
{
    private bool _isLoading = true;
    private string? _errorMessage;
    private string? _statusMessage;
    private List<ModerationRequestDto> _items = [];
    private string OldestRequestLabel => _items.Count == 0 ? "0 phút" : GetAgeLabel(_items.Min(item => item.CreatedAtUtc));
    private int CountByEntityType(string entityType) => _items.Count(item => string.Equals(item.EntityType, entityType, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
        _isLoading = false;
    }
}
