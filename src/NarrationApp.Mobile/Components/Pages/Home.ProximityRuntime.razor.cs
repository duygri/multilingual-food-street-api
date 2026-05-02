using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Components.Pages;

public partial class Home
{
    private static readonly TimeSpan AutoNarrationCooldownWindow = TimeSpan.FromMinutes(5);
    private const int ProximityPromotionDebounceSamples = 3;
    private readonly Dictionary<string, DateTimeOffset> _autoNarrationPlaybackLog = new(StringComparer.OrdinalIgnoreCase);
    private VisitorProximityQueueState _proximityQueueState = VisitorProximityQueueState.Empty;
    private string? _currentAutoNarrationPoiId;

    private VisitorProximityMatch? ResolveNextProximity(VisitorLocationSnapshot location)
    {
        var candidates = VisitorProximityEngine.EvaluateCandidates(location, _state.Pois);
        var previousState = _proximityQueueState;
        var decision = VisitorProximityQueueCoordinator.Evaluate(
            _proximityQueueState,
            candidates,
            hasNarrationLock: _isAutoPlayingFromProximity && _state.AudioPlaybackState == VisitorAudioPlaybackState.Playing,
            debounceSampleThreshold: ProximityPromotionDebounceSamples);

        _proximityQueueState = decision.State;
        TrackProximityQueueTransition(previousState, decision.State);
        return decision.State.ActiveMatch;
    }

    private DateTimeOffset? GetLastAutoNarrationPlayedAt(string poiId) =>
        _autoNarrationPlaybackLog.TryGetValue(poiId, out var playedAtUtc) ? playedAtUtc : null;

    private void RecordAutoNarrationPlayback(string poiId, DateTimeOffset playedAtUtc)
    {
        _autoNarrationPlaybackLog[poiId] = playedAtUtc;
        _currentAutoNarrationPoiId = poiId;
    }

    private void ClearCurrentAutoNarration()
    {
        _isAutoPlayingFromProximity = false;
        _currentAutoNarrationPoiId = null;
    }
}
