using NarrationApp.Shared.Visitor;

namespace NarrationApp.Mobile.Features.Home;

public sealed partial class VisitorShellState
{
    public void SetAudioAutoPlayEnabled(bool isEnabled)
    {
        AudioPreferences = AudioPreferences with { AutoPlayEnabled = isEnabled };
    }

    public void SetAudioSpokenAnnouncementsEnabled(bool isEnabled)
    {
        AudioPreferences = AudioPreferences with { SpokenAnnouncementsEnabled = isEnabled };
    }

    public void SetAudioAutoAdvanceEnabled(bool isEnabled)
    {
        AudioPreferences = AudioPreferences with { AutoAdvanceEnabled = isEnabled };
    }

    public void SetAudioSourcePreference(VisitorAudioSourcePreference preference)
    {
        AudioPreferences = AudioPreferences with { SourcePreference = preference };
    }

    public void SetAudioPlaybackSpeed(double speed)
    {
        AudioPreferences = AudioPreferences with { DefaultPlaybackSpeed = speed };
    }

    public void SetGpsBackgroundTrackingEnabled(bool isEnabled)
    {
        GpsPreferences = GpsPreferences with { BackgroundTrackingEnabled = isEnabled };
    }

    public void ApplyBackgroundTrackingStatus(VisitorBackgroundTrackingStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        GpsPreferences = GpsPreferences with
        {
            StatusLabel = status.StatusLabel
        };
    }

    public void SetGpsAutoFocusEnabled(bool isEnabled)
    {
        GpsPreferences = GpsPreferences with { AutoFocusEnabled = isEnabled };
    }

    public void SetGpsAccuracyMode(VisitorGpsAccuracyMode mode)
    {
        var batteryLabel = mode switch
        {
            VisitorGpsAccuracyMode.High => "High accuracy • pin giảm nhanh hơn",
            VisitorGpsAccuracyMode.BatterySaver => "Battery saver • giảm tần suất GPS",
            _ => "Adaptive mode • tiết kiệm pin"
        };

        GpsPreferences = GpsPreferences with
        {
            AccuracyMode = mode,
            BatteryLabel = batteryLabel
        };
    }

    public void RemoveCachedAudioItem(string itemId)
    {
        var index = _cachedAudioItems.FindIndex(item => item.Id == itemId);
        if (index >= 0)
        {
            _cachedAudioItems.RemoveAt(index);
        }
    }

    public void ClearCachedAudioItems()
    {
        _cachedAudioItems.Clear();
    }

    public void SetCachedAudioItems(IReadOnlyList<VisitorCachedAudioItem> items)
    {
        _cachedAudioItems.Clear();
        _cachedAudioItems.AddRange(items);
    }

    private void SeedSettingsDemoData()
    {
        if (_pois.Count == 0)
        {
            return;
        }

        _cachedAudioItems.Clear();
        _cachedAudioItems.AddRange(VisitorSettingsDemoData.CreateCachedAudioItems());

        _listeningHistoryDays.Clear();
        _listeningHistoryDays.AddRange(VisitorSettingsDemoData.CreateListeningHistoryDays());
    }
}
