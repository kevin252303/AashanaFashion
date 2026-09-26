using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class ColourController : Controller
{
    private readonly AppDbContext _context;

    public ColourController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search, bool? activeOnly)
    {
        var query = _context.Colours.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.ColourName.ToLower().Contains(s) || (c.ColourCode != null && c.ColourCode.ToLower().Contains(s)));
        }

        if (activeOnly == true)
        {
            query = query.Where(c => c.IsActive);
        }

        var colours = await query.OrderBy(c => c.ColourName).ToListAsync();
        ViewBag.Search = search;
        ViewBag.ActiveOnly = activeOnly ?? false;

        return View(colours);
    }

    [PermissionAuthorize("VendorMaster", "CanCreate")]
    [HttpGet]
    public IActionResult Create() => View(new Colour());

    [PermissionAuthorize("VendorMaster", "CanCreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Colour colour)
    {
        if (!ModelState.IsValid) return View(colour);

        _context.Colours.Add(colour);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Colour '{colour.ColourName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("VendorMaster", "CanEdit")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var colour = await _context.Colours.FindAsync(id);
        if (colour == null) return NotFound();
        return View(colour);
    }

    [PermissionAuthorize("VendorMaster", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Colour colour)
    {
        if (!ModelState.IsValid) return View(colour);

        _context.Entry(colour).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Colour '{colour.ColourName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("VendorMaster", "CanDelete")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var colour = await _context.Colours.FindAsync(id);
        if (colour != null)
        {
            _context.Colours.Remove(colour);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Colour '{colour.ColourName}' deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
}
