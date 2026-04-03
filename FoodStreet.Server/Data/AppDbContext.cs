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

        public DbSet<Location> Locations => Set<Location>();
        public DbSet<LocationTranslation> LocationTranslations => Set<LocationTranslation>();
        public DbSet<AudioFile> AudioFiles => Set<AudioFile>();
        public DbSet<PlayLog> PlayLogs => Set<PlayLog>();
        public DbSet<Tour> Tours => Set<Tour>();
        public DbSet<TourItem> TourItems => Set<TourItem>();
        public DbSet<TourSession> TourSessions => Set<TourSession>();
        public DbSet<PoiMenuItem> PoiMenuItems => Set<PoiMenuItem>();
        public DbSet<PoiMenuItemTranslation> PoiMenuItemTranslations => Set<PoiMenuItemTranslation>();
        public DbSet<UserLocation> UserLocations => Set<UserLocation>();
        public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // === Location Seed Data ===
            modelBuilder.Entity<Location>().HasData(
                new Location
                {
                    Id = 1,
                    Name = "Quán Phở Bò Vĩnh Khánh",
                    Description = "Quán phở bò truyền thống",
                    Address = "Đường Vĩnh Khánh, Q.4, TP.HCM",
                    Latitude = 10.776889,
                    Longitude = 106.700806,
                    Radius = 50,
                    IsApproved = true
                },
                new Location
                {
                    Id = 2,
                    Name = "Tiệm Bánh Mì Sài Gòn",
                    Description = "Bánh mì nóng giòn",
                    Address = "Đường Vĩnh Khánh, Q.4, TP.HCM",
                    Latitude = 10.762622,
                    Longitude = 106.660172,
                    Radius = 50,
                    IsApproved = true
                },
                new Location
                {
                    Id = 3,
                    Name = "Quán Cơm Tấm Bụi",
                    Description = "Cơm tấm sườn nướng",
                    Address = "Đường Vĩnh Khánh, Q.4, TP.HCM",
                    Latitude = 10.792375,
                    Longitude = 106.691689,
                    Radius = 50,
                    IsApproved = true
                }
            );

            // Category seed data
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Ốc & Hải sản", Icon = "🦪", Description = "Các món ốc, nghêu, sò, hải sản" },
                new Category { Id = 2, Name = "Lẩu & Nướng", Icon = "🍲", Description = "Lẩu các loại, đồ nướng BBQ" },
                new Category { Id = 3, Name = "Bún & Phở", Icon = "🍜", Description = "Bún, phở, hủ tiếu, mì" },
                new Category { Id = 4, Name = "Đồ uống", Icon = "🧋", Description = "Nước giải khát, trà sữa, bia" },
                new Category { Id = 5, Name = "Ăn vặt", Icon = "🍡", Description = "Bánh tráng, xiên que, đồ chiên" }
            );

            // Location - Category relationship
            modelBuilder.Entity<Location>()
                .HasOne(l => l.Category)
                .WithMany(c => c.Locations)
                .HasForeignKey(l => l.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PoiMenuItem>()
                .HasOne(menuItem => menuItem.Location)
                .WithMany(location => location.MenuItems)
                .HasForeignKey(menuItem => menuItem.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PoiMenuItemTranslation>()
                .HasOne(translation => translation.PoiMenuItem)
                .WithMany(menuItem => menuItem.Translations)
                .HasForeignKey(translation => translation.PoiMenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PoiMenuItemTranslation>()
                .HasIndex(translation => new { translation.PoiMenuItemId, translation.LanguageCode })
                .IsUnique();

            // LocationTranslation Configuration
            modelBuilder.Entity<LocationTranslation>()
                .HasOne(lt => lt.Location)
                .WithMany(l => l.Translations)
                .HasForeignKey(lt => lt.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TourSession>()
                .HasOne(session => session.Tour)
                .WithMany()
                .HasForeignKey(session => session.TourId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TourSession>()
                .HasIndex(session => session.SessionId)
                .IsUnique();

            modelBuilder.Entity<TourSession>()
                .HasIndex(session => new { session.UserId, session.IsCompleted, session.LastActivityAt });

            modelBuilder.Entity<TourSession>()
                .HasIndex(session => new { session.IsCompleted, session.CurrentLocationId, session.LastActivityAt });

            base.OnModelCreating(modelBuilder);
        }


    }

}
