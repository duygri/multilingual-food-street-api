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

        var seedUsers = new List<AppUser>
        {
            new()
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
            }
        };

        seedUsers.AddRange(
            SeedPois
                .GroupBy(sample => sample.OwnerEmail, StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var sample = group.First();

                    return new AppUser
                    {
                        Id = sample.OwnerUserId,
                        FullName = sample.OwnerFullName,
                        Email = sample.OwnerEmail,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(AppConstants.DefaultOwnerPassword),
                        PreferredLanguage = AppConstants.DefaultLanguage,
                        CreatedAtUtc = now,
                        LastLoginAtUtc = null,
                        RoleId = roles["poi_owner"].Id,
                        IsActive = true
                    };
                }));

        foreach (var user in seedUsers.Where(user => !usersByEmail.ContainsKey(user.Email)))
        {
            dbContext.AppUsers.Add(user);
            usersByEmail[user.Email] = user;
        }

        var baselineDisplayNames = seedUsers.ToDictionary(user => user.Email, user => user.FullName);

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
                    OwnerId = users[sample.OwnerEmail].Id,
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
            "Ốc Oanh",
            "oc-oanh-vinh-khanh",
            OwnerUserId,
            "Chủ quán Ốc Oanh",
            AppConstants.DefaultOwnerEmail,
            10.7607037,
            106.7032931,
            120,
            "https://www.google.com/maps/place/%E1%BB%90c+Oanh/@10.7607037,106.7032931,815m/data=!3m2!1e3!4b1!4m6!3m5!1s0x31752f6c88168a9d:0x9a8df4610d898dfd!8m2!3d10.7607037!4d106.7032931!16s%2Fg%2F11cjj5j6dq",
            "https://down-vn.img.susercontent.com/vn-11134513-7r98o-lstpv5wpypxgb0",
            "Ốc Oanh Vĩnh Khánh",
            "Quán ốc lâu năm trên đường Vĩnh Khánh, nổi tiếng với hải sản tươi, càng ghẹ rang muối ớt và bạch tuộc nướng.",
            "Ốc Oanh là một trong những quán ốc được nhắc tên nhiều nhất tại phố ẩm thực Vĩnh Khánh. Không gian quán rộng rãi, lên món nhanh và nổi bật với các món như càng ghẹ rang muối ớt, bạch tuộc nướng, chân gà nướng cùng nhiều loại ốc tươi được chế biến đậm vị.",
            "Quán ốc lâu năm nổi bật với càng ghẹ rang muối ớt và bạch tuộc nướng.",
            "Vùng kích hoạt Ốc Oanh",
            "hai-san"),
        new(
            "Ốc Sáu Nở",
            "oc-sau-no-vinh-khanh",
            Guid.Parse("1F049CDC-48E7-49B0-ACBC-BE225E4557BF"),
            "Chủ quán Ốc Sáu Nở",
            "owner-oc-sau-no@narration.app",
            10.7609643,
            106.702942,
            115,
            "https://www.google.com/maps/place/Qu%C3%A1n+%E1%BB%91c+S%C3%A1u+N%E1%BB%9F/@10.7609643,106.702942,815m/data=!3m2!1e3!4b1!4m6!3m5!1s0x31752f6c636b28f5:0x40e9948f06ee87ff!8m2!3d10.7609643!4d106.702942!16s%2Fg%2F11csbq_fb4",
            "https://down-vn.img.susercontent.com/vn-11134259-7r98o-lwc9hefp26e3e1",
            "Quán ốc Sáu Nở",
            "Quán ốc đông khách trên phố Vĩnh Khánh, nổi bật với nước chấm đậm vị và nhiều món ốc kích thước lớn.",
            "Ốc Sáu Nở là địa chỉ quen thuộc của người mê hải sản đêm ở Quận 4. Quán ghi điểm nhờ không gian thoáng, nước chấm đậm đà và các món được gọi nhiều như ốc hương xào bơ, sò điệp nướng phô mai và gân cá ngừ nướng muối.",
            "Nước chấm đậm vị và các món ốc hương xào bơ, sò điệp nướng phô mai rất được yêu thích.",
            "Vùng kích hoạt Ốc Sáu Nở",
            "hai-san"),
        new(
            "Ốc Thảo",
            "oc-thao-vinh-khanh",
            Guid.Parse("A040759D-4991-40FA-A3ED-7EF42564B590"),
            "Chủ quán Ốc Thảo",
            "owner-oc-thao@narration.app",
            10.7616801,
            106.7023637,
            110,
            "https://www.google.com/maps/place/Qu%C3%A1n+%E1%BB%90c+Th%E1%BA%A3o+Qu%E1%BA%ADn+4/@10.7616801,106.7023637,815m/data=!3m2!1e3!4b1!4m6!3m5!1s0x31752f6b70f19ba1:0xcf35a36d36a8210e!8m2!3d10.7616801!4d106.7023637!16s%2Fg%2F11b6dp9p5c",
            "https://down-vn.img.susercontent.com/vn-11134513-7r98o-lstrc6qylw9l10",
            "Ốc Thảo",
            "Quán ốc mặt bằng rộng, dễ tìm trên đường Vĩnh Khánh, nổi bật với hải sản tươi và menu đa dạng từ ốc đến tôm, cua, nghêu.",
            "Ốc Thảo gây ấn tượng bởi mặt tiền rộng và quầy hải sản phong phú ngay khi bước vào quán. Thực khách thường chọn nơi đây để thưởng thức các món ốc nhung, ốc len, tôm, cua và nghêu được trình bày bắt mắt, hợp cho nhóm bạn đi ăn tối.",
            "Menu đa dạng từ ốc, tôm, cua đến nghêu trong không gian rộng rãi, dễ tìm.",
            "Vùng kích hoạt Ốc Thảo",
            "hai-san"),
        new(
            "Ốc Đào",
            "oc-dao-vinh-khanh",
            Guid.Parse("87A046E7-7A42-44BE-9497-F5D8F84BAF8E"),
            "Chủ quán Ốc Đào",
            "owner-oc-dao@narration.app",
            10.7611372,
            106.7049786,
            105,
            "https://www.google.com/maps/place/%E1%BB%90c+%C4%90%C3%A0o+2/@10.7611372,106.7049786,815m/data=!3m2!1e3!4b1!4m6!3m5!1s0x31752f6eba26a0a5:0x527c2e69d3ba0b1f!8m2!3d10.7611372!4d106.7049786!16s%2Fg%2F11f29_mj86",
            "https://down-vn.img.susercontent.com/vn-11134513-7r98o-lstpz6jwanbt62",
            "Ốc Đào phố ẩm thực Vĩnh Khánh",
            "Quán ốc nổi tiếng với công thức nêm nếm gia truyền, nguyên liệu tươi và các món ốc đỏ nướng sa tế, ốc mỡ xào me.",
            "Ốc Đào là điểm dừng quen thuộc của nhiều thực khách muốn tìm vị ốc đậm đà kiểu gia truyền ở Vĩnh Khánh. Dù không gian không quá lớn, quán vẫn luôn đông khách nhờ nguyên liệu tươi, nêm nếm vừa miệng và các món ốc đỏ nướng sa tế, ốc mỡ xào me rất dễ gây nhớ.",
            "Quán ốc nêm vị gia truyền, nổi bật với ốc đỏ nướng sa tế và ốc mỡ xào me.",
            "Vùng kích hoạt Ốc Đào",
            "hai-san"),
        new(
            "Vũ Ốc",
            "vu-oc-vinh-khanh",
            Guid.Parse("A022C5E4-36A4-4847-AF20-00451C7CC242"),
            "Chủ quán Vũ Ốc",
            "owner-vu-oc@narration.app",
            10.7614025,
            106.7027047,
            100,
            "https://www.google.com/maps/place/Qu%C3%A1n+%E1%BB%90c+V%C5%A9/@10.7614078,106.7001298,17z/data=!3m1!4b1!4m6!3m5!1s0x31752fc819a52259:0x2d4448a9a852f49c!8m2!3d10.7614025!4d106.7027047!16s%2Fg%2F11fl9hkgz9",
            "https://down-vn.img.susercontent.com/vn-11134259-7r98o-lwcdwf07ufvd92",
            "Vũ Ốc phố ẩm thực Vĩnh Khánh",
            "Quán ốc quen thuộc của dân sành ăn với mức giá dễ tiếp cận, nước chấm me riêng và menu hải sản phong phú.",
            "Vũ Ốc là cái tên quen thuộc trên đoạn đầu phố ẩm thực Vĩnh Khánh, nơi thực khách có thể gọi nhiều món ốc và hải sản với mức giá khá mềm. Điểm làm nên khác biệt của quán là nước chấm me chua cay và các món sò điệp nướng trứng, ốc nhảy hấp sả, ốc hương muối ớt được nhiều người gọi lại.",
            "Quán ốc giá mềm, nước chấm me riêng và menu hải sản đa dạng.",
            "Vùng kích hoạt Vũ Ốc",
            "hai-san"),
        new(
            "Lãng Quán",
            "lang-quan-vinh-khanh",
            Guid.Parse("3C19371D-AD44-496B-A4E5-8E9586FF3D48"),
            "Chủ quán Lãng Quán",
            "owner-lang-quan@narration.app",
            10.7611131,
            106.7054162,
            95,
            "https://www.google.com/maps/place/L%C3%A3ng+Qu%C3%A1n/@10.7611131,106.7054162,815m/data=!3m2!1e3!4b1!4m6!3m5!1s0x31752f6c04c192d1:0x6c36ef584f8fd49a!8m2!3d10.7611131!4d106.7054162!16s%2Fg%2F11f4qq235d",
            "https://down-vn.img.susercontent.com/vn-11134513-7r98o-lsu0spu6yc9la0",
            "Lãng Quán",
            "Quán ăn vặt và món nướng nổi tiếng đông khách trên Vĩnh Khánh, được nhiều người biết đến với giò heo muối chiên giòn, dồi vịt và răng mực.",
            "Lãng Quán thường được nhắc đến như một điểm tụ tập khuya sôi động trên đường Vĩnh Khánh. Quán có thực đơn rộng từ giò heo muối chiên giòn, dồi vịt, sụn gà, răng mực đến các món nướng hải sản, đi kèm nước chấm cay nhẹ rất hợp khẩu vị.",
            "Điểm tụ tập khuya đông khách với giò heo muối chiên giòn, dồi vịt và nhiều món nướng.",
            "Vùng kích hoạt Lãng Quán",
            "an-vat"),
        new(
            "Ớt Xiêm Quán",
            "ot-xiem-quan-vinh-khanh",
            Guid.Parse("C0215866-E10C-4BBA-937E-2FA399726616"),
            "Chủ quán Ớt Xiêm Quán",
            "owner-ot-xiem@narration.app",
            10.7611663,
            106.7057009,
            90,
            "https://www.google.com/maps/place/%E1%BB%9At+Xi%C3%AAm+Qu%C3%A1n/@10.7611663,106.7057009,815m/data=!3m2!1e3!4b1!4m6!3m5!1s0x31752f6c030b5a8f:0xeecbdff49b6e51ec!8m2!3d10.7611663!4d106.7057009!16s%2Fg%2F11d_wzj_j0",
            "https://down-vn.img.susercontent.com/vn-11134513-7r98o-lstuiauuw0jo72",
            "Ớt Xiêm Quán",
            "Quán ăn được đầu tư trang trí bắt mắt, có menu phong phú từ cua rang me, bò lúc lắc đến mực nướng và các món khô lai rai.",
            "Ớt Xiêm Quán phù hợp với nhóm bạn muốn tìm một chỗ ăn tối đông vui nhưng vẫn dễ gọi món theo khẩu vị chung. Quán nổi bật với các món cua rang me, bò lúc lắc khoai tây, khô mực chiên nước mắm, khô cá sặc gỏi xoài và mực nướng có vị đậm đà vừa ăn.",
            "Quán ăn lên món nhanh, menu đa dạng từ cua rang me đến mực nướng và đồ nhắm.",
            "Vùng kích hoạt Ớt Xiêm Quán",
            "an-vat"),
        new(
            "Chili - Lẩu nướng tự chọn",
            "chili-lau-nuong-tu-chon",
            Guid.Parse("8EBF7AEB-E1D1-44C4-ACF3-96323A3B91FF"),
            "Chủ quán Chili",
            "owner-chili@narration.app",
            10.760693,
            106.7036324,
            85,
            "https://www.google.com/maps/place/Chilli+L%E1%BA%A9u+N%C6%B0%E1%BB%9Bng+Qu%C3%A1n/@10.760693,106.7036324,815m/data=!3m2!1e3!4b1!4m6!3m5!1s0x31752f6c11804f1d:0x44858852dfce7883!8m2!3d10.760693!4d106.7036324!16s%2Fg%2F11b7q6pg9r",
            "https://static.riviu.co/image/2021/01/11/ccd0124ce09dc730141b847fcb9362a0_output.jpeg",
            "Chili - Lẩu nướng tự chọn",
            "Quán lẩu nướng trong hẻm Vĩnh Khánh, nổi bật với đồ nướng tẩm ướp đậm vị và giá khá dễ tiếp cận.",
            "Chili là lựa chọn hợp lý cho những buổi tụ tập thích nướng tại bàn trong không gian nhỏ nhưng luôn sôi động. Thực khách thường gọi bò cuộn nấm, sườn bò Mỹ sốt tiêu đen, ba chỉ cuộn kim chi, lưỡi vịt nướng cùng các món gỏi khai vị lạ miệng.",
            "Quán lẩu nướng tự chọn nổi bật với bò cuộn nấm, sườn bò Mỹ sốt tiêu đen và ba chỉ cuộn kim chi.",
            "Vùng kích hoạt Chili",
            "an-vat"),
        new(
            "Quán Hỏa",
            "quan-hoa-vinh-khanh",
            Guid.Parse("44DC399D-9896-4786-854E-3B4B9FA88C1A"),
            "Chủ quán Hỏa",
            "owner-quan-hoa@narration.app",
            10.7607288,
            106.7044162,
            80,
            "https://maps.google.com/?q=10.7607288,106.7044162",
            "https://down-vn.img.susercontent.com/vn-11134513-7r98o-lstqf54bhi1l5d",
            "Quán Hỏa",
            "Quán lẩu nướng bình dân thu hút nhiều thực khách với vị nêm vừa miệng, hơi cay và nhiều món nướng hải sản dễ ăn.",
            "Quán Hỏa là điểm hẹn quen thuộc của những ai thích lẩu nướng bình dân ở khu Vĩnh Khánh. Các món heo sa tế, heo nướng, mực nướng sa tế, lẩu hàu kim chi và salad trộn thanh mát giúp quán giữ được lượng khách ổn định vào buổi tối.",
            "Quán lẩu nướng bình dân dễ ăn với heo sa tế, mực nướng sa tế và lẩu hàu kim chi.",
            "Vùng kích hoạt Quán Hỏa",
            "an-vat"),
        new(
            "An An quán",
            "an-an-quan-vinh-khanh",
            Guid.Parse("985D08E0-9E9F-48AA-8BE8-0FD8E92E2C6F"),
            "Chủ quán An An",
            "owner-an-an@narration.app",
            10.7605053,
            106.7041936,
            75,
            "https://maps.google.com/?q=10.7605053,106.7041936",
            "https://cdn01.diadiemanuong.com/ddau/640x/an-an-quan-chuyen-lau-nuong-4978cc8f636938456800089919.jpg",
            "An An quán",
            "Quán lẩu nướng mở khuya, phù hợp cho nhóm bạn muốn ăn tối muộn với các món bạch tuộc xông hơi, cơm thố hải sản và sụn gà rang muối.",
            "An An là quán quen của nhiều thực khách thích ngồi ăn khuya trên đường Vĩnh Khánh. Không gian thoáng, giá mềm và các món bạch tuộc xông hơi, cơm thố hải sản, sụn gà rang muối giúp quán luôn được nhắc tên trong nhóm quán lẩu nướng của khu phố.",
            "Quán ăn mở khuya, nổi bật với bạch tuộc xông hơi, cơm thố hải sản và sụn gà rang muối.",
            "Vùng kích hoạt An An quán",
            "an-vat"),
        new(
            "Bún cá Châu Đốc Dì Tư",
            "bun-ca-chau-doc-di-tu",
            Guid.Parse("FDE89F29-6D0A-451D-BD68-47A6D99982DD"),
            "Chủ quán Bún cá Dì Tư",
            "owner-bun-ca-di-tu@narration.app",
            10.7611272,
            106.70665,
            70,
            "https://maps.google.com/?q=10.7611272,106.70665",
            "https://down-vn.img.susercontent.com/vn-11134513-7r98o-lsu0e9oo3gyha2",
            "Bún cá Châu Đốc Dì Tư",
            "Quán bún cá kiểu Châu Đốc với nước lèo nóng, vị đậm đà, ăn kèm rau sống và nước chấm chua ngọt rất bắt vị.",
            "Bún cá Châu Đốc Dì Tư phù hợp cho cả bữa sáng lẫn bữa xế với tô bún cá có thịt cá, chả cá và nước dùng thơm vị nghệ. Điểm khiến nhiều người quay lại là phần nước chấm chua ngọt đi kèm cùng rau sống tươi, giúp tô bún tròn vị hơn.",
            "Tô bún cá kiểu Châu Đốc có nước lèo nóng, rau sống tươi và nước chấm chua ngọt đặc trưng.",
            "Vùng kích hoạt Bún cá Châu Đốc Dì Tư",
            "bun-pho"),
        new(
            "Bún thịt nướng Cô Nga",
            "bun-thit-nuong-co-nga",
            Guid.Parse("F6427336-588C-4310-9975-61287ECE7C6D"),
            "Chủ quán Cô Nga",
            "owner-bun-thit-nuong-co-nga@narration.app",
            10.7606311,
            106.7067776,
            65,
            "https://maps.google.com/?q=10.7606311,106.7067776",
            "https://down-vn.img.susercontent.com/vn-11134513-7r98o-lsv6jcem2duhfb",
            "Bún thịt nướng Cô Nga",
            "Quán bún thịt nướng quen thuộc trên đường Vĩnh Khánh với tô bún đầy đặn gồm thịt nướng, chả giò, rau sống và đồ chua.",
            "Bún thịt nướng Cô Nga là lựa chọn dễ ăn cho những ai muốn đổi vị giữa khu phố vốn nổi tiếng về ốc và đồ nướng. Tô bún ở đây có thịt nướng thơm, chả giò giòn, rau sống tươi và đồ chua cân vị, phù hợp cả khách địa phương lẫn du khách.",
            "Tô bún đầy đặn với thịt nướng, chả giò, rau sống và đồ chua cân vị.",
            "Vùng kích hoạt Bún thịt nướng Cô Nga",
            "bun-pho")
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
        Guid OwnerUserId,
        string OwnerFullName,
        string OwnerEmail,
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
