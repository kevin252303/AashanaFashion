using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models;

public class PurchaseOrderViewModel
{
    public int Id { get; set; }

    public string PoNumber { get; set; } = string.Empty;

    [Required]
    public int VendorId { get; set; }

    public string? AgentName { get; set; }

    public string? CourierServiceName { get; set; }

    public string? InvoiceNumber { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    public DateTime? ExpectedReceivingDate { get; set; }

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Pending;

    public string? Notes { get; set; }

    public decimal TransportCharge { get; set; }

    public decimal TransportChargeGST { get; set; }

    public decimal RoundOff { get; set; }

    public decimal TotalAmount { get; set; }

    public List<PurchaseOrderDetailViewModel> Details { get; set; } = new();
}

public class PurchaseOrderDetailViewModel
{
    public int Id { get; set; }

    public int SrNo { get; set; }

    [Required]
    public string ProductName { get; set; } = string.Empty;

    public string? ProductDesignNo { get; set; }

    public string? HsnCode { get; set; }

    public string Unit { get; set; } = "Piece";

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public int ReceivedQuantity { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0, 100)]
    public decimal GstPercentage { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; }

    public decimal TotalPrice => Quantity * UnitPrice;

    public decimal DiscountAmount => TotalPrice * DiscountPercentage / 100m;

    public decimal GstAmount => (TotalPrice - DiscountAmount) * GstPercentage / 100m;

    public decimal NetAmount => TotalPrice - DiscountAmount + GstAmount;
}
