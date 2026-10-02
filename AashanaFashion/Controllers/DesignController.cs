using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Authorization;
using AashanaFashion.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class DesignController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ICompanyContext _companyContext;

    public DesignController(AppDbContext context, IWebHostEnvironment env, ICompanyContext companyContext)
    {
        _context = context;
        _env = env;
        _companyContext = companyContext;
    }

    private async Task<string> SaveUploadedImageAsync(IFormFile file)
    {
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "designs");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        if (!allowed.Contains(ext)) ext = ".jpg";

        var uniqueFileName = $"design_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/designs/{uniqueFileName}";
    }

    public async Task<IActionResult> Index(string? search, int? categoryId, string? productType, bool? activeOnly, int? companyId, bool showDiscontinued = false)
    {
        var activeCompany = await _companyContext.GetActiveCompanyAsync();

        var query = _context.Designs
            .Include(d => d.CompanyRef)
            .Include(d => d.ProductCategory)
            .Include(d => d.ExtraCharges)
            .Include(d => d.HandworkWorker)
            .Include(d => d.StitchingWorker)
            .Include(d => d.ColourImages)
            .Include(d => d.DiscontinuedVariants)
            .AsQueryable();

        if (!showDiscontinued)
        {
            query = query.Where(d => !d.Discontinued);
        }

        // Multi-company product distinction:
        // companyId > 0: Specific company
        // companyId == -1: Only shared across all companies (CompanyId == null)
        // companyId == 0: All companies unrestricted
        // companyId not set (default): Active company + shared products
        if (companyId.HasValue)
        {
            if (companyId.Value > 0)
            {
                query = query.Where(d => d.CompanyId == companyId.Value);
            }
            else if (companyId.Value == -1)
            {
                query = query.Where(d => d.CompanyId == null);
            }
        }
        else
        {
            query = query.Where(d => d.CompanyId == null || d.CompanyId == activeCompany.Id);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(d => d.DesignNumber.ToLower().Contains(s)
                || (d.CommonDNo != null && d.CommonDNo.ToLower().Contains(s))
                || (d.Colours != null && d.Colours.ToLower().Contains(s))
                || (d.Sizes != null && d.Sizes.ToLower().Contains(s))
                || (d.CreationFlow != null && d.CreationFlow.ToLower().Contains(s))
                || (d.ProductCategory != null && d.ProductCategory.CategoryName.ToLower().Contains(s)));
        }

        if (categoryId.HasValue && categoryId > 0)
        {
            query = query.Where(d => d.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(productType))
        {
            query = query.Where(d => d.ProductType == productType);
        }

        if (activeOnly == true)
        {
            query = query.Where(d => d.IsActive && !d.Discontinued);
        }

        var designs = await query.OrderBy(d => d.DesignNumber).ToListAsync();

        ViewBag.Search = search;
        ViewBag.SelectedCategoryId = categoryId;
        ViewBag.SelectedProductType = productType;
        ViewBag.ActiveOnly = activeOnly;
        ViewBag.ShowDiscontinued = showDiscontinued;
        ViewBag.SelectedCompanyId = companyId;
        ViewBag.ActiveCompany = activeCompany;
        ViewBag.ArchivedCount = await _context.Designs.CountAsync(d => d.Discontinued);
        ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
        ViewBag.Companies = await _context.Companies.Where(c => c.IsActive).OrderBy(c => c.CompanyName).ToListAsync();

        return View(designs);
    }

    [PermissionAuthorize("DesignMaster", "CanCreate")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var activeCompany = await _companyContext.GetActiveCompanyAsync();

        ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
        ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
        ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
        ViewBag.Users = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ToListAsync();
        ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
        ViewBag.Processes = await _context.ProcessMasters.Where(p => p.IsActive).OrderBy(p => p.DisplayOrder).ToListAsync();
        ViewBag.Companies = await _context.Companies.Where(c => c.IsActive).OrderBy(c => c.CompanyName).ToListAsync();
        return View(new Design { CompanyId = activeCompany.Id });
    }

    [PermissionAuthorize("DesignMaster", "CanCreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        Design design,
        IFormFile? photoFile,
        List<ProductAttributeLine>? AttributeLines,
        List<ProductPricelist>? Pricelists,
        List<ProductVendor>? ProductVendors,
        List<ProductPackaging>? Packagings,
        List<ProductExtraCharge>? ExtraCharges,
        List<DesignDiscontinuedVariant>? DiscontinuedVariants,
        List<DesignComponentAssignment>? ComponentAssignments,
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
            ViewBag.Processes = await _context.ProcessMasters.Where(p => p.IsActive).OrderBy(p => p.DisplayOrder).ToListAsync();
            ViewBag.Companies = await _context.Companies.Where(c => c.IsActive).OrderBy(c => c.CompanyName).ToListAsync();
            ViewBag.SelectedColours = design.Colours?.Split(',').Select(c => c.Trim()).ToList() ?? new List<string>();
            ViewBag.SelectedSizes = design.Sizes?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>();
            return View(design);
        }

        var existing = await _context.Designs.AnyAsync(d => d.DesignNumber == design.DesignNumber && (d.CompanyId == design.CompanyId || d.CompanyId == null || design.CompanyId == null));
        if (existing)
        {
            ModelState.AddModelError("DesignNumber", "Design number already exists for this company scope.");
            ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
            ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
            ViewBag.Users = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ToListAsync();
            ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
            ViewBag.Processes = await _context.ProcessMasters.Where(p => p.IsActive).OrderBy(p => p.DisplayOrder).ToListAsync();
            ViewBag.Companies = await _context.Companies.Where(c => c.IsActive).OrderBy(c => c.CompanyName).ToListAsync();
            ViewBag.SelectedColours = design.Colours?.Split(',').Select(c => c.Trim()).ToList() ?? new List<string>();
            ViewBag.SelectedSizes = design.Sizes?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>();
            return View(design);
        }

        design.CompanyId = design.CompanyId > 0 ? design.CompanyId : null;
        if (design.CompanyId.HasValue)
        {
            var comp = await _context.Companies.FindAsync(design.CompanyId.Value);
            design.Company = comp?.CompanyName;
        }
        else
        {
            design.Company = null;
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

        if (photoFile != null && photoFile.Length > 0)
        {
            design.PhotoPath = await SaveUploadedImageAsync(photoFile);
        }

        design.HandworkWorkerId = design.HandworkWorkerId > 0 ? design.HandworkWorkerId : null;
        design.StitchingWorkerId = design.StitchingWorkerId > 0 ? design.StitchingWorkerId : null;
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

        if (!string.IsNullOrWhiteSpace(design.Components))
        {
            design.Components = string.Join(", ", design.GetComponentsList());
        }
        else
        {
            design.Components = "Chaniya, Choli, Dupatta";
        }

        if (ComponentAssignments != null && ComponentAssignments.Any())
        {
            foreach (var ca in ComponentAssignments.Where(a => a.VendorId.HasValue && a.VendorId.Value > 0 && !string.IsNullOrWhiteSpace(a.ComponentName) && !string.IsNullOrWhiteSpace(a.ProcessName)))
            {
                _context.DesignComponentAssignments.Add(new DesignComponentAssignment
                {
                    DesignId = design.Id,
                    ComponentName = ca.ComponentName.Trim(),
                    ProcessName = ca.ProcessName.Trim(),
                    VendorId = ca.VendorId,
                    EstimatedRate = ca.EstimatedRate,
                    Remarks = ca.Remarks
                });
            }
        }

        // Process colour-wise photos (supports multiple images per colour)
        var allColours = await _context.Colours.Where(c => c.IsActive).ToListAsync();
        foreach (var file in Request.Form.Files)
        {
            if ((file.Name.StartsWith("colourPhotos_") || file.Name.StartsWith("colourPhoto_")) && file.Length > 0)
            {
                string prefix = file.Name.StartsWith("colourPhotos_") ? "colourPhotos_" : "colourPhoto_";
                string rawColourName = file.Name.Substring(prefix.Length).Trim();
                string cleanColourName = rawColourName.TrimEnd('[', ']').Trim();
                var colourInfo = allColours.FirstOrDefault(c => c.ColourName.Equals(cleanColourName, StringComparison.OrdinalIgnoreCase));
                if (colourInfo == null)
                {
                    var lastUnderscore = cleanColourName.LastIndexOf('_');
                    if (lastUnderscore > 0)
                    {
                        var possibleName = cleanColourName.Substring(0, lastUnderscore);
                        colourInfo = allColours.FirstOrDefault(c => c.ColourName.Equals(possibleName, StringComparison.OrdinalIgnoreCase));
                    }
                }

                var finalColourName = colourInfo?.ColourName ?? cleanColourName;
                if (!string.IsNullOrEmpty(finalColourName))
                {
                    var savedPath = await SaveUploadedImageAsync(file);
                    _context.DesignColourImages.Add(new DesignColourImage
                    {
                        DesignId = design.Id,
                        Colour = finalColourName,
                        ColourCode = colourInfo?.ColourCode,
                        PhotoPath = savedPath,
                        CreatedDate = DateTime.Now
                    });

                    if (string.IsNullOrEmpty(design.PhotoPath))
                    {
                        design.PhotoPath = savedPath;
                    }
                }
            }
        }

        if (DiscontinuedVariants?.Any() == true)
        {
            foreach (var v in DiscontinuedVariants.Where(v => !string.IsNullOrWhiteSpace(v.Colour) || !string.IsNullOrWhiteSpace(v.Size)))
            {
                _context.DesignDiscontinuedVariants.Add(new DesignDiscontinuedVariant
                {
                    DesignId = design.Id,
                    Colour = string.IsNullOrWhiteSpace(v.Colour) ? null : v.Colour.Trim(),
                    Size = string.IsNullOrWhiteSpace(v.Size) ? null : v.Size.Trim(),
                    DiscontinuedDate = v.DiscontinuedDate != default ? v.DiscontinuedDate : DateTime.Now,
                    Reason = v.Reason?.Trim()
                });
            }
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
            .Include(d => d.HandworkWorker)
            .Include(d => d.StitchingWorker)
            .Include(d => d.ColourImages)
            .Include(d => d.DiscontinuedVariants)
            .Include(d => d.ComponentAssignments)
                .ThenInclude(a => a.Vendor)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (design == null) return NotFound();

        ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
        ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
        ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
        ViewBag.Users = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ToListAsync();
        ViewBag.SelectedColours = design.Colours?.Split(',').Select(c => c.Trim()).ToList() ?? new List<string>();
        ViewBag.SelectedSizes = design.Sizes?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>();
        ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
        ViewBag.Processes = await _context.ProcessMasters.Where(p => p.IsActive).OrderBy(p => p.DisplayOrder).ToListAsync();
        ViewBag.Companies = await _context.Companies.Where(c => c.IsActive).OrderBy(c => c.CompanyName).ToListAsync();

        return View(design);
    }

    [PermissionAuthorize("DesignMaster", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Design design,
        IFormFile? photoFile,
        List<ProductAttributeLine>? AttributeLines,
        List<ProductPricelist>? Pricelists,
        List<ProductVendor>? ProductVendors,
        List<ProductPackaging>? Packagings,
        List<ProductExtraCharge>? ExtraCharges,
        List<DesignDiscontinuedVariant>? DiscontinuedVariants,
        List<DesignComponentAssignment>? ComponentAssignments,
        List<int>? deletedColourImageIds,
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
            ViewBag.Processes = await _context.ProcessMasters.Where(p => p.IsActive).OrderBy(p => p.DisplayOrder).ToListAsync();
            ViewBag.Companies = await _context.Companies.Where(c => c.IsActive).OrderBy(c => c.CompanyName).ToListAsync();
            ViewBag.SelectedColours = selectedColours != null ? (await _context.Colours.Where(c => selectedColours.Contains(c.Id)).Select(c => c.ColourName).ToListAsync()) : new List<string>();
            ViewBag.SelectedSizes = selectedSizes != null ? (await _context.Sizes.Where(s => selectedSizes.Contains(s.Id)).Select(s => s.SizeName).ToListAsync()) : new List<string>();
            return View(design);
        }

        var existing = await _context.Designs.AnyAsync(d => d.DesignNumber == design.DesignNumber && d.Id != design.Id && (d.CompanyId == design.CompanyId || d.CompanyId == null || design.CompanyId == null));
        if (existing)
        {
            ModelState.AddModelError("DesignNumber", "Design number already exists for this company scope.");
            ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
            ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
            ViewBag.Users = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ToListAsync();
            ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
            ViewBag.Processes = await _context.ProcessMasters.Where(p => p.IsActive).OrderBy(p => p.DisplayOrder).ToListAsync();
            ViewBag.Companies = await _context.Companies.Where(c => c.IsActive).OrderBy(c => c.CompanyName).ToListAsync();
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
            .Include(d => d.ColourImages)
            .Include(d => d.DiscontinuedVariants)
            .Include(d => d.ComponentAssignments)
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
        
        dbDesign.CompanyId = design.CompanyId > 0 ? design.CompanyId : null;
        if (dbDesign.CompanyId.HasValue)
        {
            var comp = await _context.Companies.FindAsync(dbDesign.CompanyId.Value);
            dbDesign.Company = comp?.CompanyName;
        }
        else
        {
            dbDesign.Company = null;
        }
        dbDesign.InternalNotes = design.InternalNotes;

        if (photoFile != null && photoFile.Length > 0)
        {
            dbDesign.PhotoPath = await SaveUploadedImageAsync(photoFile);
        }
        else if (Request.Form["removePhoto"] == "true")
        {
            dbDesign.PhotoPath = null;
        }
        else if (!string.IsNullOrEmpty(design.PhotoPath))
        {
            dbDesign.PhotoPath = design.PhotoPath;
        }

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

        if (!string.IsNullOrWhiteSpace(design.Components))
        {
            dbDesign.Components = string.Join(", ", design.GetComponentsList());
        }
        else
        {
            dbDesign.Components = "Chaniya, Choli, Dupatta";
        }

        // Update ComponentAssignments
        var existingAssignments = await _context.DesignComponentAssignments.Where(a => a.DesignId == dbDesign.Id).ToListAsync();
        _context.DesignComponentAssignments.RemoveRange(existingAssignments);

        if (ComponentAssignments != null && ComponentAssignments.Any())
        {
            foreach (var ca in ComponentAssignments.Where(a => a.VendorId.HasValue && a.VendorId.Value > 0 && !string.IsNullOrWhiteSpace(a.ComponentName) && !string.IsNullOrWhiteSpace(a.ProcessName)))
            {
                _context.DesignComponentAssignments.Add(new DesignComponentAssignment
                {
                    DesignId = dbDesign.Id,
                    ComponentName = ca.ComponentName.Trim(),
                    ProcessName = ca.ProcessName.Trim(),
                    VendorId = ca.VendorId,
                    EstimatedRate = ca.EstimatedRate,
                    Remarks = ca.Remarks
                });
            }
        }

        var firstHw = ComponentAssignments?.FirstOrDefault(a => a.ProcessName.Contains("Handwork", StringComparison.OrdinalIgnoreCase) && a.VendorId.HasValue && a.VendorId > 0);
        dbDesign.HandworkWorkerId = firstHw != null ? firstHw.VendorId : (design.HandworkWorkerId > 0 ? design.HandworkWorkerId : null);

        var firstSt = ComponentAssignments?.FirstOrDefault(a => a.ProcessName.Contains("Stitching", StringComparison.OrdinalIgnoreCase) && a.VendorId.HasValue && a.VendorId > 0);
        dbDesign.StitchingWorkerId = firstSt != null ? firstSt.VendorId : (design.StitchingWorkerId > 0 ? design.StitchingWorkerId : null);

        dbDesign.HandworkCholi = design.HandworkCholi;
        dbDesign.HandworkChaniya = design.HandworkChaniya;
        dbDesign.HandworkDupatta = design.HandworkDupatta;

        // Process colour-wise photos (supports multiple images per colour)
        var allColours = await _context.Colours.Where(c => c.IsActive).ToListAsync();
        var selectedColourNames = dbDesign.Colours?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).ToList() ?? new List<string>();

        // Handle removals
        var deletedIdSet = new HashSet<int>();
        if (deletedColourImageIds != null)
        {
            foreach (var did in deletedColourImageIds) deletedIdSet.Add(did);
        }
        foreach (var formVal in Request.Form["deletedColourImageIds"])
        {
            if (int.TryParse(formVal, out var parsedId)) deletedIdSet.Add(parsedId);
        }

        foreach (var existingImg in dbDesign.ColourImages.ToList())
        {
            if (deletedIdSet.Contains(existingImg.Id) ||
                Request.Form[$"removeColourPhotos_{existingImg.Colour}"] == "true" ||
                Request.Form[$"removeColourPhoto_{existingImg.Colour}"] == "true" ||
                !selectedColourNames.Contains(existingImg.Colour, StringComparer.OrdinalIgnoreCase))
            {
                _context.DesignColourImages.Remove(existingImg);
            }
        }

        // Handle new / additional uploads (supports multiple photos per colour)
        foreach (var file in Request.Form.Files)
        {
            if ((file.Name.StartsWith("colourPhotos_") || file.Name.StartsWith("colourPhoto_")) && file.Length > 0)
            {
                string prefix = file.Name.StartsWith("colourPhotos_") ? "colourPhotos_" : "colourPhoto_";
                string rawColourName = file.Name.Substring(prefix.Length).Trim();
                string cleanColourName = rawColourName.TrimEnd('[', ']').Trim();
                var colourInfo = allColours.FirstOrDefault(c => c.ColourName.Equals(cleanColourName, StringComparison.OrdinalIgnoreCase));
                if (colourInfo == null)
                {
                    var lastUnderscore = cleanColourName.LastIndexOf('_');
                    if (lastUnderscore > 0)
                    {
                        var possibleName = cleanColourName.Substring(0, lastUnderscore);
                        colourInfo = allColours.FirstOrDefault(c => c.ColourName.Equals(possibleName, StringComparison.OrdinalIgnoreCase));
                    }
                }

                var finalColourName = colourInfo?.ColourName ?? cleanColourName;
                if (!string.IsNullOrEmpty(finalColourName))
                {
                    var savedPath = await SaveUploadedImageAsync(file);
                    dbDesign.ColourImages.Add(new DesignColourImage
                    {
                        DesignId = dbDesign.Id,
                        Colour = finalColourName,
                        ColourCode = colourInfo?.ColourCode,
                        PhotoPath = savedPath,
                        CreatedDate = DateTime.Now
                    });

                    if (string.IsNullOrEmpty(dbDesign.PhotoPath))
                    {
                        dbDesign.PhotoPath = savedPath;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(dbDesign.PhotoPath) && dbDesign.ColourImages.Any())
        {
            dbDesign.PhotoPath = dbDesign.ColourImages.First().PhotoPath;
        }

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

        // Discontinued Variants update
        _context.DesignDiscontinuedVariants.RemoveRange(dbDesign.DiscontinuedVariants);
        if (DiscontinuedVariants?.Any() == true)
        {
            foreach (var v in DiscontinuedVariants.Where(v => !string.IsNullOrWhiteSpace(v.Colour) || !string.IsNullOrWhiteSpace(v.Size)))
            {
                _context.DesignDiscontinuedVariants.Add(new DesignDiscontinuedVariant
                {
                    DesignId = dbDesign.Id,
                    Colour = string.IsNullOrWhiteSpace(v.Colour) ? null : v.Colour.Trim(),
                    Size = string.IsNullOrWhiteSpace(v.Size) ? null : v.Size.Trim(),
                    DiscontinuedDate = v.DiscontinuedDate != default ? v.DiscontinuedDate : DateTime.Now,
                    Reason = v.Reason?.Trim()
                });
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Product '{dbDesign.DesignNumber}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDiscontinued(int id, string? returnUrl)
    {
        var design = await _context.Designs.FindAsync(id);
        if (design == null) return NotFound();
        design.Discontinued = !design.Discontinued;
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Design '{design.DesignNumber}' is now {(design.Discontinued ? "discontinued & moved to Archive" : "restored to active products")}.";
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
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
        // 1. Remove validation errors for optional child collections (attributes, pricelists, vendors, packagings, extra charges, discontinued variants)
        var childPrefixes = new[] { "AttributeLines", "Pricelists", "ProductVendors", "Packagings", "ExtraCharges", "DiscontinuedVariants" };
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
