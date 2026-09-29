using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AashanaFashion.Models;

public class ReadyProductIndexViewModel
{
    public List<ReadyProduct> ReadyProducts { get; set; } = new();

    // Summary Metrics
    public int TotalReadySets { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public decimal TotalCostValue { get; set; }
    public int InStockCount { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int TotalDesignsCount { get; set; }

    // Filters
    public string? Search { get; set; }
    public int? DesignId { get; set; }
    public string? Colour { get; set; }
    public string? Size { get; set; }
    public ReadyProductStatus? Status { get; set; }
    public string ViewMode { get; set; } = "grid"; // "grid" or "table"
}

public class ReadyProductFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please select a Design")]
    [Display(Name = "Product / Design")]
    public int DesignId { get; set; }

    [Display(Name = "Design Number")]
    public string DesignNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select or enter a Colour")]
    [StringLength(50)]
    public string Colour { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select or enter a Size")]
    [StringLength(30)]
    public string Size { get; set; } = string.Empty;

    [Display(Name = "SKU / Item Code")]
    public string? Sku { get; set; }

    [Display(Name = "Initial Stock (Complete 3-Piece Sets)")]
    [Range(0, 100000, ErrorMessage = "Stock must be 0 or positive")]
    public int InitialQuantity { get; set; } = 0;

    [Display(Name = "Low Stock Alert Level")]
    public int MinimumStockAlert { get; set; } = 5;

    [Display(Name = "Unit Selling Price (₹)")]
    [Range(0, 1000000)]
    public decimal UnitPrice { get; set; } = 0m;

    [Display(Name = "Unit Production Cost (₹)")]
    [Range(0, 1000000)]
    public decimal CostPrice { get; set; } = 0m;

    [Display(Name = "Warehouse Rack / Bin")]
    public string? WarehouseLocation { get; set; }

    [Display(Name = "Existing Photo")]
    public string? ExistingPhotoPath { get; set; }

    [Display(Name = "Upload Ready Product Photo")]
    public IFormFile? PhotoFile { get; set; }

    [Display(Name = "Remarks / Description")]
    public string? Remarks { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ReadyProductStockAdjustViewModel
{
    public int ReadyProductId { get; set; }
    public string DesignNumber { get; set; } = string.Empty;
    public string Colour { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
    public int CurrentStock { get; set; }

    [Required]
    [Display(Name = "Adjustment Type")]
    public string AdjustmentType { get; set; } = "Add"; // "Add", "Deduct", "SetExact"

    [Required]
    [Range(1, 10000, ErrorMessage = "Quantity must be at least 1")]
    [Display(Name = "Number of 3-Piece Sets")]
    public int Quantity { get; set; } = 1;

    [Display(Name = "Reason / Notes")]
    [Required(ErrorMessage = "Please provide an adjustment reason")]
    public string Reason { get; set; } = string.Empty;

    public string? ReferenceNumber { get; set; }
}

public class ReadyProductAssemblyCandidate
{
    public int DesignId { get; set; }
    public string DesignNumber { get; set; } = string.Empty;
    public string? DesignPhotoPath { get; set; }
    public string Colour { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;

    // Component pieces available in production/WIP
    public int ChaniyaCount { get; set; }
    public int CholiCount { get; set; }
    public int DuppataCount { get; set; }

    // Number of complete sets that can be assembled: Min(Chaniya, Choli, Duppata)
    public int AssembledSetsAvailable => Math.Min(ChaniyaCount, Math.Min(CholiCount, DuppataCount));
    public int ExistingReadyStock { get; set; }
    public int? ExistingReadyProductId { get; set; }
}

public class ReadyProductAssemblyViewModel
{
    public List<ReadyProductAssemblyCandidate> Candidates { get; set; } = new();
    public int TotalPossibleSets => Candidates.Sum(c => c.AssembledSetsAvailable);
}

public class ReadyProductOutwardViewModel
{
    [Required(ErrorMessage = "Please select a Ready Product set")]
    [Display(Name = "Ready Product (3-Piece Set)")]
    public int ReadyProductId { get; set; }

    public string? DesignNumber { get; set; }
    public string? Colour { get; set; }
    public string? Size { get; set; }
    public string? PhotoPath { get; set; }
    public int CurrentStock { get; set; }

    [Required(ErrorMessage = "Please select outward category")]
    [Display(Name = "Outward Purpose / Category")]
    public ReadyProductTransactionType OutwardType { get; set; } = ReadyProductTransactionType.OutwardSales;

    [Required(ErrorMessage = "Please enter quantity to dispatch")]
    [Range(1, 100000, ErrorMessage = "Quantity must be at least 1 set")]
    [Display(Name = "Quantity to Outward (Sets)")]
    public int Quantity { get; set; } = 1;

    [Display(Name = "Recipient / Customer / Destination")]
    [StringLength(150)]
    public string? Recipient { get; set; }

    [Display(Name = "Reference / Challan / Invoice No.")]
    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [Display(Name = "Remarks / Purpose Notes")]
    [StringLength(500)]
    public string? Remarks { get; set; }

    public List<ReadyProduct>? AvailableProducts { get; set; }
}
