using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models;

public class Vendor
{
    public int Id { get; set; }

    // ——— Existing fields ———
    [Required]
    public string VendorName { get; set; } = string.Empty;
    public string? GstNumber { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PinCode { get; set; }
    public string? PanNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // ——— Purchase ———
    public string? Buyer { get; set; }
    public bool GroupRFQ { get; set; }
    public string? PurchasePaymentTerms { get; set; }
    public string? PurchasePaymentMethod { get; set; }
    public string? Box1099 { get; set; }
    public string? ReceiptReminder { get; set; }
    public string? FiscalPosition { get; set; }
    public string? CompanyId { get; set; }
    public string? Reference { get; set; }
    public string? VendorCompany { get; set; }
    public string? Website { get; set; }
    public string? Industry { get; set; }
    public string? PartnerId { get; set; }

    // ——— Accounting ———
    public string? AccountPayable { get; set; }
    public bool AutoPostBills { get; set; }
    public string? InvoiceReport { get; set; }
    public string? PeppolId { get; set; }
    public string? FollowUpLevel { get; set; }
    public string? FollowUpStatus { get; set; }
    public string? Reminders { get; set; }
    public DateTime? NextReminder { get; set; }
    public string? AccountingResponsible { get; set; }
    public string? JournalItems { get; set; }
    public string? Send { get; set; }
    public decimal? PartnerLimit { get; set; }
    public string? AnalyticDistribution { get; set; }

    // ——— Bank Accounts (existing) ———
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IfscCode { get; set; }

    // ——— Commission Structure ———
    public string? SM1Name { get; set; }
    public decimal? SM1CommissionPct { get; set; }
    public string? SM2Name { get; set; }
    public decimal? SM2CommissionPct { get; set; }
    public string? SM3Name { get; set; }
    public decimal? SM3CommissionPct { get; set; }
    public DateTime? CommissionStartDate { get; set; }
    public DateTime? CommissionEndDate { get; set; }

    // ——— Partner Assignment ———
    public bool Activation { get; set; }
    public int? LevelWeight { get; set; }
    public DateTime? LatestReview { get; set; }
    public DateTime? NextReview { get; set; }
    public DateTime? PartnershipDate { get; set; }
    public decimal? GeoLatitude { get; set; }
    public decimal? GeoLongitude { get; set; }
    public bool ComputeBasedOnAddress { get; set; }

    // ——— Navigation ———
    public List<VendorContact> Contacts { get; set; } = new();
}

public class VendorContact
{
    public int Id { get; set; }
    public int VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactRole { get; set; }
}
