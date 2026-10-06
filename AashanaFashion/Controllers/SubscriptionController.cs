using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Services;

namespace AashanaFashion.Controllers;

public class SubscriptionController : Controller
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IDoubleEntryService _doubleEntryService;

    public SubscriptionController(
        AppDbContext context,
        ITenantContext tenantContext,
        IDoubleEntryService doubleEntryService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _doubleEntryService = doubleEntryService;
    }

    // GET: /Subscription
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var tenant = await _tenantContext.GetCurrentTenantAsync();

        var usedErpSeats = await _context.Users.CountAsync(u => u.IsActive);
        var usedEmployees = await _context.Employees.CountAsync(e => e.IsActive);

        var isTrial = tenant.TrialEndsAt > DateTime.Now && tenant.PlanType == SubscriptionTier.FreeTrial;
        var expiryDate = isTrial ? tenant.TrialEndsAt : (tenant.SubscriptionEndsAt ?? tenant.TrialEndsAt);
        var daysRemaining = Math.Max(0, (int)(expiryDate - DateTime.Today).TotalDays);

        // Calculate billing estimate (Base plan + add-ons)
        decimal basePrice = tenant.PlanType switch
        {
            SubscriptionTier.FreeTrial => 0m,
            SubscriptionTier.Starter => 2499m,
            SubscriptionTier.Growth => 6999m,
            SubscriptionTier.Enterprise => 18999m,
            _ => 6999m
        };

        // Add-on calculations: base included seats: Starter=5, Growth=15, Enterprise=50
        int baseErp = tenant.PlanType == SubscriptionTier.Starter ? 5 : (tenant.PlanType == SubscriptionTier.Enterprise ? 50 : 10);
        int baseWorkers = tenant.PlanType == SubscriptionTier.Starter ? 25 : (tenant.PlanType == SubscriptionTier.Enterprise ? 250 : 50);

        int extraSeats = Math.Max(0, tenant.AllowedErpSeats - baseErp);
        int extraWorkers = Math.Max(0, tenant.AllowedEmployeeRecords - baseWorkers);

        decimal extraSeatsCost = extraSeats * 399m;
        decimal extraWorkersCost = (decimal)Math.Ceiling(extraWorkers / 25.0) * 499m;
        decimal monthlyBilling = basePrice + extraSeatsCost + extraWorkersCost;

        var vm = new SubscriptionDashboardViewModel
        {
            Tenant = tenant,
            UsedErpSeats = usedErpSeats,
            TotalErpSeats = tenant.AllowedErpSeats,
            UsedEmployeeRecords = usedEmployees,
            TotalEmployeeRecords = tenant.AllowedEmployeeRecords,
            DaysRemaining = daysRemaining,
            IsTrial = isTrial,
            MonthlyBillingEstimate = monthlyBilling
        };

        return View(vm);
    }

    // POST: /Subscription/AddSeats
    [Authorize(Roles = "Developer,Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSeats(int additionalErpSeats, int additionalWorkers)
    {
        var tenant = await _tenantContext.GetCurrentTenantAsync();

        if (additionalErpSeats < 0 || additionalWorkers < 0)
        {
            TempData["Error"] = "Added seats/workers must be positive numbers.";
            return RedirectToAction(nameof(Index));
        }

        tenant.AllowedErpSeats += additionalErpSeats;
        tenant.AllowedEmployeeRecords += additionalWorkers;

        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Capacity updated! ERP Seats: {tenant.AllowedErpSeats} (added +{additionalErpSeats}), Floor Workers: {tenant.AllowedEmployeeRecords} (added +{additionalWorkers}).";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Subscription/ChangePlan
    [Authorize(Roles = "Developer,Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePlan(SubscriptionTier newPlan)
    {
        var tenant = await _tenantContext.GetCurrentTenantAsync();
        tenant.PlanType = newPlan;

        if (newPlan == SubscriptionTier.Starter)
        {
            tenant.AllowedErpSeats = Math.Max(tenant.AllowedErpSeats, 5);
            tenant.AllowedEmployeeRecords = Math.Max(tenant.AllowedEmployeeRecords, 25);
        }
        else if (newPlan == SubscriptionTier.Growth)
        {
            tenant.AllowedErpSeats = Math.Max(tenant.AllowedErpSeats, 15);
            tenant.AllowedEmployeeRecords = Math.Max(tenant.AllowedEmployeeRecords, 100);
        }
        else if (newPlan == SubscriptionTier.Enterprise)
        {
            tenant.AllowedErpSeats = Math.Max(tenant.AllowedErpSeats, 50);
            tenant.AllowedEmployeeRecords = Math.Max(tenant.AllowedEmployeeRecords, 500);
        }

        tenant.Status = TenantStatus.Active;
        tenant.SubscriptionEndsAt = DateTime.Today.AddDays(30);

        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Plan upgraded to {newPlan}! Your quotas and privileges have been refreshed.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Subscription/Suspended
    [AllowAnonymous]
    public IActionResult Suspended()
    {
        return View();
    }

    // GET: /Subscription/RegisterTenant (Public Self-Service Sign-up)
    [AllowAnonymous]
    public IActionResult RegisterTenant()
    {
        return RedirectToAction("Login", "Account", new { tab = "register" });
    }

    // POST: /Subscription/RegisterTenant
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterTenant(RegisterTenantViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var cleanSubdomain = model.Subdomain.Trim().ToLowerInvariant()
            .Replace("https://", "")
            .Replace("http://", "")
            .Replace(".kriyex.com", "")
            .Replace(".aashanafashion.com", "")
            .Trim('/', ' ');

        // 1. Validate Subdomain Uniqueness
        var subdomainTaken = await _context.Tenants.IgnoreQueryFilters()
            .AnyAsync(t => t.Subdomain.ToLower() == cleanSubdomain);

        if (subdomainTaken)
        {
            ModelState.AddModelError(nameof(model.Subdomain), $"The subdomain '{cleanSubdomain}.kriyex.com' is already taken. Please choose another one.");
            return View(model);
        }

        // 2. Set quotas according to selected tier
        int initialErpSeats = model.PlanType switch
        {
            SubscriptionTier.Starter => 5,
            SubscriptionTier.Enterprise => 50,
            _ => 15 // Growth
        };

        int initialEmployeeRecords = model.PlanType switch
        {
            SubscriptionTier.Starter => 25,
            SubscriptionTier.Enterprise => 500,
            _ => 100 // Growth
        };

        var newTenant = new Tenant
        {
            Subdomain = cleanSubdomain,
            BusinessName = model.BusinessName.Trim(),
            PlanType = model.PlanType,
            Status = TenantStatus.Active,
            AllowedErpSeats = initialErpSeats,
            AllowedEmployeeRecords = initialEmployeeRecords,
            TrialEndsAt = DateTime.Today.AddDays(14),
            AdminEmail = model.AdminEmail.Trim(),
            Phone = model.Phone?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Tenants.Add(newTenant);
        await _context.SaveChangesAsync();

        // 3. Create Default Company for this tenant
        var words = model.BusinessName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var companyCode = words.Length >= 2
            ? $"{words[0][0]}{words[1][0]}".ToUpper()
            : (model.BusinessName.Length >= 2 ? model.BusinessName.Substring(0, 2).ToUpper() : "CO");

        var newCompany = new Company
        {
            TenantId = newTenant.Id,
            CompanyName = model.BusinessName.Trim(),
            CompanyCode = companyCode,
            Email = model.AdminEmail.Trim(),
            Phone = model.Phone?.Trim(),
            IsActive = true,
            IsDefault = true,
            CreatedDate = DateTime.Now
        };

        _context.Companies.Add(newCompany);
        await _context.SaveChangesAsync();

        // 4. Provision standard Chart of Accounts & Journals for this company
        try
        {
            await _doubleEntryService.EnsureDefaultChartOfAccountsAsync(newCompany.Id);
        }
        catch
        {
            // Non-blocking if accounts already exist
        }

        // 5. Create the Tenant Admin user
        var nameParts = model.AdminFullName.Trim().Split(' ', 2);
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

        var adminUser = new AppUser
        {
            TenantId = newTenant.Id,
            DefaultCompanyId = newCompany.Id,
            Username = model.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Role = "Admin",
            FirstName = firstName,
            LastName = lastName,
            DisplayName = model.AdminFullName.Trim(),
            Email = model.AdminEmail.Trim(),
            ContactNumber = model.Phone?.Trim(),
            IsActive = true
        };

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync();

        // 6. Sign in the new Tenant Admin immediately
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, adminUser.Username),
            new Claim(ClaimTypes.Role, adminUser.Role),
            new Claim("FullName", adminUser.FullName),
            new Claim("UserId", adminUser.Id.ToString()),
            new Claim("TenantId", newTenant.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        // Set tenant cookie
        Response.Cookies.Append("AF_ACTIVE_TENANT_ID", newTenant.Id.ToString(), new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true
        });

        TempData["Success"] = $"Congratulations! '{newTenant.BusinessName}' has been provisioned successfully! Your workspace is live at https://{newTenant.Subdomain}.kriyex.com.";
        return RedirectToAction("Index", "Production");
    }
}
