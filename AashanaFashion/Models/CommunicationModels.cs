using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models;

public enum CommunicationChannel
{
    [Display(Name = "WhatsApp")]
    WhatsApp,

    [Display(Name = "Email")]
    Email,

    [Display(Name = "Internal Note")]
    InternalNote,

    [Display(Name = "SMS")]
    SMS
}

public enum CommunicationStatus
{
    [Display(Name = "Sent")]
    Sent,

    [Display(Name = "Delivered")]
    Delivered,

    [Display(Name = "Failed")]
    Failed,

    [Display(Name = "Draft / Queued")]
    Draft
}

public class CommunicationLog : IMustHaveTenant
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;

    [Required]
    [StringLength(50)]
    public string DocumentType { get; set; } = string.Empty; // "SalesOrder", "TaxInvoice", "DeliveryChallan", "JobSlip", "PaymentReceipt", "Lead"

    public int DocumentId { get; set; }

    [StringLength(100)]
    public string DocumentReference { get; set; } = string.Empty; // e.g. "INV-2026-001", "SO-1001", "DC-501"

    public CommunicationChannel Channel { get; set; } = CommunicationChannel.WhatsApp;

    [StringLength(200)]
    public string Recipient { get; set; } = string.Empty; // Phone number or Email address

    [StringLength(150)]
    public string? RecipientName { get; set; }

    [StringLength(250)]
    public string? Subject { get; set; }

    public string Body { get; set; } = string.Empty;

    public CommunicationStatus Status { get; set; } = CommunicationStatus.Sent;

    [StringLength(150)]
    public string? ExternalMessageId { get; set; }

    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    public DateTime SentAt { get; set; } = DateTime.Now;

    [StringLength(100)]
    public string SentBy { get; set; } = "System";
}

public class DocumentChatterViewModel
{
    public string DocumentType { get; set; } = string.Empty;
    public int DocumentId { get; set; }
    public string DocumentReference { get; set; } = string.Empty;
    public string? DefaultRecipientPhone { get; set; }
    public string? DefaultRecipientEmail { get; set; }
    public string? DefaultRecipientName { get; set; }
    public List<CommunicationLog> Logs { get; set; } = new();
    public string? PreFilledWhatsAppText { get; set; }
    public string? PreFilledEmailSubject { get; set; }
    public string? PreFilledEmailHtml { get; set; }
}

public class SendCommunicationInputModel
{
    [Required]
    public string DocumentType { get; set; } = string.Empty;

    [Required]
    public int DocumentId { get; set; }

    public string DocumentReference { get; set; } = string.Empty;

    [Required]
    public string Channel { get; set; } = "WhatsApp"; // "WhatsApp", "Email", "InternalNote"

    public string Recipient { get; set; } = string.Empty;

    public string? RecipientName { get; set; }

    public string? Subject { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    public bool SendViaDirectApi { get; set; } = false;
}
