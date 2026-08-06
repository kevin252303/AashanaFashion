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
    [HttpPost]
    public async Task<IActionResult> CreateMaterial(RawMaterial material)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Index));

        material.CreatedDate = DateTime.Now;
        _context.RawMaterials.Add(material);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Added {material.Name}";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
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

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> UpdateStock(int id, decimal quantity, bool isAddition)
    {
        var material = await _context.RawMaterials.FindAsync(id);
        if (material == null) return RedirectToAction(nameof(Index));

        if (isAddition)
            material.CurrentStock += quantity;
        else
            material.CurrentStock = Math.Max(0, material.CurrentStock - quantity);

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Stock updated for {material.Name}";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
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
