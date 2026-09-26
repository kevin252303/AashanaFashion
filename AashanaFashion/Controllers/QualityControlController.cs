using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class QualityControlController : Controller
{
    private readonly AppDbContext _context;

    public QualityControlController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search, int? orderId, QcStage? stage, int? vendorId, QcResult? result)
    {
        var allInspections = await _context.QualityInspections
            .Include(q => q.ProductionOrder)
            .ThenInclude(p => p!.Design)
            .Include(q => q.Vendor)
            .Include(q => q.Defects)
            .OrderByDescending(q => q.InspectionDate)
            .ThenByDescending(q => q.Id)
            .ToListAsync();

        var allDefects = await _context.QualityDefects
            .Include(d => d.QualityInspection)
            .ToListAsync();

        // Calculate Category Breakdowns
        var totalDefectCount = allDefects.Sum(d => d.Quantity);
        var categoryBreakdown = allDefects
            .GroupBy(d => d.DefectCategory)
            .Select(g => new DefectCategoryBreakdownItem
            {
                Category = g.Key,
                CategoryName = g.Key.ToString(),
                DefectCount = g.Sum(x => x.Quantity),
                Percentage = totalDefectCount > 0
                    ? Math.Round((decimal)g.Sum(x => x.Quantity) / totalDefectCount * 100m, 1)
                    : 0m
            })
            .OrderByDescending(b => b.DefectCount)
            .ToList();

        // Calculate Vendor Scorecards
        var vendorScorecards = allInspections
            .Where(q => q.VendorId.HasValue && q.Vendor != null)
            .GroupBy(q => q.Vendor!)
            .Select(g => new VendorQualityScorecardItem
            {
                VendorId = g.Key.Id,
                VendorName = g.Key.VendorName,
                ServiceType = g.Key.Industry ?? "Job Worker",
                InspectedPieces = g.Sum(x => x.TotalInspected),
                PassedPieces = g.Sum(x => x.TotalPassed),
                DefectivePieces = g.Sum(x => x.TotalRework + x.TotalScrap)
            })
            .OrderByDescending(v => v.DefectRate)
            .ToList();

        // Filter Inspections list
        var filtered = allInspections.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            filtered = filtered.Where(q => q.InspectionNumber.ToLower().Contains(s)
                || (q.InspectorName != null && q.InspectorName.ToLower().Contains(s))
                || (q.ProductionOrder != null && q.ProductionOrder.LotNo.ToLower().Contains(s))
                || (q.ProductionOrder?.Design != null && q.ProductionOrder.Design.DesignNumber.ToLower().Contains(s))
                || (q.Vendor != null && q.Vendor.VendorName.ToLower().Contains(s))
                || (q.Remarks != null && q.Remarks.ToLower().Contains(s)));
        }
        if (orderId.HasValue)
            filtered = filtered.Where(q => q.ProductionOrderId == orderId.Value);
        if (stage.HasValue)
            filtered = filtered.Where(q => q.Stage == stage.Value);
        if (vendorId.HasValue)
            filtered = filtered.Where(q => q.VendorId == vendorId.Value);
        if (result.HasValue)
            filtered = filtered.Where(q => q.OverallResult == result.Value);

        var vm = new QcDashboardViewModel
        {
            TotalInspections = allInspections.Count,
            TotalUnitsInspected = allInspections.Sum(q => q.TotalInspected),
            TotalUnitsPassed = allInspections.Sum(q => q.TotalPassed),
            TotalUnitsRework = allInspections.Sum(q => q.TotalRework),
            TotalUnitsScrapped = allInspections.Sum(q => q.TotalScrap),
            ActiveReworkCount = allDefects.Count(d => d.ReworkStatus == ReworkStatus.PendingRework || d.ReworkStatus == ReworkStatus.SentToRework),
            RecentInspections = filtered.Take(50).ToList(),
            VendorScorecards = vendorScorecards,
            CategoryBreakdowns = categoryBreakdown,
            StageFilter = stage,
            SelectedOrderId = orderId,
            SelectedVendorId = vendorId
        };

        ViewBag.Search = search;
        ViewBag.SelectedResult = result;
        ViewBag.Orders = await _context.ProductionOrders.Include(p => p.Design).OrderByDescending(p => p.Id).ToListAsync();
        ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();

        return View(vm);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> Create(int? orderId, QcStage? stage, int? vendorId, int? entityId)
    {
        var model = new CreateInspectionViewModel
        {
            InspectionNumber = await GenerateNextQcNumberAsync(),
            InspectionDate = DateTime.Today,
            InspectorName = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "QC Inspector",
            Stage = stage ?? QcStage.FinalGarmentInspection,
            ProductionOrderId = orderId ?? 0,
            VendorId = vendorId,
            ProductionEntityId = entityId
        };

        if (orderId.HasValue)
        {
            var order = await _context.ProductionOrders.FindAsync(orderId.Value);
            if (order != null)
            {
                model.TotalInspected = order.TotalQuantity;
                model.TotalPassed = order.TotalQuantity;
            }
        }

        ViewBag.Orders = await _context.ProductionOrders.Include(p => p.Design).OrderByDescending(p => p.Id).ToListAsync();
        ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
        if (orderId.HasValue)
        {
            ViewBag.Entities = await _context.ProductionEntities.Where(e => e.ProductionOrderId == orderId.Value).ToListAsync();
        }
        else
        {
            ViewBag.Entities = new List<ProductionEntity>();
        }

        return View(model);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateInspectionViewModel model)
    {
        if (model.TotalInspected <= 0)
        {
            ModelState.AddModelError("TotalInspected", "Total inspected pieces must be at least 1.");
        }

        int sumChecked = model.TotalPassed + model.TotalRework + model.TotalScrap;
        if (sumChecked != model.TotalInspected)
        {
            ModelState.AddModelError("TotalInspected", $"Passed ({model.TotalPassed}) + Rework ({model.TotalRework}) + Scrap ({model.TotalScrap}) = {sumChecked}, which does not match Total Inspected ({model.TotalInspected}).");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Orders = await _context.ProductionOrders.Include(p => p.Design).OrderByDescending(p => p.Id).ToListAsync();
            ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
            if (model.ProductionOrderId > 0)
            {
                ViewBag.Entities = await _context.ProductionEntities.Where(e => e.ProductionOrderId == model.ProductionOrderId).ToListAsync();
            }
            else
            {
                ViewBag.Entities = new List<ProductionEntity>();
            }
            return View(model);
        }

        // Determine Overall Result
        var result = QcResult.Passed;
        if (model.TotalScrap > 0 && model.TotalPassed == 0)
            result = QcResult.Failed;
        else if (model.TotalRework > 0 || model.TotalScrap > 0)
            result = QcResult.PassedWithRework;

        var inspection = new QualityInspection
        {
            InspectionNumber = string.IsNullOrWhiteSpace(model.InspectionNumber) ? await GenerateNextQcNumberAsync() : model.InspectionNumber.Trim(),
            InspectionDate = model.InspectionDate,
            InspectorName = model.InspectorName.Trim(),
            ProductionOrderId = model.ProductionOrderId,
            ProductionEntityId = model.ProductionEntityId > 0 ? model.ProductionEntityId : null,
            Stage = model.Stage,
            VendorId = model.VendorId > 0 ? model.VendorId : null,
            TotalInspected = model.TotalInspected,
            TotalPassed = model.TotalPassed,
            TotalRework = model.TotalRework,
            TotalScrap = model.TotalScrap,
            OverallResult = result,
            Remarks = model.Remarks?.Trim(),
            CreatedDate = DateTime.Now
        };

        // Add Defect items if any logged
        if (model.Defects != null)
        {
            foreach (var d in model.Defects.Where(x => x.Quantity > 0 && !string.IsNullOrWhiteSpace(x.DefectReason)))
            {
                var reworkStatus = d.Action switch
                {
                    DefectAction.ScrapWriteOff => ReworkStatus.Scrapped,
                    DefectAction.AcceptAsBGrade => ReworkStatus.NotApplicable,
                    DefectAction.ReworkByVendor => ReworkStatus.PendingRework,
                    DefectAction.ReworkInHouse => ReworkStatus.PendingRework,
                    _ => ReworkStatus.PendingRework
                };

                inspection.Defects.Add(new QualityDefect
                {
                    DefectCategory = d.DefectCategory,
                    DefectReason = d.DefectReason.Trim(),
                    Quantity = d.Quantity,
                    Severity = d.Severity,
                    Action = d.Action,
                    ReworkStatus = reworkStatus,
                    ReworkAssignedTo = d.ReworkAssignedTo?.Trim(),
                    ResolutionNotes = d.Remarks?.Trim(),
                    CreatedDate = DateTime.Now
                });
            }
        }

        _context.QualityInspections.Add(inspection);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Quality Inspection {inspection.InspectionNumber} recorded successfully.";
        return RedirectToAction(nameof(Details), new { id = inspection.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var inspection = await _context.QualityInspections
            .Include(q => q.ProductionOrder)
            .ThenInclude(p => p!.Design)
            .Include(q => q.Vendor)
            .Include(q => q.ProductionEntity)
            .Include(q => q.Defects)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (inspection == null) return NotFound();

        return View(inspection);
    }

    public async Task<IActionResult> ReworkQueue(ReworkStatus? status, string? search)
    {
        var query = _context.QualityDefects
            .Include(d => d.QualityInspection)
            .ThenInclude(q => q!.ProductionOrder)
            .ThenInclude(p => p!.Design)
            .Include(d => d.QualityInspection)
            .ThenInclude(q => q!.Vendor)
            .Where(d => d.Action == DefectAction.ReworkInHouse || d.Action == DefectAction.ReworkByVendor)
            .AsQueryable();

        var allReworks = await query.ToListAsync();

        var filtered = allReworks.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            filtered = filtered.Where(d => (d.DefectReason != null && d.DefectReason.ToLower().Contains(s))
                || (d.ReworkAssignedTo != null && d.ReworkAssignedTo.ToLower().Contains(s))
                || (d.QualityInspection != null && d.QualityInspection.InspectionNumber.ToLower().Contains(s))
                || (d.QualityInspection?.ProductionOrder != null && d.QualityInspection.ProductionOrder.LotNo.ToLower().Contains(s))
                || (d.QualityInspection?.ProductionOrder?.Design != null && d.QualityInspection.ProductionOrder.Design.DesignNumber.ToLower().Contains(s)));
        }

        if (status.HasValue)
        {
            filtered = filtered.Where(d => d.ReworkStatus == status.Value);
        }
        else
        {
            // Default: show pending or sent to rework
            filtered = filtered.Where(d => d.ReworkStatus == ReworkStatus.PendingRework || d.ReworkStatus == ReworkStatus.SentToRework);
        }

        ViewBag.Search = search;
        ViewBag.SelectedStatus = status;

        var vm = new ReworkQueueViewModel
        {
            TotalPendingRework = allReworks.Count(d => d.ReworkStatus == ReworkStatus.PendingRework),
            TotalSentToRework = allReworks.Count(d => d.ReworkStatus == ReworkStatus.SentToRework),
            TotalCompletedRework = allReworks.Count(d => d.ReworkStatus == ReworkStatus.ReworkCompleted),
            DefectItems = filtered.OrderByDescending(d => d.CreatedDate).ToList()
        };

        ViewBag.SelectedStatus = status;
        return View(vm);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateReworkStatus(int defectId, ReworkStatus status, string? resolutionNotes)
    {
        var defect = await _context.QualityDefects
            .Include(d => d.QualityInspection)
            .FirstOrDefaultAsync(d => d.Id == defectId);

        if (defect == null) return NotFound();

        defect.ReworkStatus = status;
        defect.ResolutionNotes = resolutionNotes?.Trim();
        if (status == ReworkStatus.ReworkCompleted)
        {
            defect.ReworkCompletionDate = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Rework item status updated to {status}.";
        return RedirectToAction(nameof(ReworkQueue));
    }

    [HttpGet]
    public async Task<IActionResult> GetOrderDetails(int orderId)
    {
        var order = await _context.ProductionOrders
            .Include(p => p.Design)
            .FirstOrDefaultAsync(p => p.Id == orderId);

        if (order == null) return NotFound();

        var entities = await _context.ProductionEntities
            .Where(e => e.ProductionOrderId == orderId)
            .Select(e => new { id = e.Id, label = $"{e.EntityType} #{e.SlNo} ({e.Colour} - {e.Size}) [{e.Status}]" })
            .ToListAsync();

        return Json(new
        {
            lotNo = order.LotNo,
            designNumber = order.Design?.DesignNumber ?? "",
            quantity = order.TotalQuantity,
            entities = entities
        });
    }

    private async Task<string> GenerateNextQcNumberAsync()
    {
        var prefix = $"QC-{DateTime.Now:yyyyMM}-";
        var lastInspection = await _context.QualityInspections
            .Where(q => q.InspectionNumber.StartsWith(prefix))
            .OrderByDescending(q => q.InspectionNumber)
            .FirstOrDefaultAsync();

        int nextSeq = 1;
        if (lastInspection != null && lastInspection.InspectionNumber.Length >= prefix.Length + 4)
        {
            var seqStr = lastInspection.InspectionNumber.Substring(prefix.Length);
            if (int.TryParse(seqStr, out int currentSeq))
            {
                nextSeq = currentSeq + 1;
            }
        }

        return $"{prefix}{nextSeq:D4}";
    }
}
