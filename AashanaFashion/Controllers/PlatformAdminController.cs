using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Services;

namespace AashanaFashion.Controllers;

[Authorize(Roles = "Developer")]
public class PlatformAdminController : Controller
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IDoubleEntryService _doubleEntryService;

    public PlatformAdminController(
        AppDbContext context,
        ITenantContext tenantContext,
        IDoubleEntryService doubleEntryService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _doubleEntryService = doubleEntryService;
    }

    // GET: /PlatformAdmin
    public async Task<IActionResult> Index(string? search, string? cycleFilter, TenantStatus? statusFilter, bool? expiringSoonOnly)
    {
        var tenants = await _context.Tenants.IgnoreQueryFilters()
            .Include(t => t.SubscriptionPlan)
            .OrderByDescending(t => t.Id)
            .ToListAsync();

        var summaryItems = new List<TenantSummaryItem>();
        decimal totalMrr = 0m;
        int totalErpAllocated = 0;
        int totalErpUsed = 0;
        int totalWorkerAllocated = 0;
        int totalWorkerUsed = 0;
        int expiringCount = 0;

        // Group counts efficiently
        var userCounts = await _context.Users.IgnoreQueryFilters()
            .Where(u => u.IsActive)
            .GroupBy(u => u.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count);

        var employeeCounts = await _context.Employees.IgnoreQueryFilters()
            .Where(e => e.IsActive)
            .GroupBy(e => e.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count);

        var invoiceCounts = await _context.TaxInvoices.IgnoreQueryFilters()
            .GroupBy(i => i.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count);

        var orderCounts = await _context.ProductionOrders.IgnoreQueryFilters()
            .GroupBy(p => p.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count);

        var companyNames = await _context.Companies.IgnoreQueryFilters()
            .GroupBy(c => c.TenantId)
            .Select(g => new { TenantId = g.Key, Name = g.Select(c => c.CompanyName).FirstOrDefault() ?? "" })
            .ToDictionaryAsync(x => x.TenantId, x => x.Name);

        foreach (var t in tenants)
        {
            var activeUsers = userCounts.TryGetValue(t.Id, out var uCount) ? uCount : 0;
            var activeWorkers = employeeCounts.TryGetValue(t.Id, out var wCount) ? wCount : 0;
            var invCount = invoiceCounts.TryGetValue(t.Id, out var iCount) ? iCount : 0;
            var ordCount = orderCounts.TryGetValue(t.Id, out var oCount) ? oCount : 0;
            var compName = companyNames.TryGetValue(t.Id, out var cName) ? cName : t.BusinessName;

            var expiryDate = (t.SubscriptionEndsAt.HasValue && t.SubscriptionEndsAt.Value > DateTime.MinValue)
                ? t.SubscriptionEndsAt.Value
                : t.TrialEndsAt;

            var daysLeft = Math.Max(0, (int)(expiryDate - DateTime.Today).TotalDays);
            var isExpiringSoon = daysLeft <= 7 && t.Status == TenantStatus.Active;

            if (isExpiringSoon) expiringCount++;

            // Calculate MRR per tenant
            decimal tenantMrr = 0m;
            if (t.Status == TenantStatus.Active)
            {
                if (t.DeskSeatPricePerUser > 0 || t.FloorWorkersPriceYearly > 0)
                {
                    decimal deskRate = t.DeskSeatBillingBasis == "PerYear"
                        ? (t.DeskSeatPricePerUser / 12m)
                        : t.DeskSeatPricePerUser;
                    decimal discount = t.DeskSeatDiscountPercent; // 0, 5, or 12
                    decimal deskNetMonthly = (t.AllowedErpSeats * deskRate) * (1m - (discount / 100m));
                    decimal floorMonthly = t.FloorWorkersPriceYearly / 12m;
                    tenantMrr = deskNetMonthly + floorMonthly;
                }
                else
                {
                    // Fallback for legacy tenants
                    tenantMrr = (t.AllowedErpSeats * 500m) + (t.AllowedEmployeeRecords * 30m);
                }
            }

            totalMrr += tenantMrr;
            totalErpAllocated += t.AllowedErpSeats;
            totalErpUsed += activeUsers;
            totalWorkerAllocated += t.AllowedEmployeeRecords;
            totalWorkerUsed += activeWorkers;

            summaryItems.Add(new TenantSummaryItem
            {
                Tenant = t,
                PrimaryCompanyName = compName,
                ActiveErpUsers = activeUsers,
                ActiveEmployees = activeWorkers,
                MonthlyRevenue = tenantMrr,
                DaysLeft = daysLeft,
                TotalInvoicesCount = invCount,
                TotalOrdersCount = ordCount
            });
        }

        // Apply filters to table display
        var filteredList = summaryItems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            filteredList = filteredList.Where(x =>
                x.Tenant.BusinessName.ToLower().Contains(s) ||
                x.Tenant.Subdomain.ToLower().Contains(s) ||
                (x.Tenant.AdminEmail != null && x.Tenant.AdminEmail.ToLower().Contains(s)) ||
                (x.Tenant.Phone != null && x.Tenant.Phone.Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(cycleFilter))
        {
            filteredList = filteredList.Where(x => x.Tenant.BillingCycle == cycleFilter);
        }

        if (statusFilter.HasValue)
        {
            filteredList = filteredList.Where(x => x.Tenant.Status == statusFilter.Value);
        }

        if (expiringSoonOnly == true)
        {
            filteredList = filteredList.Where(x => x.IsExpiringSoon);
        }

        var vm = new PlatformDashboardViewModel
        {
            Tenants = filteredList.ToList(),
            TotalTenants = tenants.Count,
            ActiveTenants = tenants.Count(t => t.Status == TenantStatus.Active),
            TrialTenants = tenants.Count(t => t.PlanType == SubscriptionTier.FreeTrial || (t.SubscriptionEndsAt == null && t.TrialEndsAt > DateTime.Now)),
            SuspendedTenants = tenants.Count(t => t.Status == TenantStatus.Suspended),
            ExpiringSoonCount = expiringCount,
            TotalMrr = totalMrr,
            TotalErpSeatsAllocated = totalErpAllocated,
            TotalErpSeatsUsed = totalErpUsed,
            TotalWorkerRecordsAllocated = totalWorkerAllocated,
            TotalWorkerRecordsUsed = totalWorkerUsed
        };

        ViewBag.Search = search;
        ViewBag.CycleFilter = cycleFilter;
        ViewBag.StatusFilter = statusFilter;
        ViewBag.ExpiringSoonOnly = expiringSoonOnly ?? false;

        return View(vm);
    }

    // POST: /PlatformAdmin/QuickExtend
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickExtend(int tenantId, int days = 30)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null) return NotFound();

        var currentExpiry = (tenant.SubscriptionEndsAt.HasValue && tenant.SubscriptionEndsAt.Value > DateTime.Today)
            ? tenant.SubscriptionEndsAt.Value
            : DateTime.Today;

        tenant.SubscriptionEndsAt = currentExpiry.AddDays(days);
        if (tenant.Status == TenantStatus.Suspended)
        {
            tenant.Status = TenantStatus.Active;
        }

        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Subscription for '{tenant.BusinessName}' successfully extended by {days} days (valid until {tenant.SubscriptionEndsAt:dd MMM yyyy}).";
        return RedirectToAction(nameof(Index));
    }

    // POST: /PlatformAdmin/UpdateTenant
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTenant(
        int tenantId,
        TenantStatus status,
        int allowedErpSeats,
        decimal deskSeatPricePerUser,
        string deskSeatBillingBasis,
        string deskSeatDuration,
        int allowedEmployeeRecords,
        decimal floorWorkersPriceYearly,
        DateTime? subscriptionEndsAt)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null) return NotFound();

        decimal discountPct = deskSeatDuration switch
        {
            "ThreeYears" => 5m,
            "FiveYears" => 12m,
            _ => 0m
        };

        int months = deskSeatDuration switch
        {
            "Yearly" => 12,
            "ThreeYears" => 36,
            "FiveYears" => 60,
            _ => 1
        };

        decimal deskMonthlyRate = deskSeatBillingBasis == "PerYear"
            ? (deskSeatPricePerUser / 12m)
            : deskSeatPricePerUser;

        decimal baseDeskTotal = Math.Max(1, allowedErpSeats) * Math.Max(0, deskMonthlyRate) * months;
        decimal deskDiscount = baseDeskTotal * (discountPct / 100m);
        decimal deskNetTotal = baseDeskTotal - deskDiscount;

        int floorYears = deskSeatDuration switch
        {
            "ThreeYears" => 3,
            "FiveYears" => 5,
            _ => 1
        };
        decimal floorTotal = Math.Max(0, floorWorkersPriceYearly) * floorYears;

        tenant.Status = status;
        tenant.AllowedErpSeats = Math.Max(1, allowedErpSeats);
        tenant.AllowedEmployeeRecords = Math.Max(0, allowedEmployeeRecords);
        tenant.DeskSeatPricePerUser = Math.Max(0, deskSeatPricePerUser);
        tenant.DeskSeatBillingBasis = deskSeatBillingBasis == "PerYear" ? "PerYear" : "PerMonth";
        tenant.BillingCycle = deskSeatDuration;
        tenant.DeskSeatDiscountPercent = discountPct;
        tenant.FloorWorkersPriceYearly = Math.Max(0, floorWorkersPriceYearly);
        tenant.TotalContractAmount = deskNetTotal + floorTotal;

        if (subscriptionEndsAt.HasValue)
        {
            tenant.SubscriptionEndsAt = subscriptionEndsAt.Value;
        }

        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync();

        var rateBasisLabel = tenant.DeskSeatBillingBasis == "PerYear" ? "yr" : "mo";
        TempData["Success"] = $"Tenant '{tenant.BusinessName}' updated: {allowedErpSeats} Desk Seats @ ₹{deskSeatPricePerUser:N0}/{rateBasisLabel} ({deskSeatDuration}), {allowedEmployeeRecords} Floor Workers (₹{floorWorkersPriceYearly:N0}/yr).";
        return RedirectToAction(nameof(Index));
    }

    // POST: /PlatformAdmin/ToggleSuspend
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSuspend(int tenantId)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null) return NotFound();

        if (tenant.Status == TenantStatus.Active)
        {
            tenant.Status = TenantStatus.Suspended;
            TempData["Success"] = $"Tenant '{tenant.BusinessName}' is now SUSPENDED. Their portal access is restricted.";
        }
        else
        {
            tenant.Status = TenantStatus.Active;
            if (tenant.SubscriptionEndsAt == null || tenant.SubscriptionEndsAt < DateTime.Today)
            {
                tenant.SubscriptionEndsAt = DateTime.Today.AddDays(30);
            }
            TempData["Success"] = $"Tenant '{tenant.BusinessName}' has been REACTIVATED.";
        }

        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: /PlatformAdmin/SwitchToTenant
    public async Task<IActionResult> SwitchToTenant(int tenantId)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null) return NotFound();

        _tenantContext.SetCurrentTenant(tenant);

        TempData["Success"] = $"Switched view to client tenant: '{tenant.BusinessName}' (Workspace: {tenant.Subdomain}.kriyex.com). All queries now filter to this organization.";
        return RedirectToAction("Index", "Production");
    }

    // GET: /PlatformAdmin/SwitchToDefault
    public async Task<IActionResult> SwitchToDefault()
    {
        var defaultTenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == 1);
        if (defaultTenant != null)
        {
            _tenantContext.SetCurrentTenant(defaultTenant);
        }

        TempData["Success"] = "Switched back to Primary Tenant Context.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /PlatformAdmin/UpdateTenantModules
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTenantModules(int tenantId, List<string>? selectedModules)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null) return NotFound();

        selectedModules ??= new List<string>();
        tenant.EnabledModules = string.Join(",", selectedModules.Distinct(StringComparer.OrdinalIgnoreCase));

        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Module access updated for '{tenant.BusinessName}': {selectedModules.Count} of {AppModules.All.Count} modules enabled.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /PlatformAdmin/CreateTenant
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTenant(
        string businessName,
        string subdomain,
        int allowedErpSeats,
        decimal deskSeatPricePerUser,
        string deskSeatBillingBasis,
        string deskSeatDuration,
        int allowedEmployeeRecords,
        decimal floorWorkersPriceYearly,
        string adminFullName,
        string adminEmail,
        string adminUsername,
        string adminPassword,
        string? phone,
        List<string>? selectedModules = null)
    {
        var cleanSubdomain = subdomain.Trim().ToLowerInvariant()
            .Replace("https://", "")
            .Replace("http://", "")
            .Replace(".kriyex.com", "")
            .Replace(".aashanafashion.com", "")
            .Trim('/', ' ');

        if (await _context.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Subdomain.ToLower() == cleanSubdomain))
        {
            TempData["Error"] = $"Subdomain '{cleanSubdomain}.kriyex.com' is already in use by another client.";
            return RedirectToAction(nameof(Index));
        }

        var modulesList = (selectedModules != null && selectedModules.Any())
            ? selectedModules
            : AppModules.All.Select(m => m.Key).ToList();

        decimal discountPct = deskSeatDuration switch
        {
            "ThreeYears" => 5m,
            "FiveYears" => 12m,
            _ => 0m
        };

        int months = deskSeatDuration switch
        {
            "Yearly" => 12,
            "ThreeYears" => 36,
            "FiveYears" => 60,
            _ => 1
        };

        DateTime expiryDate = deskSeatDuration switch
        {
            "Yearly" => DateTime.Today.AddYears(1),
            "ThreeYears" => DateTime.Today.AddYears(3),
            "FiveYears" => DateTime.Today.AddYears(5),
            _ => DateTime.Today.AddMonths(1)
        };

        decimal deskMonthlyRate = deskSeatBillingBasis == "PerYear"
            ? (deskSeatPricePerUser / 12m)
            : deskSeatPricePerUser;

        decimal baseDeskTotal = Math.Max(1, allowedErpSeats) * Math.Max(0, deskMonthlyRate) * months;
        decimal deskDiscount = baseDeskTotal * (discountPct / 100m);
        decimal deskNetTotal = baseDeskTotal - deskDiscount;

        int floorYears = deskSeatDuration switch
        {
            "ThreeYears" => 3,
            "FiveYears" => 5,
            _ => 1
        };
        decimal floorTotal = Math.Max(0, floorWorkersPriceYearly) * floorYears;

        var newTenant = new Tenant
        {
            Subdomain = cleanSubdomain,
            BusinessName = businessName.Trim(),
            PlanType = SubscriptionTier.Growth,
            Status = TenantStatus.Active,
            AllowedErpSeats = Math.Max(1, allowedErpSeats),
            AllowedEmployeeRecords = Math.Max(0, allowedEmployeeRecords),
            DeskSeatPricePerUser = Math.Max(0, deskSeatPricePerUser),
            DeskSeatBillingBasis = deskSeatBillingBasis == "PerYear" ? "PerYear" : "PerMonth",
            BillingCycle = deskSeatDuration,
            DeskSeatDiscountPercent = discountPct,
            FloorWorkersPriceYearly = Math.Max(0, floorWorkersPriceYearly),
            TotalContractAmount = deskNetTotal + floorTotal,
            TrialEndsAt = DateTime.Today.AddDays(14),
            SubscriptionEndsAt = expiryDate,
            AdminEmail = adminEmail.Trim(),
            Phone = phone?.Trim(),
            EnabledModules = string.Join(",", modulesList),
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Tenants.Add(newTenant);
        await _context.SaveChangesAsync();

        var words = businessName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var companyCode = words.Length >= 2
            ? $"{words[0][0]}{words[1][0]}".ToUpper()
            : (businessName.Length >= 2 ? businessName.Substring(0, 2).ToUpper() : "CO");

        var newCompany = new Company
        {
            TenantId = newTenant.Id,
            CompanyName = businessName.Trim(),
            CompanyCode = companyCode,
            Email = adminEmail.Trim(),
            Phone = phone?.Trim(),
            IsActive = true,
            IsDefault = true,
            CreatedDate = DateTime.Now
        };

        _context.Companies.Add(newCompany);
        await _context.SaveChangesAsync();

        try
        {
            await _doubleEntryService.EnsureDefaultChartOfAccountsAsync(newCompany.Id);
        }
        catch { }

        var nameParts = adminFullName.Trim().Split(' ', 2);
        var adminUser = new AppUser
        {
            TenantId = newTenant.Id,
            DefaultCompanyId = newCompany.Id,
            Username = adminUsername.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Role = "Admin",
            FirstName = nameParts[0],
            LastName = nameParts.Length > 1 ? nameParts[1] : "",
            DisplayName = adminFullName.Trim(),
            Email = adminEmail.Trim(),
            ContactNumber = phone?.Trim(),
            IsActive = true
        };

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Client organization '{newTenant.BusinessName}' created successfully! Workspace URL: https://{newTenant.Subdomain}.kriyex.com (Admin login: {adminUsername.Trim()})";
        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // SUBSCRIPTION PLANS MANAGEMENT
    // ==========================================

    // GET: /PlatformAdmin/Plans
    public async Task<IActionResult> Plans()
    {
        var plans = await _context.SubscriptionPlans
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Id)
            .ToListAsync();

        var tenantCounts = await _context.Tenants.IgnoreQueryFilters()
            .Where(t => t.SubscriptionPlanId.HasValue)
            .GroupBy(t => t.SubscriptionPlanId!.Value)
            .Select(g => new { PlanId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PlanId, x => x.Count);

        ViewBag.TenantCounts = tenantCounts;
        return View(plans);
    }

    // POST: /PlatformAdmin/CreatePlan
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePlan(
        string name,
        string? code,
        string? description,
        int includedDeskSeats,
        decimal monthlyPrice,
        decimal yearlyPrice,
        decimal threeYearlyPrice,
        decimal fiveYearlyPrice,
        decimal extraDeskSeatPriceMonthly,
        decimal workerSlab10To50Price,
        decimal workerSlab50To100Price,
        decimal workerSlab100To200Price,
        decimal workerSlab200PlusPrice,
        int includedFloorWorkers,
        bool isPopular,
        bool isActive,
        List<string>? selectedModules)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Plan name is required.";
            return RedirectToAction(nameof(Plans));
        }

        var modulesStr = (selectedModules != null && selectedModules.Any())
            ? string.Join(",", selectedModules.Distinct(StringComparer.OrdinalIgnoreCase))
            : "*";

        var plan = new SubscriptionPlan
        {
            Name = name.Trim(),
            Code = string.IsNullOrWhiteSpace(code) ? name.Trim().ToUpper().Replace(" ", "_") : code.Trim().ToUpper(),
            Description = description?.Trim(),
            IncludedDeskSeats = Math.Max(1, includedDeskSeats),
            MonthlyPrice = Math.Max(0, monthlyPrice),
            YearlyPrice = Math.Max(0, yearlyPrice),
            ThreeYearlyPrice = Math.Max(0, threeYearlyPrice),
            FiveYearlyPrice = Math.Max(0, fiveYearlyPrice),
            ExtraDeskSeatPriceMonthly = Math.Max(0, extraDeskSeatPriceMonthly),
            WorkerSlab10To50Price = Math.Max(0, workerSlab10To50Price),
            WorkerSlab50To100Price = Math.Max(0, workerSlab50To100Price),
            WorkerSlab100To200Price = Math.Max(0, workerSlab100To200Price),
            WorkerSlab200PlusPrice = Math.Max(0, workerSlab200PlusPrice),
            IncludedFloorWorkers = Math.Max(0, includedFloorWorkers),
            EnabledModules = modulesStr,
            IsPopular = isPopular,
            IsActive = isActive,
            CreatedAt = DateTime.Now
        };

        _context.SubscriptionPlans.Add(plan);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Subscription plan '{plan.Name}' created successfully.";
        return RedirectToAction(nameof(Plans));
    }

    // POST: /PlatformAdmin/EditPlan
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPlan(
        int id,
        string name,
        string? code,
        string? description,
        int includedDeskSeats,
        decimal monthlyPrice,
        decimal yearlyPrice,
        decimal threeYearlyPrice,
        decimal fiveYearlyPrice,
        decimal extraDeskSeatPriceMonthly,
        decimal workerSlab10To50Price,
        decimal workerSlab50To100Price,
        decimal workerSlab100To200Price,
        decimal workerSlab200PlusPrice,
        int includedFloorWorkers,
        bool isPopular,
        bool isActive,
        List<string>? selectedModules)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(id);
        if (plan == null) return NotFound();

        plan.Name = name.Trim();
        plan.Code = string.IsNullOrWhiteSpace(code) ? name.Trim().ToUpper().Replace(" ", "_") : code.Trim().ToUpper();
        plan.Description = description?.Trim();
        plan.IncludedDeskSeats = Math.Max(1, includedDeskSeats);
        plan.MonthlyPrice = Math.Max(0, monthlyPrice);
        plan.YearlyPrice = Math.Max(0, yearlyPrice);
        plan.ThreeYearlyPrice = Math.Max(0, threeYearlyPrice);
        plan.FiveYearlyPrice = Math.Max(0, fiveYearlyPrice);
        plan.ExtraDeskSeatPriceMonthly = Math.Max(0, extraDeskSeatPriceMonthly);
        plan.WorkerSlab10To50Price = Math.Max(0, workerSlab10To50Price);
        plan.WorkerSlab50To100Price = Math.Max(0, workerSlab50To100Price);
        plan.WorkerSlab100To200Price = Math.Max(0, workerSlab100To200Price);
        plan.WorkerSlab200PlusPrice = Math.Max(0, workerSlab200PlusPrice);
        plan.IncludedFloorWorkers = Math.Max(0, includedFloorWorkers);
        plan.EnabledModules = (selectedModules != null && selectedModules.Any())
            ? string.Join(",", selectedModules.Distinct(StringComparer.OrdinalIgnoreCase))
            : "*";
        plan.IsPopular = isPopular;
        plan.IsActive = isActive;
        plan.UpdatedAt = DateTime.Now;

        _context.SubscriptionPlans.Update(plan);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Subscription plan '{plan.Name}' updated successfully.";
        return RedirectToAction(nameof(Plans));
    }

    // POST: /PlatformAdmin/TogglePlanActive
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePlanActive(int id)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(id);
        if (plan == null) return NotFound();

        plan.IsActive = !plan.IsActive;
        plan.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Plan '{plan.Name}' is now {(plan.IsActive ? "Active" : "Inactive")}.";
        return RedirectToAction(nameof(Plans));
    }

    // POST: /PlatformAdmin/DeletePlan
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePlan(int id)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(id);
        if (plan == null) return NotFound();

        bool inUse = await _context.Tenants.IgnoreQueryFilters().AnyAsync(t => t.SubscriptionPlanId == id);
        if (inUse)
        {
            TempData["Error"] = $"Cannot delete plan '{plan.Name}' because it is assigned to one or more client tenants. You can deactivate it instead.";
            return RedirectToAction(nameof(Plans));
        }

        _context.SubscriptionPlans.Remove(plan);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Subscription plan '{plan.Name}' deleted.";
        return RedirectToAction(nameof(Plans));
    }
}
