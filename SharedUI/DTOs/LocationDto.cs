namespace FoodStreet.Client.DTOs
{
    public class LocationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Address { get; set; }

        // GPS
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Geofencing
        public double Radius { get; set; } = 50;
        public int Priority { get; set; }

        // Media
        public string? ImageUrl { get; set; }
        public string? MapLink { get; set; }
        public string? TtsScript { get; set; }

        // Ownership / Approval
        public string? OwnerId { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Category
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }

        // Computed fields (from API)
        public double Distance { get; set; }
        public string Unit { get; set; } = "m";
        public bool HasAudio { get; set; }
        public string? AudioUrl { get; set; }
        public string AudioStatus { get; set; } = "pending";
        public string LanguageCode { get; set; } = "vi-VN";
        public int Tier { get; set; } = 3;
        public bool FallbackUsed { get; set; }
        public bool IsFallback { get; set; }
        public int FoodCount { get; set; }
    }

    /// <summary>
    /// DTO for localization API response - maps to /api/localization/location/{id}?lang=xx
    /// </summary>
    public class LocationTranslationDto
    {
        public int LocationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? TtsScript { get; set; }
        public string? AudioUrl { get; set; }
        public string? AudioStatus { get; set; }
        public string LanguageCode { get; set; } = "vi-VN";
        public int Tier { get; set; }
        public bool FallbackUsed { get; set; }
        public bool IsFallback { get; set; }

        // Display helpers
        public string LanguageLabel => LanguageCode switch
        {
            "vi-VN" => "Tiếng Việt",
            "en-US" => "English",
            "ja-JP" => "日本語",
            "ko-KR" => "한국어",
            "zh-CN" => "中文",
            _ => LanguageCode
        };

        public string LanguageFlag => LanguageCode switch
        {
            "vi-VN" => "🇻🇳",
            "en-US" => "🇺🇸",
            "ja-JP" => "🇯🇵",
            "ko-KR" => "🇰🇷",
            "zh-CN" => "🇨🇳",
            _ => "🌐"
        };
    }
}
