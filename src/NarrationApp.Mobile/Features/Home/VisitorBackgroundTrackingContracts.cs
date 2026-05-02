namespace NarrationApp.Mobile.Features.Home;

public interface IVisitorBackgroundLocationRuntime
{
    Task<VisitorBackgroundTrackingStatus> ApplyAsync(
        VisitorBackgroundTrackingRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record VisitorBackgroundTrackingRequest(
    bool IsEnabled,
    bool HasForegroundPermission,
    VisitorGpsAccuracyMode AccuracyMode);

public sealed record VisitorBackgroundTrackingStatus(
    bool IsSupported,
    bool IsRunning,
    bool HasBackgroundPermission,
    string StatusLabel);
