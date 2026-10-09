using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models;

[Table("SubscriptionPlans")]
public class SubscriptionPlan
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Plan name is required.")]
    [StringLength(100, ErrorMessage = "Plan name cannot exceed 100 characters.")]
    [Display(Name = "Plan Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Plan Code")]
    public string Code { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    // --- Desk Seat Configuration & Pricing ---
    [Display(Name = "Included Desk Seats")]
    public int IncludedDeskSeats { get; set; } = 5;

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Monthly Price (₹)")]
    public decimal MonthlyPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Yearly Price (₹)")]
    public decimal YearlyPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "3-Yearly Price (₹)")]
    public decimal ThreeYearlyPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "5-Yearly Price (₹)")]
    public decimal FiveYearlyPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Extra Desk Seat Price Monthly (₹)")]
    public decimal ExtraDeskSeatPriceMonthly { get; set; } = 399;

    // --- Floor Workers Slabs Pricing (10-50, 50-100, 100-200, 200+) ---
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "10 - 50 Workers Price (₹)")]
    public decimal WorkerSlab10To50Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "50 - 100 Workers Price (₹)")]
    public decimal WorkerSlab50To100Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "100 - 200 Workers Price (₹)")]
    public decimal WorkerSlab100To200Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "200+ Workers Price (₹)")]
    public decimal WorkerSlab200PlusPrice { get; set; }

    [Display(Name = "Included Floor Workers")]
    public int IncludedFloorWorkers { get; set; } = 50;

    // --- Modules & Features ---
    [StringLength(1000)]
    public string? EnabledModules { get; set; } = "*";

    public bool IsActive { get; set; } = true;

    public bool IsPopular { get; set; } = false;

    public int DisplayOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}
