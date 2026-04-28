namespace NarrationApp.Mobile.Features.Home;

public static class VisitorPendingDeepLinkStore
{
    private static readonly object SyncRoot = new();
    private static VisitorQrDeepLinkRequest? _pending;

    public static event Action? PendingChanged;

    public static void SetPendingUri(string? rawUri)
    {
        if (!VisitorQrDeepLinkParser.TryParse(rawUri, out var request) || request is null)
        {
            VisitorMobileDiagnostics.Log("PendingStore", $"Ignored rawUri={rawUri ?? "<null>"}");
            return;
        }

        lock (SyncRoot)
        {
            _pending = request;
        }

        VisitorMobileDiagnostics.Log("PendingStore", $"Stored code={request.Code} source={request.SourceUri}");
        PendingChanged?.Invoke();
    }

    public static VisitorQrDeepLinkRequest? Consume()
    {
        lock (SyncRoot)
        {
            var pending = _pending;
            _pending = null;
            VisitorMobileDiagnostics.Log(
                "PendingStore",
                pending is null
                    ? "Consume -> <null>"
                    : $"Consume -> code={pending.Code} source={pending.SourceUri}");
            return pending;
        }
    }

    public static void Clear()
    {
        lock (SyncRoot)
        {
            _pending = null;
        }

        VisitorMobileDiagnostics.Log("PendingStore", "Cleared pending request");
    }
}
