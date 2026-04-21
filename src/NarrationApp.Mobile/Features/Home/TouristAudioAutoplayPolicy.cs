namespace NarrationApp.Mobile.Features.Home;

public static class TouristAudioAutoplayPolicy
{
    public static bool ShouldAutoplay(
        bool autoPlayRequested,
        bool forceAutoPlay,
        string cuePoiId,
        string? activeProximityPoiId,
        string? lastAutoPlayedPoiId)
    {
        if (!autoPlayRequested)
        {
            return false;
        }

        if (string.Equals(lastAutoPlayedPoiId, cuePoiId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (forceAutoPlay)
        {
            return true;
        }

        return string.Equals(activeProximityPoiId, cuePoiId, StringComparison.OrdinalIgnoreCase);
    }
}
