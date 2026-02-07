using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Models;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace PROJECT_C_.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        { 
        }

        public DbSet<Food> Foods => Set<Food>();
        public DbSet<FoodTranslation> FoodTranslations => Set<FoodTranslation>();
        public DbSet<AudioFile> AudioFiles => Set<AudioFile>();
        public DbSet<PlayLog> PlayLogs => Set<PlayLog>();
        public DbSet<Tour> Tours => Set<Tour>();
        public DbSet<TourItem> TourItems => Set<TourItem>();
        public DbSet<UserLocation> UserLocations => Set<UserLocation>();
        public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
        public DbSet<Category> Categories => Set<Category>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Food>()
                .Property(f => f.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Food>().HasData(
                new Food
                {
                    Id = 1,
                    Name = "Pho Bo",
                    Description = "Traditional Vietnamese beef noodle soup",
                    Price = 45000,
                    Latitude = 10.776889,
                    Longitude = 106.700806
                },
                new Food
                {
                    Id = 2,
                    Name = "Banh Mi",
                    Description = "Vietnamese baguette sandwich",
                    Price = 25000,
                    Latitude = 10.762622,
                    Longitude = 106.660172
                },
                new Food
                {
                    Id = 3,
                    Name = "Com Tam",
                    Description = "Broken rice with grilled pork",
                    Price = 50000,
                    Latitude = 10.792375,
                    Longitude = 106.691689
                }
            );

            // FoodTranslation Configuration
            modelBuilder.Entity<FoodTranslation>()
                .HasOne(ft => ft.Food)
                .WithMany(f => f.Translations)
                .HasForeignKey(ft => ft.FoodId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed Data for Translations
            modelBuilder.Entity<FoodTranslation>().HasData(
                // Pho Bo
                new FoodTranslation { Id = 1, FoodId = 1, LanguageCode = "vi-VN", Name = "Phở Bò", Description = "Phở bò truyền thống Việt Nam" },
                new FoodTranslation { Id = 2, FoodId = 1, LanguageCode = "en-US", Name = "Beef Pho", Description = "Traditional Vietnamese beef noodle soup" },

                // Banh Mi
                new FoodTranslation { Id = 3, FoodId = 2, LanguageCode = "vi-VN", Name = "Bánh Mì", Description = "Bánh mì Việt Nam" },
                new FoodTranslation { Id = 4, FoodId = 2, LanguageCode = "en-US", Name = "Vietnamese Baguette", Description = "Vietnamese baguette sandwich" },

                // Com Tam
                new FoodTranslation { Id = 5, FoodId = 3, LanguageCode = "vi-VN", Name = "Cơm Tấm", Description = "Cơm tấm sườn" },
                new FoodTranslation { Id = 6, FoodId = 3, LanguageCode = "en-US", Name = "Broken Rice", Description = "Broken rice with grilled pork" }
            );

            // Category seed data
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Ốc & Hải sản", Icon = "🦪", Description = "Các món ốc, nghêu, sò, hải sản" },
                new Category { Id = 2, Name = "Lẩu & Nướng", Icon = "🍲", Description = "Lẩu các loại, đồ nướng BBQ" },
                new Category { Id = 3, Name = "Bún & Phở", Icon = "🍜", Description = "Bún, phở, hủ tiếu, mì" },
                new Category { Id = 4, Name = "Đồ uống", Icon = "🧋", Description = "Nước giải khát, trà sữa, bia" },
                new Category { Id = 5, Name = "Ăn vặt", Icon = "🍡", Description = "Bánh tráng, xiên que, đồ chiên" }
            );

            // Food - Category relationship
            modelBuilder.Entity<Food>()
                .HasOne(f => f.Category)
                .WithMany(c => c.Foods)
                .HasForeignKey(f => f.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            base.OnModelCreating(modelBuilder);
        }


    }

}
