using AashanaFashion.Data;
using AashanaFashion.Models;
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
        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View(new UserFormViewModel());

        // POST: /UserManagement/Create — Admin only
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError("Password", "Password is required for new users.");

            if (!ModelState.IsValid) return View(model);

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Username already exists.");
                return View(model);
            }

            var user = new AppUser
            {
                FullName = model.FullName,
                Username = model.Username,
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return View(new UserFormViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.Username,
                Role = user.Role,
                IsActive = user.IsActive
            });
        }

        // POST: /UserManagement/Edit/5 — Admin only
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FindAsync(model.Id);
            if (user == null) return NotFound();

            if (await _context.Users.AnyAsync(u => u.Username == model.Username && u.Id != model.Id))
            {
                ModelState.AddModelError("Username", "Username already taken.");
                return View(model);
            }

            user.FullName = model.FullName;
            user.Username = model.Username;
            user.Role = model.Role;
            user.IsActive = model.IsActive;

            if (!string.IsNullOrWhiteSpace(model.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            await _context.SaveChangesAsync();
            TempData["Success"] = $"User '{user.Username}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /UserManagement/Delete/5 — Admin only
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
