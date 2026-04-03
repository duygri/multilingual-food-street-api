namespace FoodStreet.Client.DTOs
{
    public class AdminDashboardDto
    {
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
        public int TotalAudios { get; set; }
        public int TotalTours { get; set; }
        public int ActiveTours { get; set; }
        public int TotalUsers { get; set; }
        public int ActivePoiOwners { get; set; }
        public int PendingPoiOwners { get; set; }
        public int TotalPlays30Days { get; set; }
        public int TodayPlays { get; set; }
        public int QrScans30Days { get; set; }
        public int TourStarts30Days { get; set; }
        public int TourResumes30Days { get; set; }
        public int TourCompletions30Days { get; set; }
        public int TourDismissals30Days { get; set; }
        public int ActiveTourSessions { get; set; }
        public double TourCompletionRate30Days { get; set; }
        public double TourDismissRate30Days { get; set; }
        public int UnreadAdminNotifications { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public List<AdminDashboardPoiItemDto> PendingReviewPois { get; set; } = new();
        public List<AdminDashboardPoiItemDto> RecentPois { get; set; } = new();
        public List<AdminDashboardSourceDto> SourceBreakdown { get; set; } = new();
        public List<AdminDashboardNotificationDto> RecentNotifications { get; set; } = new();
    }

    public class AdminDashboardPoiItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? OwnerEmail { get; set; }
        public bool IsApproved { get; set; }
        public bool HasAudio { get; set; }
        public string AudioStatus { get; set; } = "pending";
        public DateTime? ApprovedAt { get; set; }
    }

    public class AdminDashboardSourceDto
    {
        public string Source { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class AdminDashboardNotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? SenderName { get; set; }
        public int? RelatedId { get; set; }
    }
}
