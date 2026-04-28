using NarrationApp.Shared.DTOs.Audio;
using NarrationApp.Web.Services;

namespace NarrationApp.Web.Pages.Admin;

public partial class TranslationReview
{
    private bool CanRetrySelectedAudio =>
        _selectedPoi is not null
        && string.Equals(GetSelectedAudioStateValue(), "failed", StringComparison.OrdinalIgnoreCase)
        && CanGenerateAudioForSelectedLanguage();

    private string GetSelectedAudioStateValue() => GetAudioStateValue(SelectedAudio);

    private string GetSelectedAudioHeadline() => GetSelectedAudioStateValue() switch
    {
        "ready" => $"Audio {_selectedLanguage.ToUpperInvariant()} đã sẵn sàng",
        "generating" => $"Audio {_selectedLanguage.ToUpperInvariant()} đang được xử lý",
        "failed" => $"Audio {_selectedLanguage.ToUpperInvariant()} bị lỗi generate",
        _ => $"Chưa có audio {_selectedLanguage.ToUpperInvariant()}"
    };

    private string GetSelectedAudioDescription()
    {
        if (_selectedPoi is null)
        {
            return "Chọn một POI và ngôn ngữ để xem trạng thái audio.";
        }

        return GetSelectedAudioStateValue() switch
        {
            "ready" => "Có thể phát hoặc dùng làm tín hiệu kiểm tra trước khi rà soát tiếp.",
            "generating" => "Hệ thống đang tạo lại file nền. Ma trận sẽ đổi trạng thái ngay khi có kết quả mới.",
            "failed" => "Bản dịch đã có nhưng lần generate gần nhất lỗi. Có thể retry ngay từ đây.",
            _ when string.Equals(_selectedLanguage, "vi", StringComparison.OrdinalIgnoreCase)
                => "Tiếng Việt dùng script nguồn của POI để tạo lại audio khi cần.",
            _ => "Ngôn ngữ này cần bản dịch lưu sẵn trước khi generate audio."
        };
    }

    private bool CanGenerateAudioForSelectedLanguage()
    {
        if (_selectedPoi is null)
        {
            return false;
        }

        if (string.Equals(_selectedLanguage, "vi", StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrWhiteSpace(_selectedPoi.TtsScript);
        }

        return _selectedTranslation is not null;
    }

    private async Task RetrySelectedAudioAsync()
    {
        if (_selectedPoi is null || !CanGenerateAudioForSelectedLanguage())
        {
            return;
        }

        try
        {
            AudioDto created = string.Equals(_selectedLanguage, "vi", StringComparison.OrdinalIgnoreCase)
                ? await AudioPortalService.GenerateTtsAsync(new TtsGenerateRequest
                {
                    PoiId = _selectedPoi.Id,
                    LanguageCode = "vi",
                    Script = _selectedPoi.TtsScript,
                    VoiceProfile = "standard"
                })
                : await AudioPortalService.GenerateFromTranslationAsync(new GenerateAudioFromTranslationRequest
                {
                    PoiId = _selectedPoi.Id,
                    LanguageCode = _selectedLanguage,
                    VoiceProfile = "standard"
                });

            UpsertAudio(created);
            _statusMessage = $"Đã retry audio {_selectedLanguage} cho {_selectedPoi.Name}.";
        }
        catch (ApiException exception)
        {
            _statusMessage = exception.Message;
        }
    }

    private async Task RefreshAudioForPoiAsync(int poiId) => await RefreshAudioForPoisAsync([poiId]);

    private async Task RefreshAudioForPoisAsync(IEnumerable<int> poiIds)
    {
        var ids = poiIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        var refreshedRows = await Task.WhenAll(ids.Select(async poiId =>
        {
            try
            {
                return (PoiId: poiId, AudioItems: await AudioPortalService.GetByPoiAsync(poiId));
            }
            catch (ApiException)
            {
                return (PoiId: poiId, AudioItems: GetAudioItems(poiId));
            }
        }));

        foreach (var row in refreshedRows)
        {
            _audioByPoi[row.PoiId] = row.AudioItems;
        }
    }

    private void UpsertAudio(AudioDto audio)
    {
        _audioByPoi[audio.PoiId] = GetAudioItems(audio.PoiId)
            .Where(item => item.Id != audio.Id)
            .Append(audio)
            .OrderByDescending(item => item.GeneratedAtUtc ?? DateTime.MinValue)
            .ThenByDescending(item => item.Id)
            .ToArray();
    }
}
