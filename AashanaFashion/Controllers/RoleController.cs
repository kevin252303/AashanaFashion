using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly AppDbContext _context;

        private static readonly List<(string Key, string Display)> Modules = new()
        {
            ("ProductionOrder", "Production Orders"),
            ("DesignMaster",    "Design Master"),
            ("VendorMaster",    "Vendor Master"),
            ("UserManagement",  "User Management"),
        };

        public RoleController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var roles = await _context.UserRoles.Include(r => r.Permissions).ToListAsync();
            return View(roles);
        }

        [HttpGet]
        public IActionResult Create() => View(new UserRole());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserRole model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _context.UserRoles.AnyAsync(r => r.RoleName == model.RoleName))
            {
                ModelState.AddModelError("RoleName", "Role name already exists.");
                return View(model);
            }

            model.CreatedDate = DateTime.Now;
            _context.UserRoles.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Role '{model.RoleName}' created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _context.UserRoles.FindAsync(id);
            if (role == null) return NotFound();
            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserRole model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _context.UserRoles.AnyAsync(r => r.RoleName == model.RoleName && r.Id != model.Id))
            {
                ModelState.AddModelError("RoleName", "Role name already exists.");
                return View(model);
            }

            _context.UserRoles.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Role '{model.RoleName}' updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _context.UserRoles.FindAsync(id);
            if (role != null)
            {
                _context.UserRoles.Remove(role);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Role '{role.RoleName}' deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Permissions(int id)
        {
            var role = await _context.UserRoles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id);
            if (role == null) return NotFound();

            var vm = new RolePermissionViewModel
            {
                UserRoleId = role.Id,
                RoleName = role.RoleName,
                Modules = Modules.Select(m =>
                {
                    var existing = role.Permissions.FirstOrDefault(p => p.Module == m.Key);
                    return new ModulePermissionItem
                    {
                        Module      = m.Key,
                        DisplayName = m.Display,
                        CanView     = existing?.CanView   ?? false,
                        CanCreate   = existing?.CanCreate ?? false,
                        CanEdit     = existing?.CanEdit   ?? false,
                        CanDelete   = existing?.CanDelete ?? false,
                    };
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Permissions(RolePermissionViewModel vm)
        {
            var role = await _context.UserRoles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == vm.UserRoleId);
            if (role == null) return NotFound();

            // Remove existing and replace
            _context.RolePermissions.RemoveRange(role.Permissions);

            foreach (var m in vm.Modules)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    UserRoleId = role.Id,
                    Module     = m.Module,
                    CanView    = m.CanView,
                    CanCreate  = m.CanCreate,
                    CanEdit    = m.CanEdit,
                    CanDelete  = m.CanDelete,
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Permissions for '{role.RoleName}' saved.";
            return RedirectToAction(nameof(Index));
        }
    }
}
