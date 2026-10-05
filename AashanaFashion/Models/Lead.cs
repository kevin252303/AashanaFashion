using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models;

public enum LeadStage
{
    New,
    Contacted,
    SampleSent,
    QuotationSent,
    Won,
    Lost
}

public class Lead : IMustHaveTenant
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;

    public int CompanyId { get; set; } = 1;
    public Company? Company { get; set; }

    [Required]
    [MaxLength(50)]
    public string LeadNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? CompanyName { get; set; }

    [Required]
    [MaxLength(100)]
    public string ContactPerson { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedValue { get; set; }

    public int EstimatedQuantity { get; set; }

    public LeadStage Stage { get; set; } = LeadStage.New;

    [MaxLength(500)]
    public string? LostReason { get; set; }

    [MaxLength(100)]
    public string? Source { get; set; } = "Direct";

    public int? AssignedToUserId { get; set; }
    public AppUser? AssignedToUser { get; set; }

    public DateTime? ExpectedCloseDate { get; set; }
    public DateTime? NextFollowUpDate { get; set; }

    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? UpdatedDate { get; set; }

    public List<LeadActivity> Activities { get; set; } = new();
}

public class LeadActivity : IMustHaveTenant
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;

    public int LeadId { get; set; }
    public Lead? Lead { get; set; }

    [Required]
    [MaxLength(50)]
    public string ActivityType { get; set; } = "Note"; // Note, Call, WhatsApp, Sample, Quotation, Meeting

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public DateTime ActivityDate { get; set; } = DateTime.Now;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }
}
