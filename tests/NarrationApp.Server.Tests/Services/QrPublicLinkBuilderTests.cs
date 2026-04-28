using System.Net.NetworkInformation;
using NarrationApp.Server.Services;

namespace NarrationApp.Server.Tests.Services;

public sealed class QrPublicLinkBuilderTests
{
    [Fact]
    public void SelectPreferredLanAddress_prefers_real_wifi_with_gateway_over_virtual_host_only_adapter()
    {
        var candidates = new[]
        {
            new QrPublicLinkBuilder.LanAddressCandidate(
                "Ethernet 3",
                "VirtualBox Host-Only Ethernet Adapter",
                NetworkInterfaceType.Ethernet,
                false,
                "192.168.56.1"),
            new QrPublicLinkBuilder.LanAddressCandidate(
                "Wi-Fi",
                "Realtek RTL8852AE WiFi 6 802.11ax PCIe Adapter",
                NetworkInterfaceType.Wireless80211,
                true,
                "172.20.10.3")
        };

        var selected = QrPublicLinkBuilder.SelectPreferredLanAddress(candidates);

        Assert.Equal("172.20.10.3", selected);
    }

    [Fact]
    public void ScoreCandidate_penalizes_virtual_or_tunnel_adapters()
    {
        var physicalWifi = new QrPublicLinkBuilder.LanAddressCandidate(
            "Wi-Fi",
            "Realtek RTL8852AE WiFi 6 802.11ax PCIe Adapter",
            NetworkInterfaceType.Wireless80211,
            true,
            "172.20.10.3");
        var virtualHostOnly = new QrPublicLinkBuilder.LanAddressCandidate(
            "Ethernet 3",
            "VirtualBox Host-Only Ethernet Adapter",
            NetworkInterfaceType.Ethernet,
            false,
            "192.168.56.1");

        var wifiScore = QrPublicLinkBuilder.ScoreCandidate(physicalWifi);
        var virtualScore = QrPublicLinkBuilder.ScoreCandidate(virtualHostOnly);

        Assert.True(wifiScore > virtualScore);
    }
}
