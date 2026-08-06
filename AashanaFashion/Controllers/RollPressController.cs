using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class RollPressController : Controller
{
    private readonly AppDbContext _context;

    public RollPressController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index(int? month, int? year)
    {
        var currentMonth = month ?? DateTime.Now.Month;
        var currentYear = year ?? DateTime.Now.Year;

        var startDate = new DateTime(currentYear, currentMonth, 1);
        var endDate = startDate.AddMonths(1);

        var entries = await _context.RollPressEntries
            .Where(r => r.GivenDate >= startDate && r.GivenDate < endDate)
            .OrderByDescending(r => r.GivenDate)
            .ToListAsync();

        var summary = entries
            .GroupBy(r => r.DesignNo)
            .Select(g => new RollPressSummaryViewModel
            {
                DesignNo = g.Key,
                TotalPieces = g.Sum(x => x.NumberOfPieces),
                EntryCount = g.Count()
            })
            .ToList();

        var arrivingToday = await _context.RollPressEntries
            .Where(r => r.ArrivalDate.Date == DateTime.Today)
            .OrderByDescending(r => r.GivenDate)
            .ToListAsync();

        ViewBag.Month = currentMonth;
        ViewBag.Year = currentYear;
        ViewBag.MonthName = startDate.ToString("MMMM yyyy");
        ViewBag.ArrivingToday = arrivingToday;
        ViewBag.ArrivingTodayCount = arrivingToday.Sum(x => x.NumberOfPieces);

        var vm = new RollPressIndexViewModel
        {
            Entries = entries,
            Summary = summary
        };

        return View(vm);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public IActionResult Create() => View(new RollPressEntry 
    { 
        GivenDate = DateTime.Today,
        ArrivalDate = DateTime.Today 
    });

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RollPressEntry entry)
    {
        if (!ModelState.IsValid)
            return View(entry);

        entry.CreatedDate = DateTime.Now;
        _context.RollPressEntries.Add(entry);
        await _context.SaveChangesAsync();
        
        TempData["Success"] = $"Added {entry.NumberOfPieces} pieces for {entry.DesignNo}";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var entry = await _context.RollPressEntries.FindAsync(id);
        if (entry != null)
        {
            _context.RollPressEntries.Remove(entry);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Entry deleted";
        }
        return RedirectToAction(nameof(Index));
    }
}

public class RollPressIndexViewModel
{
    public List<RollPressEntry> Entries { get; set; } = new();
    public List<RollPressSummaryViewModel> Summary { get; set; } = new();
}

public class RollPressSummaryViewModel
{
    public string DesignNo { get; set; } = string.Empty;
    public int TotalPieces { get; set; }
    public int EntryCount { get; set; }
}
