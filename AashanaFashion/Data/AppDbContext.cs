using AashanaFashion.Models;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<ProductionOrder> ProductionOrders { get; set; }
        public DbSet<ProductionOrderDetail> ProductionOrderDetails { get; set; }
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Design> Designs { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ProductionOrder>()
                .Property(p => p.LotNo)
                .IsRequired();

            modelBuilder.Entity<ProductionOrder>()
                .HasOne(p => p.Design)
                .WithMany()
                .HasForeignKey(p => p.DesignId);

            modelBuilder.Entity<ProductionOrderDetail>()
                .HasOne(d => d.ProductionOrder)
                .WithMany(p => p.Details)
                .HasForeignKey(d => d.ProductionOrderId);

            modelBuilder.Entity<Design>()
                .Property(d => d.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<AppUser>()
                .ToTable("UserList");

            modelBuilder.Entity<UserRole>()
                .HasMany(r => r.Permissions)
                .WithOne(p => p.UserRole)
                .HasForeignKey(p => p.UserRoleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
