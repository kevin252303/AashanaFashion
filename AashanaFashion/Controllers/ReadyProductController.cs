using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class ReadyProductController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ReadyProductController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET: /ReadyProduct
    public async Task<IActionResult> Index(
        string? search,
        int? designId,
        string? colour,
        string? size,
        ReadyProductStatus? status,
        string viewMode = "grid")
    {
        var query = _context.ReadyProducts
            .Include(r => r.Design)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(r =>
                r.DesignNumber.ToLower().Contains(s) ||
                r.Colour.ToLower().Contains(s) ||
                r.Size.ToLower().Contains(s) ||
                r.Sku.ToLower().Contains(s) ||
                (r.Barcode != null && r.Barcode.ToLower().Contains(s)) ||
                (r.WarehouseLocation != null && r.WarehouseLocation.ToLower().Contains(s)) ||
                (r.Remarks != null && r.Remarks.ToLower().Contains(s)) ||
                (r.Design != null && r.Design.CommonDNo != null && r.Design.CommonDNo.ToLower().Contains(s)));
        }

        if (designId.HasValue && designId.Value > 0)
        {
            query = query.Where(r => r.DesignId == designId.Value);
        }

        if (!string.IsNullOrWhiteSpace(colour))
        {
            query = query.Where(r => r.Colour == colour);
        }

        if (!string.IsNullOrWhiteSpace(size))
        {
            query = query.Where(r => r.Size == size);
        }

        var allItems = await query.OrderBy(r => r.DesignNumber).ThenBy(r => r.Colour).ThenBy(r => r.Size).ToListAsync();

        if (status.HasValue)
        {
            allItems = allItems.Where(r => r.CurrentStatus == status.Value).ToList();
        }

        // Summary Calculations
        var allProducts = await _context.ReadyProducts.ToListAsync();
        var vm = new ReadyProductIndexViewModel
        {
            ReadyProducts = allItems,
            TotalReadySets = allProducts.Sum(r => r.QuantityOnHand),
            TotalInventoryValue = allProducts.Sum(r => r.QuantityOnHand * r.UnitPrice),
            TotalCostValue = allProducts.Sum(r => r.QuantityOnHand * r.CostPrice),
            InStockCount = allProducts.Count(r => r.CurrentStatus == ReadyProductStatus.InStock),
            LowStockCount = allProducts.Count(r => r.CurrentStatus == ReadyProductStatus.LowStock),
            OutOfStockCount = allProducts.Count(r => r.CurrentStatus == ReadyProductStatus.OutOfStock),
            TotalDesignsCount = allProducts.Select(r => r.DesignId).Distinct().Count(),

            Search = search,
            DesignId = designId,
            Colour = colour,
            Size = size,
            Status = status,
            ViewMode = string.Equals(viewMode, "table", StringComparison.OrdinalIgnoreCase) ? "table" : "grid"
        };

        ViewBag.Designs = await _context.Designs.Where(d => d.IsActive).OrderBy(d => d.DesignNumber).ToListAsync();
        ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).Select(c => c.ColourName).ToListAsync();
        ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).Select(s => s.SizeName).ToListAsync();

        return View(vm);
    }

    // GET: /ReadyProduct/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var product = await _context.ReadyProducts
            .Include(r => r.Design)
                .ThenInclude(d => d!.ProductCategory)
            .Include(r => r.Transactions.OrderByDescending(t => t.CreatedDate))
            .FirstOrDefaultAsync(r => r.Id == id);

        if (product == null) return NotFound();

        return View(product);
    }

    // GET: /ReadyProduct/Create
    [Authorize(Roles = "Admin,SuperAdmin,System Admin,Manager")]
    public async Task<IActionResult> Create(int? designId, string? colour, string? size)
    {
        ViewBag.Designs = await _context.Designs.Where(d => d.IsActive).OrderBy(d => d.DesignNumber).ToListAsync();
        ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
        ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToListAsync();

        var model = new ReadyProductFormViewModel
        {
            MinimumStockAlert = 5,
            InitialQuantity = 1
        };

        if (designId.HasValue)
        {
            var design = await _context.Designs.FindAsync(designId.Value);
            if (design != null)
            {
                model.DesignId = design.Id;
                model.DesignNumber = design.DesignNumber;
                model.UnitPrice = design.SalesPrice > 0 ? design.SalesPrice : design.Price;
                model.CostPrice = design.TotalProductionCost;
                model.ExistingPhotoPath = design.PhotoPath;
                if (!string.IsNullOrWhiteSpace(colour)) model.Colour = colour;
                if (!string.IsNullOrWhiteSpace(size)) model.Size = size;
                model.Sku = $"RP-{design.DesignNumber}-{model.Colour}-{model.Size}".Replace(" ", "");
            }
        }

        return View(model);
    }

    // POST: /ReadyProduct/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin,System Admin,Manager")]
    public async Task<IActionResult> Create(ReadyProductFormViewModel model)
    {
        if (model.DesignId <= 0)
        {
            ModelState.AddModelError("DesignId", "Please select a valid design.");
        }

        var design = await _context.Designs.FindAsync(model.DesignId);
        if (design == null)
        {
            ModelState.AddModelError("DesignId", "Selected design was not found.");
        }
        else
        {
            model.DesignNumber = design.DesignNumber;
        }

        // Check if ready product for this (Design, Colour, Size) already exists
        if (await _context.ReadyProducts.AnyAsync(r => r.DesignId == model.DesignId && r.Colour == model.Colour && r.Size == model.Size))
        {
            ModelState.AddModelError("Colour", $"A ready product set for Design '{model.DesignNumber}', Colour '{model.Colour}', and Size '{model.Size}' already exists in inventory.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Designs = await _context.Designs.Where(d => d.IsActive).OrderBy(d => d.DesignNumber).ToListAsync();
            ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToListAsync();
            return View(model);
        }

        // Handle Photo Upload
        string? photoPath = model.ExistingPhotoPath;
        if (model.PhotoFile != null && model.PhotoFile.Length > 0)
        {
            photoPath = await SaveUploadedImageAsync(model.PhotoFile);
        }
        else if (string.IsNullOrEmpty(photoPath) && design != null && !string.IsNullOrEmpty(design.PhotoPath))
        {
            photoPath = design.PhotoPath;
        }

        var sku = !string.IsNullOrWhiteSpace(model.Sku)
            ? model.Sku.Trim()
            : $"RP-{model.DesignNumber}-{model.Colour}-{model.Size}".Replace(" ", "");

        var readyProduct = new ReadyProduct
        {
            DesignId = model.DesignId,
            DesignNumber = model.DesignNumber,
            Colour = model.Colour.Trim(),
            Size = model.Size.Trim(),
            Sku = sku,
            Barcode = sku,
            ChaniyaQuantityPerSet = 1,
            CholiQuantityPerSet = 1,
            DuppataQuantityPerSet = 1,
            QuantityOnHand = model.InitialQuantity,
            AllocatedQuantity = 0,
            MinimumStockAlert = model.MinimumStockAlert,
            UnitPrice = model.UnitPrice > 0 ? model.UnitPrice : (design?.SalesPrice ?? 0),
            CostPrice = model.CostPrice,
            PhotoPath = photoPath,
            WarehouseLocation = model.WarehouseLocation?.Trim(),
            Remarks = model.Remarks?.Trim(),
            IsActive = model.IsActive,
            CreatedDate = DateTime.Now
        };

        _context.ReadyProducts.Add(readyProduct);
        await _context.SaveChangesAsync();

        // If initial quantity > 0, log an initial inward transaction
        if (model.InitialQuantity > 0)
        {
            var tx = new ReadyProductTransaction
            {
                ReadyProductId = readyProduct.Id,
                TransactionType = ReadyProductTransactionType.ManualInward,
                Quantity = model.InitialQuantity,
                BalanceAfter = model.InitialQuantity,
                ReferenceType = "Initial Stock",
                ReferenceNumber = "OPENING",
                Notes = $"Opening ready product stock for 3-piece matching set (1 Chaniya + 1 Choli + 1 Duppata).",
                CreatedBy = User.Identity?.Name ?? "Admin",
                CreatedDate = DateTime.Now
            };
            _context.ReadyProductTransactions.Add(tx);
            await _context.SaveChangesAsync();
        }

        TempData["Success"] = $"Ready Product 3-piece set for '{model.DesignNumber}' ({model.Colour}, Size {model.Size}) added successfully with {model.InitialQuantity} set(s) in stock.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /ReadyProduct/Edit/5
    [Authorize(Roles = "Admin,SuperAdmin,System Admin,Manager")]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.ReadyProducts.Include(r => r.Design).FirstOrDefaultAsync(r => r.Id == id);
        if (product == null) return NotFound();

        var model = new ReadyProductFormViewModel
        {
            Id = product.Id,
            DesignId = product.DesignId,
            DesignNumber = product.DesignNumber,
            Colour = product.Colour,
            Size = product.Size,
            Sku = product.Sku,
            InitialQuantity = product.QuantityOnHand,
            MinimumStockAlert = product.MinimumStockAlert,
            UnitPrice = product.UnitPrice,
            CostPrice = product.CostPrice,
            WarehouseLocation = product.WarehouseLocation,
            ExistingPhotoPath = product.PhotoPath ?? product.Design?.PhotoPath,
            Remarks = product.Remarks,
            IsActive = product.IsActive
        };

        ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
        ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToListAsync();

        return View(model);
    }

    // POST: /ReadyProduct/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin,System Admin,Manager")]
    public async Task<IActionResult> Edit(int id, ReadyProductFormViewModel model)
    {
        if (id != model.Id) return NotFound();

        var product = await _context.ReadyProducts.FindAsync(id);
        if (product == null) return NotFound();

        if (model.UnitPrice < 0) ModelState.AddModelError("UnitPrice", "Unit price cannot be negative.");
        if (model.CostPrice < 0) ModelState.AddModelError("CostPrice", "Cost price cannot be negative.");

        if (!ModelState.IsValid)
        {
            ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToListAsync();
            return View(model);
        }

        // Handle Photo Upload
        if (model.PhotoFile != null && model.PhotoFile.Length > 0)
        {
            product.PhotoPath = await SaveUploadedImageAsync(model.PhotoFile);
        }

        product.UnitPrice = model.UnitPrice;
        product.CostPrice = model.CostPrice;
        product.MinimumStockAlert = model.MinimumStockAlert;
        product.WarehouseLocation = model.WarehouseLocation?.Trim();
        product.Remarks = model.Remarks?.Trim();
        product.IsActive = model.IsActive;
        if (!string.IsNullOrWhiteSpace(model.Sku)) product.Sku = model.Sku.Trim();
        product.UpdatedDate = DateTime.Now;

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Ready Product '{product.DesignNumber}' ({product.Colour}, {product.Size}) updated successfully.";
        return RedirectToAction(nameof(Details), new { id = product.Id });
    }

    // GET: /ReadyProduct/AdjustStock/5
    [Authorize(Roles = "Admin,SuperAdmin,System Admin,Manager")]
    public async Task<IActionResult> AdjustStock(int id)
    {
        var product = await _context.ReadyProducts.Include(r => r.Design).FirstOrDefaultAsync(r => r.Id == id);
        if (product == null) return NotFound();

        var model = new ReadyProductStockAdjustViewModel
        {
            ReadyProductId = product.Id,
            DesignNumber = product.DesignNumber,
            Colour = product.Colour,
            Size = product.Size,
            PhotoPath = product.EffectivePhotoPath,
            CurrentStock = product.QuantityOnHand,
            AdjustmentType = "Add",
            Quantity = 1
        };

        return View(model);
    }

    // POST: /ReadyProduct/AdjustStock/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin,System Admin,Manager")]
    public async Task<IActionResult> AdjustStock(ReadyProductStockAdjustViewModel model)
    {
        var product = await _context.ReadyProducts.FindAsync(model.ReadyProductId);
        if (product == null) return NotFound();

        if (model.Quantity <= 0 && model.AdjustmentType != "SetExact")
        {
            ModelState.AddModelError("Quantity", "Quantity must be greater than zero.");
        }

        int delta = 0;
        int newBalance = product.QuantityOnHand;

        if (model.AdjustmentType == "Add")
        {
            delta = model.Quantity;
            newBalance += delta;
        }
        else if (model.AdjustmentType == "Deduct")
        {
            if (model.Quantity > product.QuantityOnHand)
            {
                ModelState.AddModelError("Quantity", $"Cannot deduct {model.Quantity} sets. Only {product.QuantityOnHand} currently available in stock.");
            }
            delta = -model.Quantity;
            newBalance += delta;
        }
        else if (model.AdjustmentType == "SetExact")
        {
            if (model.Quantity < 0)
            {
                ModelState.AddModelError("Quantity", "Stock cannot be negative.");
            }
            delta = model.Quantity - product.QuantityOnHand;
            newBalance = model.Quantity;
        }

        if (!ModelState.IsValid)
        {
            model.CurrentStock = product.QuantityOnHand;
            model.PhotoPath = product.EffectivePhotoPath;
            return View(model);
        }

        product.QuantityOnHand = newBalance;
        product.UpdatedDate = DateTime.Now;

        var tx = new ReadyProductTransaction
        {
            ReadyProductId = product.Id,
            TransactionType = delta >= 0 ? ReadyProductTransactionType.StockAdjustment : ReadyProductTransactionType.DamagedScrapped,
            Quantity = delta,
            BalanceAfter = newBalance,
            ReferenceType = "Stock Adjustment",
            ReferenceNumber = model.ReferenceNumber?.Trim(),
            Notes = $"{model.AdjustmentType} adjustment: {model.Reason}",
            CreatedBy = User.Identity?.Name ?? "Admin",
            CreatedDate = DateTime.Now
        };

        _context.ReadyProductTransactions.Add(tx);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Stock for '{product.DesignNumber}' ({product.Colour}, {product.Size}) adjusted. New stock balance: {newBalance} set(s).";
        return RedirectToAction(nameof(Details), new { id = product.Id });
    }

    // GET: /ReadyProduct/Assemble
    // Displays piece counts for matching 1 Chaniya + 1 Choli + 1 Duppata
    public async Task<IActionResult> Assemble()
    {
        // Find entities in PMS
        var entities = await _context.ProductionEntities
            .Include(e => e.ProductionOrder)
                .ThenInclude(p => p!.Design)
            .ToListAsync();

        var existingReadyProducts = await _context.ReadyProducts.ToListAsync();

        // Group by DesignId, Colour, Size
        var groups = entities
            .Where(e => e.ProductionOrder?.DesignId != null && !string.IsNullOrWhiteSpace(e.Colour) && !string.IsNullOrWhiteSpace(e.Size))
            .GroupBy(e => new
            {
                DesignId = e.ProductionOrder!.DesignId,
                DesignNumber = e.ProductionOrder.Design?.DesignNumber ?? "",
                PhotoPath = e.ProductionOrder.Design?.PhotoPath,
                Colour = e.Colour.Trim(),
                Size = e.Size.Trim()
            })
            .Select(g =>
            {
                int chaniya = g.Count(x => string.Equals(x.EntityType, "Chaniya", StringComparison.OrdinalIgnoreCase));
                int choli = g.Count(x => string.Equals(x.EntityType, "Choli", StringComparison.OrdinalIgnoreCase) || string.Equals(x.EntityType, "Blouse", StringComparison.OrdinalIgnoreCase));
                int duppata = g.Count(x => string.Equals(x.EntityType, "Duppata", StringComparison.OrdinalIgnoreCase) || string.Equals(x.EntityType, "Dupatta", StringComparison.OrdinalIgnoreCase));

                var existing = existingReadyProducts.FirstOrDefault(rp => rp.DesignId == g.Key.DesignId && rp.Colour == g.Key.Colour && rp.Size == g.Key.Size);

                return new ReadyProductAssemblyCandidate
                {
                    DesignId = g.Key.DesignId,
                    DesignNumber = g.Key.DesignNumber,
                    DesignPhotoPath = g.Key.PhotoPath,
                    Colour = g.Key.Colour,
                    Size = g.Key.Size,
                    ChaniyaCount = chaniya,
                    CholiCount = choli,
                    DuppataCount = duppata,
                    ExistingReadyStock = existing?.QuantityOnHand ?? 0,
                    ExistingReadyProductId = existing?.Id
                };
            })
            .OrderByDescending(c => c.AssembledSetsAvailable)
            .ThenBy(c => c.DesignNumber)
            .ToList();

        var vm = new ReadyProductAssemblyViewModel
        {
            Candidates = groups
        };

        return View(vm);
    }

    // POST: /ReadyProduct/AssembleSets
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin,System Admin,Manager")]
    public async Task<IActionResult> AssembleSets(int designId, string colour, string size, int setsToAssemble, string? notes)
    {
        if (setsToAssemble <= 0)
        {
            TempData["Error"] = "Quantity to assemble must be at least 1 set.";
            return RedirectToAction(nameof(Assemble));
        }

        var design = await _context.Designs.FindAsync(designId);
        if (design == null)
        {
            TempData["Error"] = "Design not found.";
            return RedirectToAction(nameof(Assemble));
        }

        // Find existing ready product or create new
        var readyProduct = await _context.ReadyProducts
            .FirstOrDefaultAsync(r => r.DesignId == designId && r.Colour == colour && r.Size == size);

        if (readyProduct == null)
        {
            var sku = $"RP-{design.DesignNumber}-{colour}-{size}".Replace(" ", "");
            readyProduct = new ReadyProduct
            {
                DesignId = designId,
                DesignNumber = design.DesignNumber,
                Colour = colour,
                Size = size,
                Sku = sku,
                Barcode = sku,
                ChaniyaQuantityPerSet = 1,
                CholiQuantityPerSet = 1,
                DuppataQuantityPerSet = 1,
                QuantityOnHand = setsToAssemble,
                AllocatedQuantity = 0,
                MinimumStockAlert = 5,
                UnitPrice = design.SalesPrice > 0 ? design.SalesPrice : design.Price,
                CostPrice = design.TotalProductionCost,
                PhotoPath = design.PhotoPath,
                Remarks = notes,
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            _context.ReadyProducts.Add(readyProduct);
            await _context.SaveChangesAsync();
        }
        else
        {
            readyProduct.QuantityOnHand += setsToAssemble;
            readyProduct.UpdatedDate = DateTime.Now;
        }

        var tx = new ReadyProductTransaction
        {
            ReadyProductId = readyProduct.Id,
            TransactionType = ReadyProductTransactionType.AssemblyFromProduction,
            Quantity = setsToAssemble,
            BalanceAfter = readyProduct.QuantityOnHand,
            ReferenceType = "Production 3-Piece Assembly",
            ReferenceNumber = $"ASSM-{DateTime.Now:yyyyMMddHHmm}",
            Notes = $"Assembled {setsToAssemble} complete 3-piece sets (1 Chaniya + 1 Choli + 1 Duppata). {notes}".Trim(),
            CreatedBy = User.Identity?.Name ?? "Admin",
            CreatedDate = DateTime.Now
        };
        _context.ReadyProductTransactions.Add(tx);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Successfully assembled {setsToAssemble} ready product set(s) for Design '{design.DesignNumber}' ({colour}, Size {size}). New Stock: {readyProduct.QuantityOnHand} sets.";
        return RedirectToAction(nameof(Details), new { id = readyProduct.Id });
    }

    // GET: /ReadyProduct/Ledger
    public async Task<IActionResult> Ledger(
        int? id,
        string? search,
        ReadyProductTransactionType? type,
        DateTime? startDate,
        DateTime? endDate)
    {
        var query = _context.ReadyProductTransactions
            .Include(t => t.ReadyProduct)
                .ThenInclude(r => r!.Design)
            .AsQueryable();

        if (id.HasValue && id.Value > 0)
        {
            query = query.Where(t => t.ReadyProductId == id.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(t => t.TransactionType == type.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(t => t.CreatedDate >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.CreatedDate < endDate.Value.Date.AddDays(1));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(t =>
                (t.ReferenceNumber != null && t.ReferenceNumber.ToLower().Contains(s)) ||
                (t.ReferenceType != null && t.ReferenceType.ToLower().Contains(s)) ||
                (t.Notes != null && t.Notes.ToLower().Contains(s)) ||
                (t.ReadyProduct != null && t.ReadyProduct.DesignNumber.ToLower().Contains(s)) ||
                (t.ReadyProduct != null && t.ReadyProduct.Colour.ToLower().Contains(s)) ||
                (t.ReadyProduct != null && t.ReadyProduct.Size.ToLower().Contains(s)) ||
                (t.ReadyProduct != null && t.ReadyProduct.Sku.ToLower().Contains(s)));
        }

        var transactions = await query.OrderByDescending(t => t.CreatedDate).ToListAsync();

        ViewBag.SelectedProductId = id;
        ViewBag.Search = search;
        ViewBag.Type = type;
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
        ViewBag.ReadyProducts = await _context.ReadyProducts.OrderBy(r => r.DesignNumber).ToListAsync();

        return View(transactions);
    }

    // GET: /ReadyProduct/PrintTag/5
    public async Task<IActionResult> PrintTag(int id)
    {
        var product = await _context.ReadyProducts
            .Include(r => r.Design)
                .ThenInclude(d => d!.ProductCategory)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (product == null) return NotFound();

        return View(product);
    }

    // Helper: Save uploaded file to wwwroot/uploads/readyproducts/
    private async Task<string> SaveUploadedImageAsync(IFormFile file)
    {
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "readyproducts");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        if (!allowed.Contains(ext)) ext = ".jpg";

        var uniqueFileName = $"rp_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/readyproducts/{uniqueFileName}";
    }

    // GET: /ReadyProduct/Outward
    [Authorize(Roles = "Admin,SuperAdmin,System Admin,Manager")]
    public async Task<IActionResult> Outward(int? id)
    {
        var products = await _context.ReadyProducts
            .Include(r => r.Design)
            .Where(r => r.IsActive)
            .OrderBy(r => r.DesignNumber)
            .ThenBy(r => r.Colour)
            .ThenBy(r => r.Size)
            .ToListAsync();

        var model = new ReadyProductOutwardViewModel
        {
            AvailableProducts = products,
            OutwardType = ReadyProductTransactionType.OutwardSales,
            Quantity = 1
        };

        if (id.HasValue && id.Value > 0)
        {
            var product = products.FirstOrDefault(p => p.Id == id.Value);
            if (product != null)
            {
                model.ReadyProductId = product.Id;
                model.DesignNumber = product.DesignNumber;
                model.Colour = product.Colour;
                model.Size = product.Size;
                model.PhotoPath = product.EffectivePhotoPath;
                model.CurrentStock = product.QuantityOnHand;
            }
        }

        return View(model);
    }

    // POST: /ReadyProduct/Outward
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,SuperAdmin,System Admin,Manager")]
    public async Task<IActionResult> Outward(ReadyProductOutwardViewModel model)
    {
        var product = await _context.ReadyProducts
            .Include(r => r.Design)
            .FirstOrDefaultAsync(r => r.Id == model.ReadyProductId);

        if (product == null)
        {
            ModelState.AddModelError("ReadyProductId", "Please select a valid Ready Product.");
        }
        else
        {
            model.DesignNumber = product.DesignNumber;
            model.Colour = product.Colour;
            model.Size = product.Size;
            model.PhotoPath = product.EffectivePhotoPath;
            model.CurrentStock = product.QuantityOnHand;

            if (model.Quantity <= 0)
            {
                ModelState.AddModelError("Quantity", "Quantity must be at least 1 set.");
            }
            else if (model.Quantity > product.QuantityOnHand)
            {
                ModelState.AddModelError("Quantity", $"Cannot outward {model.Quantity} set(s). Current stock on hand is only {product.QuantityOnHand} set(s).");
            }
        }

        if (!ModelState.IsValid)
        {
            model.AvailableProducts = await _context.ReadyProducts
                .Include(r => r.Design)
                .Where(r => r.IsActive)
                .OrderBy(r => r.DesignNumber)
                .ThenBy(r => r.Colour)
                .ToListAsync();
            return View(model);
        }

        // Deduct outward stock
        product!.QuantityOnHand -= model.Quantity;
        product.UpdatedDate = DateTime.Now;

        var refType = model.OutwardType switch
        {
            ReadyProductTransactionType.OutwardSales => "Outward Sales Dispatch",
            ReadyProductTransactionType.SampleIssue => "Sample / Showroom Issue",
            ReadyProductTransactionType.DamagedScrapped => "Damaged / Defect Write-off",
            _ => "Stock Outward"
        };

        var notes = string.IsNullOrWhiteSpace(model.Recipient)
            ? (model.Remarks ?? "Outward dispatch recorded.")
            : $"To: {model.Recipient}. {model.Remarks}".Trim();

        var tx = new ReadyProductTransaction
        {
            ReadyProductId = product.Id,
            TransactionType = model.OutwardType,
            Quantity = -model.Quantity,
            BalanceAfter = product.QuantityOnHand,
            ReferenceType = refType,
            ReferenceNumber = model.ReferenceNumber?.Trim(),
            Notes = notes,
            CreatedBy = User.Identity?.Name ?? "Staff",
            CreatedDate = DateTime.Now
        };

        _context.ReadyProductTransactions.Add(tx);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Successfully dispatched/outwarded {model.Quantity} set(s) for '{product.DesignNumber}' ({product.Colour}, {product.Size}). Remaining stock: {product.QuantityOnHand} sets.";
        return RedirectToAction(nameof(Details), new { id = product.Id });
    }

    // AJAX Endpoint: Get product stock info for dynamic outward form updates
    [HttpGet]
    public async Task<IActionResult> GetProductStockInfo(int id)
    {
        var product = await _context.ReadyProducts.Include(r => r.Design).FirstOrDefaultAsync(r => r.Id == id);
        if (product == null) return NotFound();

        return Json(new
        {
            id = product.Id,
            designNumber = product.DesignNumber,
            colour = product.Colour,
            size = product.Size,
            stock = product.QuantityOnHand,
            unitPrice = product.UnitPrice,
            photoPath = product.EffectivePhotoPath,
            sku = product.Sku
        });
    }

    // AJAX Endpoint: Get Design info for auto-filling Create form
    [HttpGet]
    public async Task<IActionResult> GetDesignDetails(int designId)
    {
        var design = await _context.Designs.FindAsync(designId);
        if (design == null) return NotFound();

        var colours = design.Colours?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).ToList() ?? new List<string>();
        var sizes = design.Sizes?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList() ?? new List<string>();

        return Json(new
        {
            designId = design.Id,
            designNumber = design.DesignNumber,
            salesPrice = design.SalesPrice > 0 ? design.SalesPrice : design.Price,
            costPrice = design.TotalProductionCost,
            photoPath = design.PhotoPath,
            colours,
            sizes
        });
    }
}

