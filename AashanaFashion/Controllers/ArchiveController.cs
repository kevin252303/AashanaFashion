using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class ArchiveController : Controller
{
    private readonly AppDbContext _context;

    public ArchiveController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? filter, string tab = "products")
    {
        // 1. Whole discontinued products
        var productQuery = _context.Designs
            .Include(d => d.ProductCategory)
            .Include(d => d.ColourImages)
            .Include(d => d.DiscontinuedVariants)
            .Where(d => d.Discontinued)
            .AsQueryable();

        // 2. Products with discontinued variants
        var variantDesignQuery = _context.Designs
            .Include(d => d.ProductCategory)
            .Include(d => d.ColourImages)
            .Include(d => d.DiscontinuedVariants)
            .Where(d => d.DiscontinuedVariants.Any())
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            productQuery = productQuery.Where(d => d.DesignNumber.ToLower().Contains(s)
                || (d.Category != null && d.Category.ToLower().Contains(s))
                || (d.ProductCategory != null && d.ProductCategory.CategoryName.ToLower().Contains(s)));

            variantDesignQuery = variantDesignQuery.Where(d => d.DesignNumber.ToLower().Contains(s)
                || (d.Category != null && d.Category.ToLower().Contains(s))
                || (d.ProductCategory != null && d.ProductCategory.CategoryName.ToLower().Contains(s)));
        }

        var discontinuedProducts = await productQuery.OrderBy(d => d.DesignNumber).ToListAsync();
        var designsWithDiscVariants = await variantDesignQuery.OrderBy(d => d.DesignNumber).ToListAsync();

        // Collect all relevant design IDs to load ReadyProduct stock in one go
        var allDesignIds = discontinuedProducts.Select(d => d.Id)
            .Union(designsWithDiscVariants.Select(d => d.Id))
            .Distinct()
            .ToList();

        var readyProducts = await _context.ReadyProducts
            .Where(rp => allDesignIds.Contains(rp.DesignId) && rp.IsActive)
            .ToListAsync();

        // Calculate Stock per Discontinued Product
        var productStockMap = new Dictionary<int, int>();
        foreach (var p in discontinuedProducts)
        {
            var stock = readyProducts.Where(rp => rp.DesignId == p.Id).Sum(rp => rp.QuantityOnHand);
            productStockMap[p.Id] = stock;
        }

        // Flatten Discontinued Variants list with their stock
        var variantItems = new List<ArchiveVariantItemViewModel>();
        foreach (var design in designsWithDiscVariants)
        {
            foreach (var v in design.DiscontinuedVariants)
            {
                var matchingStock = readyProducts
                    .Where(rp => rp.DesignId == design.Id
                        && (string.IsNullOrEmpty(v.Colour) || rp.Colour.Equals(v.Colour, StringComparison.OrdinalIgnoreCase))
                        && (string.IsNullOrEmpty(v.Size) || rp.Size.Equals(v.Size, StringComparison.OrdinalIgnoreCase)))
                    .Sum(rp => rp.QuantityOnHand);

                variantItems.Add(new ArchiveVariantItemViewModel
                {
                    VariantId = v.Id,
                    DesignId = design.Id,
                    DesignNumber = design.DesignNumber,
                    CategoryName = design.ProductCategory?.CategoryName ?? design.Category ?? "Garment",
                    Colour = v.Colour,
                    Size = v.Size,
                    DiscontinuedDate = v.DiscontinuedDate,
                    Reason = v.Reason,
                    QuantityOnHand = matchingStock,
                    UnitPrice = design.SalesPrice > 0 ? design.SalesPrice : design.Price,
                    PhotoPath = !string.IsNullOrEmpty(v.Colour) 
                        ? (design.ColourImages.FirstOrDefault(ci => ci.Colour.Equals(v.Colour, StringComparison.OrdinalIgnoreCase))?.PhotoPath ?? design.PhotoPath)
                        : design.PhotoPath
                });
            }
        }

        if (filter == "in_stock")
        {
            discontinuedProducts = discontinuedProducts.Where(p => productStockMap.GetValueOrDefault(p.Id, 0) > 0).ToList();
            variantItems = variantItems.Where(v => v.QuantityOnHand > 0).ToList();
        }
        else if (filter == "out_of_stock")
        {
            discontinuedProducts = discontinuedProducts.Where(p => productStockMap.GetValueOrDefault(p.Id, 0) == 0).ToList();
            variantItems = variantItems.Where(v => v.QuantityOnHand == 0).ToList();
        }

        // Summary KPI calculations
        int totalDiscontinuedProducts = discontinuedProducts.Count;
        int totalDiscontinuedVariants = variantItems.Count;
        int totalProductStock = productStockMap.Values.Sum();
        decimal totalStockValue = discontinuedProducts.Sum(p => productStockMap.GetValueOrDefault(p.Id, 0) * (p.SalesPrice > 0 ? p.SalesPrice : p.Price));

        ViewBag.Search = search;
        ViewBag.Filter = filter;
        ViewBag.ActiveTab = tab;
        ViewBag.TotalProducts = totalDiscontinuedProducts;
        ViewBag.TotalVariants = totalDiscontinuedVariants;
        ViewBag.TotalStock = totalProductStock;
        ViewBag.TotalStockValue = totalStockValue;
        ViewBag.ProductStockMap = productStockMap;
        ViewBag.VariantItems = variantItems;

        return View(discontinuedProducts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreProduct(int id)
    {
        var design = await _context.Designs.FindAsync(id);
        if (design == null) return NotFound();

        design.Discontinued = false;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Product '{design.DesignNumber}' has been restored and returned to active Product Master.";
        return RedirectToAction(nameof(Index), new { tab = "products" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreVariant(int id)
    {
        var variant = await _context.DesignDiscontinuedVariants
            .Include(v => v.Design)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (variant == null) return NotFound();

        var designNo = variant.Design?.DesignNumber ?? "Design";
        var col = string.IsNullOrEmpty(variant.Colour) ? "All Colours" : variant.Colour;
        var sz = string.IsNullOrEmpty(variant.Size) ? "All Sizes" : variant.Size;

        _context.DesignDiscontinuedVariants.Remove(variant);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Variant '{col} / {sz}' of '{designNo}' has been restored to active production.";
        return RedirectToAction(nameof(Index), new { tab = "variants" });
    }
}

public class ArchiveVariantItemViewModel
{
    public int VariantId { get; set; }
    public int DesignId { get; set; }
    public string DesignNumber { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Colour { get; set; }
    public string? Size { get; set; }
    public DateTime DiscontinuedDate { get; set; }
    public string? Reason { get; set; }
    public int QuantityOnHand { get; set; }
    public decimal UnitPrice { get; set; }
    public string? PhotoPath { get; set; }
}
