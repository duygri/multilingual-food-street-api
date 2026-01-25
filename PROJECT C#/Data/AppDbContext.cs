using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Models;

namespace PROJECT_C_.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        { 
        }

        public DbSet<Food> Foods => Set<Food>();

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
                    Price = 45000
                },
                new Food
                {
                    Id = 2,
                    Name = "Banh Mi",
                    Description = "Vietnamese baguette sandwich",
                    Price = 25000
                },
                new Food
                {
                    Id = 3,
                    Name = "Com Tam",
                    Description = "Broken rice with grilled pork",
                    Price = 50000
                }
            );

            base.OnModelCreating(modelBuilder);
        }


    }

}
