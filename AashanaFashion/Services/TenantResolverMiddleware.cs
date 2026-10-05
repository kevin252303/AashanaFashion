using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AashanaFashion.Data;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public class TenantResolverMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolverMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, IServiceProvider serviceProvider)
    {
        var host = context.Request.Host.Host;
        string? subdomain = ExtractSubdomain(host);

        if (!string.IsNullOrEmpty(subdomain) && !IsSystemSubdomain(subdomain))
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tenant = await db.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Subdomain.ToLower() == subdomain.ToLower() && t.IsActive);

            if (tenant != null)
            {
                tenantContext.SetCurrentTenant(tenant);
            }
        }

        // Enforce Subscription Status for active web users (exclude static files and login/subscription pages)
        var path = context.Request.Path.Value?.ToLower() ?? "";
        bool isExemptPath = path.StartsWith("/account/login") ||
                           path.StartsWith("/account/logout") ||
                           path.StartsWith("/subscription") ||
                           path.StartsWith("/css") ||
                           path.StartsWith("/js") ||
                           path.StartsWith("/lib") ||
                           path.StartsWith("/images") ||
                           path.StartsWith("/biometricapi");

        if (!isExemptPath && context.User.Identity?.IsAuthenticated == true)
        {
            var tenant = await tenantContext.GetCurrentTenantAsync();
            if (tenant.Status == TenantStatus.Suspended)
            {
                context.Response.Redirect("/Subscription/Suspended");
                return;
            }
        }

        await _next(context);
    }

    private static string? ExtractSubdomain(string host)
    {
        if (string.IsNullOrWhiteSpace(host) || host == "localhost" || host == "127.0.0.1")
            return null;

        // Strip port if present (e.g. "acme.kriyex.com:5000" -> "acme.kriyex.com")
        int colonIdx = host.IndexOf(':');
        if (colonIdx > 0)
        {
            host = host.Substring(0, colonIdx);
        }

        if (host.EndsWith(".kriyex.com", StringComparison.OrdinalIgnoreCase))
        {
            var sub = host.Substring(0, host.Length - ".kriyex.com".Length).Trim();
            if (!string.IsNullOrEmpty(sub) && !sub.Contains('.'))
                return sub.ToLowerInvariant();
        }

        var parts = host.Split('.');
        if (parts.Length > 2)
        {
            // e.g., acme.kriyex.com or acme.domain.com -> "acme"
            return parts[0].ToLowerInvariant();
        }
        else if (parts.Length == 2 && parts[1] == "localhost")
        {
            // e.g. acme.localhost -> "acme"
            return parts[0].ToLowerInvariant();
        }

        return null;
    }

    private static bool IsSystemSubdomain(string sub) =>
        sub is "www" or "app" or "api" or "admin" or "mail" or "staging" or "portal" or "kriyex";
}
