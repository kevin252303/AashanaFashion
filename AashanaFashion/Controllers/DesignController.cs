using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    [PermissionAuthorize("DesignMaster", "CanCreate")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
        ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
        ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
        return View(new Design());
    }

    [PermissionAuthorize("DesignMaster", "CanCreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        Design design,
        List<ProductAttributeLine>? AttributeLines,
        List<ProductPricelist>? Pricelists,
        List<ProductVendor>? ProductVendors,
        List<ProductPackaging>? Packagings,
        List<int>? selectedColours,
        List<int>? selectedSizes)
    {
        if (selectedColours?.Any() == true)
        {
            var colourNames = await _context.Colours.Where(c => selectedColours.Contains(c.Id)).Select(c => c.ColourName).ToListAsync();
            design.Colours = string.Join(",", colourNames);
        }
        else
        {
            design.Colours = string.Empty;
        }

        if (selectedSizes?.Any() == true)
        {
            var sizeNames = await _context.Sizes.Where(s => selectedSizes.Contains(s.Id)).OrderBy(s => s.DisplayOrder).Select(s => s.SizeName).ToListAsync();
            design.Sizes = string.Join(",", sizeNames);
        }
        else
        {
            design.Sizes = string.Empty;
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
            ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
            ViewBag.SelectedColours = design.Colours?.Split(',').Select(c => c.Trim()).ToList() ?? new List<string>();
            ViewBag.SelectedSizes = design.Sizes?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>();
            return View(design);
        }

        var existing = await _context.Designs.AnyAsync(d => d.DesignNumber == design.DesignNumber);
        if (existing)
        {
            ModelState.AddModelError("DesignNumber", "Design number already exists.");
            ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
            ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
            ViewBag.SelectedColours = design.Colours?.Split(',').Select(c => c.Trim()).ToList() ?? new List<string>();
            ViewBag.SelectedSizes = design.Sizes?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>();
            return View(design);
        }

        design.CreatedDate = DateTime.Now;
        _context.Designs.Add(design);
        await _context.SaveChangesAsync();

        if (AttributeLines?.Any() == true)
            foreach (var a in AttributeLines.Where(a => !string.IsNullOrWhiteSpace(a.Attribute)))
            {
                a.DesignId = design.Id;
                _context.ProductAttributeLines.Add(a);
            }

        if (Pricelists?.Any() == true)
            foreach (var p in Pricelists.Where(p => !string.IsNullOrWhiteSpace(p.Pricelist)))
            {
                p.DesignId = design.Id;
                _context.ProductPricelists.Add(p);
            }

        if (ProductVendors?.Any() == true)
            foreach (var pv in ProductVendors.Where(pv => pv.VendorId > 0))
            {
                pv.DesignId = design.Id;
                _context.ProductVendors.Add(pv);
            }

        if (Packagings?.Any() == true)
            foreach (var pkg in Packagings.Where(pkg => !string.IsNullOrWhiteSpace(pkg.PackagingName)))
            {
                pkg.DesignId = design.Id;
                _context.ProductPackagings.Add(pkg);
            }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Product '{design.DesignNumber}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("DesignMaster", "CanEdit")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var design = await _context.Designs
            .Include(d => d.AttributeLines)
            .Include(d => d.Pricelists)
            .Include(d => d.ProductVendors)
            .Include(d => d.Packagings)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (design == null) return NotFound();

        ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
        ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
        ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
        ViewBag.SelectedColours = design.Colours?.Split(',').Select(c => c.Trim()).ToList() ?? new List<string>();
        ViewBag.SelectedSizes = design.Sizes?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>();

        return View(design);
    }

    [PermissionAuthorize("DesignMaster", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Design design,
        List<ProductAttributeLine>? AttributeLines,
        List<ProductPricelist>? Pricelists,
        List<ProductVendor>? ProductVendors,
        List<ProductPackaging>? Packagings,
        List<int>? selectedColours,
        List<int>? selectedSizes)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
            ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
            ViewBag.SelectedColours = selectedColours != null ? (await _context.Colours.Where(c => selectedColours.Contains(c.Id)).Select(c => c.ColourName).ToListAsync()) : new List<string>();
            ViewBag.SelectedSizes = selectedSizes != null ? (await _context.Sizes.Where(s => selectedSizes.Contains(s.Id)).Select(s => s.SizeName).ToListAsync()) : new List<string>();
            return View(design);
        }

        var existing = await _context.Designs.AnyAsync(d => d.DesignNumber == design.DesignNumber && d.Id != design.Id);
        if (existing)
        {
            ModelState.AddModelError("DesignNumber", "Design number already exists.");
            ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
            ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
            ViewBag.SelectedColours = selectedColours != null ? (await _context.Colours.Where(c => selectedColours.Contains(c.Id)).Select(c => c.ColourName).ToListAsync()) : new List<string>();
            ViewBag.SelectedSizes = selectedSizes != null ? (await _context.Sizes.Where(s => selectedSizes.Contains(s.Id)).Select(s => s.SizeName).ToListAsync()) : new List<string>();
            return View(design);
        }

        var dbDesign = await _context.Designs
            .Include(d => d.AttributeLines)
            .Include(d => d.Pricelists)
            .Include(d => d.ProductVendors)
            .Include(d => d.Packagings)
            .FirstOrDefaultAsync(d => d.Id == design.Id);

        if (dbDesign == null) return NotFound();

        // Map all fields
        dbDesign.DesignNumber = design.DesignNumber;
        dbDesign.ProductType = design.ProductType;
        dbDesign.InvoicingPolicy = design.InvoicingPolicy;
        dbDesign.TrackInventory = design.TrackInventory;
        dbDesign.QuantityOnHand = design.QuantityOnHand;
        dbDesign.Discontinued = design.Discontinued;
        dbDesign.SalesPrice = design.SalesPrice;
        dbDesign.CommonDNo = design.CommonDNo;
        dbDesign.SalesTaxes = design.SalesTaxes;
        dbDesign.PurchaseTaxes = design.PurchaseTaxes;
        dbDesign.Category = design.Category;
        dbDesign.HsnSacCode = design.HsnSacCode;
        dbDesign.Company = design.Company;
        dbDesign.Property1 = design.Property1;
        dbDesign.InternalNotes = design.InternalNotes;

        dbDesign.VisibilityOfProducts = design.VisibilityOfProducts;
        dbDesign.Website = design.Website;
        dbDesign.Tags = design.Tags;
        dbDesign.IsPublished = design.IsPublished;
        dbDesign.SellWhenOutOfStock = design.SellWhenOutOfStock;
        dbDesign.Ribbon = design.Ribbon;
        dbDesign.ShowAvailableQty = design.ShowAvailableQty;
        dbDesign.OutOfStockMessage = design.OutOfStockMessage;
        dbDesign.EcommerceDescription = design.EcommerceDescription;
        dbDesign.WarningOnSalesOrders = design.WarningOnSalesOrders;
        dbDesign.QuotationDescription = design.QuotationDescription;
        dbDesign.ReInvoiceCosts = design.ReInvoiceCosts;

        dbDesign.RouteBuy = design.RouteBuy;
        dbDesign.RouteManufacture = design.RouteManufacture;
        dbDesign.RouteResupplySubcontractor = design.RouteResupplySubcontractor;
        dbDesign.RouteResupplySubcontractorOnOrder = design.RouteResupplySubcontractorOnOrder;
        dbDesign.Responsible = design.Responsible;
        dbDesign.CustomerLeadTime = design.CustomerLeadTime;
        dbDesign.SafetyFactor = design.SafetyFactor;
        dbDesign.DescriptionForReceipts = design.DescriptionForReceipts;
        dbDesign.DescriptionForInternalTransfers = design.DescriptionForInternalTransfers;
        dbDesign.DescriptionForDeliveryOrders = design.DescriptionForDeliveryOrders;

        dbDesign.PurchaseDescription = design.PurchaseDescription;
        dbDesign.WarningOnPurchaseOrders = design.WarningOnPurchaseOrders;
        dbDesign.ControlPolicy = design.ControlPolicy;

        if (selectedColours?.Any() == true)
        {
            var colourNames = await _context.Colours.Where(c => selectedColours.Contains(c.Id)).Select(c => c.ColourName).ToListAsync();
            dbDesign.Colours = string.Join(",", colourNames);
        }
        else
        {
            dbDesign.Colours = string.Empty;
        }

        if (selectedSizes?.Any() == true)
        {
            var sizeNames = await _context.Sizes.Where(s => selectedSizes.Contains(s.Id)).OrderBy(s => s.DisplayOrder).Select(s => s.SizeName).ToListAsync();
            dbDesign.Sizes = string.Join(",", sizeNames);
        }
        else
        {
            dbDesign.Sizes = string.Empty;
        }
        dbDesign.Price = design.Price;
        dbDesign.CreationFlow = design.CreationFlow;
        dbDesign.IsActive = design.IsActive;

        // Replace child collections
        _context.ProductAttributeLines.RemoveRange(dbDesign.AttributeLines);
        if (AttributeLines?.Any() == true)
            foreach (var a in AttributeLines.Where(a => !string.IsNullOrWhiteSpace(a.Attribute)))
            {
                a.Id = 0;
                a.DesignId = dbDesign.Id;
                _context.ProductAttributeLines.Add(a);
            }

        _context.ProductPricelists.RemoveRange(dbDesign.Pricelists);
        if (Pricelists?.Any() == true)
            foreach (var p in Pricelists.Where(p => !string.IsNullOrWhiteSpace(p.Pricelist)))
            {
                p.Id = 0;
                p.DesignId = dbDesign.Id;
                _context.ProductPricelists.Add(p);
            }

        _context.ProductVendors.RemoveRange(dbDesign.ProductVendors);
        if (ProductVendors?.Any() == true)
            foreach (var pv in ProductVendors.Where(pv => pv.VendorId > 0))
            {
                pv.Id = 0;
                pv.DesignId = dbDesign.Id;
                _context.ProductVendors.Add(pv);
            }

        _context.ProductPackagings.RemoveRange(dbDesign.Packagings);
        if (Packagings?.Any() == true)
            foreach (var pkg in Packagings.Where(pkg => !string.IsNullOrWhiteSpace(pkg.PackagingName)))
            {
                pkg.Id = 0;
                pkg.DesignId = dbDesign.Id;
                _context.ProductPackagings.Add(pkg);
            }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Product '{dbDesign.DesignNumber}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("DesignMaster", "CanDelete")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var design = await _context.Designs
            .Include(d => d.AttributeLines)
            .Include(d => d.Pricelists)
            .Include(d => d.ProductVendors)
            .Include(d => d.Packagings)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (design != null)
        {
            _context.ProductAttributeLines.RemoveRange(design.AttributeLines);
            _context.ProductPricelists.RemoveRange(design.Pricelists);
            _context.ProductVendors.RemoveRange(design.ProductVendors);
            _context.ProductPackagings.RemoveRange(design.Packagings);
            _context.Designs.Remove(design);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Product '{design.DesignNumber}' deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
}
