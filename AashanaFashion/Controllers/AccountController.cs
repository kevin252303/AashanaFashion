using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Services;
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
        private readonly IDoubleEntryService _doubleEntryService;

        public AccountController(AppDbContext context, IDoubleEntryService doubleEntryService)
        {
            _context = context;
            _doubleEntryService = doubleEntryService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null, string? tab = "login")
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Production");

            ViewData["ReturnUrl"] = returnUrl;
            var model = new AuthViewModel
            {
                ActiveTab = string.Equals(tab, "register", StringComparison.OrdinalIgnoreCase) ? "register" : "login"
            };
            return View(model);
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            return RedirectToAction(nameof(Login), new { returnUrl, tab = "register" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AuthViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            model.ActiveTab = "login";

            // Cleanse non-relevant Register validation errors when signing in
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Register", StringComparison.OrdinalIgnoreCase)).ToList())
            {
                ModelState.Remove(key);
            }

            if (string.IsNullOrWhiteSpace(model.Login.Username))
            {
                ModelState.AddModelError("Login.Username", "Username is required.");
            }
            if (string.IsNullOrWhiteSpace(model.Login.Password))
            {
                ModelState.AddModelError("Login.Password", "Password is required.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users.FirstOrDefault(u => u.Username == model.Login.Username && u.IsActive)
                       ?? _context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.Username == model.Login.Username && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Login.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == user.TenantId);
            if (tenant != null && tenant.Status == TenantStatus.Suspended)
            {
                ModelState.AddModelError(string.Empty, "Your company's subscription is suspended. Please contact billing/support.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName),
                new Claim("UserId", user.Id.ToString()),
                new Claim("TenantId", user.TenantId.ToString())
            };

            if (user.CustomerId.HasValue)
            {
                claims.Add(new Claim("CustomerId", user.CustomerId.Value.ToString()));
            }

            // SuperAdmin gets all roles as claims so every [Authorize(Roles=...)] passes
            if (user.Role == "SuperAdmin")
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                claims.Add(new Claim(ClaimTypes.Role, "Manager"));
                claims.Add(new Claim(ClaimTypes.Role, "Viewer"));
            }

            if (user.Role != "Developer")
            {
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
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = model.Login.RememberMe };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                authProperties);

            Response.Cookies.Append("AF_ACTIVE_TENANT_ID", user.TenantId.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true
            });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (user.Role == "Developer")
                return RedirectToAction("Index", "PlatformAdmin");

            if (user.Role == "Customer")
                return RedirectToAction("Index", "CustomerPortal");

            return RedirectToAction("Index", "Production");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(AuthViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            model.ActiveTab = "register";

            // Cleanse non-relevant Login validation errors when registering
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Login", StringComparison.OrdinalIgnoreCase)).ToList())
            {
                ModelState.Remove(key);
            }

            // Validate mandatory fields for client registration
            if (string.IsNullOrWhiteSpace(model.Register.FullName))
                ModelState.AddModelError("Register.FullName", "Full name is required.");
            if (string.IsNullOrWhiteSpace(model.Register.BusinessName))
                ModelState.AddModelError("Register.BusinessName", "Business / Company name is required.");
            if (string.IsNullOrWhiteSpace(model.Register.Email))
                ModelState.AddModelError("Register.Email", "Email address is required.");
            if (string.IsNullOrWhiteSpace(model.Register.Phone))
                ModelState.AddModelError("Register.Phone", "Phone number is required.");
            if (string.IsNullOrWhiteSpace(model.Register.City))
                ModelState.AddModelError("Register.City", "City is required.");
            if (string.IsNullOrWhiteSpace(model.Register.State))
                ModelState.AddModelError("Register.State", "State is required.");
            if (string.IsNullOrWhiteSpace(model.Register.Username))
                ModelState.AddModelError("Register.Username", "Username is required.");
            if (string.IsNullOrWhiteSpace(model.Register.Password) || model.Register.Password.Length < 6)
                ModelState.AddModelError("Register.Password", "Password must be at least 6 characters.");
            if (model.Register.Password != model.Register.ConfirmPassword)
                ModelState.AddModelError("Register.ConfirmPassword", "Passwords do not match.");

            // Check if username already exists
            if (!string.IsNullOrWhiteSpace(model.Register.Username))
            {
                var cleanUsername = model.Register.Username.Trim();
                var exists = await _context.Users.IgnoreQueryFilters()
                    .AnyAsync(u => u.Username.ToLower() == cleanUsername.ToLower());
                if (exists)
                {
                    ModelState.AddModelError("Register.Username", $"The username '{cleanUsername}' is already taken. Please choose another one.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View("Login", model);
            }

            // 1. Auto-generate subdomain from BusinessName
            var cleanBiz = new string(model.Register.BusinessName.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrWhiteSpace(cleanBiz) || cleanBiz.Length < 3)
                cleanBiz = "org" + Random.Shared.Next(100, 999);
            if (cleanBiz.Length > 25)
                cleanBiz = cleanBiz.Substring(0, 25);

            var subdomain = cleanBiz;
            int suffix = 1;
            while (await _context.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Subdomain.ToLower() == subdomain))
            {
                subdomain = $"{cleanBiz}{suffix++}";
            }

            // 2. Provision Tenant (Active, without 14-day trial restrictions)
            var newTenant = new Tenant
            {
                Subdomain = subdomain,
                BusinessName = model.Register.BusinessName.Trim(),
                PlanType = SubscriptionTier.Growth,
                Status = TenantStatus.Active,
                AllowedErpSeats = 15,
                AllowedEmployeeRecords = 100,
                TrialEndsAt = DateTime.Today.AddYears(1),
                SubscriptionEndsAt = DateTime.Today.AddYears(1),
                AdminEmail = model.Register.Email.Trim(),
                Phone = model.Register.Phone.Trim(),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Tenants.Add(newTenant);
            await _context.SaveChangesAsync();

            // 3. Create Default Company for tenant
            var words = model.Register.BusinessName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var companyCode = words.Length >= 2
                ? $"{words[0][0]}{words[1][0]}".ToUpper()
                : (model.Register.BusinessName.Length >= 2 ? model.Register.BusinessName.Substring(0, 2).ToUpper() : "CO");

            var newCompany = new Company
            {
                TenantId = newTenant.Id,
                CompanyName = model.Register.BusinessName.Trim(),
                CompanyCode = companyCode,
                Email = model.Register.Email.Trim(),
                Phone = model.Register.Phone.Trim(),
                City = model.Register.City.Trim(),
                State = model.Register.State.Trim(),
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
                // Non-blocking
            }

            // 5. Create the Tenant Admin user
            var nameParts = model.Register.FullName.Trim().Split(' ', 2);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

            var adminUser = new AppUser
            {
                TenantId = newTenant.Id,
                DefaultCompanyId = newCompany.Id,
                Username = model.Register.Username.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Register.Password),
                Role = "Admin",
                FirstName = firstName,
                LastName = lastName,
                DisplayName = model.Register.FullName.Trim(),
                Email = model.Register.Email.Trim(),
                ContactNumber = model.Register.Phone.Trim(),
                IsActive = true
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();

            // 6. RECORD AS LEAD IN DEVELOPER ACCOUNT
            try
            {
                var devUser = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Role == "Developer");
                var rootCompany = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.TenantId == 1)
                                  ?? await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync();
                int devCompanyId = rootCompany?.Id ?? 1;

                var year = DateTime.Now.Year;
                var leadCount = await _context.Leads.IgnoreQueryFilters().CountAsync() + 1;
                var leadNumber = $"LD-REG-{year}-{leadCount:D4}";

                var lead = new Lead
                {
                    TenantId = 1, // Developer / Platform tenant
                    CompanyId = devCompanyId,
                    LeadNumber = leadNumber,
                    Title = $"Client Registration: {model.Register.BusinessName.Trim()}",
                    CompanyName = model.Register.BusinessName.Trim(),
                    ContactPerson = model.Register.FullName.Trim(),
                    Email = model.Register.Email.Trim(),
                    Phone = model.Register.Phone.Trim(),
                    City = model.Register.City.Trim(),
                    State = model.Register.State.Trim(),
                    EstimatedValue = 6999m,
                    EstimatedQuantity = 1,
                    Stage = LeadStage.New,
                    Source = "Website Registration",
                    AssignedToUserId = devUser?.Id,
                    ExpectedCloseDate = DateTime.Today.AddDays(30),
                    NextFollowUpDate = DateTime.Today.AddDays(1),
                    Notes = $"[Client Registration Details]\n" +
                            $"Client Name: {model.Register.FullName.Trim()}\n" +
                            $"Business Name: {model.Register.BusinessName.Trim()}\n" +
                            $"Email: {model.Register.Email.Trim()}\n" +
                            $"Phone: {model.Register.Phone.Trim()}\n" +
                            $"City: {model.Register.City.Trim()}\n" +
                            $"State: {model.Register.State.Trim()}\n" +
                            $"Username: {model.Register.Username.Trim()}\n" +
                            $"Subdomain: {newTenant.Subdomain}.kriyex.com\n" +
                            $"Tenant ID: {newTenant.Id}\n" +
                            $"Registration Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    CreatedDate = DateTime.Now
                };

                _context.Leads.Add(lead);
                await _context.SaveChangesAsync();

                var leadActivity = new LeadActivity
                {
                    TenantId = 1,
                    LeadId = lead.Id,
                    ActivityType = "Registration",
                    Description = $"Client registered online: {model.Register.FullName} ({model.Register.BusinessName}). Location: {model.Register.City}, {model.Register.State}. Contact: {model.Register.Phone}, {model.Register.Email}.",
                    ActivityDate = DateTime.Now,
                    CreatedBy = "System Registration"
                };

                _context.LeadActivities.Add(leadActivity);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Non-blocking lead creation failure ensures client registration still succeeds
            }

            // 7. Sign in the new Tenant Admin immediately
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

            TempData["Success"] = $"Congratulations {model.Register.FullName}! '{newTenant.BusinessName}' has been registered and provisioned successfully.";
            return RedirectToAction("Index", "Production");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login");

            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (User.Identity?.IsAuthenticated != true)
                return RedirectToAction("Login");

            if (!ModelState.IsValid)
                return View(model);

            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login");
            }

            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Incorrect current password.");
                return View(model);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your password has been changed successfully.";
            return RedirectToAction("Index", "Production");
        }

        public IActionResult AccessDenied() => View();
    }
}
