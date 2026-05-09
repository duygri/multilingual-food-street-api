using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class PoiManagement
{
    private int _priorityDraft;
    private bool _isSavingPriority;

    private bool IsPrioritySaveDisabled =>
        _isSavingPriority
        || _selectedPoi is null
        || _priorityDraft <= 0
        || _priorityDraft == _selectedPoi.Priority;

    private void SyncPriorityDraft()
    {
        if (_selectedPoi is not null)
        {
            _priorityDraft = _selectedPoi.Priority;
        }
    }

    private async Task SavePriorityAsync()
    {
        if (_selectedPoi is null || IsPrioritySaveDisabled)
        {
            return;
        }

        _isSavingPriority = true;

        try
        {
            var updated = await AdminPoiOperationsService.UpdatePriorityAsync(_selectedPoi.Id, _priorityDraft);
            ReplacePoi(updated);
            _statusMessage = $"Đã cập nhật priority POI {updated.Name}.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
        catch (Exception exception)
        {
            _statusMessage = exception.Message;
        }
        finally
        {
            _isSavingPriority = false;
        }
    }
}
