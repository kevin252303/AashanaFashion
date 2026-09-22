using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models;

public class SalesOrderDetail
{
    public int Id { get; set; }

    public int SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public int SrNo { get; set; }

    public int? DesignId { get; set; }
    public Design? Design { get; set; }

    [Required]
    public string DesignNumber { get; set; } = string.Empty;

    public string Colour { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public int DispatchedQuantity { get; set; }

    [DataType(DataType.Currency)]
    public decimal UnitPrice { get; set; }

    public decimal DiscountPercentage { get; set; }

    public decimal GstPercentage { get; set; }

    [DataType(DataType.Currency)]
    public decimal TotalPrice => Quantity * UnitPrice;

    public decimal DiscountAmount => TotalPrice * DiscountPercentage / 100m;

    public decimal GstAmount => (TotalPrice - DiscountAmount) * GstPercentage / 100m;

    public decimal NetAmount => TotalPrice - DiscountAmount + GstAmount;

    public int PendingQuantity => Math.Max(0, Quantity - DispatchedQuantity);
}
