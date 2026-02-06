namespace PROJECT_C_.Configuration
{
    /// <summary>
    /// JWT Configuration settings - loaded from appsettings.json
    /// </summary>
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";
        
        /// <summary>
        /// Secret key for signing tokens (min 32 characters)
        /// </summary>
        public string Secret { get; set; } = string.Empty;
        
        /// <summary>
        /// Token issuer (typically the API domain)
        /// </summary>
        public string Issuer { get; set; } = string.Empty;
        
        /// <summary>
        /// Token audience (typically the client application)
        /// </summary>
        public string Audience { get; set; } = string.Empty;
        
        /// <summary>
        /// Access token expiry in minutes
        /// </summary>
        public int AccessTokenExpiryMinutes { get; set; } = 60;
        
        /// <summary>
        /// Refresh token expiry in days
        /// </summary>
        public int RefreshTokenExpiryDays { get; set; } = 7;
    }
}
