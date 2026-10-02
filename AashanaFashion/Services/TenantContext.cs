using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AashanaFashion.Data;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public class TenantContext : ITenantContext
{
    private const string TenantCookieName = "AF_ACTIVE_TENANT_ID";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    private Tenant? _currentTenant;
    private int? _currentTenantId;

    public TenantContext(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    public int CurrentTenantId
    {
        get
        {
            if (_currentTenantId.HasValue) return _currentTenantId.Value;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return 1; // Default fallback for CLI/background tools

            // 1. Check User Claim (fastest, no DB lookup)
            var claimVal = httpContext.User?.FindFirst("TenantId")?.Value;
            if (int.TryParse(claimVal, out var tid) && tid > 0)
            {
                _currentTenantId = tid;
                return tid;
            }

            // 2. Check Request Header (API / Mobile Kiosk)
            if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var headerVal))
            {
                if (int.TryParse(headerVal, out var hId) && hId > 0)
                {
                    _currentTenantId = hId;
                    return hId;
                }
            }

            // 3. Check Cookie
            if (httpContext.Request.Cookies.TryGetValue(TenantCookieName, out var cookieVal))
            {
                if (int.TryParse(cookieVal, out var cId) && cId > 0)
                {
                    _currentTenantId = cId;
                    return cId;
                }
            }

            // Default fallback
            _currentTenantId = 1;
            return 1;
        }
    }

    public Tenant? CurrentTenant => _currentTenant;

    public async Task<Tenant> GetCurrentTenantAsync()
    {
        if (_currentTenant != null) return _currentTenant;

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenantId = CurrentTenantId;
        _currentTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId);

        if (_currentTenant == null)
        {
            // Fallback to first active tenant or create default
            _currentTenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.IsActive)
                             ?? await EnsureDefaultTenantAsync(db);
            _currentTenantId = _currentTenant.Id;
        }

        return _currentTenant;
    }

    public void SetCurrentTenant(Tenant tenant)
    {
        _currentTenant = tenant;
        _currentTenantId = tenant.Id;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Response.Cookies.Append(TenantCookieName, tenant.Id.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true
            });
        }
    }

    private static async Task<Tenant> EnsureDefaultTenantAsync(AppDbContext db)
    {
        var defaultTenant = new Tenant
        {
            Id = 1,
            Subdomain = "default",
            BusinessName = "Aashana Fashion",
            PlanType = SubscriptionTier.Growth,
            Status = TenantStatus.Active,
            AllowedErpSeats = 10,
            AllowedEmployeeRecords = 50,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        db.Tenants.Add(defaultTenant);
        await db.SaveChangesAsync();
        return defaultTenant;
    }
}
