namespace PROJECT_C_.Configuration
{
    /// <summary>
    /// Google Cloud runtime settings for backend services.
    /// Supports API key, service-account JSON, and local gcloud/ADC fallback.
    /// </summary>
    public class GoogleCloudOptions
    {
        public const string SectionName = "GoogleCloud";

        /// <summary>
        /// Optional Google Cloud API key for REST-based Translate/TTS calls.
        /// If empty, backend falls back to ADC / gcloud access tokens.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// When true, backend will prefer service-account JSON credentials
        /// from CredentialPath or GOOGLE_APPLICATION_CREDENTIALS.
        /// </summary>
        public bool UseServiceAccountJson { get; set; }

        /// <summary>
        /// Optional absolute path to a service-account JSON file.
        /// If empty, backend falls back to GOOGLE_APPLICATION_CREDENTIALS.
        /// </summary>
        public string? CredentialPath { get; set; }

        /// <summary>
        /// Google Cloud project ID used by Translation v3 endpoints.
        /// Falls back to service-account JSON project_id, GOOGLE_CLOUD_PROJECT,
        /// GCLOUD_PROJECT, or gcloud config.
        /// </summary>
        public string? ProjectId { get; set; }

        /// <summary>
        /// CLI executable used to obtain fallback ADC access tokens locally.
        /// Defaults to "gcloud" when JSON credentials are not used.
        /// </summary>
        public string CliPath { get; set; } = "gcloud";
    }
}
