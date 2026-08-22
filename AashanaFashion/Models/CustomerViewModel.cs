using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models;

public class CustomerViewModel
{
    public int Id { get; set; }

    [Required]
    public string CustomerName { get; set; } = string.Empty;
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

    // Company info
    public string? CustomerCompany { get; set; }
    public string? Website { get; set; }
    public string? Industry { get; set; }
    public string? Reference { get; set; }
    public string? PartnerId { get; set; }

    // Sales
    public string? Salesperson { get; set; }
    public bool AddDesignOnScan { get; set; }
    public string? SalesPaymentTerms { get; set; }
    public string? SalesPaymentMethod { get; set; }
    public string? Pricelist { get; set; }
    public string? DeliveryMethod { get; set; }
    public string? Transporter { get; set; }
    public decimal? Distance { get; set; }

    // Accounting
    public string? AccountReceivable { get; set; }
    public bool AutoPostBills { get; set; }
    public string? CustomerInvoices { get; set; }
    public string? InvoiceReport { get; set; }
    public string? PeppolId { get; set; }
    public string? FollowUpLevel { get; set; }
    public string? FollowUpStatus { get; set; }
    public string? Reminders { get; set; }
    public DateTime? NextReminder { get; set; }
    public string? AccountingResponsible { get; set; }
    public string? JournalItems { get; set; }
    public string? Send { get; set; }
    public decimal? TotalReceivable { get; set; }
    public decimal? DaysSalesOutstanding { get; set; }
    public decimal? PartnerLimit { get; set; }
    public string? AnalyticDistribution { get; set; }

    // Bank
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? IfscCode { get; set; }

    // Commission
    public string? SM1Name { get; set; }
    public decimal? SM1CommissionPct { get; set; }
    public string? SM2Name { get; set; }
    public decimal? SM2CommissionPct { get; set; }
    public string? SM3Name { get; set; }
    public decimal? SM3CommissionPct { get; set; }
    public DateTime? CommissionStartDate { get; set; }
    public DateTime? CommissionEndDate { get; set; }

    // Partner Assignment
    public bool Activation { get; set; }
    public int? LevelWeight { get; set; }
    public DateTime? LatestReview { get; set; }
    public DateTime? NextReview { get; set; }
    public DateTime? PartnershipDate { get; set; }
    public decimal? GeoLatitude { get; set; }
    public decimal? GeoLongitude { get; set; }
    public bool ComputeBasedOnAddress { get; set; }

    // Contacts (child table)
    public List<CustomerContact> Contacts { get; set; } = new();
}
