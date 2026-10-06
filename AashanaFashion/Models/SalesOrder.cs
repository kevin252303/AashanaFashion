using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models;

public class SalesOrder : IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; } = 1;
    public int CompanyId { get; set; } = 1;
    public Company? Company { get; set; }

    [Required]
    public string SoNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? PricelistId { get; set; }
    public Pricelist? Pricelist { get; set; }

    public string? CustomerPoReference { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    public DateTime? ExpectedDeliveryDate { get; set; }

    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Confirmed;

    public string? ShippingAddress { get; set; }

    public string? TransporterName { get; set; }

    public string? PaymentTerms { get; set; }

    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    // ——— Whole Order Agent Discount ———
    public bool HasAgentDiscount { get; set; } = false;

    public AgentDiscountType AgentDiscountType { get; set; } = AgentDiscountType.Percentage;

    [Column(TypeName = "decimal(18,2)")]
    public decimal AgentDiscountRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AgentDiscountAmount { get; set; }

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

public enum AgentDiscountType
{
    [Display(Name = "Percentage (%)")]
    Percentage = 0,

    [Display(Name = "Fixed Amount (₹)")]
    Amount = 1
}

