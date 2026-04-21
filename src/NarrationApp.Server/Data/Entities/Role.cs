namespace NarrationApp.Server.Data.Entities;

public sealed class Role
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ICollection<AppUser> Users { get; set; } = [];
}
