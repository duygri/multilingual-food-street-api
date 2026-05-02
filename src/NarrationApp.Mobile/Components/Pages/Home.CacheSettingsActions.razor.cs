namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private Task DeleteCachedAudioItemAsync(string itemId)
    {
        return ApplySettingsStateChangeAsync(() => _state.RemoveCachedAudioItem(itemId));
    }

    private Task ClearCachedAudioItemsAsync()
    {
        return ApplySettingsStateChangeAsync(_state.ClearCachedAudioItems);
    }
}
