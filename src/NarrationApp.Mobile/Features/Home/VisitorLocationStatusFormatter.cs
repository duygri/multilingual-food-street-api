namespace NarrationApp.Mobile.Features.Home;

public static class VisitorLocationStatusFormatter
{
    public static string Build(VisitorLocationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.Source == VisitorLocationSource.Unsupported)
        {
            return string.IsNullOrWhiteSpace(snapshot.StatusLabel)
                ? "Thiết bị không hỗ trợ định vị"
                : snapshot.StatusLabel;
        }

        if (snapshot.Source == VisitorLocationSource.Error)
        {
            return string.IsNullOrWhiteSpace(snapshot.StatusLabel)
                ? "Không truy cập được vị trí"
                : snapshot.StatusLabel;
        }

        if (!snapshot.PermissionGranted)
        {
            return "Chưa cấp quyền vị trí";
        }

        if (!snapshot.IsLocationAvailable || snapshot.Latitude is null || snapshot.Longitude is null)
        {
            return string.IsNullOrWhiteSpace(snapshot.StatusLabel)
                ? "Đã bật quyền, chưa lấy được tọa độ"
                : snapshot.StatusLabel;
        }

        var prefix = snapshot.Source switch
        {
            VisitorLocationSource.LastKnown => "Vị trí gần nhất",
            VisitorLocationSource.Live => "GPS live",
            _ => "Đã định vị"
        };

        return $"{prefix} {snapshot.Latitude.Value:F4}, {snapshot.Longitude.Value:F4}";
    }
}
