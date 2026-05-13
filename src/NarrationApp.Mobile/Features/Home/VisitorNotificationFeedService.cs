using System.Globalization;
using System.Net.Http.Json;
using NarrationApp.Shared.DTOs.Common;
using NarrationApp.Shared.DTOs.Visitor;

namespace NarrationApp.Mobile.Features.Home;

public interface IVisitorNotificationFeedService
{
    Task<IReadOnlyList<VisitorNotification>> LoadAsync(
        VisitorNotificationFeedRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class VisitorNotificationFeedService(HttpClient httpClient) : IVisitorNotificationFeedService
{
    public async Task<IReadOnlyList<VisitorNotification>> LoadAsync(
        VisitorNotificationFeedRequest request,
        CancellationToken cancellationToken = default)
    {
        var endpoint = BuildEndpoint(request);
        var response = await httpClient.GetFromJsonAsync<ApiResponse<IReadOnlyList<VisitorNotificationDto>>>(
            endpoint,
            cancellationToken);

        return (response?.Data ?? [])
            .Select(item => new VisitorNotification(item.Title, item.Body, item.TimeLabel, IsLive: true))
            .ToArray();
    }

    private static string BuildEndpoint(VisitorNotificationFeedRequest request)
    {
        var queryParts = new List<string>
        {
            $"languageCode={Uri.EscapeDataString(request.LanguageCode)}",
            "take=12"
        };

        if (VisitorGeoMath.TryGetCoordinates(request.Location, out var latitude, out var longitude))
        {
            queryParts.Add($"lat={latitude.ToString(CultureInfo.InvariantCulture)}");
            queryParts.Add($"lng={longitude.ToString(CultureInfo.InvariantCulture)}");
        }

        return $"api/visitor-notifications?{string.Join("&", queryParts)}";
    }
}

public sealed record VisitorNotificationFeedRequest(
    VisitorLocationSnapshot? Location,
    string LanguageCode);
