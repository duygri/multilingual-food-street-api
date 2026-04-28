using System.Collections.Concurrent;

namespace NarrationApp.Server.Services;

public sealed class InMemoryQrWebPresenceTracker : IQrWebPresenceTracker
{
    private readonly ConcurrentDictionary<string, DateTime> _lastSeenByDeviceId = new(StringComparer.OrdinalIgnoreCase);

    public DateTime? GetLastSeenUtc(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return null;
        }

        var normalizedDeviceId = deviceId.Trim();
        return _lastSeenByDeviceId.TryGetValue(normalizedDeviceId, out var value) ? value : null;
    }

    public void Track(string deviceId, DateTime? seenAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return;
        }

        _lastSeenByDeviceId[deviceId.Trim()] = seenAtUtc ?? DateTime.UtcNow;
    }
}
