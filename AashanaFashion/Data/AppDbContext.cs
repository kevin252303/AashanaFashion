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
        public DbSet<ProductExtraCharge> ProductExtraCharges { get; set; }
        public DbSet<VendorContact> VendorContacts { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerContact> CustomerContacts { get; set; }
        public DbSet<Colour> Colours { get; set; }
        public DbSet<Size> Sizes { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Pricelist> Pricelists { get; set; }
        public DbSet<PricelistItem> PricelistItems { get; set; }
        public DbSet<AccountingTransaction> AccountingTransactions { get; set; }
        public DbSet<PurchaseOrderBill> PurchaseOrderBills { get; set; }
        public DbSet<DesignBomItem> DesignBomItems { get; set; }
        public DbSet<DesignOperationCost> DesignOperationCosts { get; set; }
        public DbSet<SalesOrder> SalesOrders { get; set; }
        public DbSet<SalesOrderDetail> SalesOrderDetails { get; set; }
        public DbSet<DeliveryChallan> DeliveryChallans { get; set; }
        public DbSet<DeliveryChallanItem> DeliveryChallanItems { get; set; }
        public DbSet<QualityInspection> QualityInspections { get; set; }
        public DbSet<QualityDefect> QualityDefects { get; set; }
        public DbSet<TaxInvoice> TaxInvoices { get; set; }
        public DbSet<TaxInvoiceItem> TaxInvoiceItems { get; set; }
        public DbSet<PaymentReceipt> PaymentReceipts { get; set; }
        public DbSet<VendorPayment> VendorPayments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
        public DbSet<SalaryRecord> SalaryRecords { get; set; }
        public DbSet<BiometricDevice> BiometricDevices { get; set; }
        public DbSet<ReadyProduct> ReadyProducts { get; set; }
        public DbSet<ReadyProductTransaction> ReadyProductTransactions { get; set; }
        public DbSet<CustomerSalesmanCommission> CustomerSalesmanCommissions { get; set; }
        public DbSet<SalesmanCommissionEntry> SalesmanCommissionEntries { get; set; }
        public DbSet<SalesReturn> SalesReturns { get; set; }
        public DbSet<SalesReturnItem> SalesReturnItems { get; set; }
        public DbSet<DesignColourImage> DesignColourImages { get; set; }
        public DbSet<DesignDiscontinuedVariant> DesignDiscontinuedVariants { get; set; }
        public DbSet<ProcessMaster> ProcessMasters { get; set; }
        public DbSet<DesignComponentAssignment> DesignComponentAssignments { get; set; }
        public DbSet<ProductionOrderComponentAssignment> ProductionOrderComponentAssignments { get; set; }
        public DbSet<JobSlip> JobSlips { get; set; }
        public DbSet<JobSlipItem> JobSlipItems { get; set; }
        public DbSet<BarcodeTagConfig> BarcodeTagConfigs { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<UserCompany> UserCompanies { get; set; }

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

            modelBuilder.Entity<ProductionOrder>()
                .HasOne(p => p.HandworkWorker)
                .WithMany()
                .HasForeignKey(p => p.HandworkWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionOrder>()
                .HasOne(p => p.StitchingWorker)
                .WithMany()
                .HasForeignKey(p => p.StitchingWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Design>()
                .HasOne(d => d.HandworkWorker)
                .WithMany()
                .HasForeignKey(d => d.HandworkWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Design>()
                .HasOne(d => d.StitchingWorker)
                .WithMany()
                .HasForeignKey(d => d.StitchingWorkerId)
                .OnDelete(DeleteBehavior.Restrict);

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

            modelBuilder.Entity<ProcessTracking>()
                .HasOne(t => t.Vendor)
                .WithMany()
                .HasForeignKey(t => t.VendorId)
                .OnDelete(DeleteBehavior.NoAction);

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

            modelBuilder.Entity<PurchaseOrderDetail>()
                .HasOne(d => d.RawMaterial)
                .WithMany()
                .HasForeignKey(d => d.RawMaterialId)
                .OnDelete(DeleteBehavior.SetNull);

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

            modelBuilder.Entity<RawMaterialTransaction>()
                .Property(t => t.UnitPrice)
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

            modelBuilder.Entity<DesignBomItem>()
                .HasOne(b => b.Design)
                .WithMany(d => d.BomItems)
                .HasForeignKey(b => b.DesignId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DesignBomItem>()
                .HasOne(b => b.RawMaterial)
                .WithMany()
                .HasForeignKey(b => b.RawMaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DesignBomItem>()
                .Property(b => b.QuantityPerPiece)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<DesignBomItem>()
                .Property(b => b.WastagePercentage)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<DesignOperationCost>()
                .HasOne(o => o.Design)
                .WithMany(d => d.OperationCosts)
                .HasForeignKey(o => o.DesignId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DesignOperationCost>()
                .Property(o => o.EstimatedCost)
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

            // --- Pricelist entities ---
            modelBuilder.Entity<Pricelist>()
                .HasMany(p => p.Items)
                .WithOne(i => i.Pricelist)
                .HasForeignKey(i => i.PricelistId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Customer>()
                .HasOne(c => c.PricelistMaster)
                .WithMany(p => p.Customers)
                .HasForeignKey(c => c.PricelistId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PricelistItem>()
                .HasOne(i => i.Design)
                .WithMany()
                .HasForeignKey(i => i.DesignId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AccountingTransaction>()
                .Property(a => a.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<AccountingTransaction>()
                .HasOne(a => a.Vendor)
                .WithMany()
                .HasForeignKey(a => a.VendorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AccountingTransaction>()
                .HasOne(a => a.Customer)
                .WithMany()
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PurchaseOrderBill>()
                .HasOne(b => b.PurchaseOrder)
                .WithMany(p => p.Bills)
                .HasForeignKey(b => b.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // ——— Sales Orders & Delivery Challans ———
            modelBuilder.Entity<SalesOrder>()
                .HasOne(s => s.Customer)
                .WithMany()
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesOrder>()
                .HasOne(s => s.Pricelist)
                .WithMany()
                .HasForeignKey(s => s.PricelistId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalesOrder>()
                .Property(s => s.TransportCharge)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SalesOrder>()
                .Property(s => s.TransportChargeGST)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SalesOrder>()
                .Property(s => s.RoundOff)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SalesOrder>()
                .Property(s => s.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SalesOrderDetail>()
                .HasOne(d => d.SalesOrder)
                .WithMany(s => s.Details)
                .HasForeignKey(d => d.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalesOrderDetail>()
                .HasOne(d => d.Design)
                .WithMany()
                .HasForeignKey(d => d.DesignId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalesOrderDetail>()
                .Property(d => d.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SalesOrderDetail>()
                .Property(d => d.DiscountPercentage)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SalesOrderDetail>()
                .Property(d => d.GstPercentage)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<DeliveryChallan>()
                .HasOne(c => c.SalesOrder)
                .WithMany(s => s.Challans)
                .HasForeignKey(c => c.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DeliveryChallan>()
                .HasOne(c => c.Customer)
                .WithMany()
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DeliveryChallanItem>()
                .HasOne(i => i.DeliveryChallan)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.DeliveryChallanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DeliveryChallanItem>()
                .HasOne(i => i.SalesOrderDetail)
                .WithMany()
                .HasForeignKey(i => i.SalesOrderDetailId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<QualityInspection>()
                .HasOne(q => q.ProductionOrder)
                .WithMany()
                .HasForeignKey(q => q.ProductionOrderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<QualityInspection>()
                .HasOne(q => q.ProductionEntity)
                .WithMany()
                .HasForeignKey(q => q.ProductionEntityId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<QualityInspection>()
                .HasOne(q => q.Vendor)
                .WithMany()
                .HasForeignKey(q => q.VendorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<QualityDefect>()
                .HasOne(d => d.QualityInspection)
                .WithMany(q => q.Defects)
                .HasForeignKey(d => d.QualityInspectionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductionEntity>()
                .Property(e => e.Barcode)
                .HasMaxLength(50);

            modelBuilder.Entity<ProductionEntity>()
                .HasIndex(e => e.Barcode);

            modelBuilder.Entity<TaxInvoice>()
                .HasOne(i => i.Customer)
                .WithMany()
                .HasForeignKey(i => i.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaxInvoice>()
                .HasOne(i => i.SalesOrder)
                .WithMany()
                .HasForeignKey(i => i.SalesOrderId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TaxInvoiceItem>()
                .HasOne(item => item.TaxInvoice)
                .WithMany(i => i.Items)
                .HasForeignKey(item => item.TaxInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaxInvoiceItem>()
                .HasOne(item => item.Design)
                .WithMany()
                .HasForeignKey(item => item.DesignId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PaymentReceipt>()
                .HasOne(r => r.TaxInvoice)
                .WithMany(i => i.Receipts)
                .HasForeignKey(r => r.TaxInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaymentReceipt>()
                .HasOne(r => r.Customer)
                .WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<VendorPayment>()
                .HasOne(p => p.Vendor)
                .WithMany()
                .HasForeignKey(p => p.VendorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VendorPayment>()
                .HasOne(p => p.PurchaseOrder)
                .WithMany()
                .HasForeignKey(p => p.PurchaseOrderId)
                .OnDelete(DeleteBehavior.SetNull);

            // ——— HR, Attendance & Payroll entities ———
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.EmployeeCode)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .Property(e => e.BaseRate)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Employee>()
                .Property(e => e.StandardDailyHours)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Employee>()
                .Property(e => e.OvertimeHourlyRate)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<AttendanceRecord>()
                .HasOne(a => a.Employee)
                .WithMany(e => e.AttendanceRecords)
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AttendanceRecord>()
                .Property(a => a.TotalHours)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<AttendanceRecord>()
                .Property(a => a.OvertimeHours)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SalaryRecord>()
                .HasOne(s => s.Employee)
                .WithMany(e => e.SalaryRecords)
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalaryRecord>()
                .Property(s => s.DaysPresent)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SalaryRecord>()
                .Property(s => s.DaysAbsent)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SalaryRecord>()
                .Property(s => s.TotalHoursWorked)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SalaryRecord>()
                .Property(s => s.TotalOvertimeHours)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SalaryRecord>()
                .Property(s => s.BaseSalaryEarned)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SalaryRecord>()
                .Property(s => s.OvertimePay)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SalaryRecord>()
                .Property(s => s.BonusAllowance)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SalaryRecord>()
                .Property(s => s.Deductions)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<SalaryRecord>()
                .Property(s => s.NetSalary)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<BiometricDevice>()
                .HasIndex(d => d.DeviceIdentifier)
                .IsUnique();

            modelBuilder.Entity<AttendanceRecord>()
                .HasOne(a => a.Device)
                .WithMany(d => d.AttendanceRecords)
                .HasForeignKey(a => a.DeviceId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProductCategory>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasIndex(c => c.CategoryName).IsUnique();
                entity.HasOne(c => c.ParentCategory)
                    .WithMany(p => p.SubCategories)
                    .HasForeignKey(c => c.ParentCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Design>(entity =>
            {
                entity.HasOne(d => d.ProductCategory)
                    .WithMany(c => c.Products)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ProductExtraCharge>(entity =>
            {
                entity.HasOne(e => e.Design)
                    .WithMany(d => d.ExtraCharges)
                    .HasForeignKey(e => e.DesignId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ReadyProduct>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.HasOne(r => r.Design)
                    .WithMany()
                    .HasForeignKey(r => r.DesignId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(r => r.Transactions)
                    .WithOne(t => t.ReadyProduct)
                    .HasForeignKey(t => t.ReadyProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(r => new { r.DesignId, r.Colour, r.Size }).IsUnique();
            });

            modelBuilder.Entity<CustomerSalesmanCommission>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasOne(c => c.Customer)
                    .WithMany(cust => cust.Commissions)
                    .HasForeignKey(c => c.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(c => c.Design)
                    .WithMany()
                    .HasForeignKey(c => c.DesignId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<SalesmanCommissionEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.TaxInvoice)
                    .WithMany()
                    .HasForeignKey(e => e.TaxInvoiceId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.AccountingTransaction)
                    .WithMany()
                    .HasForeignKey(e => e.AccountingTransactionId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<SalesReturn>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.HasOne(r => r.TaxInvoice)
                    .WithMany(i => i.Returns)
                    .HasForeignKey(r => r.TaxInvoiceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Customer)
                    .WithMany()
                    .HasForeignKey(r => r.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(r => r.Items)
                    .WithOne(i => i.SalesReturn)
                    .HasForeignKey(i => i.SalesReturnId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SalesReturnItem>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.HasOne(i => i.Design)
                    .WithMany()
                    .HasForeignKey(i => i.DesignId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(i => i.TaxInvoiceItem)
                    .WithMany()
                    .HasForeignKey(i => i.TaxInvoiceItemId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<DesignColourImage>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasOne(c => c.Design)
                    .WithMany(d => d.ColourImages)
                    .HasForeignKey(c => c.DesignId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DesignDiscontinuedVariant>(entity =>
            {
                entity.HasKey(v => v.Id);
                entity.HasOne(v => v.Design)
                    .WithMany(d => d.DiscontinuedVariants)
                    .HasForeignKey(v => v.DesignId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProcessMaster>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.ProcessName).IsRequired().HasMaxLength(100);
                entity.Property(p => p.ProcessCode).HasMaxLength(30);
                entity.HasIndex(p => p.DisplayOrder);
            });

            modelBuilder.Entity<DesignComponentAssignment>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.ComponentName).IsRequired().HasMaxLength(100);
                entity.Property(a => a.ProcessName).IsRequired().HasMaxLength(100);
                entity.HasOne(a => a.Design)
                    .WithMany(d => d.ComponentAssignments)
                    .HasForeignKey(a => a.DesignId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.Vendor)
                    .WithMany()
                    .HasForeignKey(a => a.VendorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProductionOrderComponentAssignment>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.ComponentName).IsRequired().HasMaxLength(100);
                entity.Property(a => a.ProcessName).IsRequired().HasMaxLength(100);
                entity.HasOne(a => a.ProductionOrder)
                    .WithMany(o => o.ComponentAssignments)
                    .HasForeignKey(a => a.ProductionOrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.Vendor)
                    .WithMany()
                    .HasForeignKey(a => a.VendorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<JobSlip>(entity =>
            {
                entity.HasKey(j => j.Id);
                entity.Property(j => j.SlipNumber).IsRequired().HasMaxLength(60);
                entity.Property(j => j.ProcessName).IsRequired().HasMaxLength(100);
                entity.HasOne(j => j.ProductionOrder)
                    .WithMany(o => o.JobSlips)
                    .HasForeignKey(j => j.ProductionOrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(j => j.Vendor)
                    .WithMany()
                    .HasForeignKey(j => j.VendorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<JobSlipItem>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.ComponentName).IsRequired().HasMaxLength(100);
                entity.HasOne(i => i.JobSlip)
                    .WithMany(j => j.Items)
                    .HasForeignKey(i => i.JobSlipId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ——— Multi-Company Entity Configuration & Seeding ———
            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.CompanyName).IsRequired().HasMaxLength(150);
                entity.Property(c => c.CompanyCode).IsRequired().HasMaxLength(20);

                entity.HasData(new Company
                {
                    Id = 1,
                    CompanyName = "Aashana Fashion",
                    CompanyCode = "AF",
                    Gstin = "24AABCA1234F1Z8",
                    Pan = "AABCA1234F",
                    Email = "sales@aashanafashion.com",
                    Phone = "+91 98765 43210",
                    Address1 = "101-104, Surat Textile Market, Ring Road",
                    Address2 = "Ring Road",
                    City = "Surat",
                    State = "Gujarat",
                    StateCode = 24,
                    PinCode = "395002",
                    BankName = "HDFC Bank",
                    BankAccountNumber = "50200012345678",
                    BankIfsc = "HDFC0001234",
                    BankBranch = "Ring Road Branch, Surat",
                    InvoicePrefix = "INV-",
                    SalesOrderPrefix = "SO-",
                    PurchaseOrderPrefix = "PO-",
                    IsActive = true,
                    IsDefault = true,
                    CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)
                });
            });

            modelBuilder.Entity<UserCompany>(entity =>
            {
                entity.HasKey(uc => uc.Id);
                entity.HasOne(uc => uc.User)
                    .WithMany()
                    .HasForeignKey(uc => uc.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(uc => uc.Company)
                    .WithMany()
                    .HasForeignKey(uc => uc.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ReadyProduct>(entity =>
            {
                entity.Property(r => r.CompanyId).HasDefaultValue(1);
                entity.HasOne(r => r.Company)
                    .WithMany()
                    .HasForeignKey(r => r.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ReadyProductTransaction>(entity =>
            {
                entity.Property(r => r.CompanyId).HasDefaultValue(1);
                entity.HasOne(r => r.Company)
                    .WithMany()
                    .HasForeignKey(r => r.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TaxInvoice>(entity =>
            {
                entity.Property(t => t.CompanyId).HasDefaultValue(1);
                entity.HasOne(t => t.Company)
                    .WithMany()
                    .HasForeignKey(t => t.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PaymentReceipt>(entity =>
            {
                entity.Property(p => p.CompanyId).HasDefaultValue(1);
                entity.HasOne(p => p.Company)
                    .WithMany()
                    .HasForeignKey(p => p.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SalesOrder>(entity =>
            {
                entity.Property(s => s.CompanyId).HasDefaultValue(1);
                entity.HasOne(s => s.Company)
                    .WithMany()
                    .HasForeignKey(s => s.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PurchaseOrder>(entity =>
            {
                entity.Property(p => p.CompanyId).HasDefaultValue(1);
                entity.HasOne(p => p.Company)
                    .WithMany()
                    .HasForeignKey(p => p.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProductionOrder>(entity =>
            {
                entity.Property(p => p.CompanyId).HasDefaultValue(1);
                entity.HasOne(p => p.Company)
                    .WithMany()
                    .HasForeignKey(p => p.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SalesReturn>(entity =>
            {
                entity.Property(s => s.CompanyId).HasDefaultValue(1);
                entity.HasOne(s => s.Company)
                    .WithMany()
                    .HasForeignKey(s => s.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AccountingTransaction>(entity =>
            {
                entity.Property(a => a.CompanyId).HasDefaultValue(1);
                entity.HasOne(a => a.Company)
                    .WithMany()
                    .HasForeignKey(a => a.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Design>(entity =>
            {
                entity.HasOne(d => d.CompanyRef)
                    .WithMany()
                    .HasForeignKey(d => d.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
