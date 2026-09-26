namespace AashanaFashion.Services;

public class PricelistCalculationResult
{
    public int? PricelistId { get; set; }
    public string? PricelistName { get; set; }
    public decimal BasePrice { get; set; }
    public decimal ColourExtraCharge { get; set; }
    public decimal SizeExtraCharge { get; set; }
    public decimal TotalVariantExtraCharge { get; set; }
    public decimal EffectiveBasePrice { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? AppliedRuleDescription { get; set; }
    public bool RuleApplied { get; set; }
}

public interface IPricelistService
{
    Task<PricelistCalculationResult> CalculatePriceAsync(
        int? pricelistId,
        int designId,
        decimal quantity,
        DateTime? orderDate = null,
        string? colour = null,
        string? size = null);

    Task<PricelistCalculationResult> CalculateCustomerPriceAsync(
        int customerId,
        int designId,
        decimal quantity,
        DateTime? orderDate = null,
        string? colour = null,
        string? size = null);
}
