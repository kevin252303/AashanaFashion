using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class SizeController : Controller
{
    private readonly AppDbContext _context;

    public SizeController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var sizes = await _context.Sizes.OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
        return View(sizes);
    }

    [PermissionAuthorize("VendorMaster", "CanCreate")]
    [HttpGet]
    public IActionResult Create() => View(new Size());

    [PermissionAuthorize("VendorMaster", "CanCreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Size size)
    {
        if (!ModelState.IsValid) return View(size);

        _context.Sizes.Add(size);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Size '{size.SizeName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("VendorMaster", "CanEdit")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var size = await _context.Sizes.FindAsync(id);
        if (size == null) return NotFound();
        return View(size);
    }

    [PermissionAuthorize("VendorMaster", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Size size)
    {
        if (!ModelState.IsValid) return View(size);

        _context.Entry(size).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Size '{size.SizeName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("VendorMaster", "CanDelete")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var size = await _context.Sizes.FindAsync(id);
        if (size != null)
        {
            _context.Sizes.Remove(size);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Size '{size.SizeName}' deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
}
