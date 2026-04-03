using FoodStreet.Server.Constants;
using Microsoft.AspNetCore.WebUtilities;

namespace FoodStreet.Server.Links;

public static class PoiDeepLinkBuilder
{
    public static string Build(
        string baseUrl,
        int locationId,
        string source = PlaySources.Manual,
        int? tourId = null,
        int? stopOrder = null,
        string? sessionId = null)
    {
        var uri = $"{baseUrl.TrimEnd('/')}/poi/{locationId}";

        var query = new Dictionary<string, string?>
        {
            ["entry"] = PlaySources.Normalize(source)
        };

        if (tourId.HasValue)
        {
            query["tourId"] = tourId.Value.ToString();
        }

        if (stopOrder.HasValue)
        {
            query["stop"] = stopOrder.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            query["session"] = sessionId;
        }

        return QueryHelpers.AddQueryString(uri, query!);
    }
}
