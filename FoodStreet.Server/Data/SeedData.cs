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

            // Seed Roles
            await SeedRoles(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());

            // Seed Admin User
            await SeedAdminUser(userManager);

            // Fix existing foods with English names
            // await FixExistingFoodNames(context);

            // Seed Foods (Vĩnh Khánh specialties)
            // await SeedFoods(context);

            // Seed PlayLogs for Analytics demo
            // await SeedPlayLogs(context);
        }

        private static async Task FixExistingFoodNames(AppDbContext context)
        {
            // Dictionary mapping English names to Vietnamese names
            var nameMapping = new Dictionary<string, (string ViName, string ViDesc)>
            {
                ["Mung Bean Sweet Soup"] = ("Chè Đậu Xanh Đường Phèn", "Chè đậu xanh nấu nhừ với đường phèn thanh mát"),
                ["Vinh Hoi Fresh Spring Rolls"] = ("Gỏi Cuốn Vĩnh Hội", "Gỏi cuốn tôm thịt tươi ngon với nước chấm đậu phộng"),
                ["Mixed Rice Paper Salad"] = ("Bánh Tráng Trộn", "Bánh tráng trộn đầy đủ topping, cay cay chua chua"),
                ["Phnom Penh Style Noodle Soup"] = ("Hủ Tiếu Nam Vang", "Hủ tiếu khô hoặc nước với thịt, tôm, gan heo"),
                ["Vinh Khanh Pork Organ Porridge"] = ("Cháo Lòng Vĩnh Khánh", "Cháo lòng heo truyền thống, nước dùng đậm đà, lòng giòn sần sật"),
                ["Xom Chieu Fermented Fish Noodle Soup"] = ("Bún Mắm Xóm Chiếu", "Bún mắm đặc sản miền Tây với tôm, mực, thịt heo quay"),
                ["Khanh Hoi Steamed Rice Rolls"] = ("Bánh Cuốn Khánh Hội", "Bánh cuốn nóng hổi với nhân thịt và mộc nhĩ"),
                ["Traditional Vietnamese Beef Stew"] = ("Bò Kho Truyền Thống", "Bò kho hầm mềm với bánh mì nóng giòn"),
            };

            var foods = await context.Foods.ToListAsync();
            bool hasChanges = false;

            foreach (var food in foods)
            {
                if (nameMapping.TryGetValue(food.Name, out var viData))
                {
                    food.Name = viData.ViName;
                    food.Description = viData.ViDesc;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "Seller", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task SeedAdminUser(UserManager<IdentityUser> userManager)
        {
            const string adminEmail = "admin@gmail.com";
            const string adminPassword = "Admin@123";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var admin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
            else
            {
                // Ensure existing admin has the role
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }

        private static async Task SeedFoods(AppDbContext context)
        {
            // Skip if already seeded
            if (await context.Foods.CountAsync() > 3)
                return;

            var vinhKhanhLocations = new List<Location>
            {
                new Location { Name = "Quán Cháo Lòng Vĩnh Khánh", Description = "Cháo lòng heo truyền thống", Address = "Đường Vĩnh Khánh, Q.4", Latitude = 10.7524, Longitude = 106.6958, Radius = 50, Priority = 10, IsApproved = true, ImageUrl = "https://images.unsplash.com/photo-1569058242567-93de6f36f8e6?w=400", TtsScript = "Chào mừng bạn đến với quán Cháo Lòng Vĩnh Khánh. Đây là món cháo lòng nổi tiếng nhất khu vực." },
                new Location { Name = "Quán Bún Mắm Xóm Chiếu", Description = "Bún mắm đặc sản miền Tây", Address = "Đường Vĩnh Khánh, Q.4", Latitude = 10.7531, Longitude = 106.6972, Radius = 40, Priority = 9, IsApproved = true, ImageUrl = "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?w=400", TtsScript = "Bún Mắm Xóm Chiếu - món ăn đặc sản miền Tây Nam Bộ." },
                new Location { Name = "Quán Bánh Cuốn Khánh Hội", Description = "Bánh cuốn nóng hổi", Address = "Đường Vĩnh Khánh, Q.4", Latitude = 10.7518, Longitude = 106.6945, Radius = 35, Priority = 8, IsApproved = true, ImageUrl = "https://images.unsplash.com/photo-1563245372-f21724e3856d?w=400", TtsScript = "Bánh Cuốn Khánh Hội - bánh cuốn nóng hổi, mỏng tang." },
                new Location { Name = "Quán Hủ Tiếu Nam Vang", Description = "Hủ tiếu khô hoặc nước", Address = "Đường Vĩnh Khánh, Q.4", Latitude = 10.7542, Longitude = 106.6962, Radius = 45, Priority = 7, IsApproved = true, ImageUrl = "https://images.unsplash.com/photo-1555126634-323283e090fa?w=400", TtsScript = "Hủ Tiếu Nam Vang nổi tiếng với sợi hủ tiếu dai." },
                new Location { Name = "Quán Gỏi Cuốn Vĩnh Hội", Description = "Gỏi cuốn tôm thịt tươi ngon", Address = "Đường Vĩnh Khánh, Q.4", Latitude = 10.7505, Longitude = 106.6935, Radius = 30, Priority = 6, IsApproved = true, ImageUrl = "https://images.unsplash.com/photo-1534422298391-e4f8c172dddb?w=400", TtsScript = "Gỏi Cuốn Vĩnh Hội - gỏi cuốn tươi mát." },
                new Location { Name = "Quán Bò Kho Truyền Thống", Description = "Bò kho hầm mềm", Address = "Đường Vĩnh Khánh, Q.4", Latitude = 10.7555, Longitude = 106.6988, Radius = 50, Priority = 8, IsApproved = true, ImageUrl = "https://images.unsplash.com/photo-1547928576-b822bc410e6c?w=400", TtsScript = "Bò Kho Truyền Thống - thịt bò hầm mềm." },
                new Location { Name = "Quán Chè Đậu Xanh", Description = "Chè đậu xanh đường phèn", Address = "Đường Vĩnh Khánh, Q.4", Latitude = 10.7498, Longitude = 106.6925, Radius = 25, Priority = 5, IsApproved = true, ImageUrl = "https://images.unsplash.com/photo-1563805042-7684c019e1cb?w=400", TtsScript = "Chè Đậu Xanh Đường Phèn - món chè thanh mát." },
                new Location { Name = "Quán Bánh Tráng Trộn", Description = "Bánh tráng trộn đầy đủ topping", Address = "Đường Vĩnh Khánh, Q.4", Latitude = 10.7512, Longitude = 106.6952, Radius = 30, Priority = 4, IsApproved = true, ImageUrl = "https://images.unsplash.com/photo-1559314809-0d155014e29e?w=400", TtsScript = "Bánh Tráng Trộn - snack đường phố phổ biến." }
            };

            context.Locations.AddRange(vinhKhanhLocations);
            await context.SaveChangesAsync();

            // Add foods referencing the locations
            var locations = await context.Locations.Where(l => l.Id > 3).ToListAsync();
            var vinhKhanhFoods = new List<Food>();

            var foodData = new (string Name, string Desc, decimal Price)[]
            {
                ("Cháo Lòng Vĩnh Khánh", "Cháo lòng heo truyền thống, nước dùng đậm đà, lòng giòn sần sật", 35000),
                ("Bún Mắm Xóm Chiếu", "Bún mắm đặc sản miền Tây với tôm, mực, thịt heo quay", 55000),
                ("Bánh Cuốn Khánh Hội", "Bánh cuốn nóng hổi với nhân thịt và mộc nhĩ", 30000),
                ("Hủ Tiếu Nam Vang", "Hủ tiếu khô hoặc nước với thịt, tôm, gan heo", 45000),
                ("Gỏi Cuốn Vĩnh Hội", "Gỏi cuốn tôm thịt tươi ngon với nước chấm đậu phộng", 25000),
                ("Bò Kho Truyền Thống", "Bò kho hầm mềm với bánh mì nóng giòn", 50000),
                ("Chè Đậu Xanh Đường Phèn", "Chè đậu xanh nấu nhừ với đường phèn thanh mát", 15000),
                ("Bánh Tráng Trộn", "Bánh tráng trộn đầy đủ topping, cay cay chua chua", 20000)
            };

            for (int i = 0; i < locations.Count && i < foodData.Length; i++)
            {
                vinhKhanhFoods.Add(new Food
                {
                    Name = foodData[i].Name,
                    Description = foodData[i].Desc,
                    Price = foodData[i].Price,
                    LocationId = locations[i].Id
                });
            }

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


    }
}
