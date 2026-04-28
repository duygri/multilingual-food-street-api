namespace NarrationApp.Server.Services;

public interface IQrWebPresenceTracker
{
    DateTime? GetLastSeenUtc(string deviceId);

    void Track(string deviceId, DateTime? seenAtUtc = null);
}
