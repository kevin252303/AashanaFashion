using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models;

public class FinancialDashboardViewModel
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit => TotalRevenue - TotalExpenses;
    public decimal ProfitMarginPercent => TotalRevenue > 0
        ? Math.Round(NetProfit / TotalRevenue * 100m, 1)
        : 0m;

    public decimal TotalReceivables { get; set; } // Customer balances due
    public int UnpaidInvoiceCount { get; set; }

    public decimal TotalPayables { get; set; } // Vendor dues

    public decimal TotalOutputGst { get; set; } // GST collected
    public decimal TotalInputGst { get; set; } // GST paid
    public decimal NetGstPayable => Math.Max(0m, TotalOutputGst - TotalInputGst);

    public List<AccountingTransaction> RecentTransactions { get; set; } = new();
    public List<TaxInvoice> RecentInvoices { get; set; } = new();
    public List<TaxInvoice> PendingReceivables { get; set; } = new();
}

public class CreateInvoiceViewModel
{
    [Required]
    [Display(Name = "Invoice Number")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Invoice Date")]
    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Due Date")]
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(15);

    [Display(Name = "Sales Order (Optional)")]
    public int? SalesOrderId { get; set; }

    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Required]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

    [Display(Name = "Customer GSTIN")]
    public string? CustomerGstin { get; set; }

    [Display(Name = "Customer PAN")]
    public string? CustomerPan { get; set; }

    [Display(Name = "Billing Address")]
    public string? BillingAddress { get; set; }

    [Display(Name = "Shipping Address")]
    public string? ShippingAddress { get; set; }

    [Required]
    [Display(Name = "Place of Supply")]
    public string PlaceOfSupply { get; set; } = "Gujarat (24)";

    [Display(Name = "Inter-State Sale (Apply IGST)")]
    public bool IsInterState { get; set; }

    public decimal CgstRate { get; set; } = 2.5m;
    public decimal SgstRate { get; set; } = 2.5m;
    public decimal IgstRate { get; set; } = 5.0m;

    [Display(Name = "Bank Name")]
    public string BankName { get; set; } = "State Bank of India";

    [Display(Name = "Account Number")]
    public string BankAccountNumber { get; set; } = "39201948201";

    [Display(Name = "IFSC Code")]
    public string BankIfsc { get; set; } = "SBIN0001234";

    [Display(Name = "Branch")]
    public string BankBranch { get; set; } = "Surat Textile Market Branch";

    [Display(Name = "Terms & Conditions")]
    public string? TermsAndConditions { get; set; } = "1. Goods once sold will not be taken back.\n2. Interest @ 18% p.a. will be charged if bill is not paid within due date.\n3. Subject to Surat jurisdiction only.";

    [Display(Name = "Invoice Notes")]
    public string? Notes { get; set; }

    public List<InvoiceItemInputModel> Items { get; set; } = new();
}

public class InvoiceItemInputModel
{
    public int? DesignId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string HsnCode { get; set; } = "6204";
    public string? Colour { get; set; }
    public string? Size { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GstRate { get; set; } = 5.0m;
}

public class RecordPaymentInputModel
{
    [Required]
    public int InvoiceId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Required]
    public PaymentMode PaymentMode { get; set; } = PaymentMode.BankTransfer;

    public string? ReferenceNumber { get; set; } // UTR / Cheque / Transaction Ref

    public string? Notes { get; set; }
}

public class RecordVendorPaymentInputModel
{
    [Required]
    public int VendorId { get; set; }

    public int? PurchaseOrderId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Required]
    public PaymentMode PaymentMode { get; set; } = PaymentMode.BankTransfer;

    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }
}
