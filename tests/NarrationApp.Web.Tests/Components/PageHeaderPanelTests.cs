using Bunit;
using NarrationApp.SharedUI.Components;
using NarrationApp.SharedUI.Models;

namespace NarrationApp.Web.Tests.Components;

public sealed class PageHeaderPanelTests : TestContext
{
    [Fact]
    public void Renders_heading_description_meta_items_and_action()
    {
        var cut = RenderComponent<PageHeaderPanel>(parameters => parameters
            .Add(parameter => parameter.Eyebrow, "Admin control room")
            .Add(parameter => parameter.Title, "Điều phối moderation")
            .Add(parameter => parameter.Description, "Theo dõi moderation, audio, users và trạng thái hệ thống trong cùng một bề mặt vận hành.")
            .Add(parameter => parameter.PrimaryAction, new HeroAction
            {
                Label = "Mở moderation queue",
                Href = "/admin/moderation-queue"
            })
            .Add(parameter => parameter.MetaItems, ["Queue live", "SignalR", "PostgreSQL"]));

        Assert.Contains("Admin control room", cut.Markup);
        Assert.Contains("Điều phối moderation", cut.Markup);
        Assert.Contains("Mở moderation queue", cut.Markup);
        Assert.Contains("Queue live", cut.Markup);
        Assert.Contains("SignalR", cut.Markup);
    }
}
