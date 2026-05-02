namespace NarrationApp.Server.Services;

public interface IVisitorMobilePresenceTracker
{
    VisitorMobilePresenceSnapshot? Get(string deviceId);

    IReadOnlyCollection<VisitorMobilePresenceSnapshot> GetAll();

    void Track(string deviceId, string source, string? preferredLanguage, DateTime? seenAtUtc = null);
}

public sealed record VisitorMobilePresenceSnapshot(
    string DeviceId,
    string Source,
    string PreferredLanguage,
    DateTime LastSeenAtUtc);
