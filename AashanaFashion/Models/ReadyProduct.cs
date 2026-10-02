using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models;

public enum ReadyProductStatus
{
    [Display(Name = "In Stock")]
    InStock,

    [Display(Name = "Low Stock")]
    LowStock,

    [Display(Name = "Out of Stock")]
    OutOfStock,

    [Display(Name = "Discontinued")]
    Discontinued
}

public enum ReadyProductTransactionType
{
    [Display(Name = "3-Piece Set Assembly (Production)")]
    AssemblyFromProduction,

    [Display(Name = "Manual Inward")]
    ManualInward,

    [Display(Name = "Outward Sales / Dispatch")]
    OutwardSales,

    [Display(Name = "Stock Count Adjustment")]
    StockAdjustment,

    [Display(Name = "Damaged / Defect Removal")]
    DamagedScrapped,

    [Display(Name = "Sample / Showroom Issue")]
    SampleIssue,

    [Display(Name = "Customer Sales Return")]
    CustomerSalesReturn
}

[Table("ReadyProducts")]
public class ReadyProduct : IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; } = 1;
    public int CompanyId { get; set; } = 1;
    public Company? Company { get; set; }

    [Required]
    public int DesignId { get; set; }
    public Design? Design { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Design Number")]
    public string DesignNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Colour { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string Size { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "SKU / Barcode")]
    public string Sku { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Barcode { get; set; }

    // ——— 3-Piece Matching Set Definition ———
    // One complete Ready Product set = 1 Chaniya + 1 Choli + 1 Duppata of identical design, colour, and size
    [Display(Name = "Chaniya Qty / Set")]
    public int ChaniyaQuantityPerSet { get; set; } = 1;

    [Display(Name = "Choli Qty / Set")]
    public int CholiQuantityPerSet { get; set; } = 1;

    [Display(Name = "Duppata Qty / Set")]
    public int DuppataQuantityPerSet { get; set; } = 1;

    [Display(Name = "Set Components Included")]
    public string SetComponents { get; set; } = "Chaniya, Choli, Dupatta";

    // ——— Ready Sets Inventory ———
    [Display(Name = "Ready Sets in Stock")]
    public int QuantityOnHand { get; set; } = 0;

    [Display(Name = "Allocated to Orders")]
    public int AllocatedQuantity { get; set; } = 0;

    [Display(Name = "Available for Sale")]
    public int AvailableQuantity => Math.Max(0, QuantityOnHand - AllocatedQuantity);

    [Display(Name = "Low Stock Alert Threshold")]
    public int MinimumStockAlert { get; set; } = 5;

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Unit Selling Price (₹)")]
    public decimal UnitPrice { get; set; } = 0m;

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Production Cost (₹)")]
    public decimal CostPrice { get; set; } = 0m;

    [StringLength(255)]
    [Display(Name = "Product Image")]
    public string? PhotoPath { get; set; }

    [StringLength(100)]
    [Display(Name = "Warehouse Rack / Location")]
    public string? WarehouseLocation { get; set; }

    [StringLength(500)]
    [Display(Name = "Notes / Remarks")]
    public string? Remarks { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? UpdatedDate { get; set; }

    // Navigation
    public List<ReadyProductTransaction> Transactions { get; set; } = new();

    [NotMapped]
    public ReadyProductStatus CurrentStatus
    {
        get
        {
            if (!IsActive) return ReadyProductStatus.Discontinued;
            if (QuantityOnHand <= 0) return ReadyProductStatus.OutOfStock;
            if (QuantityOnHand <= MinimumStockAlert) return ReadyProductStatus.LowStock;
            return ReadyProductStatus.InStock;
        }
    }

    [NotMapped]
    public string EffectivePhotoPath
    {
        get
        {
            if (!string.IsNullOrEmpty(PhotoPath)) return PhotoPath;
            if (Design?.ColourImages != null && !string.IsNullOrEmpty(Colour))
            {
                var colourMatch = Design.ColourImages.FirstOrDefault(ci => string.Equals(ci.Colour, Colour, StringComparison.OrdinalIgnoreCase));
                if (colourMatch != null && !string.IsNullOrEmpty(colourMatch.PhotoPath))
                    return colourMatch.PhotoPath;
            }
            return Design?.PhotoPath ?? "";
        }
    }
}

[Table("ReadyProductTransactions")]
public class ReadyProductTransaction
{
    public int Id { get; set; }

    public int CompanyId { get; set; } = 1;
    public Company? Company { get; set; }

    [Required]
    public int ReadyProductId { get; set; }
    public ReadyProduct? ReadyProduct { get; set; }

    public ReadyProductTransactionType TransactionType { get; set; }

    // Positive for additions/inward, negative for sales/outward
    public int Quantity { get; set; }

    public int BalanceAfter { get; set; }

    [StringLength(100)]
    public string? ReferenceType { get; set; } // e.g. "PMS Assembly", "Manual Inward", "Sales Order", "Adjustment"

    [StringLength(100)]
    public string? ReferenceNumber { get; set; } // Lot No, SO#, etc.

    [StringLength(500)]
    public string? Notes { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
