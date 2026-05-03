namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private async Task DeleteCachedAudioItemAsync(string itemId)
    {
        await ApplySettingsStateChangeAsync(async () =>
        {
            await OfflineCacheStore.DeleteCachedAudioAsync(itemId);
            await RefreshCachedAudioItemsAsync();
        });
    }

    private async Task ClearCachedAudioItemsAsync()
    {
        await ApplySettingsStateChangeAsync(async () =>
        {
            await OfflineCacheStore.ClearCachedAudioAsync();
            _state.ClearCachedAudioItems();
        });
    }

    private async Task RefreshCachedAudioItemsAsync()
    {
        var cachedAudioItems = await OfflineCacheStore.ListCachedAudioAsync();
        _state.SetCachedAudioItems(cachedAudioItems);
    }
}
