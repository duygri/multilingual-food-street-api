using NarrationApp.Shared.DTOs.Owner;

namespace NarrationApp.Web.Pages.Owner;

public partial class Profile
{
    private sealed class OwnerProfileEditModel
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? ManagedArea { get; set; }
        public string PreferredLanguage { get; set; } = "vi";

        public static OwnerProfileEditModel FromProfile(OwnerProfileDto profile) => new()
        {
            FullName = profile.FullName,
            Phone = profile.Phone,
            ManagedArea = profile.ManagedArea,
            PreferredLanguage = profile.PreferredLanguage
        };

        public UpdateOwnerProfileRequest ToRequest() => new()
        {
            FullName = FullName,
            Phone = Phone,
            ManagedArea = ManagedArea,
            PreferredLanguage = PreferredLanguage
        };
    }

    private sealed class PasswordEditModel
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
