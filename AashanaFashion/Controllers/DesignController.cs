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
        var designs = await _context.Designs
            .Include(d => d.ProductCategory)
            .Include(d => d.ExtraCharges)
            .OrderBy(d => d.DesignNumber)
            .ToListAsync();
        return View(designs);
    }

    [PermissionAuthorize("DesignMaster", "CanCreate")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
        ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
        ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
        ViewBag.Users = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ToListAsync();
        ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
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
        List<ProductExtraCharge>? ExtraCharges,
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

        CleanOptionalChildModelErrors(design);

        if (!ModelState.IsValid)
        {
            ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
            ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
            ViewBag.Users = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ToListAsync();
            ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
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
            ViewBag.Users = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ToListAsync();
            ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
            ViewBag.SelectedColours = design.Colours?.Split(',').Select(c => c.Trim()).ToList() ?? new List<string>();
            ViewBag.SelectedSizes = design.Sizes?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>();
            return View(design);
        }

        if (design.CategoryId.HasValue)
        {
            var cat = await _context.ProductCategories.FindAsync(design.CategoryId.Value);
            if (cat != null)
            {
                design.Category = cat.CategoryName;
                if (string.IsNullOrWhiteSpace(design.HsnSacCode) && !string.IsNullOrWhiteSpace(cat.DefaultHsnCode))
                {
                    design.HsnSacCode = cat.DefaultHsnCode;
                }
            }
        }

        design.CreatedDate = DateTime.Now;
        _context.Designs.Add(design);
        await _context.SaveChangesAsync();

        if (AttributeLines?.Any() == true)
            foreach (var a in AttributeLines.Where(a => !string.IsNullOrWhiteSpace(a.Attribute)))
            {
                a.Attribute = a.Attribute?.Trim() ?? string.Empty;
                a.Values = a.Values?.Trim() ?? string.Empty;
                a.DesignId = design.Id;
                _context.ProductAttributeLines.Add(a);
            }

        if (Pricelists?.Any() == true)
            foreach (var p in Pricelists.Where(p => !string.IsNullOrWhiteSpace(p.Pricelist)))
            {
                p.Pricelist = p.Pricelist?.Trim() ?? string.Empty;
                p.AppliedOn = p.AppliedOn?.Trim() ?? string.Empty;
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
                pkg.PackagingName = pkg.PackagingName?.Trim() ?? string.Empty;
                pkg.DesignId = design.Id;
                _context.ProductPackagings.Add(pkg);
            }

        if (ExtraCharges?.Any() == true)
            foreach (var ec in ExtraCharges.Where(ec => ec.ExtraCharge > 0 && !string.IsNullOrWhiteSpace(ec.AttributeValue)))
            {
                ec.Id = 0;
                ec.DesignId = design.Id;
                _context.ProductExtraCharges.Add(ec);
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
            .Include(d => d.ExtraCharges)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (design == null) return NotFound();

        ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
        ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
        ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
        ViewBag.Users = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ToListAsync();
        ViewBag.SelectedColours = design.Colours?.Split(',').Select(c => c.Trim()).ToList() ?? new List<string>();
        ViewBag.SelectedSizes = design.Sizes?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>();
        ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();

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
        List<ProductExtraCharge>? ExtraCharges,
        List<int>? selectedColours,
        List<int>? selectedSizes)
    {
        CleanOptionalChildModelErrors(design);

        if (!ModelState.IsValid)
        {
            ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
            ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
            ViewBag.Users = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ToListAsync();
            ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
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
            ViewBag.Users = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ToListAsync();
            ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
            ViewBag.SelectedColours = selectedColours != null ? (await _context.Colours.Where(c => selectedColours.Contains(c.Id)).Select(c => c.ColourName).ToListAsync()) : new List<string>();
            ViewBag.SelectedSizes = selectedSizes != null ? (await _context.Sizes.Where(s => selectedSizes.Contains(s.Id)).Select(s => s.SizeName).ToListAsync()) : new List<string>();
            return View(design);
        }

        var dbDesign = await _context.Designs
            .Include(d => d.AttributeLines)
            .Include(d => d.Pricelists)
            .Include(d => d.ProductVendors)
            .Include(d => d.Packagings)
            .Include(d => d.ExtraCharges)
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
        dbDesign.CategoryId = design.CategoryId;
        if (design.CategoryId.HasValue)
        {
            var cat = await _context.ProductCategories.FindAsync(design.CategoryId.Value);
            if (cat != null)
            {
                dbDesign.Category = cat.CategoryName;
                if (string.IsNullOrWhiteSpace(dbDesign.HsnSacCode) && !string.IsNullOrWhiteSpace(cat.DefaultHsnCode))
                {
                    dbDesign.HsnSacCode = cat.DefaultHsnCode;
                }
            }
        }
        else
        {
            dbDesign.Category = design.Category;
        }
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
                a.Attribute = a.Attribute?.Trim() ?? string.Empty;
                a.Values = a.Values?.Trim() ?? string.Empty;
                a.DesignId = dbDesign.Id;
                _context.ProductAttributeLines.Add(a);
            }

        _context.ProductPricelists.RemoveRange(dbDesign.Pricelists);
        if (Pricelists?.Any() == true)
            foreach (var p in Pricelists.Where(p => !string.IsNullOrWhiteSpace(p.Pricelist)))
            {
                p.Id = 0;
                p.Pricelist = p.Pricelist?.Trim() ?? string.Empty;
                p.AppliedOn = p.AppliedOn?.Trim() ?? string.Empty;
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
                pkg.PackagingName = pkg.PackagingName?.Trim() ?? string.Empty;
                pkg.DesignId = dbDesign.Id;
                _context.ProductPackagings.Add(pkg);
            }

        if (dbDesign.ExtraCharges?.Any() == true)
        {
            _context.ProductExtraCharges.RemoveRange(dbDesign.ExtraCharges);
        }
        else
        {
            var existingCharges = await _context.ProductExtraCharges.Where(e => e.DesignId == dbDesign.Id).ToListAsync();
            _context.ProductExtraCharges.RemoveRange(existingCharges);
        }

        if (ExtraCharges?.Any() == true)
            foreach (var ec in ExtraCharges.Where(ec => ec.ExtraCharge > 0 && !string.IsNullOrWhiteSpace(ec.AttributeValue)))
            {
                ec.Id = 0;
                ec.DesignId = dbDesign.Id;
                _context.ProductExtraCharges.Add(ec);
            }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Product '{dbDesign.DesignNumber}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetVariantPrice(int designId, string? colour, string? size)
    {
        var design = await _context.Designs
            .Include(d => d.ExtraCharges)
            .FirstOrDefaultAsync(d => d.Id == designId);

        if (design == null) return NotFound();

        decimal basePrice = design.SalesPrice > 0 ? design.SalesPrice : design.Price;
        decimal colourCharge = 0m;
        decimal sizeCharge = 0m;

        if (!string.IsNullOrWhiteSpace(colour))
        {
            var cMatch = design.ExtraCharges.FirstOrDefault(e => e.AttributeType == "Colour" &&
                string.Equals(e.AttributeValue.Trim(), colour.Trim(), StringComparison.OrdinalIgnoreCase));
            if (cMatch != null) colourCharge = cMatch.ExtraCharge;
        }

        if (!string.IsNullOrWhiteSpace(size))
        {
            var sMatch = design.ExtraCharges.FirstOrDefault(e => e.AttributeType == "Size" &&
                string.Equals(e.AttributeValue.Trim(), size.Trim(), StringComparison.OrdinalIgnoreCase));
            if (sMatch != null) sizeCharge = sMatch.ExtraCharge;
        }

        decimal totalExtra = colourCharge + sizeCharge;
        decimal effectivePrice = basePrice + totalExtra;

        return Json(new
        {
            designId,
            basePrice,
            colourCharge,
            sizeCharge,
            totalExtra,
            effectivePrice,
            extraCharges = design.ExtraCharges.Select(e => new { e.AttributeType, e.AttributeValue, e.ExtraCharge, e.Remarks })
        });
    }

    [PermissionAuthorize("DesignMaster", "CanView")]
    [HttpGet]
    public async Task<IActionResult> Bom(int id)
    {
        var design = await _context.Designs
            .Include(d => d.BomItems)
                .ThenInclude(b => b.RawMaterial)
            .Include(d => d.OperationCosts)
                .ThenInclude(o => o.Vendor)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (design == null) return NotFound();

        ViewBag.RawMaterials = await _context.RawMaterials.OrderBy(m => m.Name).ToListAsync();
        ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
        return View(design);
    }

    [PermissionAuthorize("DesignMaster", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bom(
        int id,
        decimal SalesPrice,
        List<DesignBomItem>? BomItems,
        List<DesignOperationCost>? OperationCosts)
    {
        var design = await _context.Designs
            .Include(d => d.BomItems)
            .Include(d => d.OperationCosts)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (design == null) return NotFound();

        design.SalesPrice = SalesPrice;

        // Replace BOM items
        _context.DesignBomItems.RemoveRange(design.BomItems);
        if (BomItems?.Any() == true)
        {
            foreach (var b in BomItems.Where(x => x.RawMaterialId > 0 && x.QuantityPerPiece > 0))
            {
                design.BomItems.Add(new DesignBomItem
                {
                    DesignId = design.Id,
                    RawMaterialId = b.RawMaterialId,
                    Component = string.IsNullOrWhiteSpace(b.Component) ? "General" : b.Component.Trim(),
                    QuantityPerPiece = b.QuantityPerPiece,
                    WastagePercentage = b.WastagePercentage,
                    Remarks = b.Remarks
                });
            }
        }

        // Replace Operation costs
        _context.DesignOperationCosts.RemoveRange(design.OperationCosts);
        if (OperationCosts?.Any() == true)
        {
            foreach (var o in OperationCosts.Where(x => !string.IsNullOrWhiteSpace(x.OperationName) && x.EstimatedCost > 0))
            {
                design.OperationCosts.Add(new DesignOperationCost
                {
                    DesignId = design.Id,
                    OperationName = o.OperationName.Trim(),
                    EstimatedCost = o.EstimatedCost,
                    VendorId = o.VendorId > 0 ? o.VendorId : null,
                    Remarks = o.Remarks
                });
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Bill of Materials & Cost Sheet saved for Design '{design.DesignNumber}'.";
        return RedirectToAction(nameof(Bom), new { id = design.Id });
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
            .Include(d => d.BomItems)
            .Include(d => d.OperationCosts)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (design != null)
        {
            _context.ProductAttributeLines.RemoveRange(design.AttributeLines);
            _context.ProductPricelists.RemoveRange(design.Pricelists);
            _context.ProductVendors.RemoveRange(design.ProductVendors);
            _context.ProductPackagings.RemoveRange(design.Packagings);
            _context.DesignBomItems.RemoveRange(design.BomItems);
            _context.DesignOperationCosts.RemoveRange(design.OperationCosts);
            _context.Designs.Remove(design);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Product '{design.DesignNumber}' deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    private void CleanOptionalChildModelErrors(Design design)
    {
        // 1. Remove validation errors for optional child collections (attributes, pricelists, vendors, packagings, extra charges)
        var childPrefixes = new[] { "AttributeLines", "Pricelists", "ProductVendors", "Packagings", "ExtraCharges" };
        foreach (var key in ModelState.Keys.Where(k => childPrefixes.Any(p => k.StartsWith(p, StringComparison.OrdinalIgnoreCase))).ToList())
        {
            ModelState.Remove(key);
        }

        // 2. If SalesPrice was submitted as empty or invalid, default to 0
        if (ModelState.TryGetValue("SalesPrice", out var spEntry) && spEntry.Errors.Any())
        {
            ModelState.Remove("SalesPrice");
            design.SalesPrice = 0m;
        }

        // 3. Remove validation errors for optional fields on Design
        var optionalFieldNames = new[] {
            "InvoicingPolicy", "CommonDNo", "SalesTaxes", "PurchaseTaxes", "Category",
            "HsnSacCode", "Company", "Property1", "InternalNotes", "VisibilityOfProducts",
            "Website", "Tags", "Ribbon", "OutOfStockMessage", "EcommerceDescription",
            "WarningOnSalesOrders", "QuotationDescription", "ReInvoiceCosts", "Responsible",
            "PurchaseDescription", "WarningOnPurchaseOrders", "ControlPolicy", "PhotoPath", "CreationFlow"
        };
        foreach (var f in optionalFieldNames)
        {
            if (ModelState.ContainsKey(f))
            {
                ModelState.Remove(f);
            }
        }

        // 4. Remove any remaining "The value '' is invalid" errors left on optional fields
        foreach (var key in ModelState.Keys.ToList())
        {
            var entry = ModelState[key];
            if (entry != null && entry.Errors.Any(e => e.ErrorMessage.Contains("invalid") || string.IsNullOrEmpty(e.ErrorMessage)))
            {
                if (key != "DesignNumber")
                {
                    ModelState.Remove(key);
                }
            }
        }
    }
}
