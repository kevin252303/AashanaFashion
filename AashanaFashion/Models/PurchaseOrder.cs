using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models;

public class PurchaseOrder
{
    public int Id { get; set; }

    [Required]
    public string PoNumber { get; set; } = string.Empty;

    public int VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public string? AgentName { get; set; }

    public string? CourierServiceName { get; set; }

    public string? InvoiceNumber { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    public DateTime? ExpectedReceivingDate { get; set; }

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Pending;

    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    [DataType(DataType.Currency)]
    public decimal TransportCharge { get; set; }

    public decimal TransportChargeGST { get; set; }

    [DataType(DataType.Currency)]
    public decimal RoundOff { get; set; }

    [DataType(DataType.Currency)]
    public decimal TotalAmount { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public List<PurchaseOrderDetail> Details { get; set; } = new();
}

public enum PurchaseOrderStatus
{
    Pending,
    Approved,
    PartiallyReceived,
    Received,
    Cancelled
}
