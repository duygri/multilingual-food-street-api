namespace NarrationApp.Server.Data.Entities;

public sealed class AppUser
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string PreferredLanguage { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? ManagedArea { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public Guid RoleId { get; set; }

    public bool IsActive { get; set; }

    public Role? Role { get; set; }

    public ICollection<Poi> OwnedPois { get; set; } = [];

    public ICollection<VisitEvent> VisitEvents { get; set; } = [];

    public ICollection<Notification> Notifications { get; set; } = [];

    public ICollection<ModerationRequest> RequestedModerationRequests { get; set; } = [];

    public ICollection<ModerationRequest> ReviewedModerationRequests { get; set; } = [];
}
