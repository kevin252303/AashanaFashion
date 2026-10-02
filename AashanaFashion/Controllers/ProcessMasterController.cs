using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Authorization;

namespace AashanaFashion.Controllers;

[Authorize]
public class ProcessMasterController : Controller
{
    private readonly AppDbContext _context;

    public ProcessMasterController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, bool? activeOnly)
    {
        var query = _context.ProcessMasters.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.ProcessName.ToLower().Contains(term) ||
                                     (p.ProcessCode != null && p.ProcessCode.ToLower().Contains(term)) ||
                                     (p.Description != null && p.Description.ToLower().Contains(term)));
        }

        if (activeOnly == true)
        {
            query = query.Where(p => p.IsActive);
        }

        var list = await query.OrderBy(p => p.DisplayOrder).ThenBy(p => p.ProcessName).ToListAsync();

        ViewBag.Search = search;
        ViewBag.ActiveOnly = activeOnly;
        ViewBag.TotalProcesses = await _context.ProcessMasters.CountAsync();
        ViewBag.ActiveCount = await _context.ProcessMasters.CountAsync(p => p.IsActive);
        ViewBag.InactiveCount = await _context.ProcessMasters.CountAsync(p => !p.IsActive);

        return View(list);
    }

    [PermissionAuthorize("ProcessMaster", "CanCreate")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        int nextOrder = 1;
        if (await _context.ProcessMasters.AnyAsync())
        {
            nextOrder = (await _context.ProcessMasters.MaxAsync(p => p.DisplayOrder)) + 1;
        }

        var model = new ProcessMaster
        {
            DisplayOrder = nextOrder,
            IsActive = true
        };

        return View(model);
    }

    [PermissionAuthorize("ProcessMaster", "CanCreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProcessMaster model)
    {
        if (string.IsNullOrWhiteSpace(model.ProcessName))
        {
            ModelState.AddModelError("ProcessName", "Process Name is required.");
        }
        else
        {
            var exists = await _context.ProcessMasters.AnyAsync(p => p.ProcessName.ToLower() == model.ProcessName.Trim().ToLower());
            if (exists)
            {
                ModelState.AddModelError("ProcessName", "A process with this name already exists.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.ProcessName = model.ProcessName.Trim();
        model.ProcessCode = model.ProcessCode?.Trim();
        model.Description = model.Description?.Trim();
        model.CreatedDate = DateTime.Now;

        _context.ProcessMasters.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Process '{model.ProcessName}' created successfully with sequence #{model.DisplayOrder}.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("ProcessMaster", "CanEdit")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var process = await _context.ProcessMasters.FindAsync(id);
        if (process == null) return NotFound();

        return View(process);
    }

    [PermissionAuthorize("ProcessMaster", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProcessMaster model)
    {
        if (string.IsNullOrWhiteSpace(model.ProcessName))
        {
            ModelState.AddModelError("ProcessName", "Process Name is required.");
        }
        else
        {
            var exists = await _context.ProcessMasters.AnyAsync(p => p.ProcessName.ToLower() == model.ProcessName.Trim().ToLower() && p.Id != model.Id);
            if (exists)
            {
                ModelState.AddModelError("ProcessName", "Another process with this name already exists.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var dbProcess = await _context.ProcessMasters.FindAsync(model.Id);
        if (dbProcess == null) return NotFound();

        dbProcess.ProcessName = model.ProcessName.Trim();
        dbProcess.ProcessCode = model.ProcessCode?.Trim();
        dbProcess.DisplayOrder = model.DisplayOrder;
        dbProcess.Description = model.Description?.Trim();
        dbProcess.IsActive = model.IsActive;

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Process '{dbProcess.ProcessName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("ProcessMaster", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveUp(int id)
    {
        var current = await _context.ProcessMasters.FindAsync(id);
        if (current == null) return NotFound();

        // Find the process immediately preceding this one in DisplayOrder
        var previous = await _context.ProcessMasters
            .Where(p => p.DisplayOrder < current.DisplayOrder)
            .OrderByDescending(p => p.DisplayOrder)
            .FirstOrDefaultAsync();

        if (previous != null)
        {
            int temp = current.DisplayOrder;
            current.DisplayOrder = previous.DisplayOrder;
            previous.DisplayOrder = temp;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Moved '{current.ProcessName}' up in creation order.";
        }

        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("ProcessMaster", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveDown(int id)
    {
        var current = await _context.ProcessMasters.FindAsync(id);
        if (current == null) return NotFound();

        // Find the process immediately succeeding this one in DisplayOrder
        var next = await _context.ProcessMasters
            .Where(p => p.DisplayOrder > current.DisplayOrder)
            .OrderBy(p => p.DisplayOrder)
            .FirstOrDefaultAsync();

        if (next != null)
        {
            int temp = current.DisplayOrder;
            current.DisplayOrder = next.DisplayOrder;
            next.DisplayOrder = temp;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Moved '{current.ProcessName}' down in creation order.";
        }

        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("ProcessMaster", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var process = await _context.ProcessMasters.FindAsync(id);
        if (process == null) return NotFound();

        process.IsActive = !process.IsActive;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Process '{process.ProcessName}' is now {(process.IsActive ? "Active" : "Inactive")}.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("ProcessMaster", "CanDelete")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var process = await _context.ProcessMasters.FindAsync(id);
        if (process == null) return NotFound();

        _context.ProcessMasters.Remove(process);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Process '{process.ProcessName}' deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveProcesses()
    {
        var processes = await _context.ProcessMasters
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new
            {
                p.Id,
                p.ProcessName,
                p.ProcessCode,
                p.DisplayOrder,
                p.Description
            })
            .ToListAsync();

        return Json(processes);
    }
}
