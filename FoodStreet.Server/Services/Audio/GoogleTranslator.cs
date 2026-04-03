using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using FoodStreet.Server.Services.GoogleCloud;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PROJECT_C_.Configuration;

namespace FoodStreet.Server.Services.Audio
{
    public class GoogleTranslator
    {
        private readonly HttpClient _httpClient;
        private readonly GoogleCloudOptions _options;
        private readonly IGoogleCloudAccessTokenProvider _accessTokenProvider;
        private readonly ILogger<GoogleTranslator> _logger;
        private int _authModeLogged;

        public GoogleTranslator(
            HttpClient httpClient,
            IOptions<GoogleCloudOptions> options,
            IGoogleCloudAccessTokenProvider accessTokenProvider,
            ILogger<GoogleTranslator> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _accessTokenProvider = accessTokenProvider;
            _logger = logger;
        }

        public async Task<string> TranslateTextAsync(string sourceText, string targetLanguage)
        {
            var normalizedTarget = NormalizeTargetLanguage(targetLanguage);

            if (string.IsNullOrWhiteSpace(sourceText) || normalizedTarget == "vi")
                return sourceText;

            try
            {
                LogAuthModeOnce();

                if (!string.IsNullOrWhiteSpace(_options.ApiKey))
                {
                    return await TranslateWithApiKeyAsync(sourceText, normalizedTarget);
                }

                return await TranslateWithAdcAsync(sourceText, normalizedTarget);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GoogleTranslator] Lỗi khi dịch văn bản sang {Lang}", targetLanguage);
            }

            return sourceText; 
        }

        private void LogAuthModeOnce()
        {
            if (Interlocked.Exchange(ref _authModeLogged, 1) != 0)
            {
                return;
            }

            _logger.LogInformation(
                "[GoogleTranslator] Runtime auth mode: {AuthMode}",
                _accessTokenProvider.GetAuthMode());
        }

        private static string NormalizeTargetLanguage(string targetLanguage)
        {
            var normalized = targetLanguage?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
                return "vi";

            return normalized switch
            {
                "vi-vn" => "vi",
                "en-us" => "en",
                "ja-jp" => "ja",
                "ko-kr" => "ko",
                "zh-cn" => "zh-CN",
                _ => normalized
            };
        }

        private async Task<string> TranslateWithApiKeyAsync(string sourceText, string normalizedTarget)
        {
            string url = $"https://translation.googleapis.com/language/translate/v2?key={_options.ApiKey}";

            var requestBody = new
            {
                q = sourceText,
                target = normalizedTarget
            };

            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);

            var translations = document.RootElement
                .GetProperty("data")
                .GetProperty("translations");

            if (translations.GetArrayLength() > 0)
            {
                return translations[0].GetProperty("translatedText").GetString() ?? sourceText;
            }

            return sourceText;
        }

        private async Task<string> TranslateWithAdcAsync(string sourceText, string normalizedTarget)
        {
            var accessToken = await _accessTokenProvider.GetAccessTokenAsync();
            var projectId = await _accessTokenProvider.GetProjectIdAsync();
            var url = $"https://translation.googleapis.com/v3/projects/{projectId}/locations/global:translateText";

            var requestBody = new
            {
                contents = new[] { sourceText },
                mimeType = "text/plain",
                targetLanguageCode = normalizedTarget
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(requestBody)
            };
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);

            var translations = document.RootElement.GetProperty("translations");

            if (translations.GetArrayLength() > 0)
            {
                return translations[0].GetProperty("translatedText").GetString() ?? sourceText;
            }

            return sourceText;
        }
    }
}
