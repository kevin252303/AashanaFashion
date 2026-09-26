using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models;

public enum PricelistDiscountPolicy
{
    [Display(Name = "Discount included in price")]
    DiscountIncluded,

    [Display(Name = "Show public price & discount to the customer")]
    ShowDiscount
}

public enum PricelistAppliedOn
{
    [Display(Name = "All Products")]
    AllProducts,

    [Display(Name = "Product Category")]
    Category,

    [Display(Name = "Specific Product")]
    Product
}

public enum PricelistComputeMethod
{
    [Display(Name = "Fixed Price")]
    FixedPrice,

    [Display(Name = "Percentage Discount")]
    Percentage,

    [Display(Name = "Formula")]
    Formula
}

public class Pricelist
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    [Display(Name = "Pricelist Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string Currency { get; set; } = "INR (₹)";

    [Display(Name = "Discount Policy")]
    public PricelistDiscountPolicy DiscountPolicy { get; set; } = PricelistDiscountPolicy.DiscountIncluded;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    [Display(Name = "Description / Notes")]
    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Navigation properties
    public List<PricelistItem> Items { get; set; } = new();
    public List<Customer> Customers { get; set; } = new();

    [NotMapped]
    public string DiscountPolicyDisplay => DiscountPolicy == PricelistDiscountPolicy.DiscountIncluded
        ? "Discount Included"
        : "Show Discount %";
}

public class PricelistItem
{
    public int Id { get; set; }

    public int PricelistId { get; set; }
    public Pricelist? Pricelist { get; set; }

    [Display(Name = "Apply On")]
    public PricelistAppliedOn AppliedOn { get; set; } = PricelistAppliedOn.AllProducts;

    [StringLength(100)]
    [Display(Name = "Category")]
    public string? Category { get; set; }

    [Display(Name = "Product / Design")]
    public int? DesignId { get; set; }
    public Design? Design { get; set; }

    [Display(Name = "Min. Quantity")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal MinQuantity { get; set; } = 1m;

    [Display(Name = "Compute Price")]
    public PricelistComputeMethod ComputationMethod { get; set; } = PricelistComputeMethod.Percentage;

    [Display(Name = "Fixed Price (₹)")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? FixedPrice { get; set; }

    [Display(Name = "Discount %")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountPercentage { get; set; }

    [Display(Name = "Surcharge (₹)")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Surcharge { get; set; }

    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [NotMapped]
    public string DisplaySummary
    {
        get
        {
            var target = AppliedOn switch
            {
                PricelistAppliedOn.Product => Design != null ? $"Design: {Design.DesignNumber}" : "Product",
                PricelistAppliedOn.Category => $"Category: {Category}",
                _ => "All Products"
            };

            var rule = ComputationMethod switch
            {
                PricelistComputeMethod.FixedPrice => $"Fixed ₹{FixedPrice?.ToString("N2") ?? "0.00"}",
                PricelistComputeMethod.Percentage => $"{DiscountPercentage?.ToString("N1") ?? "0"}% Off",
                PricelistComputeMethod.Formula => $"{DiscountPercentage?.ToString("N1") ?? "0"}% Off + ₹{Surcharge?.ToString("N2") ?? "0.00"}",
                _ => ""
            };

            var qty = MinQuantity > 1 ? $" (Min {MinQuantity:N0} pcs)" : "";
            return $"{target} ➔ {rule}{qty}";
        }
    }
}

public class QuickCreatePricelistModel
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public PricelistDiscountPolicy DiscountPolicy { get; set; } = PricelistDiscountPolicy.DiscountIncluded;

    public PricelistAppliedOn AppliedOn { get; set; } = PricelistAppliedOn.AllProducts;

    public PricelistComputeMethod ComputationMethod { get; set; } = PricelistComputeMethod.Percentage;

    public decimal? DiscountPercentage { get; set; }

    public decimal? FixedPrice { get; set; }

    public decimal MinQuantity { get; set; } = 1m;

    public string? Description { get; set; }
}
