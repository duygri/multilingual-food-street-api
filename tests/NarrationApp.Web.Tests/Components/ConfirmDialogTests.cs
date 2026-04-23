using Bunit;
using Microsoft.AspNetCore.Components;
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
    public void Does_not_render_dialog_markup_when_closed()
    {
        var cut = RenderComponent<ConfirmDialog>(parameters => parameters
            .Add(parameter => parameter.IsOpen, false)
            .Add(parameter => parameter.Title, "Hidden"));

        Assert.DoesNotContain("confirm-dialog", cut.Markup);
        Assert.DoesNotContain("Hidden", cut.Markup);
    }
}
