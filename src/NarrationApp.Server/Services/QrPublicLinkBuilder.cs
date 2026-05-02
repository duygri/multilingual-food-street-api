using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Options;
using NarrationApp.Server.Configuration;
using NarrationApp.Shared.DTOs.QR;

namespace NarrationApp.Server.Services;

public sealed class QrPublicLinkBuilder(IOptions<PublicQrOptions> options)
{
    private readonly PublicQrOptions _options = options.Value;

    public QrCodeDto Enrich(HttpContext context, QrCodeDto dto)
    {
        var publicUrl = BuildPublicUrl(context, dto.Code);
        return new QrCodeDto
        {
            Id = dto.Id,
            Code = dto.Code,
            TargetType = dto.TargetType,
            TargetId = dto.TargetId,
            LocationHint = dto.LocationHint,
            ExpiresAtUtc = dto.ExpiresAtUtc,
            ScanCount = dto.ScanCount,
            PublicUrl = publicUrl,
            AppDeepLink = BuildAppDeepLink(dto.Code)
        };
    }

    public string BuildPublicUrl(HttpContext context, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var baseAddress = ResolveBaseAddress(context);
        return new Uri(baseAddress, $"qr/{Uri.EscapeDataString(code.Trim())}").ToString();
    }

    public string BuildAppDeepLink(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var scheme = string.IsNullOrWhiteSpace(_options.AppScheme) ? "foodstreet" : _options.AppScheme.Trim().TrimEnd(':');
        return $"{scheme}://qr/{Uri.EscapeDataString(code.Trim())}";
    }

    private Uri ResolveBaseAddress(HttpContext context)
    {
        var requestBaseAddress = new Uri($"{context.Request.Scheme}://{context.Request.Host}/", UriKind.Absolute);

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            var configured = _options.BaseUrl.Trim();
            if (Uri.TryCreate(configured, UriKind.Absolute, out var absoluteBaseAddress))
            {
                return EnsureTrailingSlash(absoluteBaseAddress);
            }

            return EnsureTrailingSlash(new Uri(requestBaseAddress, configured));
        }

        if (IsLoopbackHost(context.Request.Host.Host) && TryResolveLanBaseAddress(out var lanBaseAddress))
        {
            return lanBaseAddress;
        }

        return EnsureTrailingSlash(requestBaseAddress);
    }

    private bool TryResolveLanBaseAddress(out Uri baseAddress)
    {
        baseAddress = null!;
        var lanAddress = ResolvePrivateIpv4Address();
        if (lanAddress is null)
        {
            return false;
        }

        var port = _options.LoopbackPublicPort > 0 ? _options.LoopbackPublicPort : 5000;
        baseAddress = new Uri($"http://{lanAddress}:{port}/", UriKind.Absolute);
        return true;
    }

    private static bool IsLoopbackHost(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(host, out var parsed) && IPAddress.IsLoopback(parsed);
    }

    private static string? ResolvePrivateIpv4Address()
    {
        var candidates = NetworkInterface.GetAllNetworkInterfaces()
            .Where(interfaceCard => interfaceCard.OperationalStatus == OperationalStatus.Up)
            .Where(interfaceCard => interfaceCard.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(interfaceCard => interfaceCard.GetIPProperties().UnicastAddresses
                .Where(address => address.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Where(address => !IPAddress.IsLoopback(address.Address))
                .Select(address => new LanAddressCandidate(
                    interfaceCard.Name,
                    interfaceCard.Description,
                    interfaceCard.NetworkInterfaceType,
                    interfaceCard.GetIPProperties().GatewayAddresses.Any(item => item.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.Any.Equals(item.Address)),
                    address.Address.ToString())))
            .Where(candidate => IsPrivateIpv4Address(candidate.Address))
            .ToArray();

        return SelectPreferredLanAddress(candidates)
            ?? Dns.GetHostAddresses(Dns.GetHostName())
                .Where(address => address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Where(address => !IPAddress.IsLoopback(address))
                .Select(address => address.ToString())
                .Where(IsPrivateIpv4Address)
                .FirstOrDefault();
    }

    internal static string? SelectPreferredLanAddress(IEnumerable<LanAddressCandidate> candidates)
    {
        return candidates
            .OrderByDescending(ScoreCandidate)
            .ThenBy(candidate => candidate.Address, StringComparer.Ordinal)
            .Select(candidate => candidate.Address)
            .FirstOrDefault();
    }

    internal static int ScoreCandidate(LanAddressCandidate candidate)
    {
        var score = 0;

        if (candidate.HasGateway)
        {
            score += 1000;
        }

        score += candidate.InterfaceType switch
        {
            NetworkInterfaceType.Wireless80211 => 400,
            NetworkInterfaceType.Ethernet or NetworkInterfaceType.GigabitEthernet or NetworkInterfaceType.FastEthernetFx or NetworkInterfaceType.FastEthernetT => 250,
            NetworkInterfaceType.Ppp or NetworkInterfaceType.Tunnel => -500,
            _ => 0
        };

        var identity = $"{candidate.InterfaceName} {candidate.InterfaceDescription}".ToLowerInvariant();

        if (identity.Contains("wi-fi", StringComparison.Ordinal) || identity.Contains("wifi", StringComparison.Ordinal))
        {
            score += 100;
        }

        if (VirtualAdapterMarkers.Any(identity.Contains))
        {
            score -= 1500;
        }

        return score;
    }

    private static bool IsPrivateIpv4Address(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !IPAddress.TryParse(value, out var address))
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] switch
        {
            10 => true,
            172 when bytes[1] >= 16 && bytes[1] <= 31 => true,
            192 when bytes[1] == 168 => true,
            _ => false
        };
    }

    private static Uri EnsureTrailingSlash(Uri uri)
    {
        var value = uri.ToString();
        return value.EndsWith("/", StringComparison.Ordinal) ? uri : new Uri($"{value}/", UriKind.Absolute);
    }

    private static IReadOnlyList<string> VirtualAdapterMarkers { get; } =
    [
        "virtual",
        "virtualbox",
        "host-only",
        "vmware",
        "hyper-v",
        "vethernet",
        "wintun",
        "tunnel",
        "vpn",
        "loopback",
        "tap"
    ];

    internal sealed record LanAddressCandidate(
        string InterfaceName,
        string InterfaceDescription,
        NetworkInterfaceType InterfaceType,
        bool HasGateway,
        string Address);
}
