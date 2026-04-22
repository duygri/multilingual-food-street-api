using Microsoft.EntityFrameworkCore;
using NarrationApp.Server.Data.Entities;

namespace NarrationApp.Server.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Poi> Pois => Set<Poi>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<PoiTranslation> PoiTranslations => Set<PoiTranslation>();

    public DbSet<AudioAsset> AudioAssets => Set<AudioAsset>();

    public DbSet<Geofence> Geofences => Set<Geofence>();

    public DbSet<Tour> Tours => Set<Tour>();

    public DbSet<TourStop> TourStops => Set<TourStop>();

    public DbSet<TourSession> TourSessions => Set<TourSession>();

    public DbSet<QrCode> QrCodes => Set<QrCode>();

    public DbSet<VisitEvent> VisitEvents => Set<VisitEvent>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<ModerationRequest> ModerationRequests => Set<ModerationRequest>();

    public DbSet<ManagedLanguage> ManagedLanguages => Set<ManagedLanguage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureRoles(modelBuilder);
        ConfigureUsers(modelBuilder);
        ConfigureCategories(modelBuilder);
        ConfigurePois(modelBuilder);
        ConfigureTours(modelBuilder);
        ConfigureQrs(modelBuilder);
        ConfigureVisitEvents(modelBuilder);
        ConfigureNotifications(modelBuilder);
        ConfigureModerationRequests(modelBuilder);
        ConfigureManagedLanguages(modelBuilder);
    }

    private static void ConfigureRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Name).HasMaxLength(50).IsRequired();
            entity.Property(role => role.Description).HasMaxLength(250).IsRequired();
            entity.HasIndex(role => role.Name).IsUnique();
        });
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("app_users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.FullName).HasMaxLength(150).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(255).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(user => user.PreferredLanguage).HasMaxLength(10).IsRequired();
            entity.Property(user => user.Phone).HasMaxLength(30);
            entity.Property(user => user.ManagedArea).HasMaxLength(250);
            entity.Property(user => user.CreatedAtUtc)
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasOne(user => user.Role)
                .WithMany(role => role.Users)
                .HasForeignKey(user => user.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).HasMaxLength(120).IsRequired();
            entity.Property(category => category.Slug).HasMaxLength(120).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(500).IsRequired();
            entity.Property(category => category.Icon).HasMaxLength(80).IsRequired();
            entity.HasIndex(category => category.Slug).IsUnique();
            entity.HasIndex(category => new { category.DisplayOrder, category.Name });
        });
    }

    private static void ConfigurePois(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Poi>(entity =>
        {
            entity.ToTable("pois");
            entity.HasKey(poi => poi.Id);
            entity.Property(poi => poi.Name).HasMaxLength(200).IsRequired();
            entity.Property(poi => poi.Slug).HasMaxLength(200).IsRequired();
            entity.Property(poi => poi.Description).HasMaxLength(4000).IsRequired();
            entity.Property(poi => poi.TtsScript).HasMaxLength(8000).IsRequired();
            entity.Property(poi => poi.MapLink).HasMaxLength(500);
            entity.Property(poi => poi.ImageUrl).HasMaxLength(500);
            entity.HasIndex(poi => poi.Slug).IsUnique();
            entity.HasOne(poi => poi.Owner)
                .WithMany(user => user.OwnedPois)
                .HasForeignKey(poi => poi.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(poi => poi.Category)
                .WithMany(category => category.Pois)
                .HasForeignKey(poi => poi.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PoiTranslation>(entity =>
        {
            entity.ToTable("poi_translations");
            entity.HasKey(translation => translation.Id);
            entity.Property(translation => translation.LanguageCode).HasMaxLength(10).IsRequired();
            entity.Property(translation => translation.Title).HasMaxLength(200).IsRequired();
            entity.Property(translation => translation.Description).HasMaxLength(1000).IsRequired();
            entity.Property(translation => translation.Story).HasMaxLength(4000).IsRequired();
            entity.Property(translation => translation.Highlight).HasMaxLength(500).IsRequired();
            entity.HasIndex(translation => new { translation.PoiId, translation.LanguageCode }).IsUnique();
            entity.HasOne(translation => translation.Poi)
                .WithMany(poi => poi.Translations)
                .HasForeignKey(translation => translation.PoiId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AudioAsset>(entity =>
        {
            entity.ToTable("audio_assets");
            entity.HasKey(audio => audio.Id);
            entity.Property(audio => audio.LanguageCode).HasMaxLength(10).IsRequired();
            entity.Property(audio => audio.Provider).HasMaxLength(100).IsRequired();
            entity.Property(audio => audio.StoragePath).HasMaxLength(500).IsRequired();
            entity.Property(audio => audio.Url).HasMaxLength(500).IsRequired();
            entity.HasIndex(audio => new { audio.PoiId, audio.LanguageCode, audio.SourceType }).IsUnique();
            entity.HasOne(audio => audio.Poi)
                .WithMany(poi => poi.AudioAssets)
                .HasForeignKey(audio => audio.PoiId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Geofence>(entity =>
        {
            entity.ToTable("geofences");
            entity.HasKey(geofence => geofence.Id);
            entity.Property(geofence => geofence.Name).HasMaxLength(120).IsRequired();
            entity.Property(geofence => geofence.TriggerAction).HasMaxLength(50).IsRequired();
            entity.HasOne(geofence => geofence.Poi)
                .WithMany(poi => poi.Geofences)
                .HasForeignKey(geofence => geofence.PoiId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTours(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tour>(entity =>
        {
            entity.ToTable("tours");
            entity.HasKey(tour => tour.Id);
            entity.Property(tour => tour.Title).HasMaxLength(200).IsRequired();
            entity.Property(tour => tour.Description).HasMaxLength(2000).IsRequired();
            entity.Property(tour => tour.CoverImage).HasMaxLength(500);
        });

        modelBuilder.Entity<TourStop>(entity =>
        {
            entity.ToTable("tour_stops");
            entity.HasKey(stop => stop.Id);
            entity.HasIndex(stop => new { stop.TourId, stop.Sequence }).IsUnique();
            entity.HasOne(stop => stop.Tour)
                .WithMany(tour => tour.Stops)
                .HasForeignKey(stop => stop.TourId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(stop => stop.Poi)
                .WithMany(poi => poi.TourStops)
                .HasForeignKey(stop => stop.PoiId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TourSession>(entity =>
        {
            entity.ToTable("tour_sessions");
            entity.HasKey(session => session.Id);
            entity.HasIndex(session => new { session.UserId, session.UpdatedAt });
            entity.HasIndex(session => new { session.UserId, session.TourId, session.Status });
            entity.HasOne(session => session.Tour)
                .WithMany()
                .HasForeignKey(session => session.TourId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(session => session.User)
                .WithMany()
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureQrs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QrCode>(entity =>
        {
            entity.ToTable("qr_codes");
            entity.HasKey(code => code.Id);
            entity.Property(code => code.Code).HasMaxLength(80).IsRequired();
            entity.Property(code => code.TargetType).HasMaxLength(50).IsRequired();
            entity.Property(code => code.LocationHint).HasMaxLength(250);
            entity.HasIndex(code => code.Code).IsUnique();
        });
    }

    private static void ConfigureVisitEvents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VisitEvent>(entity =>
        {
            entity.ToTable("visit_events");
            entity.HasKey(visitEvent => visitEvent.Id);
            entity.Property(visitEvent => visitEvent.DeviceId).HasMaxLength(120).IsRequired();
            entity.Property(visitEvent => visitEvent.Source).HasMaxLength(50).IsRequired();
            entity.HasIndex(visitEvent => new { visitEvent.PoiId, visitEvent.CreatedAt });
            entity.HasOne(visitEvent => visitEvent.User)
                .WithMany(user => user.VisitEvents)
                .HasForeignKey(visitEvent => visitEvent.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(visitEvent => visitEvent.Poi)
                .WithMany(poi => poi.VisitEvents)
                .HasForeignKey(visitEvent => visitEvent.PoiId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureNotifications(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.Title).HasMaxLength(200).IsRequired();
            entity.Property(notification => notification.Message).HasMaxLength(1000).IsRequired();
            entity.HasIndex(notification => new { notification.UserId, notification.IsRead });
            entity.HasOne(notification => notification.User)
                .WithMany(user => user.Notifications)
                .HasForeignKey(notification => notification.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureModerationRequests(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ModerationRequest>(entity =>
        {
            entity.ToTable("moderation_requests");
            entity.HasKey(request => request.Id);
            entity.Property(request => request.EntityType).HasMaxLength(50).IsRequired();
            entity.Property(request => request.EntityId).HasMaxLength(100).IsRequired();
            entity.Property(request => request.ReviewNote).HasMaxLength(1000);
            entity.HasIndex(request => new { request.EntityType, request.EntityId, request.Status });
            entity.HasOne(request => request.RequestedByUser)
                .WithMany(user => user.RequestedModerationRequests)
                .HasForeignKey(request => request.RequestedBy)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(request => request.ReviewedByUser)
                .WithMany(user => user.ReviewedModerationRequests)
                .HasForeignKey(request => request.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureManagedLanguages(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ManagedLanguage>(entity =>
        {
            entity.ToTable("managed_languages");
            entity.HasKey(language => language.Code);
            entity.Property(language => language.Code).HasMaxLength(10).IsRequired();
            entity.Property(language => language.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(language => language.NativeName).HasMaxLength(120).IsRequired();
            entity.Property(language => language.FlagCode).HasMaxLength(10).IsRequired();
            entity.HasIndex(language => language.IsActive);
        });
    }
}
