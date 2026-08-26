using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace AashanaFashion.Authorization
{
    public class PermissionAuthorizeAttribute : TypeFilterAttribute
    {
        public PermissionAuthorizeAttribute(string module, string action)
            : base(typeof(PermissionAuthorizeFilter))
        {
            Arguments = new object[] { module, action };
        }
    }

    public class PermissionAuthorizeFilter : IAuthorizationFilter
    {
        private readonly string _module;
        private readonly string _action;

        public PermissionAuthorizeFilter(string module, string action)
        {
            _module = module;
            _action = action;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var role = user.FindFirst(ClaimTypes.Role)?.Value;

            // Accounting module is restricted strictly to "System Admin" only
            if (_module == "Accounting")
            {
                if (role == "System Admin")
                    return;

                context.Result = new ForbidResult();
                return;
            }

            // SuperAdmin and Admin always have full access
            if (role == "SuperAdmin" || role == "Admin")
                return;

            // Check permission claim: "Permission.ProductionOrder.CanEdit" = "true"
            var claimKey = $"Permission.{_module}.{_action}";
            if (user.FindFirst(claimKey)?.Value == "true")
                return;

            context.Result = new ForbidResult();
        }
    }
}
