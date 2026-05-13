namespace NarrationApp.Server.Services;

public interface IQrWebPresenceTracker
{
    DateTime? GetLastSeenUtc(string deviceId);

    IReadOnlyCollection<QrWebPresenceSnapshot> GetAll();

    void Track(string deviceId, DateTime? seenAtUtc = null);
}

public sealed record QrWebPresenceSnapshot(
    string DeviceId,
    DateTime LastSeenAtUtc);
