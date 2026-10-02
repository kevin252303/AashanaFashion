using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models;

public enum ReturnReason
{
    [Display(Name = "Defective / Damaged Piece")]
    Defective,

    [Display(Name = "Size / Fit Mismatch")]
    SizeMismatch,

    [Display(Name = "Wrong Item / Colour Sent")]
    WrongItem,

    [Display(Name = "Customer Cancellation / Rejection")]
    CustomerRejection,

    [Display(Name = "Unsold / Consignment Return")]
    UnsoldStock,

    [Display(Name = "Other")]
    Other
}

public class SalesReturn
{
    public int Id { get; set; }

    public int CompanyId { get; set; } = 1;
    public Company? Company { get; set; }

    [Required]
    [StringLength(50)]
    public string ReturnNumber { get; set; } = string.Empty; // e.g. "RET-202609-0001"

    public DateTime ReturnDate { get; set; } = DateTime.Today;

    public int TaxInvoiceId { get; set; }
    public TaxInvoice? TaxInvoice { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Required]
    [StringLength(150)]
    public string CustomerName { get; set; } = string.Empty;

    public ReturnReason Reason { get; set; } = ReturnReason.CustomerRejection;

    [StringLength(300)]
    public string? Remarks { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GrandTotal { get; set; }

    // Total commission deducted across all salesmen for this return
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCommissionDeducted { get; set; }

    public bool RestockInventory { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public List<SalesReturnItem> Items { get; set; } = new();
}

public class SalesReturnItem
{
    public int Id { get; set; }

    public int SalesReturnId { get; set; }
    public SalesReturn? SalesReturn { get; set; }

    public int? TaxInvoiceItemId { get; set; }
    public TaxInvoiceItem? TaxInvoiceItem { get; set; }

    public int? DesignId { get; set; }
    public Design? Design { get; set; }

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Colour { get; set; }

    [StringLength(30)]
    public string? Size { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxableAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GstRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
}

public class SalesReturnCreateViewModel
{
    public int TaxInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    public DateTime ReturnDate { get; set; } = DateTime.Today;
    public ReturnReason Reason { get; set; } = ReturnReason.CustomerRejection;
    public string? Remarks { get; set; }
    public bool RestockInventory { get; set; } = true;

    public List<SalesReturnItemInput> Items { get; set; } = new();
}

public class SalesReturnItemInput
{
    public int TaxInvoiceItemId { get; set; }
    public int? DesignId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Colour { get; set; }
    public string? Size { get; set; }
    public int OriginalQuantity { get; set; }
    public int AlreadyReturnedQuantity { get; set; }
    public int ReturnQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal GstRate { get; set; }
}
