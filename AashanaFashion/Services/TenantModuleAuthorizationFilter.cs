using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public class TenantModuleAuthorizationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User;

        // Skip if not authenticated or user is Developer / SuperAdmin
        if (user.Identity?.IsAuthenticated != true || user.IsInRole("Developer") || user.IsInRole("SuperAdmin"))
        {
            await next();
            return;
        }

        var controllerName = context.RouteData.Values["controller"]?.ToString();
        if (string.IsNullOrEmpty(controllerName))
        {
            await next();
            return;
        }

        var requiredModule = AppModules.GetModuleForController(controllerName);
        if (string.IsNullOrEmpty(requiredModule))
        {
            // Core/exempt controller (Account, Subscription, UserManagement, Role, etc.)
            await next();
            return;
        }

        var tenantContext = httpContext.RequestServices.GetRequiredService<ITenantContext>();
        var tenant = await tenantContext.GetCurrentTenantAsync();

        if (tenant != null && !tenant.HasModule(requiredModule))
        {
            // Check if AJAX / JSON request
            bool isAjax = httpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          (httpContext.Request.Headers.ContainsKey("Accept") &&
                           httpContext.Request.Headers["Accept"].ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase));

            if (isAjax)
            {
                context.Result = new JsonResult(new
                {
                    success = false,
                    message = $"The '{requiredModule}' module is not enabled for your organization's subscription plan. Please contact your administrator."
                })
                {
                    StatusCode = 403
                };
                return;
            }

            context.Result = new RedirectToActionResult("ModuleRestricted", "Subscription", new { module = requiredModule });
            return;
        }

        await next();
    }
}
