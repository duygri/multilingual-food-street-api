namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private void ClearSettingsFeedback()
    {
        _aboutStatusMessage = null;
    }

    private void ShowSettingsFeedback(string message)
    {
        _aboutStatusMessage = message;
    }

    private Task ApplySettingsStateChangeAsync(Action applyStateChange)
    {
        ClearSettingsFeedback();
        applyStateChange();
        return Task.CompletedTask;
    }

    private async Task ApplySettingsStateChangeAsync(Func<Task> applyStateChangeAsync)
    {
        ClearSettingsFeedback();
        await applyStateChangeAsync();
    }
}
