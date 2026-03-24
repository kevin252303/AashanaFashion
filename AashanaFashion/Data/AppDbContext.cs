using AashanaFashion.Models;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Data
{
    public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // This represents your table in the database
        public DbSet<BatchDashboardViewModel> ProductionOrders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Optional: Seed initial data or configure table constraints here
            modelBuilder.Entity<ProductionOrder>()
                .Property(p => p.OrderNumber)
                .IsRequired();
        }
    }
}
