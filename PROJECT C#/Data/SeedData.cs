using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;
using PROJECT_C_.Models;

namespace PROJECT_C_.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // Ensure database is created
            await context.Database.MigrateAsync();

            // Seed Admin User
            await SeedAdminUser(userManager);

            // Seed Foods (Vĩnh Khánh specialties)
            await SeedFoods(context);

            // Seed PlayLogs for Analytics demo
            await SeedPlayLogs(context);
        }

        private static async Task SeedAdminUser(UserManager<IdentityUser> userManager)
        {
            const string adminEmail = "admin@vinhkhanh.app";
            const string adminPassword = "Admin@123";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(admin, adminPassword);
            }
        }

        private static async Task SeedFoods(AppDbContext context)
        {
            // Skip if already seeded
            if (await context.Foods.CountAsync() > 3)
                return;

            var vinhKhanhFoods = new List<Food>
            {
                new Food
                {
                    Name = "Cháo Lòng Vĩnh Khánh",
                    Description = "Cháo lòng heo truyền thống, nước dùng đậm đà, lòng giòn sần sật",
                    Price = 35000,
                    Latitude = 10.7524,
                    Longitude = 106.6958,
                    Radius = 50,
                    Priority = 10,
                    ImageUrl = "https://images.unsplash.com/photo-1569058242567-93de6f36f8e6?w=400",
                    TtsScript = "Chào mừng bạn đến với quán Cháo Lòng Vĩnh Khánh. Đây là món cháo lòng nổi tiếng nhất khu vực, với nước dùng đậm đà và lòng heo giòn sần sật."
                },
                new Food
                {
                    Name = "Bún Mắm Xóm Chiếu",
                    Description = "Bún mắm đặc sản miền Tây với tôm, mực, thịt heo quay",
                    Price = 55000,
                    Latitude = 10.7531,
                    Longitude = 106.6972,
                    Radius = 40,
                    Priority = 9,
                    ImageUrl = "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?w=400",
                    TtsScript = "Bún Mắm Xóm Chiếu - món ăn đặc sản miền Tây Nam Bộ với nước dùng mắm đậm đà, tôm tươi, mực giòn và thịt heo quay thơm lừng."
                },
                new Food
                {
                    Name = "Bánh Cuốn Khánh Hội",
                    Description = "Bánh cuốn nóng hổi với nhân thịt và mộc nhĩ",
                    Price = 30000,
                    Latitude = 10.7518,
                    Longitude = 106.6945,
                    Radius = 35,
                    Priority = 8,
                    ImageUrl = "https://images.unsplash.com/photo-1563245372-f21724e3856d?w=400",
                    TtsScript = "Bánh Cuốn Khánh Hội - bánh cuốn nóng hổi, mỏng tang với nhân thịt heo xay và mộc nhĩ giòn. Chấm với nước mắm chua ngọt và chả lụa."
                },
                new Food
                {
                    Name = "Hủ Tiếu Nam Vang",
                    Description = "Hủ tiếu khô hoặc nước với thịt, tôm, gan heo",
                    Price = 45000,
                    Latitude = 10.7542,
                    Longitude = 106.6962,
                    Radius = 45,
                    Priority = 7,
                    ImageUrl = "https://images.unsplash.com/photo-1555126634-323283e090fa?w=400",
                    TtsScript = "Hủ Tiếu Nam Vang nổi tiếng với sợi hủ tiếu dai, nước dùng trong veo, thịt heo băm, tôm tươi và gan heo thái mỏng."
                },
                new Food
                {
                    Name = "Gỏi Cuốn Vĩnh Hội",
                    Description = "Gỏi cuốn tôm thịt tươi ngon với nước chấm đậu phộng",
                    Price = 25000,
                    Latitude = 10.7505,
                    Longitude = 106.6935,
                    Radius = 30,
                    Priority = 6,
                    ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?w=400",
                    TtsScript = "Gỏi Cuốn Vĩnh Hội - gỏi cuốn tươi mát với tôm luộc, thịt heo, bún, rau sống, cuộn trong bánh tráng mỏng. Chấm với nước sốt đậu phộng thơm béo."
                },
                new Food
                {
                    Name = "Bò Kho Truyền Thống",
                    Description = "Bò kho hầm mềm với bánh mì nóng giòn",
                    Price = 50000,
                    Latitude = 10.7555,
                    Longitude = 106.6988,
                    Radius = 50,
                    Priority = 8,
                    ImageUrl = "https://images.unsplash.com/photo-1547928576-b822bc410e6c?w=400",
                    TtsScript = "Bò Kho Truyền Thống - thịt bò hầm mềm trong nước sốt cà ri đậm đà, ăn kèm bánh mì giòn rụm hoặc bún tươi."
                },
                new Food
                {
                    Name = "Chè Đậu Xanh Đường Phèn",
                    Description = "Chè đậu xanh nấu nhừ với đường phèn thanh mát",
                    Price = 15000,
                    Latitude = 10.7498,
                    Longitude = 106.6925,
                    Radius = 25,
                    Priority = 5,
                    ImageUrl = "https://images.unsplash.com/photo-1563805042-7684c019e1cb?w=400",
                    TtsScript = "Chè Đậu Xanh Đường Phèn - món chè thanh mát với đậu xanh hầm nhừ, đường phèn ngọt thanh. Phục vụ cả nóng và lạnh."
                },
                new Food
                {
                    Name = "Bánh Tráng Trộn",
                    Description = "Bánh tráng trộn đầy đủ topping, cay cay chua chua",
                    Price = 20000,
                    Latitude = 10.7512,
                    Longitude = 106.6952,
                    Radius = 30,
                    Priority = 4,
                    ImageUrl = "https://images.unsplash.com/photo-1559314809-0d155014e29e?w=400",
                    TtsScript = "Bánh Tráng Trộn - snack đường phố phổ biến với bánh tráng cắt sợi, trứng cút, khô bò, rau răm, đậu phộng và nước sốt me cay."
                }
            };

            context.Foods.AddRange(vinhKhanhFoods);
            await context.SaveChangesAsync();

            // Add translations
            var foods = await context.Foods.ToListAsync();
            var translations = new List<FoodTranslation>();

            foreach (var food in foods.Skip(3)) // Skip the 3 seeded foods
            {
                translations.Add(new FoodTranslation
                {
                    FoodId = food.Id,
                    LanguageCode = "vi-VN",
                    Name = food.Name,
                    Description = food.Description
                });
                translations.Add(new FoodTranslation
                {
                    FoodId = food.Id,
                    LanguageCode = "en-US",
                    Name = TranslateToEnglish(food.Name),
                    Description = TranslateDescToEnglish(food.Name)
                });
            }

            context.FoodTranslations.AddRange(translations);
            await context.SaveChangesAsync();
        }

        private static string TranslateToEnglish(string name) => name switch
        {
            "Cháo Lòng Vĩnh Khánh" => "Vinh Khanh Pork Organ Porridge",
            "Bún Mắm Xóm Chiếu" => "Xom Chieu Fermented Fish Noodle Soup",
            "Bánh Cuốn Khánh Hội" => "Khanh Hoi Steamed Rice Rolls",
            "Hủ Tiếu Nam Vang" => "Phnom Penh Style Noodle Soup",
            "Gỏi Cuốn Vĩnh Hội" => "Vinh Hoi Fresh Spring Rolls",
            "Bò Kho Truyền Thống" => "Traditional Vietnamese Beef Stew",
            "Chè Đậu Xanh Đường Phèn" => "Mung Bean Sweet Soup",
            "Bánh Tráng Trộn" => "Mixed Rice Paper Salad",
            _ => name
        };

        private static string TranslateDescToEnglish(string name) => name switch
        {
            "Cháo Lòng Vĩnh Khánh" => "Traditional pork organ porridge with rich broth and crispy organs",
            "Bún Mắm Xóm Chiếu" => "Southern Vietnamese fish sauce noodle soup with shrimp, squid and roasted pork",
            "Bánh Cuốn Khánh Hội" => "Hot steamed rice rolls filled with minced pork and wood ear mushroom",
            "Hủ Tiếu Nam Vang" => "Dry or soup noodles with pork, shrimp and pork liver",
            "Gỏi Cuốn Vĩnh Hội" => "Fresh spring rolls with shrimp and pork, served with peanut sauce",
            "Bò Kho Truyền Thống" => "Slow-cooked beef stew served with crispy bread",
            "Chè Đậu Xanh Đường Phèn" => "Refreshing mung bean sweet soup with rock sugar",
            "Bánh Tráng Trộn" => "Mixed rice paper with full toppings, spicy and tangy flavor",
            _ => name
        };

        private static async Task SeedPlayLogs(AppDbContext context)
        {
            // Skip if already has data
            if (await context.PlayLogs.AnyAsync())
                return;

            var foods = await context.Foods.ToListAsync();
            if (!foods.Any()) return;

            var random = new Random(42); // Fixed seed for reproducibility
            var playLogs = new List<PlayLog>();
            var sources = new[] { "qr_scan", "geofence", "manual" };
            var devices = new[] { "mobile", "desktop", "tablet" };

            // Generate 200 sample play logs over the last 30 days
            for (int i = 0; i < 200; i++)
            {
                var food = foods[random.Next(foods.Count)];
                var daysAgo = random.Next(0, 30);
                var hoursAgo = random.Next(0, 24);

                playLogs.Add(new PlayLog
                {
                    FoodId = food.Id,
                    PlayedAt = DateTime.UtcNow.AddDays(-daysAgo).AddHours(-hoursAgo),
                    DurationSeconds = random.Next(10, 120),
                    SessionId = $"session_{random.Next(1, 50)}",
                    DeviceType = devices[random.Next(devices.Length)],
                    Language = random.Next(10) < 7 ? "vi-VN" : "en-US",
                    Source = sources[random.Next(sources.Length)],
                    Latitude = food.Latitude + (random.NextDouble() - 0.5) * 0.001,
                    Longitude = food.Longitude + (random.NextDouble() - 0.5) * 0.001
                });
            }

            context.PlayLogs.AddRange(playLogs);
            await context.SaveChangesAsync();
        }
    }
}
