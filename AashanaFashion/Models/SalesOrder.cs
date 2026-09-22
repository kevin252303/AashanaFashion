using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models;

public class SalesOrder
{
    public int Id { get; set; }

    [Required]
    public string SoNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string? CustomerPoReference { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    public DateTime? ExpectedDeliveryDate { get; set; }

    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Confirmed;

    public string? ShippingAddress { get; set; }

    public string? TransporterName { get; set; }

    public string? PaymentTerms { get; set; }

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

    public List<SalesOrderDetail> Details { get; set; } = new();

    public List<DeliveryChallan> Challans { get; set; } = new();
}

public enum SalesOrderStatus
{
    Draft,
    Confirmed,
    InProduction,
    PartiallyDispatched,
    Dispatched,
    Cancelled
}
