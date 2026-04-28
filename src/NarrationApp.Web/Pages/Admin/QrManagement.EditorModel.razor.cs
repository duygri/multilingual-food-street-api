using NarrationApp.Shared.DTOs.QR;

namespace NarrationApp.Web.Pages.Admin;

public partial class QrManagement
{
    private sealed class QrEditorModel
    {
        public string TargetType { get; set; } = "poi";
        public int PoiId { get; set; }
        public string? LocationHint { get; set; }
        public string ExpiresAtLocal { get; set; } = string.Empty;

        public static QrEditorModel CreateDefault() => new();

        public CreateQrRequest ToRequest() => new()
        {
            TargetType = TargetType,
            TargetId = TargetType == "open_app" ? 0 : PoiId,
            LocationHint = string.IsNullOrWhiteSpace(LocationHint) ? null : LocationHint.Trim(),
            ExpiresAtUtc = DateTime.TryParse(ExpiresAtLocal, out var localDateTime)
                ? DateTime.SpecifyKind(localDateTime, DateTimeKind.Local).ToUniversalTime()
                : null
        };
    }
}
