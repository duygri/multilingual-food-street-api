using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace NarrationApp.Server.Services;

public interface IQrDeviceConfigService
{
    QrDeviceConfigDecision Decide();
}

public sealed record QrDeviceConfigDecision(
    int Value,
    bool ShouldOpenApp,
    string Label,
    string Notice,
    string LaunchMode)
{
    public string BuildAppDeepLink(string appDeepLink)
    {
        if (string.IsNullOrWhiteSpace(LaunchMode))
        {
            return appDeepLink;
        }

        var separator = appDeepLink.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{appDeepLink}{separator}qrLaunch={Uri.EscapeDataString(LaunchMode)}";
    }

    public static QrDeviceConfigDecision FromValue(int value)
    {
        if (value == 0)
        {
            return new QrDeviceConfigDecision(
                Value: 0,
                ShouldOpenApp: true,
                Label: "Cấu hình mạnh (0)",
                Notice: "App sẽ hiển thị thông báo QR cấu hình mạnh và mở danh sách POI trong app.",
                LaunchMode: "strong0");
        }

        return new QrDeviceConfigDecision(
            Value: 1,
            ShouldOpenApp: false,
            Label: "Cấu hình yếu (1)",
            Notice: "Máy yếu nên giữ bản web fallback để xem POI và nghe audio trực tiếp.",
            LaunchMode: "weak1");
    }
}

public sealed class QrDeviceConfigService(IConfiguration configuration) : IQrDeviceConfigService
{
    private const string ForcedValueKey = "QrDeviceConfig:ForcedValue";

    public QrDeviceConfigDecision Decide()
    {
        var configuredValue = configuration[ForcedValueKey];
        if (int.TryParse(configuredValue, out var forcedValue) && forcedValue is 0 or 1)
        {
            return QrDeviceConfigDecision.FromValue(forcedValue);
        }

        return QrDeviceConfigDecision.FromValue(RandomNumberGenerator.GetInt32(0, 2));
    }
}
