using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models;

public class Company : IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; } = 1;
    public Tenant? Tenant { get; set; }

    [Required]
    [StringLength(150)]
    [Display(Name = "Company Name")]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    [Display(Name = "Company Code / Short Name")]
    public string CompanyCode { get; set; } = "AF";

    [StringLength(20)]
    [Display(Name = "GSTIN")]
    public string? Gstin { get; set; }

    [StringLength(20)]
    [Display(Name = "PAN")]
    public string? Pan { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(250)]
    [Display(Name = "Address Line 1")]
    public string? Address1 { get; set; }

    [StringLength(250)]
    [Display(Name = "Address Line 2")]
    public string? Address2 { get; set; }

    [StringLength(100)]
    public string? City { get; set; } = "Surat";

    [StringLength(100)]
    public string? State { get; set; } = "Gujarat";

    [Display(Name = "State Code")]
    public int StateCode { get; set; } = 24;

    [StringLength(20)]
    [Display(Name = "Pin Code")]
    public string? PinCode { get; set; } = "395002";

    // Bank Details for Invoices & Payments
    [StringLength(100)]
    [Display(Name = "Bank Name")]
    public string? BankName { get; set; }

    [StringLength(50)]
    [Display(Name = "Bank Account Number")]
    public string? BankAccountNumber { get; set; }

    [StringLength(30)]
    [Display(Name = "IFSC Code")]
    public string? BankIfsc { get; set; }

    [StringLength(100)]
    [Display(Name = "Branch Name")]
    public string? BankBranch { get; set; }

    [StringLength(100)]
    [Display(Name = "UPI ID / VPA")]
    public string? UpiId { get; set; }

    [StringLength(100)]
    [Display(Name = "Authorized Signatory")]
    public string? AuthorizedSignatory { get; set; } = "Authorized Signatory";

    // Numbering Prefixes & Formats
    [StringLength(20)]
    [Display(Name = "Invoice Prefix")]
    public string InvoicePrefix { get; set; } = "INV-";

    [StringLength(60)]
    [Display(Name = "Invoice Number Format")]
    public string InvoiceNumberFormat { get; set; } = "{PREFIX}{YYYY}{MM}-{0000}";

    [StringLength(20)]
    [Display(Name = "Sales Order Prefix")]
    public string SalesOrderPrefix { get; set; } = "SO-";

    [StringLength(60)]
    [Display(Name = "Sales Order Number Format")]
    public string SalesOrderNumberFormat { get; set; } = "{PREFIX}{YYYY}-{0000}";

    [StringLength(20)]
    [Display(Name = "Purchase Order Prefix")]
    public string PurchaseOrderPrefix { get; set; } = "PO-";

    [StringLength(60)]
    [Display(Name = "Purchase Order Number Format")]
    public string PurchaseOrderNumberFormat { get; set; } = "{PREFIX}{0000}";

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Default Company")]
    public bool IsDefault { get; set; } = false;

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
