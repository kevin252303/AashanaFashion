using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models;

public class SalesOrderViewModel
{
    public int Id { get; set; }

    public string SoNumber { get; set; } = string.Empty;

    [Required]
    public int CustomerId { get; set; }

    public string? CustomerPoReference { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    public DateTime? ExpectedDeliveryDate { get; set; }

    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Confirmed;

    public string? ShippingAddress { get; set; }

    public string? TransporterName { get; set; }

    public string? PaymentTerms { get; set; }

    public string? Notes { get; set; }

    public decimal TransportCharge { get; set; }

    public decimal TransportChargeGST { get; set; }

    public decimal RoundOff { get; set; }

    public decimal TotalAmount { get; set; }

    public List<SalesOrderDetailViewModel> Details { get; set; } = new();
}

public class SalesOrderDetailViewModel
{
    public int Id { get; set; }

    public int SrNo { get; set; }

    public int? DesignId { get; set; }

    [Required]
    public string DesignNumber { get; set; } = string.Empty;

    public string Colour { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public int DispatchedQuantity { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; }

    [Range(0, 100)]
    public decimal GstPercentage { get; set; }

    public decimal TotalPrice => Quantity * UnitPrice;

    public decimal DiscountAmount => TotalPrice * DiscountPercentage / 100m;

    public decimal GstAmount => (TotalPrice - DiscountAmount) * GstPercentage / 100m;

    public decimal NetAmount => TotalPrice - DiscountAmount + GstAmount;

    public int PendingQuantity => Math.Max(0, Quantity - DispatchedQuantity);
}

public class CreateChallanViewModel
{
    public int SalesOrderId { get; set; }
    public string SoNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ChallanNumber { get; set; } = string.Empty;
    public DateTime ChallanDate { get; set; } = DateTime.Now;
    public string? TransporterName { get; set; }
    public string? VehicleNumber { get; set; }
    public string? LrNumber { get; set; }
    public string? EwayBillNumber { get; set; }
    public int NumberOfBoxes { get; set; } = 1;
    public string? ShippingAddress { get; set; }
    public string? Notes { get; set; }
    public List<CreateChallanItemViewModel> Items { get; set; } = new();
}

public class CreateChallanItemViewModel
{
    public int SalesOrderDetailId { get; set; }
    public string DesignNumber { get; set; } = string.Empty;
    public string Colour { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public int OrderedQuantity { get; set; }
    public int PreviouslyDispatched { get; set; }
    public int PendingQuantity { get; set; }
    public int DispatchQuantity { get; set; }
    public string? Remarks { get; set; }
}
