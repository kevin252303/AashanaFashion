using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models;

public enum InvoicePaymentStatus
{
    [Display(Name = "Unpaid")]
    Unpaid,

    [Display(Name = "Partially Paid")]
    PartiallyPaid,

    [Display(Name = "Paid / Settled")]
    Paid,

    [Display(Name = "Cancelled")]
    Cancelled
}

public enum PaymentMode
{
    [Display(Name = "Bank Transfer (NEFT / RTGS / IMPS)")]
    BankTransfer,

    [Display(Name = "Cheque")]
    Cheque,

    [Display(Name = "UPI (PhonePe / GPay / Paytm)")]
    UPI,

    [Display(Name = "Cash")]
    Cash,

    [Display(Name = "Credit / Debit Card")]
    Card
}

public class TaxInvoice
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(15);

    public int? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Required]
    [StringLength(150)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? CustomerGstin { get; set; }

    [StringLength(20)]
    public string? CustomerPan { get; set; }

    [StringLength(300)]
    public string? BillingAddress { get; set; }

    [StringLength(300)]
    public string? ShippingAddress { get; set; }

    [StringLength(100)]
    public string PlaceOfSupply { get; set; } = "Gujarat (24)";

    public bool IsInterState { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxableAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CgstRate { get; set; } = 2.5m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal CgstAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SgstRate { get; set; } = 2.5m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal SgstAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal IgstRate { get; set; } = 5.0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal IgstAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RoundOff { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GrandTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }

    public decimal BalanceDue => Math.Max(0m, GrandTotal - PaidAmount);

    public InvoicePaymentStatus PaymentStatus { get; set; } = InvoicePaymentStatus.Unpaid;

    // Bank Details for NEFT/RTGS payments
    [StringLength(100)]
    public string BankName { get; set; } = "State Bank of India";

    [StringLength(50)]
    public string BankAccountNumber { get; set; } = "39201948201";

    [StringLength(20)]
    public string BankIfsc { get; set; } = "SBIN0001234";

    [StringLength(100)]
    public string BankBranch { get; set; } = "Surat Textile Market Branch";

    [StringLength(500)]
    public string? TermsAndConditions { get; set; } = "1. Goods once sold will not be taken back.\n2. Interest @ 18% p.a. will be charged if bill is not paid within due date.\n3. Subject to Surat jurisdiction only.";

    [StringLength(500)]
    public string? Notes { get; set; }

    // E-Way Bill & Logistics
    [StringLength(20)]
    public string? EwayBillNumber { get; set; }

    public DateTime? EwayBillDate { get; set; }

    [StringLength(100)]
    public string? TransporterName { get; set; }

    [StringLength(20)]
    public string? TransporterId { get; set; }

    [StringLength(20)]
    public string? VehicleNumber { get; set; }

    public int? DistanceKm { get; set; } = 50;

    [StringLength(10)]
    public string? VehicleType { get; set; } = "R"; // R = Regular

    [StringLength(10)]
    public string? TransMode { get; set; } = "1"; // 1 = Road

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public List<TaxInvoiceItem> Items { get; set; } = new();
    public List<PaymentReceipt> Receipts { get; set; } = new();
}

public class TaxInvoiceItem
{
    public int Id { get; set; }

    public int TaxInvoiceId { get; set; }
    public TaxInvoice? TaxInvoice { get; set; }

    public int? DesignId { get; set; }
    public Design? Design { get; set; }

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [StringLength(20)]
    public string HsnCode { get; set; } = "6204"; // Standard HSN for women's ethnic wear

    [StringLength(50)]
    public string? Colour { get; set; }

    [StringLength(20)]
    public string? Size { get; set; }

    public int Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxableValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GstRate { get; set; } = 5.0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
}

public class PaymentReceipt
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string ReceiptNumber { get; set; } = string.Empty;

    public DateTime PaymentDate { get; set; } = DateTime.Today;

    public int TaxInvoiceId { get; set; }
    public TaxInvoice? TaxInvoice { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    public PaymentMode PaymentMode { get; set; } = PaymentMode.BankTransfer;

    [StringLength(100)]
    public string? ReferenceNumber { get; set; } // UTR / Cheque / Transaction ref

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}

public class VendorPayment
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string VoucherNumber { get; set; } = string.Empty;

    public DateTime PaymentDate { get; set; } = DateTime.Today;

    public int VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public int? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    public PaymentMode PaymentMode { get; set; } = PaymentMode.BankTransfer;

    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
