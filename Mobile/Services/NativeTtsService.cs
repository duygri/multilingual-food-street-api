using System;
using System.Threading.Tasks;
using FoodStreet.Client.Services;
using Microsoft.Maui.Media;

namespace FoodStreet.Mobile.Services
{
    public class NativeTtsService : ITtsService
    {
        public async Task PlayTextAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            try
            {
                await TextToSpeech.Default.SpeakAsync(text, new SpeechOptions { Volume = 1.0f });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TTS] Error: {ex.Message}");
            }
        }

        public Task PlayAudioFileAsync(string url)
        {
            Console.WriteLine($"[TTS] PlayAudioFileAsync called but not natively implemented without Plugin.Maui.Audio: {url}");
            return Task.CompletedTask;
        }

        public Task StopAllAsync()
        {
            return Task.CompletedTask;
        }
    }
}
