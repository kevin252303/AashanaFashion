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
        public DbSet<RawMaterialTransaction> RawMaterialTransactions { get; set; }
        public DbSet<ProductionEntity> ProductionEntities { get; set; }
        public DbSet<ProcessTracking> ProcessTrackings { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }
        public DbSet<ProductAttributeLine> ProductAttributeLines { get; set; }
        public DbSet<ProductPricelist> ProductPricelists { get; set; }
        public DbSet<ProductVendor> ProductVendors { get; set; }
        public DbSet<ProductPackaging> ProductPackagings { get; set; }
        public DbSet<VendorContact> VendorContacts { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerContact> CustomerContacts { get; set; }

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

            modelBuilder.Entity<Design>()
                .Property(d => d.SalesPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Design>()
                .Property(d => d.QuantityOnHand)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Design>()
                .Property(d => d.SafetyFactor)
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

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.TransportCharge)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.RoundOff)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PurchaseOrder>()
                .Property(p => p.TransportChargeGST)
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

            modelBuilder.Entity<RawMaterialTransaction>()
                .HasOne(t => t.RawMaterial)
                .WithMany()
                .HasForeignKey(t => t.RawMaterialId);

            modelBuilder.Entity<RawMaterialTransaction>()
                .Property(t => t.Quantity)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RawMaterialTransaction>()
                .Property(t => t.BalanceAfter)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RawMaterial>()
                .Property(m => m.CurrentStock)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RawMaterial>()
                .Property(m => m.MinimumStock)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RawMaterial>()
                .Property(m => m.Rate)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RawMaterialRequirement>()
                .Property(r => r.Quantity)
                .HasColumnType("decimal(18,2)");

            // ——— New Product-related entities ———

            modelBuilder.Entity<ProductAttributeLine>()
                .HasOne(a => a.Design)
                .WithMany(d => d.AttributeLines)
                .HasForeignKey(a => a.DesignId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductPricelist>()
                .HasOne(p => p.Design)
                .WithMany(d => d.Pricelists)
                .HasForeignKey(p => p.DesignId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductPricelist>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ProductPricelist>()
                .Property(p => p.MinQuantity)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ProductVendor>()
                .HasOne(p => p.Design)
                .WithMany(d => d.ProductVendors)
                .HasForeignKey(p => p.DesignId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductVendor>()
                .HasOne(p => p.Vendor)
                .WithMany()
                .HasForeignKey(p => p.VendorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProductVendor>()
                .Property(p => p.Quantity)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ProductVendor>()
                .Property(p => p.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ProductPackaging>()
                .HasOne(p => p.Design)
                .WithMany(d => d.Packagings)
                .HasForeignKey(p => p.DesignId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductPackaging>()
                .Property(p => p.Quantity)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<VendorContact>()
                .HasOne(c => c.Vendor)
                .WithMany(v => v.Contacts)
                .HasForeignKey(c => c.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Vendor>()
                .Property(v => v.PartnerLimit)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Vendor>()
                .Property(v => v.SM1CommissionPct)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Vendor>()
                .Property(v => v.SM2CommissionPct)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Vendor>()
                .Property(v => v.SM3CommissionPct)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Vendor>()
                .Property(v => v.GeoLatitude)
                .HasColumnType("decimal(18,8)");

            modelBuilder.Entity<Vendor>()
                .Property(v => v.GeoLongitude)
                .HasColumnType("decimal(18,8)");

            // ——— Customer-related entities ———

            modelBuilder.Entity<CustomerContact>()
                .HasOne(c => c.Customer)
                .WithMany(c => c.Contacts)
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Customer>()
                .Property(c => c.Distance)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Customer>()
                .Property(c => c.TotalReceivable)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Customer>()
                .Property(c => c.DaysSalesOutstanding)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Customer>()
                .Property(c => c.PartnerLimit)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Customer>()
                .Property(c => c.SM1CommissionPct)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Customer>()
                .Property(c => c.SM2CommissionPct)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Customer>()
                .Property(c => c.SM3CommissionPct)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Customer>()
                .Property(c => c.GeoLatitude)
                .HasColumnType("decimal(18,8)");

            modelBuilder.Entity<Customer>()
                .Property(c => c.GeoLongitude)
                .HasColumnType("decimal(18,8)");
        }
    }
}
