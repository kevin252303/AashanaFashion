using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class DyingController : Controller
{
    private readonly AppDbContext _context;

    public DyingController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index(int? month, int? year)
    {
        var currentMonth = month ?? DateTime.Now.Month;
        var currentYear = year ?? DateTime.Now.Year;

        var startDate = new DateTime(currentYear, currentMonth, 1);
        var endDate = startDate.AddMonths(1);

        var entries = await _context.DyingEntries
            .Where(d => d.EntryDate >= startDate && d.EntryDate < endDate)
            .OrderByDescending(d => d.EntryDate)
            .ToListAsync();

        var summary = entries
            .GroupBy(d => d.LotNo)
            .Select(g => new DyingSummaryViewModel
            {
                LotNo = g.Key,
                TotalMeters = g.Sum(x => x.Meters),
                EntryCount = g.Count()
            })
            .ToList();

        var totalMeters = entries.Sum(e => e.Meters);

        ViewBag.Month = currentMonth;
        ViewBag.Year = currentYear;
        ViewBag.TotalMeters = totalMeters;
        ViewBag.MonthName = startDate.ToString("MMMM yyyy");

        var vm = new DyingIndexViewModel
        {
            Entries = entries,
            Summary = summary
        };

        return View(vm);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new DyingEntry { EntryDate = DateTime.Today });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DyingEntry entry)
    {
        if (!ModelState.IsValid)
            return View(entry);

        entry.CreatedDate = DateTime.Now;
        _context.DyingEntries.Add(entry);
        await _context.SaveChangesAsync();
        
        TempData["Success"] = $"Added {entry.Meters} meters for Lot {entry.LotNo}";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var entry = await _context.DyingEntries.FindAsync(id);
        if (entry != null)
        {
            _context.DyingEntries.Remove(entry);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Entry deleted";
        }
        return RedirectToAction(nameof(Index));
    }
}

public class DyingIndexViewModel
{
    public List<DyingEntry> Entries { get; set; } = new();
    public List<DyingSummaryViewModel> Summary { get; set; } = new();
}

public class DyingSummaryViewModel
{
    public string LotNo { get; set; } = string.Empty;
    public decimal TotalMeters { get; set; }
    public int EntryCount { get; set; }
}
