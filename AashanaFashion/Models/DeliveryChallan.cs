using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models;

public class DeliveryChallan
{
    public int Id { get; set; }

    [Required]
    public string ChallanNumber { get; set; } = string.Empty;

    public int SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime ChallanDate { get; set; } = DateTime.Now;

    public string? TransporterName { get; set; }

    public string? VehicleNumber { get; set; }

    public string? LrNumber { get; set; }

    public string? EwayBillNumber { get; set; }

    public int NumberOfBoxes { get; set; } = 1;

    public string? DispatchedBy { get; set; }

    public string? ShippingAddress { get; set; }

    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public List<DeliveryChallanItem> Items { get; set; } = new();

    public int TotalQuantity => Items.Sum(i => i.QuantityDispatched);
}

public class DeliveryChallanItem
{
    public int Id { get; set; }

    public int DeliveryChallanId { get; set; }
    public DeliveryChallan? DeliveryChallan { get; set; }

    public int? SalesOrderDetailId { get; set; }
    public SalesOrderDetail? SalesOrderDetail { get; set; }

    public string DesignNumber { get; set; } = string.Empty;

    public string Colour { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public int QuantityDispatched { get; set; }

    public string? Remarks { get; set; }
}
