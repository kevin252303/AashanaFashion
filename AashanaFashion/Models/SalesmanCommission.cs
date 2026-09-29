using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models;

public enum CommissionBasis
{
    [Display(Name = "Category-wise (Default)")]
    Category,

    [Display(Name = "All Products / Order Total")]
    AllProducts,

    [Display(Name = "Specific Design / Product")]
    Design,

    [Display(Name = "Fixed Amount per Piece")]
    FixedPerPiece
}

public enum CommissionCalcType
{
    [Display(Name = "Percentage (%)")]
    Percentage,

    [Display(Name = "Fixed Amount (₹)")]
    FixedAmount
}

public class CustomerSalesmanCommission
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Required]
    [StringLength(100)]
    public string SalesmanName { get; set; } = string.Empty;

    public int? UserId { get; set; }
    public AppUser? User { get; set; }

    // Basis: Category (default), AllProducts, Design, FixedPerPiece
    public CommissionBasis Basis { get; set; } = CommissionBasis.Category;

    // Target (e.g. Category Name, Design Number, or "All")
    [StringLength(100)]
    public string? TargetValue { get; set; }

    public int? DesignId { get; set; }
    public Design? Design { get; set; }

    public CommissionCalcType CalcType { get; set; } = CommissionCalcType.Percentage;

    [Column(TypeName = "decimal(18,2)")]
    public decimal CommissionRate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(250)]
    public string? Notes { get; set; }
}

public class SalesmanCommissionEntry
{
    public int Id { get; set; }

    public int? TaxInvoiceId { get; set; }
    public TaxInvoice? TaxInvoice { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Required]
    [StringLength(100)]
    public string SalesmanName { get; set; } = string.Empty;

    public DateTime EntryDate { get; set; } = DateTime.Now;

    public CommissionBasis Basis { get; set; } = CommissionBasis.Category;

    [StringLength(100)]
    public string? CategoryOrTarget { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalesAmount { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CommissionRate { get; set; }

    public CommissionCalcType CalcType { get; set; } = CommissionCalcType.Percentage;

    [Column(TypeName = "decimal(18,2)")]
    public decimal CommissionAmount { get; set; }

    public bool IsPaid { get; set; } = false;

    public DateTime? PaidDate { get; set; }

    [StringLength(100)]
    public string? PaymentReference { get; set; }

    public int? AccountingTransactionId { get; set; }
    public AccountingTransaction? AccountingTransaction { get; set; }
}

public class SalesCommissionDashboardViewModel
{
    public decimal TotalCommissionIncurred { get; set; }
    public decimal TotalCommissionPaid { get; set; }
    public decimal TotalCommissionPending { get; set; }
    public int TotalInvoicesWithCommission { get; set; }

    public List<SalesmanSummaryItem> SalesmanSummaries { get; set; } = new();
    public List<SalesmanCommissionEntry> RecentEntries { get; set; } = new();
}

public class SalesmanSummaryItem
{
    public string SalesmanName { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal PaidCommission { get; set; }
    public decimal PendingCommission { get; set; }
    public int InvoiceCount { get; set; }
}
