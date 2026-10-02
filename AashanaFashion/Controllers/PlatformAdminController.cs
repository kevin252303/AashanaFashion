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

[Authorize(Roles = "SuperAdmin,Admin")]
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
    public async Task<IActionResult> Index(string? search, SubscriptionTier? planFilter, TenantStatus? statusFilter, bool? expiringSoonOnly)
    {
        var tenants = await _context.Tenants.IgnoreQueryFilters().OrderByDescending(t => t.Id).ToListAsync();

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
            decimal basePrice = t.PlanType switch
            {
                SubscriptionTier.FreeTrial => 0m,
                SubscriptionTier.Starter => 2499m,
                SubscriptionTier.Growth => 6999m,
                SubscriptionTier.Enterprise => 18999m,
                _ => 6999m
            };

            int baseErp = t.PlanType == SubscriptionTier.Starter ? 5 : (t.PlanType == SubscriptionTier.Enterprise ? 50 : 15);
            int baseWorkers = t.PlanType == SubscriptionTier.Starter ? 25 : (t.PlanType == SubscriptionTier.Enterprise ? 500 : 100);

            int extraSeats = Math.Max(0, t.AllowedErpSeats - baseErp);
            int extraWorkers = Math.Max(0, t.AllowedEmployeeRecords - baseWorkers);

            decimal extraSeatsCost = extraSeats * 399m;
            decimal extraWorkersCost = (decimal)Math.Ceiling(extraWorkers / 25.0) * 499m;
            decimal tenantMrr = t.Status == TenantStatus.Active ? (basePrice + extraSeatsCost + extraWorkersCost) : 0m;

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

        if (planFilter.HasValue)
        {
            filteredList = filteredList.Where(x => x.Tenant.PlanType == planFilter.Value);
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
        ViewBag.PlanFilter = planFilter;
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
        SubscriptionTier plan,
        TenantStatus status,
        int allowedErpSeats,
        int allowedEmployeeRecords,
        DateTime? subscriptionEndsAt)
    {
        var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null) return NotFound();

        tenant.PlanType = plan;
        tenant.Status = status;
        tenant.AllowedErpSeats = Math.Max(1, allowedErpSeats);
        tenant.AllowedEmployeeRecords = Math.Max(0, allowedEmployeeRecords);
        if (subscriptionEndsAt.HasValue)
        {
            tenant.SubscriptionEndsAt = subscriptionEndsAt.Value;
        }

        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Tenant '{tenant.BusinessName}' updated: Plan {plan}, Status {status}, {allowedErpSeats} ERP Seats, {allowedEmployeeRecords} Workers.";
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

        TempData["Success"] = $"Switched view to client tenant: '{tenant.BusinessName}' (Subdomain: {tenant.Subdomain}). All queries now filter to this organization.";
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

    // POST: /PlatformAdmin/CreateTenant
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTenant(
        string businessName,
        string subdomain,
        SubscriptionTier planType,
        int allowedErpSeats,
        int allowedEmployeeRecords,
        string adminFullName,
        string adminEmail,
        string adminUsername,
        string adminPassword,
        string? phone)
    {
        var cleanSubdomain = subdomain.Trim().ToLowerInvariant();

        if (await _context.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Subdomain.ToLower() == cleanSubdomain))
        {
            TempData["Error"] = $"Subdomain '{cleanSubdomain}' is already in use by another client.";
            return RedirectToAction(nameof(Index));
        }

        var newTenant = new Tenant
        {
            Subdomain = cleanSubdomain,
            BusinessName = businessName.Trim(),
            PlanType = planType,
            Status = TenantStatus.Active,
            AllowedErpSeats = Math.Max(1, allowedErpSeats),
            AllowedEmployeeRecords = Math.Max(0, allowedEmployeeRecords),
            TrialEndsAt = DateTime.Today.AddDays(14),
            SubscriptionEndsAt = DateTime.Today.AddDays(30),
            AdminEmail = adminEmail.Trim(),
            Phone = phone?.Trim(),
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

        TempData["Success"] = $"Client organization '{newTenant.BusinessName}' created successfully with {newTenant.AllowedErpSeats} ERP seats and {newTenant.AllowedEmployeeRecords} floor workers!";
        return RedirectToAction(nameof(Index));
    }
}
