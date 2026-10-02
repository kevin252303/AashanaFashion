using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class BarcodeController : Controller
{
    private readonly AppDbContext _context;

    public BarcodeController(AppDbContext context) => _context = context;

    public async Task<IActionResult> PrintTags(int orderId, string? entityType, string? format)
    {
        var order = await _context.ProductionOrders
            .Include(p => p.Design)
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.Id == orderId);

        if (order == null) return NotFound();

        var query = _context.ProductionEntities
            .Where(e => e.ProductionOrderId == orderId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(entityType) && entityType != "All")
        {
            query = query.Where(e => e.EntityType == entityType);
        }

        var entities = await query.OrderBy(e => e.SlNo).ThenBy(e => e.EntityType).ToListAsync();

        // Ensure all entities have barcodes (backfill if generated before Phase 5)
        bool hasMissingBarcodes = false;
        foreach (var entity in entities)
        {
            if (string.IsNullOrEmpty(entity.Barcode))
            {
                entity.Barcode = BarcodeService.FormatEntityBarcode(order.Id, entity.SlNo, entity.EntityType);
                hasMissingBarcodes = true;
            }
        }
        if (hasMissingBarcodes)
        {
            await _context.SaveChangesAsync();
        }

        var config = await _context.BarcodeTagConfigs.FirstOrDefaultAsync(c => c.IsDefault);
        if (config == null)
        {
            config = new BarcodeTagConfig { ConfigName = "Default", IsDefault = true };
            _context.BarcodeTagConfigs.Add(config);
            await _context.SaveChangesAsync();
        }

        var tags = entities.Select(e => new EntityBarcodeTagItem
        {
            EntityId = e.Id,
            Barcode = e.Barcode ?? BarcodeService.FormatEntityBarcode(order.Id, e.SlNo, e.EntityType),
            DesignNumber = order.Design?.DesignNumber ?? "AF-DESIGN",
            LotNo = order.LotNo,
            EntityType = e.EntityType,
            Colour = e.Colour,
            Size = e.Size,
            SlNo = e.SlNo,
            Status = e.Status,
            Price = order.Design?.SalesPrice,
            Category = order.Design?.Category,
            HsnCode = order.Design?.HsnSacCode,
            OrderDate = order.CreatedDate,
            BarcodeSvg = BarcodeService.GenerateCode128Svg(e.Barcode ?? $"{e.Id}", barHeight: config.BarcodeHeight, moduleWidth: 2, showText: config.ShowBarcodeText),
            QrCodeSvg = BarcodeService.GenerateQrCodeSvg(e.Barcode ?? $"{e.Id}", size: 68)
        }).ToList();

        var vm = new PrintTagsViewModel
        {
            Order = order,
            Tags = tags,
            Format = string.IsNullOrEmpty(format) ? "ThermalRoll" : format,
            FilterEntityType = entityType,
            Config = config
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ConfigureTag()
    {
        var config = await _context.BarcodeTagConfigs.FirstOrDefaultAsync(c => c.IsDefault);
        if (config == null)
        {
            config = new BarcodeTagConfig { ConfigName = "Default", IsDefault = true };
            _context.BarcodeTagConfigs.Add(config);
            await _context.SaveChangesAsync();
        }

        ViewBag.SampleBarcodeSvg = BarcodeService.GenerateCode128Svg("AF-2026-101", barHeight: config.BarcodeHeight, moduleWidth: 2, showText: config.ShowBarcodeText);
        ViewBag.SampleQrCodeSvg = BarcodeService.GenerateQrCodeSvg("AF-2026-101", size: 68);

        return View(config);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfigureTag(BarcodeTagConfig model, string? dynamicFieldsJson)
    {
        var existing = await _context.BarcodeTagConfigs.FirstOrDefaultAsync(c => c.Id == model.Id)
                       ?? await _context.BarcodeTagConfigs.FirstOrDefaultAsync(c => c.IsDefault);

        if (existing == null)
        {
            existing = new BarcodeTagConfig { IsDefault = true };
            _context.BarcodeTagConfigs.Add(existing);
        }

        existing.TagWidthMm = model.TagWidthMm > 0 ? model.TagWidthMm : 50;
        existing.TagHeightMm = model.TagHeightMm > 0 ? model.TagHeightMm : 30;
        existing.FontSize = model.FontSize ?? "normal";
        existing.ShowBrandName = model.ShowBrandName;
        existing.BrandName = model.BrandName ?? "Aashana Fashion";
        existing.ShowSubtitle = model.ShowSubtitle;
        existing.Subtitle = model.Subtitle;

        existing.ShowDesignNumber = model.ShowDesignNumber;
        existing.DesignNumberLabel = model.DesignNumberLabel ?? "Design";
        existing.ShowLotNo = model.ShowLotNo;
        existing.LotNoLabel = model.LotNoLabel ?? "Lot";
        existing.ShowSerialNumber = model.ShowSerialNumber;
        existing.SerialNumberLabel = model.SerialNumberLabel ?? "#";
        existing.ShowComponent = model.ShowComponent;
        existing.ComponentLabel = model.ComponentLabel ?? "Part";
        existing.ShowColour = model.ShowColour;
        existing.ColourLabel = model.ColourLabel ?? "Colour";
        existing.ShowSize = model.ShowSize;
        existing.SizeLabel = model.SizeLabel ?? "Size";

        existing.ShowBarcode = model.ShowBarcode;
        existing.ShowBarcodeText = model.ShowBarcodeText;
        existing.ShowQrCode = model.ShowQrCode;
        existing.BarcodeHeight = model.BarcodeHeight > 0 ? model.BarcodeHeight : 34;

        existing.ShowPrice = model.ShowPrice;
        existing.PriceLabel = model.PriceLabel ?? "M.R.P. ₹";
        existing.ShowCategory = model.ShowCategory;
        existing.CategoryLabel = model.CategoryLabel ?? "Category";
        existing.ShowHsnCode = model.ShowHsnCode;
        existing.HsnLabel = model.HsnLabel ?? "HSN";
        existing.ShowMfgDate = model.ShowMfgDate;
        existing.MfgDateLabel = model.MfgDateLabel ?? "Mfg Date";

        existing.ShowCustomField1 = model.ShowCustomField1;
        existing.CustomField1Label = model.CustomField1Label;
        existing.CustomField1Value = model.CustomField1Value;

        existing.ShowCustomField2 = model.ShowCustomField2;
        existing.CustomField2Label = model.CustomField2Label;
        existing.CustomField2Value = model.CustomField2Value;

        existing.ShowCustomField3 = model.ShowCustomField3;
        existing.CustomField3Label = model.CustomField3Label;
        existing.CustomField3Value = model.CustomField3Value;

        existing.ShowFooterText = model.ShowFooterText;
        existing.FooterText = model.FooterText;

        if (!string.IsNullOrEmpty(dynamicFieldsJson))
        {
            existing.DynamicCustomFieldsJson = dynamicFieldsJson;
        }
        else if (!string.IsNullOrEmpty(model.DynamicCustomFieldsJson))
        {
            existing.DynamicCustomFieldsJson = model.DynamicCustomFieldsJson;
        }

        existing.UpdatedDate = DateTime.Now;
        existing.UpdatedBy = User.Identity?.Name ?? "User";

        await _context.SaveChangesAsync();
        TempData["Success"] = "Barcode Tag configuration saved successfully.";
        return RedirectToAction(nameof(ConfigureTag));
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveConfigAjax([FromBody] BarcodeTagConfig config)
    {
        if (config == null) return BadRequest("Invalid configuration payload.");

        var existing = await _context.BarcodeTagConfigs.FirstOrDefaultAsync(c => c.IsDefault);
        if (existing == null)
        {
            config.IsDefault = true;
            config.ConfigName = "Default";
            config.UpdatedDate = DateTime.Now;
            config.UpdatedBy = User.Identity?.Name ?? "User";
            _context.BarcodeTagConfigs.Add(config);
        }
        else
        {
            existing.TagWidthMm = config.TagWidthMm > 0 ? config.TagWidthMm : 50;
            existing.TagHeightMm = config.TagHeightMm > 0 ? config.TagHeightMm : 30;
            existing.FontSize = config.FontSize ?? "normal";
            existing.ShowBrandName = config.ShowBrandName;
            existing.BrandName = config.BrandName ?? "Aashana Fashion";
            existing.ShowSubtitle = config.ShowSubtitle;
            existing.Subtitle = config.Subtitle;

            existing.ShowDesignNumber = config.ShowDesignNumber;
            existing.DesignNumberLabel = config.DesignNumberLabel ?? "Design";
            existing.ShowLotNo = config.ShowLotNo;
            existing.LotNoLabel = config.LotNoLabel ?? "Lot";
            existing.ShowSerialNumber = config.ShowSerialNumber;
            existing.SerialNumberLabel = config.SerialNumberLabel ?? "#";
            existing.ShowComponent = config.ShowComponent;
            existing.ComponentLabel = config.ComponentLabel ?? "Part";
            existing.ShowColour = config.ShowColour;
            existing.ColourLabel = config.ColourLabel ?? "Colour";
            existing.ShowSize = config.ShowSize;
            existing.SizeLabel = config.SizeLabel ?? "Size";

            existing.ShowBarcode = config.ShowBarcode;
            existing.ShowBarcodeText = config.ShowBarcodeText;
            existing.ShowQrCode = config.ShowQrCode;
            existing.BarcodeHeight = config.BarcodeHeight > 0 ? config.BarcodeHeight : 34;

            existing.ShowPrice = config.ShowPrice;
            existing.PriceLabel = config.PriceLabel ?? "M.R.P. ₹";
            existing.ShowCategory = config.ShowCategory;
            existing.CategoryLabel = config.CategoryLabel ?? "Category";
            existing.ShowHsnCode = config.ShowHsnCode;
            existing.HsnLabel = config.HsnLabel ?? "HSN";
            existing.ShowMfgDate = config.ShowMfgDate;
            existing.MfgDateLabel = config.MfgDateLabel ?? "Mfg Date";

            existing.ShowCustomField1 = config.ShowCustomField1;
            existing.CustomField1Label = config.CustomField1Label;
            existing.CustomField1Value = config.CustomField1Value;

            existing.ShowCustomField2 = config.ShowCustomField2;
            existing.CustomField2Label = config.CustomField2Label;
            existing.CustomField2Value = config.CustomField2Value;

            existing.ShowCustomField3 = config.ShowCustomField3;
            existing.CustomField3Label = config.CustomField3Label;
            existing.CustomField3Value = config.CustomField3Value;

            existing.ShowFooterText = config.ShowFooterText;
            existing.FooterText = config.FooterText;
            existing.DynamicCustomFieldsJson = config.DynamicCustomFieldsJson;

            existing.UpdatedDate = DateTime.Now;
            existing.UpdatedBy = User.Identity?.Name ?? "User";
        }

        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Barcode Tag configuration updated successfully." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetConfig()
    {
        var existing = await _context.BarcodeTagConfigs.FirstOrDefaultAsync(c => c.IsDefault);
        if (existing != null)
        {
            _context.BarcodeTagConfigs.Remove(existing);
        }

        var defaultConfig = new BarcodeTagConfig { ConfigName = "Default", IsDefault = true, UpdatedDate = DateTime.Now };
        _context.BarcodeTagConfigs.Add(defaultConfig);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Barcode Tag configuration has been reset to system defaults.";
        return RedirectToAction(nameof(ConfigureTag));
    }

    public async Task<IActionResult> PrintLotTag(int orderId)
    {
        var order = await _context.ProductionOrders
            .Include(p => p.Design)
                .ThenInclude(d => d!.HandworkWorker)
            .Include(p => p.Design)
                .ThenInclude(d => d!.StitchingWorker)
            .Include(p => p.Details)
            .Include(p => p.HandworkWorker)
            .Include(p => p.StitchingWorker)
            .FirstOrDefaultAsync(p => p.Id == orderId);

        if (order == null) return NotFound();

        string lotBarcode = $"LOT-{order.LotNo}";
        ViewBag.BarcodeSvg = BarcodeService.GenerateCode128Svg(lotBarcode, barHeight: 55, moduleWidth: 2, showText: true);
        ViewBag.QrCodeSvg = BarcodeService.GenerateQrCodeSvg(lotBarcode, size: 110);

        return View(order);
    }

    public async Task<IActionResult> Scanner()
    {
        ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
        ViewBag.Processes = new[] { "Dying", "Roll", "Handwork", "Stitching" };
        return View();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Lookup([FromBody] BarcodeLookupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Code))
        {
            return Json(new { success = false, message = "No barcode provided." });
        }

        var code = request.Code.Trim();

        // 1. Try finding by Barcode or ID
        var entity = await _context.ProductionEntities
            .Include(e => e.ProductionOrder)
            .ThenInclude(p => p!.Design)
            .Include(e => e.ProcessTrackings)
            .ThenInclude(t => t.Vendor)
            .FirstOrDefaultAsync(e => e.Barcode == code || e.Id.ToString() == code);

        if (entity != null)
        {
            var activeTracking = entity.ProcessTrackings.FirstOrDefault(t => !t.ActualReturnDate.HasValue);
            var history = entity.ProcessTrackings
                .OrderByDescending(t => t.GivenDate)
                .Select(t => new ScanProcessHistoryItem
                {
                    ProcessName = t.ProcessName,
                    VendorName = t.Vendor?.VendorName,
                    GivenDate = t.GivenDate?.ToString("dd/MM/yyyy"),
                    ActualReturnDate = t.ActualReturnDate?.ToString("dd/MM/yyyy"),
                    IsComplete = t.IsComplete
                }).ToList();

            return Json(new ScanLookupResultViewModel
            {
                Success = true,
                EntityId = entity.Id,
                Barcode = entity.Barcode ?? $"{entity.Id}",
                OrderId = entity.ProductionOrderId,
                LotNo = entity.ProductionOrder?.LotNo ?? "—",
                DesignNumber = entity.ProductionOrder?.Design?.DesignNumber ?? "—",
                EntityType = entity.EntityType,
                Colour = entity.Colour,
                Size = entity.Size,
                SlNo = entity.SlNo,
                Status = entity.Status,
                CurrentProcess = activeTracking?.ProcessName,
                CurrentVendor = activeTracking?.Vendor?.VendorName,
                CurrentTrackingId = activeTracking?.Id,
                ExpectedReturn = activeTracking?.ExpectedReturnDate,
                History = history
            });
        }

        // 2. Check if scanned code is a Lot barcode (e.g. LOT-xxxx or xxxx)
        var lotNo = code.StartsWith("LOT-", StringComparison.OrdinalIgnoreCase) ? code.Substring(4) : code;
        var order = await _context.ProductionOrders
            .Include(p => p.Design)
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.LotNo.ToLower() == lotNo.ToLower());

        if (order != null)
        {
            return Json(new
            {
                success = true,
                isLot = true,
                orderId = order.Id,
                lotNo = order.LotNo,
                designNumber = order.Design?.DesignNumber ?? "—",
                totalQuantity = order.TotalQuantity,
                status = order.Status.ToString()
            });
        }

        return Json(new { success = false, message = $"Barcode '{code}' not found in system." });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ExecuteScanAction([FromBody] ScanActionRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Barcode))
        {
            return Json(new { success = false, message = "Invalid request." });
        }

        var entity = await _context.ProductionEntities
            .Include(e => e.ProcessTrackings)
            .FirstOrDefaultAsync(e => e.Barcode == req.Barcode || e.Id.ToString() == req.Barcode);

        if (entity == null)
        {
            return Json(new { success = false, message = "Garment entity not found." });
        }

        if (req.ActionType == "Return")
        {
            var activeTracking = entity.ProcessTrackings.FirstOrDefault(t => !t.ActualReturnDate.HasValue);
            if (activeTracking != null)
            {
                activeTracking.ActualReturnDate = DateTime.Today;
                if (!string.IsNullOrWhiteSpace(req.Notes))
                {
                    activeTracking.Remarks = req.Notes.Trim();
                }

                bool allComplete = entity.ProcessTrackings.All(t => t.ActualReturnDate.HasValue);
                if (allComplete)
                {
                    entity.Status = "Completed";
                }

                await _context.SaveChangesAsync();
                await SyncOrderStatus(entity.ProductionOrderId);

                return Json(new
                {
                    success = true,
                    message = $"✓ Marked {entity.EntityType} #{entity.SlNo} as returned from {activeTracking.ProcessName}!"
                });
            }
            else
            {
                return Json(new { success = false, message = "Entity does not have an active ongoing process to return." });
            }
        }
        else if (req.ActionType == "SendProcess")
        {
            if (string.IsNullOrWhiteSpace(req.ProcessName))
            {
                return Json(new { success = false, message = "Process name is required." });
            }

            var tracking = new ProcessTracking
            {
                ProductionEntityId = entity.Id,
                ProcessName = req.ProcessName,
                VendorId = req.VendorId,
                GivenDate = DateTime.Today,
                ExpectedReturnDate = req.ExpectedReturn ?? DateTime.Today.AddDays(3),
                Remarks = req.Notes
            };

            _context.ProcessTrackings.Add(tracking);

            entity.Status = req.ProcessName switch
            {
                "Dying" => "AtDying",
                "Roll" => "AtRoll",
                "Handwork" => "AtHandwork",
                "Stitching" => "AtStitching",
                _ => entity.Status
            };

            await _context.SaveChangesAsync();
            await SyncOrderStatus(entity.ProductionOrderId);

            return Json(new
            {
                success = true,
                message = $"✓ Dispatched {entity.EntityType} #{entity.SlNo} to {req.ProcessName}!"
            });
        }
        else if (req.ActionType == "Complete")
        {
            entity.Status = "Completed";
            await _context.SaveChangesAsync();
            await SyncOrderStatus(entity.ProductionOrderId);

            return Json(new
            {
                success = true,
                message = $"✓ Marked {entity.EntityType} #{entity.SlNo} as Completed / Ready!"
            });
        }

        return Json(new { success = false, message = "Unknown action type." });
    }

    private async Task SyncOrderStatus(int orderId)
    {
        var order = await _context.ProductionOrders.FindAsync(orderId);
        if (order == null) return;

        var entities = await _context.ProductionEntities
            .Where(e => e.ProductionOrderId == orderId)
            .ToListAsync();

        if (!entities.Any()) return;

        if (entities.All(e => e.Status == "Completed"))
            order.Status = OrderStatus.ReadyToDispatch;
        else if (entities.Any(e => e.Status == "AtStitching"))
            order.Status = OrderStatus.AtStitching;
        else if (entities.Any(e => e.Status == "AtHandwork"))
            order.Status = OrderStatus.AtHandwork;
        else if (entities.Any(e => e.Status == "AtDying"))
            order.Status = OrderStatus.AtDying;

        await _context.SaveChangesAsync();
    }
}

public class BarcodeLookupRequest
{
    public string Code { get; set; } = string.Empty;
}
