namespace PROJECT_C_.DTOs
{
    /// <summary>
    /// Request model for user login
    /// </summary>
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for user registration
    /// </summary>
    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string Role { get; set; } = "POI Owner";
    }

    /// <summary>
    /// Response model for successful authentication
    /// </summary>
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string? AccessToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? Email { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
    }
}
