using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models;

public class PurchaseOrderDetail
{
    public int Id { get; set; }

    public int PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public int SrNo { get; set; }

    [Required]
    public string ProductName { get; set; } = string.Empty;

    public string? ProductDesignNo { get; set; }

    public string? HsnCode { get; set; }

    /// <summary>Meter or Piece</summary>
    public string Unit { get; set; } = "Piece";

    public int Quantity { get; set; }

    public int ReceivedQuantity { get; set; }

    [DataType(DataType.Currency)]
    public decimal UnitPrice { get; set; }

    public decimal GstPercentage { get; set; }

    public decimal DiscountPercentage { get; set; }

    [DataType(DataType.Currency)]
    public decimal TotalPrice => Quantity * UnitPrice;

    public decimal DiscountAmount => TotalPrice * DiscountPercentage / 100m;

    public decimal GstAmount => (TotalPrice - DiscountAmount) * GstPercentage / 100m;

    public decimal NetAmount => TotalPrice - DiscountAmount + GstAmount;
}
