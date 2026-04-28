using NarrationApp.Shared.DTOs.Languages;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class LanguageManagement
{
    private async Task LoadAsync()
    {
        try
        {
            _languages = await LanguagePortalService.GetAsync();
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

    private void ToggleForm()
    {
        _isFormOpen = !_isFormOpen;
        if (_isFormOpen)
        {
            _draft = new LanguageDraft();
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            var created = await LanguagePortalService.CreateAsync(new CreateManagedLanguageRequest
            {
                Code = _draft.Code.Trim().ToLowerInvariant(),
                DisplayName = _draft.DisplayName.Trim(),
                NativeName = _draft.NativeName.Trim(),
                FlagCode = _draft.FlagCode.Trim().ToUpperInvariant()
            });

            _languages = _languages.Where(item => !string.Equals(item.Code, created.Code, StringComparison.OrdinalIgnoreCase))
                .Append(created)
                .OrderBy(item => item.Role)
                .ThenBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _statusMessage = $"Đã thêm ngôn ngữ {created.Code}.";
            _isFormOpen = false;
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
    }
}
