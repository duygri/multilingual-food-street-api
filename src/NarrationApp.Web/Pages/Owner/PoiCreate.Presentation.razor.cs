using Microsoft.AspNetCore.Components.Forms;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Web.Pages.Owner;

public partial class PoiCreate
{
    private int? ParseCategoryId()
    {
        return int.TryParse(_categoryId, out var parsedCategoryId)
            ? parsedCategoryId
            : null;
    }

    private static string GetNarrationLabel(NarrationMode mode) => mode switch
    {
        NarrationMode.TtsOnly => "Chỉ TTS",
        NarrationMode.RecordedOnly => "Chỉ audio nguồn",
        NarrationMode.Both => "Kết hợp",
        _ => mode.ToString()
    };

    private static string GetFileMetaLabel(IBrowserFile file)
    {
        return $"{file.ContentType} | {Math.Max(file.Size / 1024d, 1):0.#} KB";
    }
}
