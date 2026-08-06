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
        public DbSet<DyingEntry> DyingEntries { get; set; }
        public DbSet<RollPressEntry> RollPressEntries { get; set; }
        public DbSet<RawMaterial> RawMaterials { get; set; }
        public DbSet<RawMaterialRequirement> RawMaterialRequirements { get; set; }
        public DbSet<ProductionEntity> ProductionEntities { get; set; }
        public DbSet<ProcessTracking> ProcessTrackings { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }

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

            modelBuilder.Entity<ProductionEntity>()
                .HasOne(e => e.ProductionOrder)
                .WithMany()
                .HasForeignKey(e => e.ProductionOrderId);

            modelBuilder.Entity<ProcessTracking>()
                .HasOne(p => p.ProductionEntity)
                .WithMany(e => e.ProcessTrackings)
                .HasForeignKey(p => p.ProductionEntityId);

            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(p => p.Vendor)
                .WithMany()
                .HasForeignKey(p => p.VendorId);

            modelBuilder.Entity<PurchaseOrder>()
                .HasMany(p => p.Details)
                .WithOne(d => d.PurchaseOrder)
                .HasForeignKey(d => d.PurchaseOrderId);

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PurchaseOrderDetail>()
                .Property(d => d.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PurchaseOrderDetail>()
                .Property(d => d.GstPercentage)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PurchaseOrderDetail>()
                .Property(d => d.DiscountPercentage)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.TransportCharge)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.RoundOff)
                .HasColumnType("decimal(18,2)");
        }
    }
}
