using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models;

public enum BankFormat
{
    [Display(Name = "HDFC Bank (ENet Corporate)")]
    HdfcENet,

    [Display(Name = "ICICI Bank (CIB Corporate Upload)")]
    IciciCib,

    [Display(Name = "State Bank of India (SBI CMP / Saral)")]
    SbiCmp,

    [Display(Name = "Axis Bank (CMS / Enlite)")]
    AxisCms,

    [Display(Name = "Kotak Mahindra Bank (CMS Payout)")]
    KotakCms,

    [Display(Name = "Universal RBI NEFT / RTGS (All Banks)")]
    StandardNeftRtgs
}

public enum BankPaymentBatchStatus
{
    Draft,
    Exported,
    UploadedToBank,
    Processed,
    Cancelled
}

public enum BeneficiaryType
{
    Vendor,
    Karigar,
    Employee,
    Other
}

[Table("BankPaymentBatches")]
public class BankPaymentBatch : IMustHaveTenant
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;

    public int CompanyId { get; set; } = 1;
    public Company? Company { get; set; }

    [Required]
    [StringLength(50)]
    public string BatchNumber { get; set; } = string.Empty;

    public BankFormat BankFormat { get; set; } = BankFormat.HdfcENet;

    [Required]
    [StringLength(50)]
    public string DebitAccountNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string? DebitBankName { get; set; }

    [StringLength(20)]
    public string? DebitIfsc { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public int TotalBeneficiaries { get; set; }

    public BankPaymentBatchStatus Status { get; set; } = BankPaymentBatchStatus.Draft;

    [StringLength(100)]
    public string? BankReferenceUtr { get; set; }

    public DateTime? ExportedAt { get; set; }

    [StringLength(100)]
    public string? ExportedBy { get; set; }

    public DateTime? ProcessedAt { get; set; }

    [StringLength(100)]
    public string? ProcessedBy { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<BankPaymentBatchItem> Items { get; set; } = new();
}

[Table("BankPaymentBatchItems")]
public class BankPaymentBatchItem : IMustHaveTenant
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;

    public int BankPaymentBatchId { get; set; }
    public BankPaymentBatch? BankPaymentBatch { get; set; }

    public BeneficiaryType BeneficiaryType { get; set; } = BeneficiaryType.Vendor;

    public int? BeneficiaryId { get; set; }

    [Required]
    [StringLength(150)]
    public string BeneficiaryName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string IfscCode { get; set; } = string.Empty;

    [StringLength(100)]
    public string? BankName { get; set; }

    [StringLength(100)]
    public string? BankBranch { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [StringLength(20)]
    public string TransactionType { get; set; } = "NEFT"; // NEFT, RTGS, IFT, IMPS

    [StringLength(100)]
    public string? ReferenceNumber { get; set; } // PO Bill No, Job Slip No, Month-Year

    [StringLength(50)]
    public string? SourceType { get; set; } // "PurchaseOrder", "JobSlip", "SalaryRecord", "DirectVendor"

    public int? SourceId { get; set; }

    [StringLength(100)]
    public string? BeneficiaryEmail { get; set; }

    [StringLength(30)]
    public string? BeneficiaryPhone { get; set; }

    [StringLength(200)]
    public string? Narration { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Exported, Processed, Failed

    [StringLength(100)]
    public string? UtrNumber { get; set; }

    [StringLength(200)]
    public string? ValidationErrors { get; set; }
}

public class PendingPayableItemViewModel
{
    public string Key { get; set; } = string.Empty; // e.g. "po_12", "job_5", "sal_8"
    public BeneficiaryType BeneficiaryType { get; set; }
    public int? BeneficiaryId { get; set; }
    public string BeneficiaryName { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? BankBranch { get; set; }
    public string? AccountNumber { get; set; }
    public string? IfscCode { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string SourceType { get; set; } = string.Empty; // PurchaseOrder, JobSlip, SalaryRecord
    public int SourceId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime ReferenceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PayableAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool HasValidBankDetails { get; set; }
    public string? BankValidationError { get; set; }
}

public class BankBulkPaymentHubViewModel
{
    public Company Company { get; set; } = new();
    public decimal TotalPendingPayable { get; set; }
    public decimal TotalPendingVendorBills { get; set; }
    public decimal TotalPendingJobSlips { get; set; }
    public decimal TotalPendingSalaries { get; set; }
    public int PendingCount { get; set; }

    public List<PendingPayableItemViewModel> VendorBills { get; set; } = new();
    public List<PendingPayableItemViewModel> JobSlips { get; set; } = new();
    public List<PendingPayableItemViewModel> Salaries { get; set; } = new();
    public List<Vendor> AvailableVendors { get; set; } = new();

    public List<BankPaymentBatch> RecentBatches { get; set; } = new();
}

public class CreateBankPaymentBatchInput
{
    public BankFormat BankFormat { get; set; } = BankFormat.HdfcENet;
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public string? CustomDebitAccount { get; set; }
    public string? Notes { get; set; }
    public List<string> SelectedKeys { get; set; } = new();
    public List<decimal>? CustomAmounts { get; set; }
}

public class MarkBatchProcessedInput
{
    public int BatchId { get; set; }
    [Required]
    public string BankReferenceUtr { get; set; } = string.Empty;
    public DateTime ProcessedDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
}
