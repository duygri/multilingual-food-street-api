using Bunit;
using NarrationApp.SharedUI.Components;
using NarrationApp.SharedUI.Models;

namespace NarrationApp.Web.Tests.Components;

public sealed class SystemStatusRailTests : TestContext
{
    [Fact]
    public void Renders_system_status_items()
    {
        var cut = RenderComponent<SystemStatusRail>(parameters => parameters
            .Add(parameter => parameter.Label, "System Status")
            .Add(parameter => parameter.Items,
            [
                new SystemStatusItem { Label = "API", Value = "Operational", Tone = StatusTone.Good },
                new SystemStatusItem { Label = "DB", Value = "Connected", Tone = StatusTone.Info },
                new SystemStatusItem { Label = "SignalR", Value = "Live", Tone = StatusTone.Good }
            ]));

        Assert.Contains("System Status", cut.Markup);
        Assert.Contains("Operational", cut.Markup);
        Assert.Contains("Connected", cut.Markup);
        Assert.Contains("Live", cut.Markup);
    }
}
