using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models;

public enum SubscriptionTier
{
    [Display(Name = "Standard")]
    FreeTrial,

    [Display(Name = "Starter (Small Garment Shop / Job Work)")]
    Starter,

    [Display(Name = "Growth (Garment Manufacturer / Brand)")]
    Growth,

    [Display(Name = "Enterprise (Textile Mill / Multi-Factory)")]
    Enterprise
}

public enum TenantStatus
{
    [Display(Name = "Active")]
    Active,

    [Display(Name = "Past Due")]
    PastDue,

    [Display(Name = "Suspended")]
    Suspended,

    [Display(Name = "Cancelled")]
    Cancelled
}

public interface IMustHaveTenant
{
    public int TenantId { get; set; }
}

[Table("Tenants")]
public class Tenant
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Subdomain { get; set; } = "default"; // e.g. "aashana", "texfab", "craftwear"

    [Required]
    [StringLength(150)]
    public string BusinessName { get; set; } = string.Empty;

    public SubscriptionTier PlanType { get; set; } = SubscriptionTier.Growth;

    public int? SubscriptionPlanId { get; set; }
    [ForeignKey("SubscriptionPlanId")]
    public virtual SubscriptionPlan? SubscriptionPlan { get; set; }

    [StringLength(50)]
    public string BillingCycle { get; set; } = "Monthly"; // "Monthly", "Yearly", "ThreeYears", "FiveYears"

    [StringLength(20)]
    public string DeskSeatBillingBasis { get; set; } = "PerMonth"; // "PerMonth", "PerYear"

    [Column(TypeName = "decimal(18,2)")]
    public decimal DeskSeatPricePerUser { get; set; } = 500m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DeskSeatDiscountPercent { get; set; } = 0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal FloorWorkersPriceYearly { get; set; } = 0m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalContractAmount { get; set; } = 0m;

    [StringLength(50)]
    public string WorkerSlab { get; set; } = "10-50"; // "10-50", "50-100", "100-200", "200+"

    public TenantStatus Status { get; set; } = TenantStatus.Active;

    // ERP Web Portal Users Quota (e.g. 10 Users)
    public int AllowedErpSeats { get; set; } = 10;

    // Factory Floor Worker Payroll Records Quota (e.g. 50 Employees)
    public int AllowedEmployeeRecords { get; set; } = 50;

    public DateTime TrialEndsAt { get; set; } = DateTime.Today.AddDays(14);

    public DateTime? SubscriptionEndsAt { get; set; }

    [StringLength(100)]
    public string? AdminEmail { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? CustomConnectionString { get; set; } // For Enterprise dedicated DB support

    /// <summary>
    /// Comma-separated list of enabled module keys (e.g. "Sales,Purchase,Accounting").
    /// If empty or null, all modules are enabled by default for backwards compatibility.
    /// </summary>
    [StringLength(1000)]
    public string EnabledModules { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool HasModule(string moduleKey)
    {
        if (string.IsNullOrWhiteSpace(EnabledModules) || EnabledModules.Trim() == "*")
            return true;

        var list = EnabledModules.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return list.Contains(moduleKey, StringComparer.OrdinalIgnoreCase);
    }

    public List<string> GetEnabledModuleList()
    {
        if (string.IsNullOrWhiteSpace(EnabledModules) || EnabledModules.Trim() == "*")
        {
            return AppModules.All.Select(m => m.Key).ToList();
        }
        return EnabledModules.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}

public class SubscriptionDashboardViewModel
{
    public Tenant Tenant { get; set; } = null!;
    public int UsedErpSeats { get; set; }
    public int TotalErpSeats { get; set; }
    public int UsedEmployeeRecords { get; set; }
    public int TotalEmployeeRecords { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsTrial { get; set; }
    public decimal MonthlyBillingEstimate { get; set; }

    public double ErpSeatPercentage => TotalErpSeats > 0 ? Math.Min(100, Math.Round((double)UsedErpSeats / TotalErpSeats * 100, 1)) : 0;
    public double EmployeePercentage => TotalEmployeeRecords > 0 ? Math.Min(100, Math.Round((double)UsedEmployeeRecords / TotalEmployeeRecords * 100, 1)) : 0;
}

public class RegisterTenantViewModel
{
    [Required(ErrorMessage = "Company / Business Name is required.")]
    [StringLength(100, ErrorMessage = "Business name cannot exceed 100 characters.")]
    [Display(Name = "Business / Brand Name")]
    public string BusinessName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subdomain is required.")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "Subdomain must be between 3 and 30 characters.")]
    [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Subdomain can only contain lowercase letters, numbers, and hyphens (e.g. 'craftwear' or 'tex-fab').")]
    [Display(Name = "Organization Subdomain")]
    public string Subdomain { get; set; } = string.Empty;

    [Required(ErrorMessage = "Admin full name is required.")]
    [StringLength(100)]
    [Display(Name = "Admin Full Name")]
    public string AdminFullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Admin email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [StringLength(100)]
    [Display(Name = "Admin Email")]
    public string AdminEmail { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    [Display(Name = "Contact Phone (Optional)")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Admin username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be at least 3 characters.")]
    [Display(Name = "Admin Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm Password is required.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Selected Plan")]
    public SubscriptionTier PlanType { get; set; } = SubscriptionTier.Growth;
}

public class TenantSummaryItem
{
    public Tenant Tenant { get; set; } = null!;
    public string PrimaryCompanyName { get; set; } = string.Empty;
    public int ActiveErpUsers { get; set; }
    public int ActiveEmployees { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int DaysLeft { get; set; }
    public bool IsExpiringSoon => DaysLeft <= 7 && Tenant.Status == TenantStatus.Active;
    public bool IsExpired => DaysLeft <= 0 && Tenant.Status != TenantStatus.Cancelled;
    public int TotalInvoicesCount { get; set; }
    public int TotalOrdersCount { get; set; }
    public int EnabledModulesCount => Tenant.GetEnabledModuleList().Count;
}

public class PlatformDashboardViewModel
{
    public List<TenantSummaryItem> Tenants { get; set; } = new();
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public int TrialTenants { get; set; }
    public int SuspendedTenants { get; set; }
    public int ExpiringSoonCount { get; set; }

    public decimal TotalMrr { get; set; }
    public decimal TotalArr => TotalMrr * 12;
    public decimal AverageRevenuePerTenant => ActiveTenants > 0 ? Math.Round(TotalMrr / ActiveTenants, 2) : 0;

    public int TotalErpSeatsAllocated { get; set; }
    public int TotalErpSeatsUsed { get; set; }
    public int TotalWorkerRecordsAllocated { get; set; }
    public int TotalWorkerRecordsUsed { get; set; }
}
