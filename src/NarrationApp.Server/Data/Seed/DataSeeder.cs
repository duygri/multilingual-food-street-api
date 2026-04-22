using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data.Entities;
using NarrationApp.Shared.Constants;
using NarrationApp.Shared.Enums;

namespace NarrationApp.Server.Data.Seed;

public sealed class DataSeeder(AppDbContext dbContext, ILogger<DataSeeder> logger)
{
    private static readonly Guid AdminRoleId = Guid.Parse("4B4096D9-899A-4C8C-918F-4877F9A52D11");
    private static readonly Guid OwnerRoleId = Guid.Parse("A62E0A1F-EF78-4471-9235-31E12B33674E");
    private static readonly Guid TouristRoleId = Guid.Parse("0D84C6F8-7282-4D89-92A9-60B89B7B3A82");
    private static readonly Guid AdminUserId = Guid.Parse("2BC73450-C84F-4D15-BD6C-2C4AF31D8D61");
    private static readonly Guid OwnerUserId = Guid.Parse("E8266528-C71F-4CE5-BF1C-B6AE6B9844F6");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var roles = await SeedRolesAsync(cancellationToken);
        var users = await SeedUsersAsync(roles, cancellationToken);
        await SeedManagedLanguagesAsync(cancellationToken);
        await SeedCategoriesAsync(cancellationToken);
        await SeedPoisAsync(users, cancellationToken);
    }

    private async Task<Dictionary<string, Role>> SeedRolesAsync(CancellationToken cancellationToken)
    {
        var rolesByName = await dbContext.Roles
            .ToDictionaryAsync(role => role.Name, cancellationToken);

        var seedRoles = new[]
        {
            new Role { Id = AdminRoleId, Name = "admin", Description = "System administrator" },
            new Role { Id = OwnerRoleId, Name = "poi_owner", Description = "Point of interest owner" },
            new Role { Id = TouristRoleId, Name = "tourist", Description = "Tourist application user" }
        };

        foreach (var role in seedRoles.Where(role => !rolesByName.ContainsKey(role.Name)))
        {
            dbContext.Roles.Add(role);
            rolesByName[role.Name] = role;
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded baseline roles.");
        }

        return rolesByName;
    }

    private async Task<Dictionary<string, AppUser>> SeedUsersAsync(
        IReadOnlyDictionary<string, Role> roles,
        CancellationToken cancellationToken)
    {
        var usersByEmail = await dbContext.AppUsers
            .ToDictionaryAsync(user => user.Email, cancellationToken);
        var now = DateTime.UtcNow;

        var seedUsers = new[]
        {
            new AppUser
            {
                Id = AdminUserId,
                FullName = "System Admin",
                Email = AppConstants.DefaultAdminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(AppConstants.DefaultAdminPassword),
                PreferredLanguage = AppConstants.DefaultLanguage,
                CreatedAtUtc = now,
                LastLoginAtUtc = null,
                RoleId = roles["admin"].Id,
                IsActive = true
            },
            new AppUser
            {
                Id = OwnerUserId,
                FullName = "Demo Owner",
                Email = AppConstants.DefaultOwnerEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(AppConstants.DefaultOwnerPassword),
                PreferredLanguage = AppConstants.DefaultLanguage,
                CreatedAtUtc = now,
                LastLoginAtUtc = null,
                RoleId = roles["poi_owner"].Id,
                IsActive = true
            }
        };

        foreach (var user in seedUsers.Where(user => !usersByEmail.ContainsKey(user.Email)))
        {
            dbContext.AppUsers.Add(user);
            usersByEmail[user.Email] = user;
        }

        var baselineDisplayNames = new Dictionary<string, string>
        {
            [AppConstants.DefaultAdminEmail] = "System Admin",
            [AppConstants.DefaultOwnerEmail] = "Demo Owner"
        };

        foreach (var existingUser in usersByEmail.Values.Where(user =>
                     string.IsNullOrWhiteSpace(user.FullName) &&
                     baselineDisplayNames.ContainsKey(user.Email)))
        {
            existingUser.FullName = baselineDisplayNames[existingUser.Email];
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded baseline users.");
        }

        return usersByEmail;
    }

    private async Task<Dictionary<string, Category>> SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        var categoriesBySlug = await dbContext.Categories
            .ToDictionaryAsync(item => item.Slug, cancellationToken);

        foreach (var category in SeedCategories.Where(item => !categoriesBySlug.ContainsKey(item.Slug)))
        {
            var entity = new Category
            {
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                Icon = category.Icon,
                DisplayOrder = category.DisplayOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Categories.Add(entity);
            categoriesBySlug[entity.Slug] = entity;
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded baseline categories.");
        }

        return categoriesBySlug;
    }

    private async Task SeedManagedLanguagesAsync(CancellationToken cancellationToken)
    {
        var existingCodes = await dbContext.ManagedLanguages
            .Select(item => item.Code)
            .ToListAsync(cancellationToken);

        foreach (var definition in SeedLanguages.Where(item => !existingCodes.Contains(item.Code, StringComparer.OrdinalIgnoreCase)))
        {
            dbContext.ManagedLanguages.Add(new ManagedLanguage
            {
                Code = definition.Code,
                DisplayName = definition.DisplayName,
                NativeName = definition.NativeName,
                FlagCode = definition.FlagCode,
                Role = definition.Role,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded managed languages.");
        }
    }

    private async Task SeedPoisAsync(
        IReadOnlyDictionary<string, AppUser> users,
        CancellationToken cancellationToken)
    {
        var ownerId = users[AppConstants.DefaultOwnerEmail].Id;
        var now = DateTime.UtcNow;
        var categoryIdsBySlug = await dbContext.Categories
            .ToDictionaryAsync(item => item.Slug, item => item.Id, cancellationToken);

        if (!await dbContext.Pois.AnyAsync(cancellationToken))
        {
            var pois = SeedPois
                .Select(sample => new Poi
                {
                    Name = sample.Name,
                    Slug = sample.Slug,
                    OwnerId = ownerId,
                    Lat = sample.Lat,
                    Lng = sample.Lng,
                    Priority = sample.Priority,
                    CategoryId = categoryIdsBySlug[sample.CategorySlug],
                    NarrationMode = NarrationMode.Both,
                    Description = sample.TranslationDescription,
                    TtsScript = sample.TranslationStory,
                    MapLink = sample.MapLink,
                    ImageUrl = sample.ImageUrl,
                    Status = PoiStatus.Published,
                    CreatedAt = now
                })
                .ToList();

            dbContext.Pois.AddRange(pois);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} sample POIs.", pois.Count);
        }

        var poiLookup = await dbContext.Pois
            .ToDictionaryAsync(poi => poi.Slug, cancellationToken);

        foreach (var sample in SeedPois)
        {
            var poi = poiLookup[sample.Slug];

            var hasTranslation = await dbContext.PoiTranslations
                .AnyAsync(
                    translation => translation.PoiId == poi.Id && translation.LanguageCode == AppConstants.DefaultLanguage,
                    cancellationToken);

            if (!hasTranslation)
            {
                dbContext.PoiTranslations.Add(new PoiTranslation
                {
                    PoiId = poi.Id,
                    LanguageCode = AppConstants.DefaultLanguage,
                    Title = sample.TranslationTitle,
                    Description = sample.TranslationDescription,
                    Story = sample.TranslationStory,
                    Highlight = sample.Highlight,
                    IsFallback = false
                });
            }

            var hasGeofence = await dbContext.Geofences
                .AnyAsync(
                    geofence => geofence.PoiId == poi.Id && geofence.Name == sample.GeofenceName,
                    cancellationToken);

            if (!hasGeofence)
            {
                dbContext.Geofences.Add(new Geofence
                {
                    PoiId = poi.Id,
                    Name = sample.GeofenceName,
                    RadiusMeters = AppConstants.DefaultGeofenceRadiusMeters,
                    Priority = sample.Priority,
                    DebounceSeconds = AppConstants.DefaultDebounceSeconds,
                    CooldownSeconds = AppConstants.DefaultCooldownSeconds,
                    IsActive = true,
                    TriggerAction = "auto_play",
                    NearestOnly = true
                });
            }
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded sample translations and geofences.");
        }
    }

    private static IReadOnlyList<SeedCategoryDefinition> SeedCategories { get; } =
    [
        new("Hải sản", "hai-san", "Các món hải sản đặc trưng của khu vực Vĩnh Khánh.", "🦐", 10),
        new("Bún/Phở", "bun-pho", "Nhóm món nước và món ăn sáng quen thuộc.", "🍜", 20),
        new("Ăn vặt", "an-vat", "Các món ăn nhanh, món vặt và món ăn đường phố.", "🍢", 30),
        new("Đồ uống", "do-uong", "Các điểm nổi bật về cà phê, trà, nước mát và thức uống địa phương.", "🥤", 40)
    ];

    private static IReadOnlyList<SeedPoiDefinition> SeedPois { get; } =
    [
        new(
            "Cầu Khánh Hội",
            "cau-khanh-hoi",
            10.76091,
            106.70544,
            100,
            "https://maps.google.com/?q=10.76091,106.70544",
            "https://images.unsplash.com/photo-1480714378408-67cf0d13bc1b?auto=format&fit=crop&w=1200&q=80",
            "Cầu Khánh Hội",
            "Cây cầu nối Quận 1 với khu Khánh Hội, ghi dấu nhịp phát triển giao thương ven sông Sài Gòn.",
            "Cầu Khánh Hội là cửa ngõ quan trọng đưa du khách từ trung tâm thành phố sang khu vực Khánh Hội, nơi lưu giữ nhiều lớp ký ức về cảng thị và đời sống ven sông.",
            "Điểm nhìn đẹp ra sông Sài Gòn và khu đô thị trung tâm.",
            "Vùng kích hoạt Cầu Khánh Hội",
            "do-uong"),
        new(
            "Phố Ẩm Thực Vĩnh Khánh",
            "pho-am-thuc-vinh-khanh",
            10.75986,
            106.70188,
            95,
            "https://maps.google.com/?q=10.75986,106.70188",
            "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=1200&q=80",
            "Phố Ẩm Thực Vĩnh Khánh",
            "Khu phố nổi tiếng với nhịp sống sôi động về đêm và nhiều món ăn đặc trưng của Quận 4.",
            "Vĩnh Khánh là điểm dừng chân hấp dẫn với văn hóa ẩm thực đường phố phong phú, phản ánh nhịp sống trẻ và năng động của khu vực Khánh Hội - Vĩnh Hội.",
            "Không gian ẩm thực đặc sắc, nhộn nhịp về đêm.",
            "Vùng kích hoạt Phố Ẩm Thực Vĩnh Khánh",
            "hai-san"),
        new(
            "Chợ Xóm Chiếu",
            "cho-xom-chieu",
            10.75811,
            106.70463,
            90,
            "https://maps.google.com/?q=10.75811,106.70463",
            "https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=1200&q=80",
            "Chợ Xóm Chiếu",
            "Khu chợ truyền thống lâu đời, gắn với đời sống thương hồ và cư dân lao động Quận 4.",
            "Chợ Xóm Chiếu là một trong những không gian sinh hoạt tiêu biểu của Quận 4, nơi du khách có thể cảm nhận rõ nét nhịp sống địa phương và văn hóa mua bán truyền thống.",
            "Điểm trải nghiệm văn hóa bản địa rõ nét nhất khu vực.",
            "Vùng kích hoạt Chợ Xóm Chiếu",
            "an-vat"),
        new(
            "Đình Vĩnh Hội",
            "dinh-vinh-hoi",
            10.75582,
            106.70347,
            85,
            "https://maps.google.com/?q=10.75582,106.70347",
            "https://images.unsplash.com/photo-1533676802871-eca1ae998cd5?auto=format&fit=crop&w=1200&q=80",
            "Đình Vĩnh Hội",
            "Không gian tín ngưỡng lâu đời của cư dân địa phương, phản ánh chiều sâu văn hóa của vùng đất Vĩnh Hội.",
            "Đình Vĩnh Hội lưu giữ kiến trúc và đời sống tín ngưỡng dân gian của cộng đồng cư dân ven kênh rạch, tạo nên một điểm dừng giàu bản sắc giữa đô thị hiện đại.",
            "Không gian văn hóa tâm linh đặc trưng của Quận 4.",
            "Vùng kích hoạt Đình Vĩnh Hội",
            "bun-pho"),
        new(
            "Bến Vân Đồn",
            "ben-van-don",
            10.76337,
            106.69982,
            80,
            "https://maps.google.com/?q=10.76337,106.69982",
            "https://images.unsplash.com/photo-1477959858617-67f85cf4f1df?auto=format&fit=crop&w=1200&q=80",
            "Bến Vân Đồn",
            "Tuyến đường ven kênh với góc nhìn rộng ra trung tâm, từng là trục giao thương quan trọng của khu vực Quận 4.",
            "Bến Vân Đồn gắn với lịch sử phát triển giao thông thủy và mở ra tầm nhìn toàn cảnh về sông, bến cảng và nhịp chuyển động của đô thị Sài Gòn.",
            "Tuyến ven sông phù hợp cho trải nghiệm tham quan bằng audio tự động.",
            "Vùng kích hoạt Bến Vân Đồn",
            "do-uong")
    ];

    private sealed record SeedCategoryDefinition(
        string Name,
        string Slug,
        string Description,
        string Icon,
        int DisplayOrder);

    private static IReadOnlyList<SeedLanguageDefinition> SeedLanguages { get; } =
    [
        new("vi", "Tiếng Việt", "Tiếng Việt", "VN", ManagedLanguageRole.Source),
        new("en", "English", "English", "GB", ManagedLanguageRole.TranslationAudio),
        new("ja", "Japanese", "日本語", "JP", ManagedLanguageRole.TranslationAudio),
        new("ko", "Korean", "한국어", "KR", ManagedLanguageRole.TranslationAudio),
        new("zh", "Chinese", "中文", "CN", ManagedLanguageRole.TranslationAudio)
    ];

    private sealed record SeedPoiDefinition(
        string Name,
        string Slug,
        double Lat,
        double Lng,
        int Priority,
        string MapLink,
        string ImageUrl,
        string TranslationTitle,
        string TranslationDescription,
        string TranslationStory,
        string Highlight,
        string GeofenceName,
        string CategorySlug);

    private sealed record SeedLanguageDefinition(
        string Code,
        string DisplayName,
        string NativeName,
        string FlagCode,
        ManagedLanguageRole Role);
}
