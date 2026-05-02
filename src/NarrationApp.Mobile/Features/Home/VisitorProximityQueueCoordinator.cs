namespace NarrationApp.Mobile.Features.Home;

public sealed record VisitorProximityQueueState(
    VisitorProximityMatch? ActiveMatch,
    VisitorProximityMatch? PendingMatch,
    int PendingStableSampleCount,
    VisitorProximityMatch? QueuedMatch)
{
    public static VisitorProximityQueueState Empty { get; } = new(null, null, 0, null);
}

public sealed record VisitorProximityQueueDecision(VisitorProximityQueueState State, bool ActiveChanged);

public static class VisitorProximityQueueCoordinator
{
    public static VisitorProximityQueueDecision Evaluate(
        VisitorProximityQueueState current,
        IReadOnlyList<VisitorProximityMatch> candidates,
        bool hasNarrationLock,
        int debounceSampleThreshold = 3)
    {
        if (candidates.Count == 0)
        {
            return new VisitorProximityQueueDecision(
                VisitorProximityQueueState.Empty,
                current.ActiveMatch is not null);
        }

        var topCandidate = candidates[0];
        if (current.ActiveMatch is null)
        {
            return new VisitorProximityQueueDecision(
                CreateState(topCandidate, null, 0, BestExcluding(candidates, topCandidate)),
                ActiveChanged: true);
        }

        var activeInRange = FindByPoiId(candidates, current.ActiveMatch.PoiId);
        if (activeInRange is null)
        {
            return new VisitorProximityQueueDecision(
                CreateState(topCandidate, null, 0, BestExcluding(candidates, topCandidate)),
                ActiveChanged: !SamePoi(current.ActiveMatch, topCandidate));
        }

        if (hasNarrationLock)
        {
            return new VisitorProximityQueueDecision(
                CreateState(activeInRange, null, 0, BestExcluding(candidates, activeInRange)),
                ActiveChanged: false);
        }

        if (SamePoi(activeInRange, topCandidate))
        {
            return new VisitorProximityQueueDecision(
                CreateState(activeInRange, null, 0, BestExcluding(candidates, activeInRange)),
                ActiveChanged: false);
        }

        var pendingCount = current.PendingMatch is not null && SamePoi(current.PendingMatch, topCandidate)
            ? current.PendingStableSampleCount + 1
            : 1;

        if (pendingCount >= debounceSampleThreshold)
        {
            return new VisitorProximityQueueDecision(
                CreateState(topCandidate, null, 0, BestExcluding(candidates, topCandidate)),
                ActiveChanged: true);
        }

        return new VisitorProximityQueueDecision(
            CreateState(activeInRange, topCandidate, pendingCount, topCandidate),
            ActiveChanged: false);
    }

    private static VisitorProximityQueueState CreateState(
        VisitorProximityMatch activeMatch,
        VisitorProximityMatch? pendingMatch,
        int pendingStableSampleCount,
        VisitorProximityMatch? queuedMatch) =>
        new(activeMatch, pendingMatch, pendingStableSampleCount, queuedMatch);

    private static VisitorProximityMatch? BestExcluding(
        IReadOnlyList<VisitorProximityMatch> candidates,
        VisitorProximityMatch excludedMatch) =>
        candidates.FirstOrDefault(candidate => !SamePoi(candidate, excludedMatch));

    private static VisitorProximityMatch? FindByPoiId(
        IReadOnlyList<VisitorProximityMatch> candidates,
        string poiId) =>
        candidates.FirstOrDefault(candidate => string.Equals(candidate.PoiId, poiId, StringComparison.OrdinalIgnoreCase));

    private static bool SamePoi(VisitorProximityMatch? left, VisitorProximityMatch? right) =>
        left is not null
        && right is not null
        && string.Equals(left.PoiId, right.PoiId, StringComparison.OrdinalIgnoreCase);
}
