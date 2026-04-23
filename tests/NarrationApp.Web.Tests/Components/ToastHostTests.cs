using Bunit;
using Microsoft.AspNetCore.Components;
using NarrationApp.SharedUI.Components;

namespace NarrationApp.Web.Tests.Components;

public sealed class ToastHostTests : TestContext
{
    [Fact]
    public void Renders_toast_host_with_items_and_live_region_hooks()
    {
        var cut = RenderComponent<ToastHost>(parameters => parameters
            .Add(parameter => parameter.Toasts,
            [
                new ToastHost.ToastItem("profile-saved", "Đã lưu hồ sơ", "Thông tin owner đã được cập nhật.", "success"),
                new ToastHost.ToastItem("moderation-error", "Chưa gửi được", "Vui lòng thử lại sau.", "danger")
            ]));

        var host = cut.Find(".toast-host");

        Assert.Equal("polite", host.GetAttribute("aria-live"));
        Assert.Contains("Đã lưu hồ sơ", cut.Markup);
        Assert.Contains("Thông tin owner đã được cập nhật.", cut.Markup);
        Assert.Contains("toast-host__toast--success", cut.Markup);
        Assert.Contains("toast-host__toast--danger", cut.Markup);
        Assert.Equal(2, cut.FindAll("[data-toast-id]").Count);
    }

    [Fact]
    public void Invokes_dismiss_callback_with_toast_id()
    {
        string? dismissedId = null;

        var cut = RenderComponent<ToastHost>(parameters => parameters
            .Add(parameter => parameter.Toasts,
            [
                new ToastHost.ToastItem("profile-saved", "Đã lưu hồ sơ", "Thông tin owner đã được cập nhật.")
            ])
            .Add(parameter => parameter.OnDismiss, EventCallback.Factory.Create<string>(this, id => dismissedId = id)));

        cut.Find("button[data-action='dismiss-toast']").Click();

        Assert.Equal("profile-saved", dismissedId);
    }

    [Fact]
    public void Renders_custom_child_content_for_service_driven_hosts()
    {
        var cut = RenderComponent<ToastHost>(parameters => parameters
            .Add(parameter => parameter.ChildContent, (RenderFragment)(builder => builder.AddMarkupContent(0, "<article data-testid='custom-toast'>Queued toast</article>"))));

        Assert.Contains("Queued toast", cut.Markup);
        Assert.NotNull(cut.Find("[data-testid='custom-toast']"));
    }
}
