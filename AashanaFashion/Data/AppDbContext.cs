using AashanaFashion.Models;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<ProductionOrder> ProductionOrders { get; set; }
        public DbSet<AppUser> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProductionOrder>()
                .Property(p => p.DesignNumber)
                .IsRequired();

            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();
        }
    }
}
