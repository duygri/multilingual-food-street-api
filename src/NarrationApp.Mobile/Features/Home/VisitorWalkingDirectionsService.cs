using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NarrationApp.Mobile.Features.Home;

public interface IVisitorWalkingDirectionsService
{
    Task<VisitorWalkingDirectionsResult> LoadWalkingRouteAsync(
        VisitorLocationSnapshot? origin,
        VisitorPoi destination,
        CancellationToken cancellationToken = default);
}

public sealed class VisitorWalkingDirectionsService(HttpClient httpClient, VisitorMapOptions mapOptions) : IVisitorWalkingDirectionsService
{
    private const string MissingLocationStatus = "Chưa có vị trí hiện tại để vẽ đường đi bộ.";
    private const string MissingTokenStatus = "Chưa có Mapbox token để vẽ đường đi trong app.";
    private const string RouteUnavailableStatus = "Không vẽ được đường đi bộ trong app lúc này.";

    public async Task<VisitorWalkingDirectionsResult> LoadWalkingRouteAsync(
        VisitorLocationSnapshot? origin,
        VisitorPoi destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (!VisitorGeoMath.TryGetCoordinates(origin, out var originLatitude, out var originLongitude))
        {
            return VisitorWalkingDirectionsResult.Unavailable(MissingLocationStatus);
        }

        if (IsMissingMapboxToken(mapOptions.AccessToken))
        {
            return VisitorWalkingDirectionsResult.Unavailable(MissingTokenStatus);
        }

        try
        {
            var requestUri = BuildDirectionsUri(
                mapOptions.AccessToken,
                originLatitude,
                originLongitude,
                destination.Latitude,
                destination.Longitude);
            var response = await httpClient.GetFromJsonAsync<MapboxDirectionsResponse>(requestUri, cancellationToken);
            var route = response?.Routes?.FirstOrDefault();
            var coordinates = route?.Geometry?.Coordinates;
            if (coordinates is null || coordinates.Length < 2)
            {
                return VisitorWalkingDirectionsResult.Unavailable(BuildMapboxFailureStatus(response));
            }

            var points = coordinates
                .Where(coordinate => coordinate.Length >= 2)
                .Select(coordinate => new VisitorMapRoutePoint(coordinate[1], coordinate[0]))
                .ToArray();
            if (points.Length < 2)
            {
                return VisitorWalkingDirectionsResult.Unavailable(BuildMapboxFailureStatus(response));
            }

            var distanceMeters = Math.Max(1, (int)Math.Round(route!.DistanceMeters));
            var durationMinutes = Math.Max(1, (int)Math.Ceiling(route.DurationSeconds / 60d));
            var statusLabel = $"Đi bộ {VisitorWalkingDirectionsPresentationFormatter.FormatDistance(distanceMeters)} • khoảng {VisitorWalkingDirectionsPresentationFormatter.FormatDuration(durationMinutes)}";

            return new VisitorWalkingDirectionsResult(
                true,
                new VisitorMapRoute(points, statusLabel, distanceMeters, durationMinutes),
                statusLabel);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException or JsonException)
        {
            return VisitorWalkingDirectionsResult.Unavailable(RouteUnavailableStatus);
        }
    }

    private static Uri BuildDirectionsUri(
        string accessToken,
        double originLatitude,
        double originLongitude,
        double destinationLatitude,
        double destinationLongitude)
    {
        var coordinates = string.Create(
            CultureInfo.InvariantCulture,
            $"{originLongitude:F6},{originLatitude:F6};{destinationLongitude:F6},{destinationLatitude:F6}");
        var url =
            "https://api.mapbox.com/directions/v5/mapbox/walking/"
            + coordinates
            + "?alternatives=false&geometries=geojson&overview=full&steps=false&access_token="
            + Uri.EscapeDataString(accessToken);

        return new Uri(url);
    }

    private static bool IsMissingMapboxToken(string? accessToken) =>
        string.IsNullOrWhiteSpace(accessToken)
        || accessToken.Trim().StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);

    private static string BuildMapboxFailureStatus(MapboxDirectionsResponse? response) =>
        string.IsNullOrWhiteSpace(response?.Message)
            ? RouteUnavailableStatus
            : $"{RouteUnavailableStatus} ({response.Message})";

    private sealed record MapboxDirectionsResponse(
        [property: JsonPropertyName("code")] string? Code,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("routes")] MapboxRouteResponse[]? Routes);

    private sealed record MapboxRouteResponse(
        [property: JsonPropertyName("distance")] double DistanceMeters,
        [property: JsonPropertyName("duration")] double DurationSeconds,
        [property: JsonPropertyName("geometry")] MapboxGeometryResponse? Geometry);

    private sealed record MapboxGeometryResponse(
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("coordinates")] double[][]? Coordinates);
}

public sealed record VisitorWalkingDirectionsResult(
    bool IsAvailable,
    VisitorMapRoute? Route,
    string StatusLabel)
{
    public static VisitorWalkingDirectionsResult Unavailable(string statusLabel) =>
        new(false, null, statusLabel);
}
