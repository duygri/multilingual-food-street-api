using NarrationApp.Shared.DTOs.Poi;

namespace NarrationApp.Web.Pages.Owner;

public partial class PoiDetail
{
    private bool HasPoiEditorChanges =>
        _poi is not null
        && _editor is not null
        && !PoiEditorMatches(_editor, _poi);

    private bool IsPoiSaveDisabled => _isSaving || !HasPoiEditorChanges;

    private string PoiSaveButtonClass => IsPoiSaveDisabled
        ? "app-button app-button--save-disabled"
        : "app-button app-button--primary";

    private static bool PoiEditorMatches(PoiEditModel editor, PoiDto poi)
    {
        return string.Equals(Normalize(editor.Name), Normalize(poi.Name), StringComparison.Ordinal)
            && string.Equals(Normalize(editor.Slug), Normalize(poi.Slug), StringComparison.Ordinal)
            && editor.Lat.Equals(poi.Lat)
            && editor.Lng.Equals(poi.Lng)
            && editor.Priority == poi.Priority
            && editor.CategoryId == poi.CategoryId
            && editor.NarrationMode == poi.NarrationMode
            && string.Equals(Normalize(editor.MapLink), Normalize(poi.MapLink), StringComparison.Ordinal)
            && string.Equals(Normalize(editor.Description), Normalize(poi.Description), StringComparison.Ordinal)
            && string.Equals(Normalize(editor.TtsScript), Normalize(poi.TtsScript), StringComparison.Ordinal);
    }

    private static string Normalize(string? value) => value ?? string.Empty;
}
