using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FoodStreet.Client.Services;
using Microsoft.Maui.Media;
using Plugin.Maui.Audio;

namespace FoodStreet.Mobile.Services
{
    public class NativeTtsService : ITtsService
    {
        private CancellationTokenSource? _ttsCts;
        private readonly IAudioManager _audioManager;
        private readonly HttpClient _httpClient;
        private IAudioPlayer? _audioPlayer;

        public NativeTtsService(IAudioManager audioManager)
        {
            _audioManager = audioManager;
            _httpClient = new HttpClient();
        }

        public async Task PlayTextAsync(string text, string? languageCode = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            try
            {
                // Cancel any ongoing speech first
                _ttsCts?.Cancel();
                _ttsCts?.Dispose();
                _ttsCts = new CancellationTokenSource();

                // Dừng audio player nếu đang phát
                _audioPlayer?.Stop();
                
                var options = new SpeechOptions { Volume = 1.0f };
                var locale = await ResolveLocaleAsync(languageCode);
                if (locale != null)
                {
                    options.Locale = locale;
                }

                await TextToSpeech.Default.SpeakAsync(text, options, _ttsCts.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[TTS] Error: {ex.Message}");
            }
        }

        public async Task PlayAudioFileAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                // Download file as Stream
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                var stream = await response.Content.ReadAsStreamAsync();

                _audioPlayer?.Dispose();
                _audioPlayer = _audioManager.CreatePlayer(stream);
                _audioPlayer.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TTS] PlayAudioFileAsync error: {ex.Message}");
                // Fallback: notify user audibly that audio is loading error or fallback to TTS
                await PlayTextAsync("Đã kích hoạt tệp âm thanh.");
            }
        }

        // FIX Bug 5: actually cancel current TTS and Audio
        public Task StopAllAsync()
        {
            try
            {
                _ttsCts?.Cancel();
                _ttsCts?.Dispose();
                _ttsCts = null;

                _audioPlayer?.Stop();
                _audioPlayer?.Dispose();
                _audioPlayer = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TTS] StopAll error: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        private static async Task<Locale?> ResolveLocaleAsync(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return null;
            }

            try
            {
                var normalized = NormalizeLanguageCode(languageCode);
                var locales = await TextToSpeech.Default.GetLocalesAsync();

                var exact = locales.FirstOrDefault(locale =>
                    string.Equals(locale.Language, normalized, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals($"{locale.Language}-{locale.Country}", normalized, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals($"{locale.Language}_{locale.Country}", normalized, StringComparison.OrdinalIgnoreCase));

                if (exact != null)
                {
                    return exact;
                }

                var languagePrefix = normalized.Split('-', '_')[0];
                return locales.FirstOrDefault(locale =>
                    string.Equals(locale.Language, languagePrefix, StringComparison.OrdinalIgnoreCase) ||
                    locale.Language.StartsWith(languagePrefix, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeLanguageCode(string languageCode)
        {
            return languageCode.Split(',')[0].Trim();
        }
    }
}

