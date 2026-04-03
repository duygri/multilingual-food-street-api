using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PROJECT_C_.Configuration;

namespace FoodStreet.Server.Services.GoogleCloud;

public interface IGoogleCloudAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task<string> GetProjectIdAsync(CancellationToken cancellationToken = default);
    string GetAuthMode();
    string? GetConfiguredCredentialPath();
    string? GetLastAuthError();
}

public sealed class GoogleCloudAccessTokenProvider : IGoogleCloudAccessTokenProvider
{
    private const string CloudPlatformScope = "https://www.googleapis.com/auth/cloud-platform";
    private static readonly TimeSpan MinimumTokenLifetime = TimeSpan.FromMinutes(1);

    private readonly GoogleCloudOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GoogleCloudAccessTokenProvider> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _cachedTokenExpiresAt = DateTimeOffset.MinValue;
    private string? _cachedProjectId;
    private ServiceAccountCredentialDocument? _cachedCredentialDocument;
    private string? _cachedCredentialDocumentPath;
    private string? _lastAuthError;

    public GoogleCloudAccessTokenProvider(
        IOptions<GoogleCloudOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<GoogleCloudAccessTokenProvider> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_cachedToken)
            && DateTimeOffset.UtcNow < _cachedTokenExpiresAt)
        {
            return _cachedToken;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_cachedToken)
                && DateTimeOffset.UtcNow < _cachedTokenExpiresAt)
            {
                return _cachedToken;
            }

            var failureMessages = new List<string>();

            var serviceAccountResult = await TryGetServiceAccountAccessTokenAsync(cancellationToken);
            if (serviceAccountResult is not null)
            {
                _cachedToken = serviceAccountResult.Value.Token.Trim();
                _cachedTokenExpiresAt = serviceAccountResult.Value.ExpiresAt;
                _lastAuthError = null;
                return _cachedToken;
            }

            if (!string.IsNullOrWhiteSpace(_lastAuthError))
            {
                failureMessages.Add($"Service account JSON: {_lastAuthError}");
            }

            var token = await TryExecuteGcloudAsync("auth application-default print-access-token", cancellationToken)
                ?? await TryExecuteGcloudAsync("auth print-access-token", cancellationToken);

            if (!string.IsNullOrWhiteSpace(_lastAuthError))
            {
                failureMessages.Add($"gcloud fallback: {_lastAuthError}");
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException(
                    "Cannot acquire Google Cloud access token. Configure a service-account JSON via " +
                    "GOOGLE_APPLICATION_CREDENTIALS / GoogleCloud:CredentialPath, or run " +
                    "'gcloud auth application-default login' and set a quota project before using Translate/TTS." +
                    (failureMessages.Count == 0 ? string.Empty : $" Latest failure(s): {string.Join(" || ", failureMessages.Distinct(StringComparer.Ordinal))}"));
            }

            _cachedToken = token.Trim();
            _cachedTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(50);
            _lastAuthError = null;
            return _cachedToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> GetProjectIdAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_cachedProjectId))
        {
            return _cachedProjectId;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_cachedProjectId))
            {
                return _cachedProjectId;
            }

            var projectId = _options.ProjectId
                ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT")
                ?? Environment.GetEnvironmentVariable("GCLOUD_PROJECT");

            if (string.IsNullOrWhiteSpace(projectId))
            {
                var credential = await TryLoadServiceAccountCredentialAsync(cancellationToken);
                projectId = credential?.ProjectId;
            }

            if (string.IsNullOrWhiteSpace(projectId))
            {
                projectId = await TryExecuteGcloudAsync("config get-value project", cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(projectId)
                || string.Equals(projectId.Trim(), "(unset)", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Google Cloud project ID is missing. Set GoogleCloud:ProjectId, provide a service-account JSON with project_id, " +
                    "or configure 'gcloud config set project <PROJECT_ID>'.");
            }

            _cachedProjectId = projectId.Trim();
            return _cachedProjectId;
        }
        finally
        {
            _lock.Release();
        }
    }

    public string GetAuthMode()
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return "API key";
        }

        var credentialPath = GetConfiguredCredentialPath();
        if (!string.IsNullOrWhiteSpace(credentialPath))
        {
            return "Service account JSON";
        }

        return "ADC / gcloud";
    }

    public string? GetConfiguredCredentialPath()
    {
        var rawPath = !string.IsNullOrWhiteSpace(_options.CredentialPath)
            ? _options.CredentialPath
            : Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return null;
        }

        return Environment.ExpandEnvironmentVariables(rawPath.Trim());
    }

    public string? GetLastAuthError() => _lastAuthError;

    private async Task<AccessTokenResult?> TryGetServiceAccountAccessTokenAsync(CancellationToken cancellationToken)
    {
        var credential = await TryLoadServiceAccountCredentialAsync(cancellationToken);
        if (credential is null)
        {
            return null;
        }

        try
        {
            var issuedAt = DateTimeOffset.UtcNow;
            var expiresAt = issuedAt.AddMinutes(55);
            var assertion = CreateSignedAssertion(credential, issuedAt, expiresAt);
            using var request = new HttpRequestMessage(HttpMethod.Post, credential.TokenUri ?? "https://oauth2.googleapis.com/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                    ["assertion"] = assertion
                })
            };

            using var response = await _httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                SetLastAuthError(
                    $"Service-account token exchange failed ({(int)response.StatusCode} {response.StatusCode}): " +
                    $"{TrimForLog(responseBody)}");
                _logger.LogWarning(
                    "Service-account token exchange failed with status {StatusCode}: {Body}",
                    response.StatusCode,
                    string.IsNullOrWhiteSpace(responseBody) ? "(empty)" : responseBody);
                return null;
            }

            var tokenResponse = JsonSerializer.Deserialize<ServiceAccountTokenResponse>(responseBody);
            if (tokenResponse is null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                SetLastAuthError("Service-account token response did not contain an access token.");
                _logger.LogWarning("Service-account token response did not contain an access token.");
                return null;
            }

            var effectiveExpiresAt = issuedAt.AddSeconds(tokenResponse.ExpiresIn <= 0 ? 300 : tokenResponse.ExpiresIn);
            if (effectiveExpiresAt - DateTimeOffset.UtcNow < MinimumTokenLifetime)
            {
                effectiveExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
            }

            return new AccessTokenResult(tokenResponse.AccessToken, effectiveExpiresAt);
        }
        catch (Exception ex)
        {
            SetLastAuthError($"Service-account JSON flow failed: {FlattenExceptionMessage(ex)}");
            _logger.LogWarning(ex, "Failed to acquire access token using service-account JSON.");
            return null;
        }
    }

    private async Task<ServiceAccountCredentialDocument?> TryLoadServiceAccountCredentialAsync(CancellationToken cancellationToken)
    {
        var credentialPath = GetConfiguredCredentialPath();
        if (string.IsNullOrWhiteSpace(credentialPath))
        {
            return null;
        }

        if (!_options.UseServiceAccountJson && string.IsNullOrWhiteSpace(_options.CredentialPath))
        {
            // Respect the explicit API-key path first, but still allow env-driven JSON when present.
        }

        if (_cachedCredentialDocument is not null
            && string.Equals(_cachedCredentialDocumentPath, credentialPath, StringComparison.OrdinalIgnoreCase))
        {
            return _cachedCredentialDocument;
        }

        if (!File.Exists(credentialPath))
        {
            SetLastAuthError($"Credential file does not exist: {credentialPath}");
            _logger.LogWarning("Configured Google Cloud credential file does not exist: {CredentialPath}", credentialPath);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(credentialPath, cancellationToken);
            var credential = JsonSerializer.Deserialize<ServiceAccountCredentialDocument>(json);
            if (credential is null
                || string.IsNullOrWhiteSpace(credential.ClientEmail)
                || string.IsNullOrWhiteSpace(credential.PrivateKey))
            {
                SetLastAuthError("Google Cloud credential file is missing required service-account fields.");
                _logger.LogWarning("Google Cloud credential file is missing required service-account fields.");
                return null;
            }

            _cachedCredentialDocument = credential;
            _cachedCredentialDocumentPath = credentialPath;
            return credential;
        }
        catch (Exception ex)
        {
            SetLastAuthError($"Failed to load credential file: {FlattenExceptionMessage(ex)}");
            _logger.LogWarning(ex, "Failed to load Google Cloud credential file from {CredentialPath}", credentialPath);
            return null;
        }
    }

    private static string CreateSignedAssertion(
        ServiceAccountCredentialDocument credential,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        var tokenUri = credential.TokenUri ?? "https://oauth2.googleapis.com/token";
        var headerJson = JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT" });
        var payloadJson = JsonSerializer.Serialize(new
        {
            iss = credential.ClientEmail,
            sub = credential.ClientEmail,
            aud = tokenUri,
            scope = CloudPlatformScope,
            iat = issuedAt.ToUnixTimeSeconds(),
            exp = expiresAt.ToUnixTimeSeconds()
        });

        var unsignedToken = $"{Base64UrlEncode(headerJson)}.{Base64UrlEncode(payloadJson)}";

        using var rsa = RSA.Create();
        var privateKey = credential.PrivateKey
            ?? throw new InvalidOperationException("Service-account credential is missing private_key.");
        rsa.ImportFromPem(privateKey.ToCharArray());
        var signature = rsa.SignData(
            Encoding.UTF8.GetBytes(unsignedToken),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return $"{unsignedToken}.{Base64UrlEncode(signature)}";
    }

    private static string Base64UrlEncode(string value)
        => Base64UrlEncode(Encoding.UTF8.GetBytes(value));

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private async Task<string?> TryExecuteGcloudAsync(string arguments, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = string.IsNullOrWhiteSpace(_options.CliPath) ? "gcloud" : _options.CliPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode == 0)
            {
                return string.IsNullOrWhiteSpace(stdout) ? null : stdout.Trim();
            }

            SetLastAuthError(
                $"gcloud {arguments} failed with exit code {process.ExitCode}: {TrimForLog(stderr)}");
            _logger.LogWarning(
                "gcloud command '{Arguments}' failed with exit code {ExitCode}: {Error}",
                arguments,
                process.ExitCode,
                string.IsNullOrWhiteSpace(stderr) ? "(no stderr)" : stderr.Trim());

            return null;
        }
        catch (Exception ex)
        {
            SetLastAuthError($"Failed to execute gcloud {arguments}: {FlattenExceptionMessage(ex)}");
            _logger.LogWarning(ex, "Failed to execute gcloud command '{Arguments}'", arguments);
            return null;
        }
    }

    private void SetLastAuthError(string message)
    {
        _lastAuthError = message;
    }

    private static string FlattenExceptionMessage(Exception ex)
    {
        var messages = new List<string>();
        Exception? current = ex;
        while (current is not null)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
            {
                messages.Add(current.Message.Trim());
            }

            current = current.InnerException;
        }

        return string.Join(" | ", messages.Distinct(StringComparer.Ordinal));
    }

    private static string TrimForLog(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "(empty)";
        }

        const int maxLength = 220;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : $"{trimmed[..maxLength]}...";
    }

    private readonly record struct AccessTokenResult(string Token, DateTimeOffset ExpiresAt);

    private sealed class ServiceAccountCredentialDocument
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("project_id")]
        public string? ProjectId { get; set; }

        [JsonPropertyName("private_key_id")]
        public string? PrivateKeyId { get; set; }

        [JsonPropertyName("private_key")]
        public string? PrivateKey { get; set; }

        [JsonPropertyName("client_email")]
        public string? ClientEmail { get; set; }

        [JsonPropertyName("token_uri")]
        public string? TokenUri { get; set; }
    }

    private sealed class ServiceAccountTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }
}
