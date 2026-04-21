using Bunit;
using Microsoft.AspNetCore.Components;
using NarrationApp.SharedUI.Components;
using NarrationApp.SharedUI.Models;

namespace NarrationApp.Web.Tests.Components;

public sealed class DashboardSurfaceComponentTests : TestContext
{
    [Fact]
    public void StatusBadge_renders_label_and_tone_class()
    {
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(parameter => parameter.Label, "Ready")
            .Add(parameter => parameter.Tone, StatusTone.Good));

        Assert.Contains("Ready", cut.Markup);
        Assert.Contains("status-badge--good", cut.Markup);
    }

    [Fact]
    public void StatTile_renders_label_value_and_hint()
    {
        var cut = RenderComponent<StatTile>(parameters => parameters
            .Add(parameter => parameter.Label, "Audio assets")
            .Add(parameter => parameter.Value, "128")
            .Add(parameter => parameter.Hint, "Kho audio sẵn sàng"));

        Assert.Contains("Audio assets", cut.Markup);
        Assert.Contains("128", cut.Markup);
        Assert.Contains("Kho audio sẵn sàng", cut.Markup);
    }

    [Fact]
    public void PanelShell_renders_header_badge_actions_and_body()
    {
        var cut = RenderComponent<PanelShell>(parameters => parameters
            .Add(parameter => parameter.Eyebrow, "Signal floor")
            .Add(parameter => parameter.Title, "Sàn tín hiệu")
            .Add(parameter => parameter.Description, "Theo dõi moderation, audio và queue vận hành.")
            .Add(parameter => parameter.BadgeLabel, "4 pending")
            .Add(parameter => parameter.BadgeTone, StatusTone.Warn)
            .Add(parameter => parameter.HeaderActions, (RenderFragment)(builder =>
            {
                builder.AddMarkupContent(0, "<button type=\"button\">Lọc dữ liệu</button>");
            }))
            .AddChildContent("<div>Body content</div>"));

        Assert.Contains("Signal floor", cut.Markup);
        Assert.Contains("Sàn tín hiệu", cut.Markup);
        Assert.Contains("4 pending", cut.Markup);
        Assert.Contains("Lọc dữ liệu", cut.Markup);
        Assert.Contains("Body content", cut.Markup);
    }

    [Fact]
    public void DataTableShell_renders_table_content_when_not_empty()
    {
        var cut = RenderComponent<DataTableShell>(parameters => parameters
            .Add(parameter => parameter.Eyebrow, "POI heat")
            .Add(parameter => parameter.Title, "POI heat lane")
            .Add(parameter => parameter.BadgeLabel, "6 hotspot")
            .Add(parameter => parameter.TableContent, (RenderFragment)(builder =>
            {
                builder.AddMarkupContent(0, """
                    <thead>
                        <tr><th>POI</th><th>Visits</th></tr>
                    </thead>
                    <tbody>
                        <tr><td>Bún mắm</td><td>640</td></tr>
                    </tbody>
                    """);
            })));

        Assert.Contains("POI heat lane", cut.Markup);
        Assert.Contains("<table", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Bún mắm", cut.Markup);
    }

    [Fact]
    public void DataTableShell_renders_empty_state_when_marked_empty()
    {
        var cut = RenderComponent<DataTableShell>(parameters => parameters
            .Add(parameter => parameter.Eyebrow, "POI heat")
            .Add(parameter => parameter.Title, "POI heat lane")
            .Add(parameter => parameter.IsEmpty, true)
            .Add(parameter => parameter.EmptyStateContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<EmptyStateBlock>(0);
                builder.AddAttribute(1, nameof(EmptyStateBlock.Eyebrow), "Heatmap");
                builder.AddAttribute(2, nameof(EmptyStateBlock.Title), "Chưa có tín hiệu");
                builder.AddAttribute(3, nameof(EmptyStateBlock.Message), "Hệ thống chưa ghi nhận heatmap.");
                builder.CloseComponent();
            })));

        Assert.Contains("Chưa có tín hiệu", cut.Markup);
        Assert.Contains("Hệ thống chưa ghi nhận heatmap.", cut.Markup);
        Assert.DoesNotContain("<table", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyStateBlock_renders_compact_state_copy()
    {
        var cut = RenderComponent<EmptyStateBlock>(parameters => parameters
            .Add(parameter => parameter.Eyebrow, "Moderation")
            .Add(parameter => parameter.Title, "Queue đã trống")
            .Add(parameter => parameter.Message, "Hiện chưa có yêu cầu cần admin xử lý."));

        Assert.Contains("Moderation", cut.Markup);
        Assert.Contains("Queue đã trống", cut.Markup);
        Assert.Contains("Hiện chưa có yêu cầu cần admin xử lý.", cut.Markup);
    }
}
