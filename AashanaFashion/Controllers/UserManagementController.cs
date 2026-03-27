using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers
{
    [Authorize]
    public class UserManagementController : Controller
    {
        private readonly AppDbContext _context;

        public UserManagementController(AppDbContext context) => _context = context;

        // GET: /UserManagement — all authenticated roles can view
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // GET: /UserManagement/Create — Admin only
        [PermissionAuthorize("UserManagement", "CanCreate")]
        public async Task<IActionResult> Create()
        {
            var roles = await _context.UserRoles.Where(r => r.IsActive).OrderBy(r => r.RoleName).ToListAsync();
            return View(new UserFormViewModel { AvailableRoles = roles });
        }

        // POST: /UserManagement/Create — Admin only
        [PermissionAuthorize("UserManagement", "CanCreate")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError("Password", "Password is required for new users.");

            if (!ModelState.IsValid)
            {
                model.AvailableRoles = await _context.UserRoles.Where(r => r.IsActive).OrderBy(r => r.RoleName).ToListAsync();
                return View(model);
            }

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Username already exists.");
                model.AvailableRoles = await _context.UserRoles.Where(r => r.IsActive).OrderBy(r => r.RoleName).ToListAsync();
                return View(model);
            }

            var user = new AppUser
            {
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                DisplayName = model.DisplayName,
                Username = model.Username,
                Email = model.Email,
                ContactNumber = model.ContactNumber,
                Address = model.Address,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password!),
                Role = model.Role,
                IsActive = model.IsActive
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"User '{user.Username}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /UserManagement/Edit/5 — Admin only
        [PermissionAuthorize("UserManagement", "CanEdit")]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var roles = await _context.UserRoles.Where(r => r.IsActive).OrderBy(r => r.RoleName).ToListAsync();
            return View(new UserFormViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                DisplayName = user.DisplayName,
                Username = user.Username,
                Email = user.Email,
                ContactNumber = user.ContactNumber,
                Address = user.Address,
                Role = user.Role,
                IsActive = user.IsActive,
                AvailableRoles = roles
            });
        }

        // POST: /UserManagement/Edit/5 — Admin only
        [PermissionAuthorize("UserManagement", "CanEdit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = await _context.UserRoles.Where(r => r.IsActive).OrderBy(r => r.RoleName).ToListAsync();
                return View(model);
            }

            var user = await _context.Users.FindAsync(model.Id);
            if (user == null) return NotFound();

            if (await _context.Users.AnyAsync(u => u.Username == model.Username && u.Id != model.Id))
            {
                ModelState.AddModelError("Username", "Username already taken.");
                model.AvailableRoles = await _context.UserRoles.Where(r => r.IsActive).OrderBy(r => r.RoleName).ToListAsync();
                return View(model);
            }

            user.FirstName = model.FirstName;
            user.MiddleName = model.MiddleName;
            user.LastName = model.LastName;
            user.DisplayName = model.DisplayName;
            user.Username = model.Username;
            user.Email = model.Email;
            user.ContactNumber = model.ContactNumber;
            user.Address = model.Address;
            user.Role = model.Role;
            user.IsActive = model.IsActive;

            if (!string.IsNullOrWhiteSpace(model.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            await _context.SaveChangesAsync();
            TempData["Success"] = $"User '{user.Username}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /UserManagement/Delete/5 — Admin only
        [PermissionAuthorize("UserManagement", "CanDelete")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = int.Parse(User.FindFirst("UserId")!.Value);
            if (id == currentUserId)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"User '{user.Username}' deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /UserManagement/ToggleActive/5 — Admin only
        [PermissionAuthorize("UserManagement", "CanEdit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"User '{user.Username}' {(user.IsActive ? "activated" : "deactivated")}.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
