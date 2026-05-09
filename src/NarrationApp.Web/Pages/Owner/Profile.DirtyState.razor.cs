using NarrationApp.Shared.DTOs.Owner;

namespace NarrationApp.Web.Pages.Owner;

public partial class Profile
{
    private bool HasProfileEditorChanges =>
        _profile is not null
        && _editor is not null
        && !ProfileEditorMatches(_editor, _profile);

    private bool IsProfileSaveDisabled => _isSavingProfile || !HasProfileEditorChanges;

    private string ProfileSaveButtonClass => IsProfileSaveDisabled
        ? "app-button app-button--save-disabled"
        : "app-button app-button--primary";

    private static bool ProfileEditorMatches(OwnerProfileEditModel editor, OwnerProfileDto profile)
    {
        return string.Equals(Normalize(editor.FullName), Normalize(profile.FullName), StringComparison.Ordinal)
            && string.Equals(Normalize(editor.Phone), Normalize(profile.Phone), StringComparison.Ordinal)
            && string.Equals(Normalize(editor.ManagedArea), Normalize(profile.ManagedArea), StringComparison.Ordinal)
            && string.Equals(Normalize(editor.PreferredLanguage), Normalize(profile.PreferredLanguage), StringComparison.Ordinal);
    }

    private static string Normalize(string? value) => value ?? string.Empty;
}
