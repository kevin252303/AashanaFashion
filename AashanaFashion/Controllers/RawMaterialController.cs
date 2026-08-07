using AashanaFashion.Authorization;
using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class RawMaterialController : Controller
{
    private readonly AppDbContext _context;

    public RawMaterialController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var materials = await _context.RawMaterials.OrderBy(m => m.Name).ToListAsync();
        var requirements = await _context.RawMaterialRequirements
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();

        var vm = new RawMaterialViewModel
        {
            Materials = materials,
            Requirements = requirements
        };

        return View(vm);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult CreateMaterial()
    {
        return View(new RawMaterial());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMaterial(RawMaterial material)
    {
        if (!ModelState.IsValid) return View(material);

        material.CreatedDate = DateTime.Now;
        _context.RawMaterials.Add(material);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Added {material.Name}";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> EditMaterial(int id)
    {
        var material = await _context.RawMaterials.FindAsync(id);
        if (material == null) return NotFound();
        return View(material);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMaterial(RawMaterial material)
    {
        if (!ModelState.IsValid) return View(material);

        var existing = await _context.RawMaterials.FindAsync(material.Id);
        if (existing == null) return NotFound();

        existing.Name = material.Name;
        existing.Description = material.Description;
        existing.Unit = material.Unit;
        existing.MinimumStock = material.MinimumStock;
        existing.Rate = material.Rate;

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Updated {material.Name}";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> InOut(int? materialId)
    {
        var vm = new RawMaterialInOutViewModel
        {
            AvailableMaterials = await _context.RawMaterials.OrderBy(m => m.Name).ToListAsync()
        };

        if (materialId.HasValue)
        {
            var material = await _context.RawMaterials.FindAsync(materialId.Value);
            if (material != null)
            {
                vm.MaterialId = material.Id;
                vm.MaterialName = material.Name;
                vm.Unit = material.Unit;
                vm.CurrentStock = material.CurrentStock;
                vm.MinimumStock = material.MinimumStock;
                vm.RecentTransactions = await _context.RawMaterialTransactions
                    .Where(t => t.RawMaterialId == materialId)
                    .OrderByDescending(t => t.CreatedDate)
                    .Take(20)
                    .ToListAsync();
            }
        }

        return View(vm);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InOut(RawMaterialInOutViewModel model)
    {
        var material = await _context.RawMaterials.FindAsync(model.MaterialId);
        if (material == null) return NotFound();

        var validLines = model.Lines?.Where(l => l.Quantity > 0).ToList() ?? new();
        if (!validLines.Any())
        {
            ModelState.AddModelError("", "Add at least one entry with quantity > 0.");
            model.AvailableMaterials = await _context.RawMaterials.OrderBy(m => m.Name).ToListAsync();
            model.MaterialName = material.Name;
            model.Unit = material.Unit;
            model.CurrentStock = material.CurrentStock;
            model.MinimumStock = material.MinimumStock;
            return View(model);
        }

        foreach (var line in validLines)
        {
            if (line.Type == "Outward" && line.Quantity > material.CurrentStock)
            {
                ModelState.AddModelError("", $"Cannot issue {line.Quantity} {material.Unit} of '{material.Name}' — only {material.CurrentStock} available.");
                model.AvailableMaterials = await _context.RawMaterials.OrderBy(m => m.Name).ToListAsync();
                model.MaterialName = material.Name;
                model.Unit = material.Unit;
                model.CurrentStock = material.CurrentStock;
                model.MinimumStock = material.MinimumStock;
                return View(model);
            }

            var adjustment = line.Type == "Inward" ? line.Quantity : -line.Quantity;
            material.CurrentStock += adjustment;

            _context.RawMaterialTransactions.Add(new RawMaterialTransaction
            {
                RawMaterialId = material.Id,
                Type = line.Type,
                Quantity = line.Quantity,
                BalanceAfter = material.CurrentStock,
                Remarks = line.Remarks,
                CreatedDate = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Stock updated for {material.Name}";
        return RedirectToAction(nameof(InOut), new { materialId = material.Id });
    }

    [PermissionAuthorize("RawMaterial", "CanCreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RaiseRequirement(RawMaterialRequirement requirement)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Index));

        requirement.CreatedDate = DateTime.Now;
        requirement.Status = "Pending";
        _context.RawMaterialRequirements.Add(requirement);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Requirement raised";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("RawMaterial", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRequirementStatus(int id, string status)
    {
        var requirement = await _context.RawMaterialRequirements.FindAsync(id);
        if (requirement == null) return RedirectToAction(nameof(Index));

        requirement.Status = status;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Requirement marked as {status}";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> DeleteMaterial(int id)
    {
        var material = await _context.RawMaterials.FindAsync(id);
        if (material != null)
        {
            _context.RawMaterials.Remove(material);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Material deleted";
        }
        return RedirectToAction(nameof(Index));
    }
}

public class RawMaterialViewModel
{
    public List<RawMaterial> Materials { get; set; } = new();
    public List<RawMaterialRequirement> Requirements { get; set; } = new();
}
