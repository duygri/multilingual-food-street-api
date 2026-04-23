using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NarrationApp.SharedUI.Components;

namespace NarrationApp.Web.Tests.Components;

public sealed class ConfirmDialogTests : TestContext
{
    [Fact]
    public void Renders_accessible_confirm_dialog_when_open()
    {
        var cut = RenderComponent<ConfirmDialog>(parameters => parameters
            .Add(parameter => parameter.IsOpen, true)
            .Add(parameter => parameter.Title, "Gửi duyệt POI?")
            .Add(parameter => parameter.Message, "POI sẽ được chuyển sang hàng chờ admin kiểm duyệt.")
            .Add(parameter => parameter.ConfirmText, "Gửi duyệt")
            .Add(parameter => parameter.CancelText, "Hủy")
            .Add(parameter => parameter.ChildContent, (RenderFragment)(builder => builder.AddMarkupContent(0, "<p>Kiểm tra lại nội dung trước khi gửi.</p>"))));

        var dialog = cut.Find(".confirm-dialog");

        Assert.Equal("dialog", dialog.GetAttribute("role"));
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        Assert.Equal("-1", dialog.GetAttribute("tabindex"));
        Assert.Equal("confirm-dialog", dialog.GetAttribute("data-focus-target"));
        Assert.Equal("autofocus", cut.Find("button[data-action='confirm']").GetAttribute("autofocus"));
        Assert.Contains("Gửi duyệt POI?", cut.Markup);
        Assert.Contains("POI sẽ được chuyển sang hàng chờ admin kiểm duyệt.", cut.Markup);
        Assert.Contains("Kiểm tra lại nội dung trước khi gửi.", cut.Markup);
        Assert.Equal("Gửi duyệt", cut.Find("button[data-action='confirm']").TextContent.Trim());
        Assert.Equal("Hủy", cut.Find("button[data-action='cancel']").TextContent.Trim());
    }

    [Fact]
    public void Invokes_confirm_and_cancel_callbacks_from_rendered_buttons()
    {
        var confirmed = false;
        var canceled = false;

        var cut = RenderComponent<ConfirmDialog>(parameters => parameters
            .Add(parameter => parameter.IsOpen, true)
            .Add(parameter => parameter.Title, "Xóa POI?")
            .Add(parameter => parameter.OnConfirm, EventCallback.Factory.Create(this, () => confirmed = true))
            .Add(parameter => parameter.OnCancel, EventCallback.Factory.Create(this, () => canceled = true)));

        cut.Find("button[data-action='confirm']").Click();
        cut.Find("button[data-action='cancel']").Click();

        Assert.True(confirmed);
        Assert.True(canceled);
    }

    [Fact]
    public void Pressing_escape_invokes_cancel_callback()
    {
        var canceled = false;

        var cut = RenderComponent<ConfirmDialog>(parameters => parameters
            .Add(parameter => parameter.IsOpen, true)
            .Add(parameter => parameter.Title, "Xóa POI?")
            .Add(parameter => parameter.OnCancel, EventCallback.Factory.Create(this, () => canceled = true)));

        cut.Find(".confirm-dialog").TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "Escape" });

        Assert.True(canceled);
    }

    [Fact]
    public void Renders_focus_trap_sentinels_and_reports_wrap_targets()
    {
        var wrapTargets = new List<string>();

        var cut = RenderComponent<ConfirmDialog>(parameters => parameters
            .Add(parameter => parameter.IsOpen, true)
            .Add(parameter => parameter.Title, "Gửi duyệt POI?")
            .Add(parameter => parameter.OnFocusTrap, EventCallback.Factory.Create<string>(this, target => wrapTargets.Add(target))));

        var sentinels = cut.FindAll("[data-focus-sentinel]");

        Assert.Equal(2, sentinels.Count);
        Assert.Equal("start", sentinels[0].GetAttribute("data-focus-sentinel"));
        Assert.Equal("end", sentinels[1].GetAttribute("data-focus-sentinel"));
        Assert.All(sentinels, sentinel => Assert.Equal("0", sentinel.GetAttribute("tabindex")));

        sentinels[0].TriggerEvent("onfocus", new FocusEventArgs());
        sentinels[1].TriggerEvent("onfocus", new FocusEventArgs());

        Assert.Equal(["last", "first"], wrapTargets);
    }

    [Fact]
    public void Does_not_render_dialog_markup_when_closed()
    {
        var cut = RenderComponent<ConfirmDialog>(parameters => parameters
            .Add(parameter => parameter.IsOpen, false)
            .Add(parameter => parameter.Title, "Hidden"));

        Assert.DoesNotContain("confirm-dialog", cut.Markup);
        Assert.DoesNotContain("Hidden", cut.Markup);
    }
}
