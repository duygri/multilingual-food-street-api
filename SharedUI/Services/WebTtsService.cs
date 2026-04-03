using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace FoodStreet.Client.Services
{
    public class WebTtsService : ITtsService
    {
        private readonly IJSRuntime _js;

        public WebTtsService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task PlayTextAsync(string text, string? languageCode = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            try
            {
                await _js.InvokeVoidAsync("TtsService.playText", text, languageCode);
            }
            catch { }
        }

        public async Task PlayAudioFileAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                await _js.InvokeVoidAsync("TtsService.playAudioFile", url);
            }
            catch { }
        }

        public async Task StopAllAsync()
        {
            try
            {
                await _js.InvokeVoidAsync("TtsService.stopAll");
            }
            catch { }
        }
    }
}
