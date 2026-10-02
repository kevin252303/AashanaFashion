using System.Security.Claims;
using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Services;

public class CompanyContext : ICompanyContext
{
    private const string CookieName = "AF_ACTIVE_COMPANY_ID";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _context;

    private Company? _cachedActiveCompany;

    public CompanyContext(IHttpContextAccessor httpContextAccessor, AppDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    public async Task<int> GetActiveCompanyIdAsync()
    {
        var company = await GetActiveCompanyAsync();
        return company.Id;
    }

    public async Task<Company> GetActiveCompanyAsync()
    {
        if (_cachedActiveCompany != null)
        {
            return _cachedActiveCompany;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        int? targetCompanyId = null;

        // 1. Try reading cookie
        if (httpContext != null && httpContext.Request.Cookies.TryGetValue(CookieName, out var cookieVal))
        {
            if (int.TryParse(cookieVal, out var cid) && cid > 0)
            {
                targetCompanyId = cid;
            }
        }

        // 2. Validate allowed company for the user
        var allowedCompanies = await GetAllowedCompaniesAsync();
        if (targetCompanyId.HasValue)
        {
            _cachedActiveCompany = allowedCompanies.FirstOrDefault(c => c.Id == targetCompanyId.Value);
        }

        // 3. Fallback to default or first allowed company
        if (_cachedActiveCompany == null)
        {
            _cachedActiveCompany = allowedCompanies.FirstOrDefault(c => c.IsDefault) 
                                   ?? allowedCompanies.FirstOrDefault();
        }

        // 4. Ultimate fallback if db has no allowed company yet (e.g. initial setup)
        if (_cachedActiveCompany == null)
        {
            _cachedActiveCompany = await _context.Companies.FirstOrDefaultAsync(c => c.IsActive)
                                   ?? new Company { Id = 1, CompanyName = "Aashana Fashion", CompanyCode = "AF", IsDefault = true };
        }

        return _cachedActiveCompany;
    }

    public async Task<List<Company>> GetAllowedCompaniesAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = httpContext?.User;

        // If not logged in or Admin / SuperAdmin, can view all active companies
        bool isElevated = user?.IsInRole("Admin") == true || 
                          user?.IsInRole("SuperAdmin") == true || 
                          user?.IsInRole("Manager") == true ||
                          user?.Identity?.IsAuthenticated != true;

        if (isElevated)
        {
            return await _context.Companies
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.IsDefault)
                .ThenBy(c => c.CompanyName)
                .ToListAsync();
        }

        // Specific user assignment check
        var username = user?.Identity?.Name;
        if (!string.IsNullOrEmpty(username))
        {
            var appUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (appUser != null)
            {
                var assignedCompanyIds = await _context.UserCompanies
                    .Where(uc => uc.UserId == appUser.Id)
                    .Select(uc => uc.CompanyId)
                    .ToListAsync();

                if (assignedCompanyIds.Any())
                {
                    return await _context.Companies
                        .Where(c => c.IsActive && assignedCompanyIds.Contains(c.Id))
                        .OrderByDescending(c => c.IsDefault)
                        .ThenBy(c => c.CompanyName)
                        .ToListAsync();
                }
            }
        }

        // Default to all active companies
        return await _context.Companies
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.CompanyName)
            .ToListAsync();
    }

    public void SetActiveCompanyId(int companyId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        var options = new CookieOptions
        {
            Expires = DateTimeOffset.Now.AddDays(30),
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            HttpOnly = false // accessible to client if needed
        };

        httpContext.Response.Cookies.Append(CookieName, companyId.ToString(), options);
        _cachedActiveCompany = null; // Invalidate cache
    }
}
