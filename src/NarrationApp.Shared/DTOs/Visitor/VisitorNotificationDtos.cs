namespace NarrationApp.Shared.DTOs.Visitor;

public sealed class VisitorNotificationDto
{
    public string Title { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public string TimeLabel { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }
}

public sealed class VisitorNotificationsQuery
{
    public double? Lat { get; init; }

    public double? Lng { get; init; }

    public string? LanguageCode { get; init; }

    public int Take { get; init; } = 12;
}
