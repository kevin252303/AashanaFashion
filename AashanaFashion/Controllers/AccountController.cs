using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AashanaFashion.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context) => _context = context;

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Production");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users.FirstOrDefault(u =>
                u.Username == model.Username && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName),
                new Claim("UserId", user.Id.ToString())
            };

            // SuperAdmin gets all roles as claims so every [Authorize(Roles=...)] passes
            if (user.Role == "SuperAdmin")
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                claims.Add(new Claim(ClaimTypes.Role, "Manager"));
                claims.Add(new Claim(ClaimTypes.Role, "Viewer"));
            }

            // Load role permissions from UserRoleList and add as claims
            var roleRecord = await _context.UserRoles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.RoleName == user.Role && r.IsActive);

            if (roleRecord != null)
            {
                foreach (var perm in roleRecord.Permissions)
                {
                    if (perm.CanView)   claims.Add(new Claim($"Permission.{perm.Module}.CanView",   "true"));
                    if (perm.CanCreate) claims.Add(new Claim($"Permission.{perm.Module}.CanCreate", "true"));
                    if (perm.CanEdit)   claims.Add(new Claim($"Permission.{perm.Module}.CanEdit",   "true"));
                    if (perm.CanDelete) claims.Add(new Claim($"Permission.{perm.Module}.CanDelete", "true"));
                }
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = model.RememberMe };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Production");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        public IActionResult AccessDenied() => View();
    }
}
