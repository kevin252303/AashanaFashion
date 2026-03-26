using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class DesignController : Controller
{
    private readonly AppDbContext _context;

    public DesignController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var designs = await _context.Designs.OrderBy(d => d.DesignNumber).ToListAsync();
        return View(designs);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Create() => View(new Design());

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Design design)
    {
        if (!ModelState.IsValid) return View(design);

        var existing = await _context.Designs.AnyAsync(d => d.DesignNumber == design.DesignNumber);
        if (existing)
        {
            ModelState.AddModelError("DesignNumber", "Design number already exists.");
            return View(design);
        }

        design.CreatedDate = DateTime.Now;
        _context.Designs.Add(design);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Design '{design.DesignNumber}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var design = await _context.Designs.FindAsync(id);
        if (design == null) return NotFound();
        return View(design);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Design design)
    {
        if (!ModelState.IsValid) return View(design);

        var existing = await _context.Designs.AnyAsync(d => d.DesignNumber == design.DesignNumber && d.Id != design.Id);
        if (existing)
        {
            ModelState.AddModelError("DesignNumber", "Design number already exists.");
            return View(design);
        }

        _context.Designs.Update(design);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Design '{design.DesignNumber}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var design = await _context.Designs.FindAsync(id);
        if (design != null)
        {
            _context.Designs.Remove(design);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Design '{design.DesignNumber}' deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
}
