namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private Task DeleteCachedAudioItemAsync(string itemId)
    {
        _state.RemoveCachedAudioItem(itemId);
        return Task.CompletedTask;
    }

    private Task ClearCachedAudioItemsAsync()
    {
        _state.ClearCachedAudioItems();
        return Task.CompletedTask;
    }
}
