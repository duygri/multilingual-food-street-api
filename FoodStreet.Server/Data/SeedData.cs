using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FoodStreet.Server.Constants;
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

            // Seed POI menu items
            await SeedPoiMenuItems(context);
            await SeedPoiMenuTranslations(context);
        }

        private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { AppRoles.Admin, AppRoles.PoiOwner, AppRoles.Tourist };
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

        private static async Task SeedPoiMenuItems(AppDbContext context)
        {
            if (await context.PoiMenuItems.AnyAsync())
            {
                return;
            }

            var locations = await context.Locations
                .AsNoTracking()
                .OrderBy(location => location.Id)
                .Select(location => new { location.Id, location.Name })
                .ToListAsync();

            if (locations.Count == 0)
            {
                return;
            }

            var seededItems = new List<PoiMenuItem>();

            foreach (var location in locations)
            {
                if (location.Name.Contains("Phở", StringComparison.OrdinalIgnoreCase))
                {
                    seededItems.Add(new PoiMenuItem
                    {
                        LocationId = location.Id,
                        Name = "Phở bò tái nạm",
                        Description = "Nước dùng đậm vị, bánh phở mềm, ăn kèm rau thơm.",
                        Price = 65000m,
                        Currency = "VND",
                        IsAvailable = true,
                        SortOrder = 1,
                        UpdatedAt = DateTime.UtcNow
                    });
                    seededItems.Add(new PoiMenuItem
                    {
                        LocationId = location.Id,
                        Name = "Phở bò viên",
                        Description = "Viên bò mềm, nước phở nóng, phù hợp bữa tối.",
                        Price = 70000m,
                        Currency = "VND",
                        IsAvailable = true,
                        SortOrder = 2,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else if (location.Name.Contains("Bánh Mì", StringComparison.OrdinalIgnoreCase))
                {
                    seededItems.Add(new PoiMenuItem
                    {
                        LocationId = location.Id,
                        Name = "Bánh mì thập cẩm",
                        Description = "Pate, chả lụa, đồ chua và nước sốt nhà làm.",
                        Price = 35000m,
                        Currency = "VND",
                        IsAvailable = true,
                        SortOrder = 1,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    seededItems.Add(new PoiMenuItem
                    {
                        LocationId = location.Id,
                        Name = "Món đặc trưng của quán",
                        Description = "Món nổi bật được đề xuất để khách khám phá tại POI này.",
                        Price = 72000m,
                        Currency = "VND",
                        IsAvailable = true,
                        SortOrder = 1,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            if (seededItems.Count == 0)
            {
                return;
            }

            context.PoiMenuItems.AddRange(seededItems);
            await context.SaveChangesAsync();
        }

        private static async Task SeedPoiMenuTranslations(AppDbContext context)
        {
            if (await context.PoiMenuItemTranslations.AnyAsync())
            {
                return;
            }

            var menuItems = await context.PoiMenuItems
                .AsNoTracking()
                .Include(item => item.Location)
                .OrderBy(item => item.Id)
                .ToListAsync();

            if (menuItems.Count == 0)
            {
                return;
            }

            var translations = new List<PoiMenuItemTranslation>();

            foreach (var menuItem in menuItems)
            {
                var englishName = menuItem.Name switch
                {
                    "Phở bò tái nạm" => "Rare beef pho",
                    "Phở bò viên" => "Beef meatball pho",
                    "Bánh mì thập cẩm" => "Special Vietnamese baguette",
                    "Món đặc trưng của quán" => "Signature dish",
                    _ => $"{menuItem.Name} (EN)"
                };

                var englishDescription = menuItem.Description switch
                {
                    "Nước dùng đậm vị, bánh phở mềm, ăn kèm rau thơm." => "Rich broth, soft noodles, served with fresh herbs.",
                    "Viên bò mềm, nước phở nóng, phù hợp bữa tối." => "Tender beef balls in hot pho broth, ideal for dinner.",
                    "Pate, chả lụa, đồ chua và nước sốt nhà làm." => "Pate, Vietnamese pork roll, pickles, and house-made sauce.",
                    "Món nổi bật được đề xuất để khách khám phá tại POI này." => "A recommended signature dish for visitors at this POI.",
                    _ => menuItem.Description
                };

                translations.Add(new PoiMenuItemTranslation
                {
                    PoiMenuItemId = menuItem.Id,
                    LanguageCode = "en-US",
                    Name = englishName,
                    Description = englishDescription,
                    GeneratedAt = DateTime.UtcNow,
                    IsFallback = true
                });
            }

            if (translations.Count == 0)
            {
                return;
            }

            context.PoiMenuItemTranslations.AddRange(translations);
            await context.SaveChangesAsync();
        }
    }
}
