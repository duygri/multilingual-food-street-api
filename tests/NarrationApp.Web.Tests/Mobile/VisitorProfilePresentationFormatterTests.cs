using NarrationApp.Mobile.Features.Home;

namespace NarrationApp.Web.Tests.Mobile;

public sealed class VisitorProfilePresentationFormatterTests
{
    [Fact]
    public void Display_values_fall_back_to_local_visitor_defaults()
    {
        var displayName = VisitorProfilePresentationFormatter.GetDisplayName("   ");
        var displayEmail = VisitorProfilePresentationFormatter.GetDisplayEmail(null);
        var modeLabel = VisitorProfilePresentationFormatter.GetModeLabel();

        Assert.Equal("Khách tham quan", displayName);
        Assert.Equal("Chưa đặt email liên hệ", displayEmail);
        Assert.Equal("Cục bộ trên thiết bị • không cần đăng nhập", modeLabel);
    }

    [Theory]
    [InlineData("", "KQ")]
    [InlineData("Lan", "LA")]
    [InlineData("Nguyen Thi Lan", "NL")]
    public void Initials_are_derived_from_display_name(string name, string expectedInitials)
    {
        var initials = VisitorProfilePresentationFormatter.GetInitials(name);

        Assert.Equal(expectedInitials, initials);
    }

    [Fact]
    public void Draft_values_are_normalized_before_save()
    {
        var normalizedName = VisitorProfilePresentationFormatter.NormalizeDraftName("  ");
        var normalizedEmail = VisitorProfilePresentationFormatter.NormalizeDraftEmail("  demo@narration.app  ");

        Assert.Equal("Khách tham quan", normalizedName);
        Assert.Equal("demo@narration.app", normalizedEmail);
    }
}
