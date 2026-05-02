using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NarrationApp.Server.Configuration;

namespace NarrationApp.Server.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
public sealed class AppLinksController(IOptions<MobileAppLinksOptions> options) : ControllerBase
{
    private readonly MobileAppLinksOptions _options = options.Value;

    [HttpGet("/.well-known/assetlinks.json")]
    public IActionResult GetAndroidAssetLinks()
    {
        var payload = _options.Android
            .Where(item => !string.IsNullOrWhiteSpace(item.PackageName))
            .Select(item => new
            {
                packageName = item.PackageName.Trim(),
                fingerprints = item.Sha256CertFingerprints
                    .Where(IsConfiguredFingerprint)
                    .Select(fingerprint => fingerprint.Trim())
                    .ToArray()
            })
            .Where(item => item.fingerprints.Length > 0)
            .Select(item => new
            {
                relation = new[] { "delegate_permission/common.handle_all_urls" },
                target = new
                {
                    @namespace = "android_app",
                    package_name = item.packageName,
                    sha256_cert_fingerprints = item.fingerprints
                }
            })
            .ToArray();

        return new JsonResult(payload);
    }

    private static bool IsConfiguredFingerprint(string? fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            return false;
        }

        return !fingerprint.Trim().StartsWith("REPLACE_WITH_", StringComparison.OrdinalIgnoreCase);
    }
}
