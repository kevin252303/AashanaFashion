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
        var recent = await _context.RawMaterialTransactions
            .Include(t => t.RawMaterial)
            .OrderByDescending(t => t.CreatedDate)
            .Take(10)
            .ToListAsync();

        var vm = new RawMaterialViewModel
        {
            Materials = materials,
            Requirements = requirements,
            TotalValuation = materials.Sum(m => m.CurrentStock * m.Rate),
            LowStockCount = materials.Count(m => m.CurrentStock <= m.MinimumStock && m.CurrentStock > 0),
            OutOfStockCount = materials.Count(m => m.CurrentStock <= 0),
            RecentTransactions = recent
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

    [HttpGet]
    public async Task<IActionResult> Ledger(int? materialId, string? type, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.RawMaterialTransactions
            .Include(t => t.RawMaterial)
            .AsQueryable();

        if (materialId.HasValue && materialId.Value > 0)
        {
            query = query.Where(t => t.RawMaterialId == materialId.Value);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(t => t.Type == type);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.CreatedDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.CreatedDate < toDate.Value.Date.AddDays(1));
        }

        var transactions = await query.OrderByDescending(t => t.CreatedDate).ToListAsync();
        var materials = await _context.RawMaterials.OrderBy(m => m.Name).ToListAsync();

        var vm = new RawMaterialLedgerViewModel
        {
            MaterialId = materialId,
            Type = type,
            FromDate = fromDate,
            ToDate = toDate,
            Transactions = transactions,
            Materials = materials,
            TotalInward = transactions.Where(t => t.Type == "Inward").Sum(t => t.Quantity),
            TotalOutward = transactions.Where(t => t.Type == "Outward").Sum(t => t.Quantity),
            TotalAdjustment = transactions.Where(t => t.Type == "Adjustment").Sum(t => t.Quantity)
        };

        return View(vm);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> AdjustStock(int? materialId)
    {
        var materials = await _context.RawMaterials.OrderBy(m => m.Name).ToListAsync();
        var vm = new RawMaterialAdjustmentViewModel
        {
            AvailableMaterials = materials,
            MaterialId = materialId ?? 0
        };

        if (materialId.HasValue && materialId.Value > 0)
        {
            var mat = materials.FirstOrDefault(m => m.Id == materialId.Value);
            if (mat != null)
            {
                vm.CurrentStock = mat.CurrentStock;
                vm.Unit = mat.Unit;
                vm.MaterialName = mat.Name;
                vm.NewQuantity = mat.CurrentStock;
            }
        }

        return View(vm);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(RawMaterialAdjustmentViewModel model)
    {
        var material = await _context.RawMaterials.FindAsync(model.MaterialId);
        if (material == null)
        {
            ModelState.AddModelError("", "Selected material not found.");
            model.AvailableMaterials = await _context.RawMaterials.OrderBy(m => m.Name).ToListAsync();
            return View(model);
        }

        if (model.AdjustmentType == "Count")
        {
            var diff = model.NewQuantity - material.CurrentStock;
            material.CurrentStock = model.NewQuantity;
            _context.RawMaterialTransactions.Add(new RawMaterialTransaction
            {
                RawMaterialId = material.Id,
                Type = "Adjustment",
                Quantity = diff,
                BalanceAfter = material.CurrentStock,
                ReferenceType = "PhysicalCount",
                Remarks = string.IsNullOrWhiteSpace(model.Reason) ? "Physical Stock Count Reconciled" : model.Reason,
                CreatedDate = DateTime.Now
            });
        }
        else
        {
            var delta = model.AdjustmentType == "Add" ? model.Quantity : -model.Quantity;
            if (material.CurrentStock + delta < 0)
            {
                ModelState.AddModelError("", $"Cannot reduce stock below 0. Current stock is {material.CurrentStock}.");
                model.AvailableMaterials = await _context.RawMaterials.OrderBy(m => m.Name).ToListAsync();
                model.MaterialName = material.Name;
                model.CurrentStock = material.CurrentStock;
                model.Unit = material.Unit;
                return View(model);
            }

            material.CurrentStock += delta;
            _context.RawMaterialTransactions.Add(new RawMaterialTransaction
            {
                RawMaterialId = material.Id,
                Type = "Adjustment",
                Quantity = delta,
                BalanceAfter = material.CurrentStock,
                ReferenceType = "ManualAdjustment",
                Remarks = string.IsNullOrWhiteSpace(model.Reason) ? "Manual stock adjustment" : model.Reason,
                CreatedDate = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Stock adjusted for {material.Name}. New Stock: {material.CurrentStock} {material.Unit}";
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
    public decimal TotalValuation { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public List<RawMaterialTransaction> RecentTransactions { get; set; } = new();
}
