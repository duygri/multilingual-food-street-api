using Microsoft.AspNetCore.Mvc;
using NarrationApp.Server.Controllers;
using NarrationApp.Server.Services;

namespace NarrationApp.Server.Tests.Controllers;

public sealed class VisitorPresenceControllerTests
{
    [Fact]
    public void Offline_clears_existing_mobile_presence()
    {
        var tracker = new InMemoryVisitorMobilePresenceTracker();
        var controller = new VisitorPresenceController(tracker);
        var deviceId = "android-device-samsung-sm-a155f-0caf0b";

        controller.Heartbeat(new VisitorPresenceController.VisitorPresenceHeartbeatRequest
        {
            DeviceId = deviceId,
            Source = "mobile-presence",
            PreferredLanguage = "vi-VN"
        });

        Assert.NotNull(tracker.Get(deviceId));

        var result = controller.Offline(new VisitorPresenceController.VisitorPresenceOfflineRequest
        {
            DeviceId = deviceId
        });

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Null(tracker.Get(deviceId));
    }
}
