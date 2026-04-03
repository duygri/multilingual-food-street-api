namespace PROJECT_C_.DTOs
{
    public class MenuItemDto
    {
        public int Id { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "VND";
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        public int SortOrder { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TranslationCount { get; set; }
        public string LanguageCode { get; set; } = "vi-VN";
        public string RequestedLanguageCode { get; set; } = "vi-VN";
        public int Tier { get; set; } = 3;
        public bool FallbackUsed { get; set; }
        public bool IsFallback { get; set; }
    }

    public class UpsertMenuItemRequest
    {
        public int LocationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "VND";
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; } = true;
        public int SortOrder { get; set; }
    }

    public class MenuTranslationItemDto
    {
        public int Id { get; set; }
        public int PoiMenuItemId { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string MenuItemName { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsFallback { get; set; }
        public DateTime? GeneratedAt { get; set; }
    }

    public class UpsertMenuTranslationRequest
    {
        public int PoiMenuItemId { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
