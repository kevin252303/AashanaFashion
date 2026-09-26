using Microsoft.EntityFrameworkCore;
using AashanaFashion.Data;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public class PricelistService : IPricelistService
{
    private readonly AppDbContext _context;

    public PricelistService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PricelistCalculationResult> CalculateCustomerPriceAsync(
        int customerId,
        int designId,
        decimal quantity,
        DateTime? orderDate = null,
        string? colour = null,
        string? size = null)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        return await CalculatePriceAsync(customer?.PricelistId, designId, quantity, orderDate, colour, size);
    }

    public async Task<PricelistCalculationResult> CalculatePriceAsync(
        int? pricelistId,
        int designId,
        decimal quantity,
        DateTime? orderDate = null,
        string? colour = null,
        string? size = null)
    {
        var design = await _context.Designs.FindAsync(designId);
        decimal basePrice = design?.SalesPrice ?? 0m;
        if (basePrice <= 0 && design?.Price > 0)
        {
            basePrice = design.Price;
        }

        decimal colourCharge = 0m;
        decimal sizeCharge = 0m;

        if (design != null)
        {
            var extraCharges = await _context.ProductExtraCharges
                .Where(e => e.DesignId == designId)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(colour))
            {
                var cMatch = extraCharges.FirstOrDefault(e => e.AttributeType == "Colour" &&
                    string.Equals(e.AttributeValue.Trim(), colour.Trim(), StringComparison.OrdinalIgnoreCase));
                if (cMatch != null) colourCharge = cMatch.ExtraCharge;
            }

            if (!string.IsNullOrWhiteSpace(size))
            {
                var sMatch = extraCharges.FirstOrDefault(e => e.AttributeType == "Size" &&
                    string.Equals(e.AttributeValue.Trim(), size.Trim(), StringComparison.OrdinalIgnoreCase));
                if (sMatch != null) sizeCharge = sMatch.ExtraCharge;
            }
        }

        decimal totalVariantExtra = colourCharge + sizeCharge;
        decimal effectiveBasePrice = basePrice + totalVariantExtra;

        var result = new PricelistCalculationResult
        {
            PricelistId = pricelistId,
            BasePrice = basePrice,
            ColourExtraCharge = colourCharge,
            SizeExtraCharge = sizeCharge,
            TotalVariantExtraCharge = totalVariantExtra,
            EffectiveBasePrice = effectiveBasePrice,
            UnitPrice = effectiveBasePrice,
            DiscountPercentage = 0m,
            DiscountAmount = 0m,
            RuleApplied = false
        };

        if (!pricelistId.HasValue || pricelistId.Value <= 0 || design == null)
        {
            return result;
        }

        var pricelist = await _context.Pricelists
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == pricelistId.Value && p.IsActive);

        if (pricelist == null)
        {
            return result;
        }

        result.PricelistName = pricelist.Name;
        var date = (orderDate ?? DateTime.Today).Date;

        // Filter valid rules by date range and minimum quantity
        var candidateRules = pricelist.Items
            .Where(r => (!r.StartDate.HasValue || r.StartDate.Value.Date <= date)
                     && (!r.EndDate.HasValue || r.EndDate.Value.Date >= date)
                     && r.MinQuantity <= quantity)
            .ToList();

        if (!candidateRules.Any())
        {
            return result;
        }

        // Specificity matching:
        // 1. Specific Product
        // 2. Product Category
        // 3. All Products
        PricelistItem? bestRule = candidateRules
            .Where(r => r.AppliedOn == PricelistAppliedOn.Product && r.DesignId == designId)
            .OrderByDescending(r => r.MinQuantity)
            .FirstOrDefault();

        if (bestRule == null && !string.IsNullOrWhiteSpace(design.Category))
        {
            bestRule = candidateRules
                .Where(r => r.AppliedOn == PricelistAppliedOn.Category &&
                            string.Equals(r.Category?.Trim(), design.Category.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.MinQuantity)
                .FirstOrDefault();
        }

        if (bestRule == null)
        {
            bestRule = candidateRules
                .Where(r => r.AppliedOn == PricelistAppliedOn.AllProducts)
                .OrderByDescending(r => r.MinQuantity)
                .FirstOrDefault();
        }

        if (bestRule == null)
        {
            return result;
        }

        result.RuleApplied = true;
        result.AppliedRuleDescription = bestRule.DisplaySummary;

        switch (bestRule.ComputationMethod)
        {
            case PricelistComputeMethod.FixedPrice:
                result.UnitPrice = Math.Max(0m, (bestRule.FixedPrice ?? basePrice) + totalVariantExtra);
                if (effectiveBasePrice > 0 && result.UnitPrice < effectiveBasePrice)
                {
                    result.DiscountAmount = effectiveBasePrice - result.UnitPrice;
                    result.DiscountPercentage = Math.Round((result.DiscountAmount / effectiveBasePrice) * 100m, 2);
                }
                break;

            case PricelistComputeMethod.Percentage:
                decimal discPct = Math.Max(0m, bestRule.DiscountPercentage ?? 0m);
                result.DiscountPercentage = discPct;
                result.DiscountAmount = Math.Round(effectiveBasePrice * (discPct / 100m), 2);
                result.UnitPrice = Math.Max(0m, effectiveBasePrice - result.DiscountAmount);
                break;

            case PricelistComputeMethod.Formula:
                decimal formulaDiscPct = Math.Max(0m, bestRule.DiscountPercentage ?? 0m);
                decimal surcharge = bestRule.Surcharge ?? 0m;
                decimal discountedBase = effectiveBasePrice * (1m - (formulaDiscPct / 100m));
                result.UnitPrice = Math.Max(0m, Math.Round(discountedBase + surcharge, 2));
                if (effectiveBasePrice > 0 && result.UnitPrice < effectiveBasePrice)
                {
                    result.DiscountAmount = effectiveBasePrice - result.UnitPrice;
                    result.DiscountPercentage = Math.Round((result.DiscountAmount / effectiveBasePrice) * 100m, 2);
                }
                break;
        }

        return result;
    }
}
