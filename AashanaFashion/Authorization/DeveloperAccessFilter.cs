using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AashanaFashion.Authorization;

public class DeveloperAccessFilter : IActionFilter
{
    private static readonly HashSet<string> AllowedControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "PlatformAdmin",
        "Subscription",
        "Account",
        "Home"
    };

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated == true && user.IsInRole("Developer"))
        {
            var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
            if (!AllowedControllers.Contains(controller))
            {
                context.Result = new RedirectToActionResult("Index", "PlatformAdmin", null);
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
