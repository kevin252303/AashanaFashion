using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Services;

namespace AashanaFashion.Controllers;

[Authorize]
public class PricelistController : Controller
{
    private readonly AppDbContext _context;
    private readonly IPricelistService _pricelistService;

    public PricelistController(AppDbContext context, IPricelistService pricelistService)
    {
        _context = context;
        _pricelistService = pricelistService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, bool? activeOnly)
    {
        var query = _context.Pricelists
            .Include(p => p.Items)
            .Include(p => p.Customers)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || (p.Description != null && p.Description.ToLower().Contains(term)));
        }

        if (activeOnly == true)
        {
            query = query.Where(p => p.IsActive);
        }

        var list = await query.OrderByDescending(p => p.IsActive).ThenBy(p => p.Name).ToListAsync();
        ViewBag.Search = search;
        ViewBag.ActiveOnly = activeOnly;

        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var pricelist = await _context.Pricelists
            .Include(p => p.Items)
                .ThenInclude(i => i.Design)
            .Include(p => p.Customers)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pricelist == null) return NotFound();

        return View(pricelist);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        var model = new Pricelist
        {
            Currency = "INR (₹)",
            DiscountPolicy = PricelistDiscountPolicy.DiscountIncluded,
            IsActive = true
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Pricelist model, List<PricelistItem>? items)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("Name", "Pricelist Name is required.");
        }

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(model);
        }

        model.Name = model.Name.Trim();
        model.Description = model.Description?.Trim();
        model.CreatedDate = DateTime.Now;

        if (items != null)
        {
            foreach (var item in items)
            {
                if (item.MinQuantity <= 0) item.MinQuantity = 1m;
                model.Items.Add(item);
            }
        }

        _context.Pricelists.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Pricelist '{model.Name}' created successfully with {model.Items.Count} rule(s).";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var pricelist = await _context.Pricelists
            .Include(p => p.Items)
                .ThenInclude(i => i.Design)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pricelist == null) return NotFound();

        await LoadDropdownsAsync();
        return View(pricelist);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Pricelist model, List<PricelistItem>? items)
    {
        if (id != model.Id) return NotFound();

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError("Name", "Pricelist Name is required.");
        }

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(model);
        }

        var dbPricelist = await _context.Pricelists
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (dbPricelist == null) return NotFound();

        dbPricelist.Name = model.Name.Trim();
        dbPricelist.Currency = model.Currency;
        dbPricelist.DiscountPolicy = model.DiscountPolicy;
        dbPricelist.IsActive = model.IsActive;
        dbPricelist.Description = model.Description?.Trim();

        // Replace Items
        _context.PricelistItems.RemoveRange(dbPricelist.Items);
        dbPricelist.Items.Clear();

        if (items != null)
        {
            foreach (var item in items)
            {
                if (item.MinQuantity <= 0) item.MinQuantity = 1m;
                dbPricelist.Items.Add(new PricelistItem
                {
                    PricelistId = id,
                    AppliedOn = item.AppliedOn,
                    Category = item.Category?.Trim(),
                    DesignId = item.DesignId > 0 ? item.DesignId : null,
                    MinQuantity = item.MinQuantity,
                    ComputationMethod = item.ComputationMethod,
                    FixedPrice = item.FixedPrice,
                    DiscountPercentage = item.DiscountPercentage,
                    Surcharge = item.Surcharge,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate
                });
            }
        }

        // Also sync any Customer text field
        var linkedCustomers = await _context.Customers.Where(c => c.PricelistId == id).ToListAsync();
        foreach (var cust in linkedCustomers)
        {
            cust.Pricelist = dbPricelist.Name;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Pricelist '{dbPricelist.Name}' updated successfully.";
        return RedirectToAction(nameof(Details), new { id = dbPricelist.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var pricelist = await _context.Pricelists.FindAsync(id);
        if (pricelist != null)
        {
            pricelist.IsActive = !pricelist.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Pricelist '{pricelist.Name}' is now {(pricelist.IsActive ? "Active" : "Inactive")}.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var pricelist = await _context.Pricelists
            .Include(p => p.Customers)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pricelist != null)
        {
            if (pricelist.Customers.Any())
            {
                TempData["Error"] = $"Cannot delete Pricelist '{pricelist.Name}' because it is assigned to {pricelist.Customers.Count} customer(s). Please unassign it first or deactivate the pricelist.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.Pricelists.Remove(pricelist);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Pricelist '{pricelist.Name}' deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Quick creation modal endpoint directly from Customer Master form!
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCreate([FromBody] QuickCreatePricelistModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            return BadRequest(new { success = false, message = "Pricelist name is required." });
        }

        var trimmedName = model.Name.Trim();
        var existing = await _context.Pricelists.FirstOrDefaultAsync(p => p.Name.ToLower() == trimmedName.ToLower());
        if (existing != null)
        {
            return BadRequest(new { success = false, message = $"A pricelist with name '{trimmedName}' already exists." });
        }

        var pl = new Pricelist
        {
            Name = trimmedName,
            Currency = "INR (₹)",
            DiscountPolicy = model.DiscountPolicy,
            IsActive = true,
            Description = model.Description?.Trim(),
            CreatedDate = DateTime.Now
        };

        // Add initial default rule if specified
        if (model.DiscountPercentage > 0 || model.FixedPrice > 0)
        {
            pl.Items.Add(new PricelistItem
            {
                AppliedOn = model.AppliedOn,
                ComputationMethod = model.ComputationMethod,
                DiscountPercentage = model.DiscountPercentage,
                FixedPrice = model.FixedPrice,
                MinQuantity = model.MinQuantity > 0 ? model.MinQuantity : 1m
            });
        }
        else
        {
            // Default 0% baseline rule for all products
            pl.Items.Add(new PricelistItem
            {
                AppliedOn = PricelistAppliedOn.AllProducts,
                ComputationMethod = PricelistComputeMethod.Percentage,
                DiscountPercentage = 0m,
                MinQuantity = 1m
            });
        }

        _context.Pricelists.Add(pl);
        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            id = pl.Id,
            name = pl.Name,
            policy = pl.DiscountPolicyDisplay,
            rulesCount = pl.Items.Count
        });
    }

    [HttpGet]
    public async Task<IActionResult> CalculatePrice(int? pricelistId, int designId, decimal quantity, string? colour = null, string? size = null)
    {
        var result = await _pricelistService.CalculatePriceAsync(pricelistId, designId, quantity, null, colour, size);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomerPrice(int customerId, int designId, decimal quantity, string? colour = null, string? size = null)
    {
        var result = await _pricelistService.CalculateCustomerPriceAsync(customerId, designId, quantity, null, colour, size);
        return Json(result);
    }

    private async Task LoadDropdownsAsync()
    {
        ViewBag.Designs = await _context.Designs
            .OrderBy(d => d.DesignNumber)
            .Select(d => new { d.Id, d.DesignNumber, d.SalesPrice, d.Category })
            .ToListAsync();

        var masterCats = await _context.ProductCategories
            .Where(c => c.IsActive)
            .Select(c => c.CategoryName)
            .ToListAsync();

        var designCats = await _context.Designs
            .Where(d => !string.IsNullOrEmpty(d.Category))
            .Select(d => d.Category!)
            .Distinct()
            .ToListAsync();

        ViewBag.Categories = masterCats.Union(designCats).OrderBy(c => c).ToList();
    }
}
