namespace NarrationApp.Mobile.Features.Home;

public static class VisitorAudioAutoplayPolicy
{
    public static bool ShouldAutoplay(
        bool autoPlayRequested,
        bool forceAutoPlay,
        string cuePoiId,
        string? activeProximityPoiId,
        string? lastAutoPlayedPoiId,
        DateTimeOffset? lastAutoPlayedAtUtc = null,
        DateTimeOffset? nowUtc = null,
        TimeSpan? cooldownWindow = null,
        bool hasAutoNarrationLock = false,
        string? currentAutoNarrationPoiId = null)
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
            return CanProceedWithAutoplay(
                cuePoiId,
                lastAutoPlayedAtUtc,
                nowUtc,
                cooldownWindow,
                hasAutoNarrationLock,
                currentAutoNarrationPoiId);
        }

        return string.Equals(activeProximityPoiId, cuePoiId, StringComparison.OrdinalIgnoreCase)
            && CanProceedWithAutoplay(
                cuePoiId,
                lastAutoPlayedAtUtc,
                nowUtc,
                cooldownWindow,
                hasAutoNarrationLock,
                currentAutoNarrationPoiId);
    }

    private static bool CanProceedWithAutoplay(
        string cuePoiId,
        DateTimeOffset? lastAutoPlayedAtUtc,
        DateTimeOffset? nowUtc,
        TimeSpan? cooldownWindow,
        bool hasAutoNarrationLock,
        string? currentAutoNarrationPoiId)
    {
        if (hasAutoNarrationLock
            && !string.Equals(currentAutoNarrationPoiId, cuePoiId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (lastAutoPlayedAtUtc is null || cooldownWindow is null)
        {
            return true;
        }

        var effectiveNowUtc = nowUtc ?? DateTimeOffset.UtcNow;
        return effectiveNowUtc - lastAutoPlayedAtUtc.Value >= cooldownWindow.Value;
    }
}
