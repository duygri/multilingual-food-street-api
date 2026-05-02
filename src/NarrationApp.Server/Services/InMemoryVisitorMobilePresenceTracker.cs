using System.Collections.Concurrent;

namespace NarrationApp.Server.Services;

public sealed class InMemoryVisitorMobilePresenceTracker : IVisitorMobilePresenceTracker
{
    private readonly ConcurrentDictionary<string, VisitorMobilePresenceSnapshot> _presenceByDeviceId = new(StringComparer.OrdinalIgnoreCase);

    public VisitorMobilePresenceSnapshot? Get(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return null;
        }

        return _presenceByDeviceId.TryGetValue(deviceId.Trim(), out var value) ? value : null;
    }

    public IReadOnlyCollection<VisitorMobilePresenceSnapshot> GetAll()
    {
        return _presenceByDeviceId.Values.ToArray();
    }

    public void Track(string deviceId, string source, string? preferredLanguage, DateTime? seenAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return;
        }

        var normalizedDeviceId = deviceId.Trim();
        var normalizedSource = string.IsNullOrWhiteSpace(source) ? "mobile-presence" : source.Trim();
        var normalizedLanguage = string.IsNullOrWhiteSpace(preferredLanguage) ? string.Empty : preferredLanguage.Trim();

        _presenceByDeviceId[normalizedDeviceId] = new VisitorMobilePresenceSnapshot(
            normalizedDeviceId,
            normalizedSource,
            normalizedLanguage,
            seenAtUtc ?? DateTime.UtcNow);
    }
}
