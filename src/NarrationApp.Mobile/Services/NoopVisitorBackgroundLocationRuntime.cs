using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Mobile.Services;

public sealed class NoopVisitorBackgroundLocationRuntime : IVisitorBackgroundLocationRuntime
{
    public Task<VisitorBackgroundTrackingStatus> ApplyAsync(
        VisitorBackgroundTrackingRequest request,
        CancellationToken cancellationToken = default)
    {
        var status = request.IsEnabled
            ? new VisitorBackgroundTrackingStatus(
                IsSupported: false,
                IsRunning: false,
                HasBackgroundPermission: false,
                StatusLabel: "Background tracking chưa hỗ trợ trên nền tảng này")
            : new VisitorBackgroundTrackingStatus(
                IsSupported: false,
                IsRunning: false,
                HasBackgroundPermission: false,
                StatusLabel: "Background tracking đang tắt");

        return Task.FromResult(status);
    }
}
