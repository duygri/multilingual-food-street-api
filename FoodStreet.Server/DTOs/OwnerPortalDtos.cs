namespace PROJECT_C_.DTOs;

public class OwnerDashboardDto
{
    public string OwnerId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string RoleDisplayName { get; set; } = "POI Owner";
    public int TotalPois { get; set; }
    public int ApprovedPois { get; set; }
    public int PendingPois { get; set; }
    public int PoisWithAudio { get; set; }
    public int TotalPoiTranslations { get; set; }
    public int FullyLocalizedPois { get; set; }
    public double PoiTranslationCoveragePercent { get; set; }
    public int TotalMenuItems { get; set; }
    public int AvailableMenuItems { get; set; }
    public int TotalMenuTranslations { get; set; }
    public int FullyLocalizedMenuItems { get; set; }
    public double MenuTranslationCoveragePercent { get; set; }
    public int TotalPlays30Days { get; set; }
    public int QrScans30Days { get; set; }
    public int TourStarts30Days { get; set; }
    public int TourResumes30Days { get; set; }
    public int TourProgressEvents30Days { get; set; }
    public int TourCompletions30Days { get; set; }
    public int TourDismissals30Days { get; set; }
    public int ActiveTourSessions { get; set; }
    public double TourCompletionRate30Days { get; set; }
    public double TourDismissRate30Days { get; set; }
    public int UnreadNotifications { get; set; }
    public DateTime? LastPlayedAt { get; set; }
    public List<OwnerPoiAnalyticsItemDto> RecentPois { get; set; } = new();
}

public class OwnerProfileDto
{
    public string OwnerId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string RoleDisplayName { get; set; } = "POI Owner";
    public int TotalPois { get; set; }
    public int ApprovedPois { get; set; }
    public int PendingPois { get; set; }
    public int PoisWithAudio { get; set; }
    public int TotalPoiTranslations { get; set; }
    public int FullyLocalizedPois { get; set; }
    public double PoiTranslationCoveragePercent { get; set; }
    public int TotalMenuItems { get; set; }
    public int AvailableMenuItems { get; set; }
    public int TotalMenuTranslations { get; set; }
    public int FullyLocalizedMenuItems { get; set; }
    public double MenuTranslationCoveragePercent { get; set; }
}

public class UpdateOwnerProfileDto
{
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

public class OwnerAnalyticsDto
{
    public int Days { get; set; }
    public int TotalPois { get; set; }
    public int ApprovedPois { get; set; }
    public int PendingPois { get; set; }
    public int PoisWithAudio { get; set; }
    public int TotalPoiTranslations { get; set; }
    public int FullyLocalizedPois { get; set; }
    public double PoiTranslationCoveragePercent { get; set; }
    public int TotalMenuItems { get; set; }
    public int AvailableMenuItems { get; set; }
    public int TotalMenuTranslations { get; set; }
    public int FullyLocalizedMenuItems { get; set; }
    public double MenuTranslationCoveragePercent { get; set; }
    public int TotalPlays { get; set; }
    public int TotalQrScans { get; set; }
    public int TotalTourStarts { get; set; }
    public int TotalTourResumes { get; set; }
    public int TotalTourProgressEvents { get; set; }
    public int TotalTourCompletions { get; set; }
    public int TotalTourDismissals { get; set; }
    public int ActiveTourSessions { get; set; }
    public double TourCompletionRate { get; set; }
    public double TourDismissRate { get; set; }
    public double AvgListenDurationSeconds { get; set; }
    public DateTime? LastPlayedAt { get; set; }
    public List<OwnerPoiAnalyticsItemDto> Pois { get; set; } = new();
    public List<OwnerBreakdownDto> Sources { get; set; } = new();
    public List<OwnerBreakdownDto> Languages { get; set; } = new();
}

public class OwnerPoiAnalyticsItemDto
{
    public int LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ImageUrl { get; set; }
    public string? CategoryName { get; set; }
    public bool IsApproved { get; set; }
    public bool HasAudio { get; set; }
    public string AudioStatus { get; set; } = "pending";
    public int TranslationCount { get; set; }
    public double TranslationCoveragePercent { get; set; }
    public int MenuItemCount { get; set; }
    public int MenuTranslationCount { get; set; }
    public int FullyLocalizedMenuItems { get; set; }
    public double MenuTranslationCoveragePercent { get; set; }
    public int PlayCount { get; set; }
    public int QrScanCount { get; set; }
    public int TourStartCount { get; set; }
    public int TourResumeCount { get; set; }
    public int TourProgressCount { get; set; }
    public int TourCompletionCount { get; set; }
    public int TourDismissCount { get; set; }
    public double TourCompletionRate { get; set; }
    public double AvgListenDurationSeconds { get; set; }
    public string? TopLanguage { get; set; }
    public string? LastSource { get; set; }
    public DateTime? LastPlayedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class OwnerBreakdownDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}
