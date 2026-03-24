using Microsoft.JSInterop;

namespace FoodStreet.Client.Services
{
    public record PlayableItem(int Id, string Title, string Subtitle, string? ImageUrl, string? AudioUrl, string? TtsScript);

    public class TourPlayerService : IAsyncDisposable
    {
        private readonly IJSRuntime _js;
        private DotNetObjectReference<TourPlayerService>? _dotNetRef;

        public bool IsVisible { get; private set; }
        public bool IsPlaying { get; private set; }
        public PlayableItem? CurrentItem { get; private set; }
        public bool IsTtsMode { get; private set; } // true if playing TTS, false if playing MP3

        public event Action? OnChange;

        public TourPlayerService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task InitializeAsync()
        {
            if (_dotNetRef == null)
            {
                _dotNetRef = DotNetObjectReference.Create(this);
                try {
                    await _js.InvokeVoidAsync("TourPlayer.init", _dotNetRef);
                } catch {
                    // Suppress if JS is not loaded yet
                }
            }
        }

        public async Task Play(PlayableItem item)
        {
            await InitializeAsync();

            CurrentItem = item;
            IsVisible = true;
            NotifyStateChanged();

            if (!string.IsNullOrEmpty(item.AudioUrl))
            {
                IsTtsMode = false;
                await _js.InvokeVoidAsync("TourPlayer.playAudio", item.AudioUrl);
            }
            else if (!string.IsNullOrEmpty(item.TtsScript))
            {
                IsTtsMode = true;
                await _js.InvokeVoidAsync("TourPlayer.playTts", item.TtsScript);
            }
            else
            {
                // Both empty
                IsVisible = false;
                NotifyStateChanged();
            }
        }

        public async Task PlayAudio(string title, string url, string? subtitle = null)
        {
            await Play(new PlayableItem(0, title, subtitle ?? "", null, url, null));
        }

        public async Task TogglePlay()
        {
            if (IsPlaying)
            {
                await _js.InvokeVoidAsync("TourPlayer.pause");
            }
            else
            {
                await _js.InvokeVoidAsync("TourPlayer.resume");
            }
        }

        public async Task Close()
        {
            await _js.InvokeVoidAsync("TourPlayer.stop");
            IsVisible = false;
            CurrentItem = null;
            IsPlaying = false;
            NotifyStateChanged();
        }

        [JSInvokable]
        public void OnPlayerStateChanged(bool isPlaying)
        {
            if (IsPlaying != isPlaying)
            {
                IsPlaying = isPlaying;
                NotifyStateChanged();
            }
        }

        [JSInvokable]
        public void OnPlayerEnded()
        {
            IsPlaying = false;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();

        public async ValueTask DisposeAsync()
        {
            _dotNetRef?.Dispose();
            if (_js != null)
            {
                try {
                    await _js.InvokeVoidAsync("TourPlayer.stop");
                } catch { } // Ignore during teardown
            }
        }
    }
}
