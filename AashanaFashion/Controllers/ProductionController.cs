using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Authorization;
using AashanaFashion.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers
{
    [Authorize]
    public class ProductionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IReadyInventoryService _readyInventoryService;
        private readonly IJobSlipService _jobSlipService;
        private readonly ICompanyContext _companyContext;
        private readonly IPmsSyncService _pmsSyncService;

        public ProductionController(AppDbContext context, IReadyInventoryService readyInventoryService, IJobSlipService jobSlipService, ICompanyContext companyContext, IPmsSyncService pmsSyncService)
        {
            _context = context;
            _readyInventoryService = readyInventoryService;
            _jobSlipService = jobSlipService;
            _companyContext = companyContext;
            _pmsSyncService = pmsSyncService;
        }

        // All roles can view
        public async Task<IActionResult> Index(string? search, OrderStatus? status)
        {
            var activeCompany = await _companyContext.GetActiveCompanyAsync();

            var totalOrders = await _context.ProductionOrders.Where(o => o.CompanyId == activeCompany.Id).ToListAsync();
            ViewBag.Total = totalOrders.Count;
            ViewBag.ReadyToDispatch = totalOrders.Count(o => o.Status == OrderStatus.ReadyToDispatch);
            ViewBag.Dispatched = totalOrders.Count(o => o.Status == OrderStatus.Dispatched);
            ViewBag.InProgress = totalOrders.Count(o => o.Status != OrderStatus.ReadyToDispatch && o.Status != OrderStatus.Dispatched);
            ViewBag.ActiveCompanyName = activeCompany.CompanyName;

            var query = _context.ProductionOrders
                .Include(p => p.Design)
                    .ThenInclude(d => d!.HandworkWorker)
                .Include(p => p.Design)
                    .ThenInclude(d => d!.StitchingWorker)
                .Include(p => p.HandworkWorker)
                .Include(p => p.StitchingWorker)
                .Include(p => p.ComponentAssignments)
                    .ThenInclude(a => a.Vendor)
                .Include(p => p.JobSlips)
                .Where(p => p.CompanyId == activeCompany.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => p.LotNo.ToLower().Contains(s) || (p.Design != null && p.Design.DesignNumber.ToLower().Contains(s)));
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            var orders = await query.OrderByDescending(p => p.CreatedDate).ToListAsync();
            ViewBag.Search = search;
            ViewBag.SelectedStatus = status;

            var batches = orders.Select(b =>
            {
                var steps = b.Design?.GetCreationSteps() ?? new List<string>();
                var completed = b.GetCompletedProcessesList();

                string currentStage;
                if (b.Status == OrderStatus.Dispatched)
                {
                    currentStage = "Dispatched";
                }
                else if (b.Status == OrderStatus.ReadyToDispatch)
                {
                    currentStage = "Ready to Dispatch";
                }
                else if (!string.IsNullOrEmpty(b.CurrentProcess))
                {
                    currentStage = b.CurrentProcess;
                }
                else
                {
                    var uncompleted = steps.FirstOrDefault(s => !completed.Contains(s));
                    currentStage = uncompleted ?? b.Status.ToString();
                }

                int progressPct = 0;
                if (b.Status == OrderStatus.Dispatched || b.Status == OrderStatus.ReadyToDispatch)
                {
                    progressPct = 100;
                }
                else if (steps.Any())
                {
                    progressPct = (completed.Count * 100) / steps.Count;
                }

                return new BatchDashboardViewModel
                {
                    Id = b.Id,
                    DesignNumber = b.Design?.DesignNumber ?? "",
                    LotNo = b.LotNo,
                    TotalQuantity = b.TotalQuantity,
                    CurrentStage = currentStage,
                    Status = b.Status,
                    CreationSteps = steps,
                    CompletedProcesses = completed,
                    CurrentProcess = currentStage,
                    ProgressPercentage = progressPct,
                    HandworkWorkerName = b.HandworkWorker?.VendorName ?? b.Design?.HandworkWorker?.VendorName,
                    StitchingWorkerName = b.StitchingWorker?.VendorName ?? b.Design?.StitchingWorker?.VendorName,
                    HandworkParts = b.HandworkComponentsSummary,
                    PhotoPath = b.Design?.PhotoPath,
                    ComponentAssignments = b.ComponentAssignments,
                    JobSlipCount = b.JobSlips?.Count ?? 0,
                    VerificationStatus = new Dictionary<string, bool>
                    {
                        { "RawMaterial", b.IsRawMaterialVerified },
                        { "Dying", b.IsDyingVerified },
                        { "Handwork", b.IsHandworkVerified },
                        { "Stitching", b.IsStitchingVerified }
                    }
                };
            }).ToList();

            return View(batches);
        }

        // Admin only — Create
        [PermissionAuthorize("ProductionOrder", "CanCreate")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var activeCompany = await _companyContext.GetActiveCompanyAsync();

            var designs = await _context.Designs
                .Include(d => d.HandworkWorker)
                .Include(d => d.StitchingWorker)
                .Include(d => d.DiscontinuedVariants)
                .Include(d => d.ComponentAssignments)
                    .ThenInclude(a => a.Vendor)
                .Where(d => !d.Discontinued && d.IsActive && (d.CompanyId == null || d.CompanyId == activeCompany.Id))
                .OrderBy(d => d.DesignNumber)
                .ToListAsync();
            ViewBag.Designs = designs;
            ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
            ViewBag.Processes = await _context.ProcessMasters.Where(p => p.IsActive).OrderBy(p => p.DisplayOrder).ToListAsync();
            var nextLotNo = await GenerateNextLotNumberAsync();
            return View(new ProductionOrder { LotNo = nextLotNo });
        }

        [PermissionAuthorize("ProductionOrder", "CanCreate")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductionOrder order, [FromForm] List<ProductionOrderDetail> Details, [FromForm] List<ProductionOrderComponentAssignment>? ComponentAssignments)
        {
            var activeCompany = await _companyContext.GetActiveCompanyAsync();

            if (string.IsNullOrWhiteSpace(order.LotNo))
            {
                order.LotNo = await GenerateNextLotNumberAsync();
            }
            else
            {
                order.LotNo = order.LotNo.Trim();
            }

            if (await _context.ProductionOrders.AnyAsync(o => o.LotNo == order.LotNo))
            {
                ModelState.AddModelError("LotNo", $"Lot No '{order.LotNo}' already exists. Please choose a unique Lot No.");
            }

            var design = await _context.Designs
                .Include(d => d.DiscontinuedVariants)
                .Include(d => d.ComponentAssignments)
                    .ThenInclude(a => a.Vendor)
                .FirstOrDefaultAsync(d => d.Id == order.DesignId);

            if (design == null)
            {
                ModelState.AddModelError("DesignId", "Selected design was not found.");
            }
            else
            {
                if (design.Discontinued)
                {
                    ModelState.AddModelError("DesignId", $"Design '{design.DesignNumber}' is discontinued. No new production orders can be created for discontinued products.");
                }

                if (Details != null && Details.Any(d => d.Quantity > 0))
                {
                    foreach (var d in Details.Where(d => d.Quantity > 0))
                    {
                        if (design.IsVariantDiscontinued(d.Colour, d.Size))
                        {
                            ModelState.AddModelError("", $"Variant '{d.Colour} / {d.Size}' of Design '{design.DesignNumber}' is discontinued. No new production orders can be scheduled for discontinued variants.");
                        }
                    }
                }
            }

            if (!ModelState.IsValid || order.DesignId == 0)
            {
                ViewBag.Designs = await _context.Designs
                    .Include(d => d.HandworkWorker)
                    .Include(d => d.StitchingWorker)
                    .Include(d => d.DiscontinuedVariants)
                    .Include(d => d.ComponentAssignments)
                        .ThenInclude(a => a.Vendor)
                    .Where(d => !d.Discontinued && d.IsActive && (d.CompanyId == null || d.CompanyId == activeCompany.Id))
                    .OrderBy(d => d.DesignNumber)
                    .ToListAsync();
                ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
                ViewBag.Processes = await _context.ProcessMasters.Where(p => p.IsActive).OrderBy(p => p.DisplayOrder).ToListAsync();
                return View(order);
            }

            if (!string.IsNullOrWhiteSpace(order.Components))
            {
                order.Components = string.Join(", ", order.GetComponentsList());
            }
            else if (design != null && !string.IsNullOrWhiteSpace(design.Components))
            {
                order.Components = design.Components;
            }
            else
            {
                order.Components = "Chaniya, Choli, Dupatta";
            }

            var firstHw = ComponentAssignments?.FirstOrDefault(a => a.ProcessName.Contains("Handwork", StringComparison.OrdinalIgnoreCase) && a.VendorId.HasValue && a.VendorId > 0);
            order.HandworkWorkerId = firstHw != null ? firstHw.VendorId : (order.HandworkWorkerId > 0 ? order.HandworkWorkerId : null);

            var firstSt = ComponentAssignments?.FirstOrDefault(a => a.ProcessName.Contains("Stitching", StringComparison.OrdinalIgnoreCase) && a.VendorId.HasValue && a.VendorId > 0);
            order.StitchingWorkerId = firstSt != null ? firstSt.VendorId : (order.StitchingWorkerId > 0 ? order.StitchingWorkerId : null);
            order.CompanyId = activeCompany.Id;
            order.CreatedDate = DateTime.Now;
            
            if (Details != null && Details.Any(d => d.Quantity > 0))
            {
                order.Details = Details.Where(d => d.Quantity > 0).ToList();
                order.TotalQuantity = order.Details.Sum(d => d.Quantity);
            }

            var creationSteps = design?.GetCreationSteps() ?? new List<string>();
            if (string.IsNullOrEmpty(order.CurrentProcess))
            {
                order.CurrentProcess = creationSteps.FirstOrDefault() ?? "Cutting";
            }
            order.CompletedProcesses = "";

            _context.ProductionOrders.Add(order);
            await _context.SaveChangesAsync();

            if (ComponentAssignments != null && ComponentAssignments.Any())
            {
                foreach (var ca in ComponentAssignments.Where(a => a.VendorId.HasValue && a.VendorId.Value > 0 && !string.IsNullOrWhiteSpace(a.ComponentName) && !string.IsNullOrWhiteSpace(a.ProcessName)))
                {
                    _context.ProductionOrderComponentAssignments.Add(new ProductionOrderComponentAssignment
                    {
                        ProductionOrderId = order.Id,
                        ComponentName = ca.ComponentName.Trim(),
                        ProcessName = ca.ProcessName.Trim(),
                        VendorId = ca.VendorId,
                        Rate = ca.Rate,
                        Remarks = ca.Remarks
                    });
                }
                await _context.SaveChangesAsync();

                // Auto-generate Job Slips for external vendor assignments
                await _jobSlipService.GenerateJobSlipsForOrderAsync(order.Id, User.Identity?.Name);
            }

            // Auto-generate tracking entities for this lot
            var initEntityStatus = order.Status switch
            {
                OrderStatus.AtDying => "AtDying",
                OrderStatus.AtHandwork => "AtHandwork",
                OrderStatus.AtStitching => "AtStitching",
                OrderStatus.ReadyToDispatch => "Completed",
                OrderStatus.Dispatched => "Dispatched",
                _ => "Created"
            };

            int slNo = 1;
            if (order.Details != null && order.Details.Any())
            {
                foreach (var detail in order.Details)
                {
                    for (int i = 0; i < detail.Quantity; i++)
                    {
                        _context.ProductionEntities.Add(new ProductionEntity
                        {
                            ProductionOrderId = order.Id,
                            EntityType = "Garment",
                            Colour = detail.Colour,
                            Size = detail.Size,
                            SlNo = slNo,
                            Barcode = BarcodeService.FormatEntityBarcode(order.Id, slNo),
                            Status = initEntityStatus,
                            CreatedDate = DateTime.Now
                        });
                        slNo++;
                    }
                }
            }
            else if (order.TotalQuantity > 0)
            {
                for (int i = 0; i < order.TotalQuantity; i++)
                {
                    _context.ProductionEntities.Add(new ProductionEntity
                    {
                        ProductionOrderId = order.Id,
                        EntityType = "Garment",
                        Colour = "Standard",
                        Size = "Free Size",
                        SlNo = slNo,
                        Barcode = BarcodeService.FormatEntityBarcode(order.Id, slNo),
                        Status = initEntityStatus,
                        CreatedDate = DateTime.Now
                    });
                    slNo++;
                }
            }

            if (slNo > 1)
            {
                await _context.SaveChangesAsync();
            }

            if (order.Status == OrderStatus.ReadyToDispatch || order.Status == OrderStatus.Dispatched)
            {
                await _readyInventoryService.InwardLotToReadyStockAsync(order.Id);
            }

            await _pmsSyncService.SyncOrderTrackingAsync(order.Id);

            TempData["Success"] = $"Order '{order.LotNo}' created with {order.TotalQuantity} tracking entities.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetNextLotNumber()
        {
            var nextLotNo = await GenerateNextLotNumberAsync();
            return Json(new { lotNo = nextLotNo });
        }

        private async Task<string> GenerateNextLotNumberAsync()
        {
            var prefix = $"LOT-{DateTime.Now:yyyyMM}-";
            var existingLots = await _context.ProductionOrders
                .Where(o => o.LotNo.StartsWith(prefix))
                .Select(o => o.LotNo)
                .ToListAsync();

            int maxSeq = 0;
            foreach (var lot in existingLots)
            {
                if (lot.Length >= prefix.Length + 4)
                {
                    var seqStr = lot.Substring(prefix.Length);
                    if (int.TryParse(seqStr, out int cur) && cur > maxSeq)
                    {
                        maxSeq = cur;
                    }
                }
            }

            return $"{prefix}{(maxSeq + 1):D4}";
        }

        // Admin + Manager — Update Status
        [PermissionAuthorize("ProductionOrder", "CanEdit")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var order = await _context.ProductionOrders
                .Include(p => p.Design)
                    .ThenInclude(d => d!.BomItems)
                        .ThenInclude(b => b.RawMaterial)
                .Include(p => p.Design)
                    .ThenInclude(d => d!.ComponentAssignments)
                        .ThenInclude(a => a.Vendor)
                .Include(p => p.ComponentAssignments)
                    .ThenInclude(a => a.Vendor)
                .Include(p => p.Details)
                .Include(p => p.JobSlips)
                    .ThenInclude(js => js.Vendor)
                .Include(p => p.JobSlips)
                    .ThenInclude(js => js.Items)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (order == null) return NotFound();

            if (string.IsNullOrWhiteSpace(order.Components) && order.Design != null && !string.IsNullOrWhiteSpace(order.Design.Components))
            {
                order.Components = order.Design.Components;
            }
            if ((order.ComponentAssignments == null || !order.ComponentAssignments.Any()) && order.Design?.ComponentAssignments?.Any() == true)
            {
                order.ComponentAssignments = order.Design.ComponentAssignments.Select(ca => new ProductionOrderComponentAssignment
                {
                    ProductionOrderId = order.Id,
                    ComponentName = ca.ComponentName,
                    ProcessName = ca.ProcessName,
                    VendorId = ca.VendorId,
                    Vendor = ca.Vendor,
                    Rate = ca.EstimatedRate,
                    Remarks = ca.Remarks
                }).ToList();
            }

            if ((order.JobSlips == null || !order.JobSlips.Any()) && order.ComponentAssignments?.Any(a => a.VendorId.HasValue && a.VendorId > 0) == true)
            {
                order.JobSlips = await _jobSlipService.GenerateJobSlipsForOrderAsync(order.Id, User.Identity?.Name);
            }

            var configuredSteps = order.Design?.GetCreationSteps() ?? new List<string> { "Dying", "Handwork", "Stitching" };
            if (string.IsNullOrEmpty(order.CurrentProcess))
            {
                if (order.Status == OrderStatus.Dispatched)
                {
                    order.CurrentProcess = "Dispatched";
                    order.SetCompletedProcessesList(configuredSteps);
                }
                else if (order.Status == OrderStatus.ReadyToDispatch)
                {
                    order.CurrentProcess = "Ready to Dispatch";
                    order.SetCompletedProcessesList(configuredSteps);
                }
                else
                {
                    var completed = new List<string>();
                    string current = configuredSteps.FirstOrDefault() ?? "Cutting";
                    if (order.Status == OrderStatus.AtStitching)
                    {
                        var stitchIdx = configuredSteps.FindIndex(s => s.Equals("Stitching", StringComparison.OrdinalIgnoreCase));
                        if (stitchIdx > 0)
                        {
                            completed.AddRange(configuredSteps.Take(stitchIdx));
                            current = configuredSteps[stitchIdx];
                        }
                    }
                    else if (order.Status == OrderStatus.AtHandwork)
                    {
                        var hwIdx = configuredSteps.FindIndex(s => s.Equals("Handwork", StringComparison.OrdinalIgnoreCase));
                        if (hwIdx > 0)
                        {
                            completed.AddRange(configuredSteps.Take(hwIdx));
                            current = configuredSteps[hwIdx];
                        }
                    }
                    else if (order.Status == OrderStatus.AtDying)
                    {
                        var dyeIdx = configuredSteps.FindIndex(s => s.Equals("Dying", StringComparison.OrdinalIgnoreCase));
                        if (dyeIdx > 0)
                        {
                            completed.AddRange(configuredSteps.Take(dyeIdx));
                            current = configuredSteps[dyeIdx];
                        }
                    }
                    order.CurrentProcess = current;
                    order.SetCompletedProcessesList(completed);
                }
            }

            var designs = await _context.Designs
                .Include(d => d.HandworkWorker)
                .Include(d => d.StitchingWorker)
                .Include(d => d.ComponentAssignments)
                    .ThenInclude(a => a.Vendor)
                .OrderBy(d => d.DesignNumber).ToListAsync();
            ViewBag.Designs = designs;
            ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
            ViewBag.Processes = await _context.ProcessMasters.Where(p => p.IsActive).OrderBy(p => p.DisplayOrder).ToListAsync();
            return View(order);
        }

        [PermissionAuthorize("ProductionOrder", "CanEdit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueMaterials(int id)
        {
            var order = await _context.ProductionOrders
                .Include(p => p.Design)
                    .ThenInclude(d => d!.BomItems)
                        .ThenInclude(b => b.RawMaterial)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (order == null) return NotFound();

            if (order.IsMaterialIssued)
            {
                TempData["Error"] = "Materials have already been issued for this lot.";
                return RedirectToAction(nameof(Edit), new { id = order.Id });
            }

            if (order.Design?.BomItems == null || !order.Design.BomItems.Any())
            {
                TempData["Error"] = $"No Bill of Materials (BOM) recipe found for Design '{order.Design?.DesignNumber}'. Please configure BOM first.";
                return RedirectToAction(nameof(Edit), new { id = order.Id });
            }

            foreach (var item in order.Design.BomItems)
            {
                if (item.RawMaterial != null && item.EffectiveQuantity > 0)
                {
                    var totalRequired = order.TotalQuantity * item.EffectiveQuantity;
                    item.RawMaterial.CurrentStock -= totalRequired;

                    _context.RawMaterialTransactions.Add(new RawMaterialTransaction
                    {
                        RawMaterialId = item.RawMaterialId,
                        Type = "Outward",
                        Quantity = totalRequired,
                        BalanceAfter = item.RawMaterial.CurrentStock,
                        UnitPrice = item.RawMaterial.Rate,
                        ReferenceType = "ProductionOrder",
                        ReferenceId = order.Id,
                        Remarks = $"Issued for Lot #{order.LotNo} ({order.TotalQuantity} pcs of {order.Design.DesignNumber})",
                        CreatedDate = DateTime.Now
                    });
                }
            }

            order.IsMaterialIssued = true;
            order.MaterialIssuedDate = DateTime.Now;
            order.IsRawMaterialVerified = true;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Raw materials successfully issued from inventory for Lot '{order.LotNo}'. Stock ledger updated.";
            return RedirectToAction(nameof(Edit), new { id = order.Id });
        }

        [PermissionAuthorize("ProductionOrder", "CanEdit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromForm] List<ProductionOrderDetail> Details, [FromForm] List<ProductionOrderComponentAssignment>? ComponentAssignments)
        {
            var order = await _context.ProductionOrders
                .Include(p => p.Details)
                .Include(p => p.ComponentAssignments)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (order == null) return NotFound();

            var oldStatus = order.Status;
            if (Request.Form.ContainsKey("IsRawMaterialVerified"))
                order.IsRawMaterialVerified = true;
            if (Request.Form.ContainsKey("IsDyingVerified"))
                order.IsDyingVerified = true;
            if (Request.Form.ContainsKey("IsHandworkVerified"))
                order.IsHandworkVerified = true;
            if (Request.Form.ContainsKey("IsStitchingVerified"))
                order.IsStitchingVerified = true;

            if (Request.Form.ContainsKey("CurrentProcess"))
            {
                order.CurrentProcess = Request.Form["CurrentProcess"].ToString().Trim();
            }
            if (Request.Form.ContainsKey("CompletedProcesses"))
            {
                order.CompletedProcesses = Request.Form["CompletedProcesses"].ToString().Trim();
            }

            if (Request.Form.ContainsKey("Status") && Enum.TryParse<OrderStatus>(Request.Form["Status"], out var parsedStatus))
            {
                order.Status = parsedStatus;
            }
            else
            {
                if (order.CurrentProcess == "Dispatched")
                    order.Status = OrderStatus.Dispatched;
                else if (order.CurrentProcess == "Ready to Dispatch")
                    order.Status = OrderStatus.ReadyToDispatch;
                else
                    order.Status = MapProcessToOrderStatus(order.CurrentProcess);
            }

            var entityStatus = order.Status switch
            {
                OrderStatus.ReadyToDispatch => "Completed",
                OrderStatus.Dispatched => "Dispatched",
                _ => order.CurrentProcess ?? "Created"
            };
            var lotEntities = await _context.ProductionEntities.Where(e => e.ProductionOrderId == order.Id).ToListAsync();
            foreach (var ent in lotEntities)
            {
                ent.Status = entityStatus;
            }

            if (User.IsInRole("Admin"))
            {
                if (Request.Form["DesignId"].Count > 0)
                    order.DesignId = int.Parse(Request.Form["DesignId"]!);
                if (Request.Form["LotNo"].Count > 0)
                    order.LotNo = Request.Form["LotNo"]!;
            }

            if (Details != null && Details.Any())
            {
                var existingDetails = order.Details.ToList();
                _context.ProductionOrderDetails.RemoveRange(existingDetails);

                var newDetails = Details.Where(d => d.Quantity > 0).ToList();
                foreach (var detail in newDetails)
                {
                    detail.ProductionOrderId = order.Id;
                }
                _context.ProductionOrderDetails.AddRange(newDetails);
                order.TotalQuantity = newDetails.Sum(d => d.Quantity);
            }

            if (Request.Form.ContainsKey("Components") && !string.IsNullOrWhiteSpace(Request.Form["Components"]))
            {
                order.Components = Request.Form["Components"]!;
            }

            // Update ComponentAssignments
            var existingAssignments = await _context.ProductionOrderComponentAssignments.Where(a => a.ProductionOrderId == order.Id).ToListAsync();
            _context.ProductionOrderComponentAssignments.RemoveRange(existingAssignments);

            if (ComponentAssignments != null && ComponentAssignments.Any())
            {
                foreach (var ca in ComponentAssignments.Where(a => a.VendorId.HasValue && a.VendorId.Value > 0 && !string.IsNullOrWhiteSpace(a.ComponentName) && !string.IsNullOrWhiteSpace(a.ProcessName)))
                {
                    _context.ProductionOrderComponentAssignments.Add(new ProductionOrderComponentAssignment
                    {
                        ProductionOrderId = order.Id,
                        ComponentName = ca.ComponentName.Trim(),
                        ProcessName = ca.ProcessName.Trim(),
                        VendorId = ca.VendorId,
                        Rate = ca.Rate,
                        Remarks = ca.Remarks
                    });
                }

                var firstHw = ComponentAssignments.FirstOrDefault(a => a.ProcessName.Contains("Handwork", StringComparison.OrdinalIgnoreCase) && a.VendorId.HasValue && a.VendorId > 0);
                if (firstHw != null) order.HandworkWorkerId = firstHw.VendorId;

                var firstSt = ComponentAssignments.FirstOrDefault(a => a.ProcessName.Contains("Stitching", StringComparison.OrdinalIgnoreCase) && a.VendorId.HasValue && a.VendorId > 0);
                if (firstSt != null) order.StitchingWorkerId = firstSt.VendorId;
            }

            if (Request.Form.ContainsKey("StitchingWorkerId") && (ComponentAssignments == null || !ComponentAssignments.Any(a => a.ProcessName.Contains("Stitching", StringComparison.OrdinalIgnoreCase))))
            {
                if (int.TryParse(Request.Form["StitchingWorkerId"], out int swId) && swId > 0)
                    order.StitchingWorkerId = swId;
                else
                    order.StitchingWorkerId = null;
            }

            if (Request.Form.ContainsKey("HandworkWorkerId") && (ComponentAssignments == null || !ComponentAssignments.Any(a => a.ProcessName.Contains("Handwork", StringComparison.OrdinalIgnoreCase))))
            {
                if (int.TryParse(Request.Form["HandworkWorkerId"], out int hwId) && hwId > 0)
                    order.HandworkWorkerId = hwId;
                else
                    order.HandworkWorkerId = null;
            }

            order.HandworkCholi = Request.Form.ContainsKey("HandworkCholi") && (Request.Form["HandworkCholi"] == "true" || Request.Form["HandworkCholi"] == "on");
            order.HandworkChaniya = Request.Form.ContainsKey("HandworkChaniya") && (Request.Form["HandworkChaniya"] == "true" || Request.Form["HandworkChaniya"] == "on");
            order.HandworkDupatta = Request.Form.ContainsKey("HandworkDupatta") && (Request.Form["HandworkDupatta"] == "true" || Request.Form["HandworkDupatta"] == "on");

            await _context.SaveChangesAsync();

            // Auto-generate / sync Job Slips for external vendor assignments
            await _jobSlipService.GenerateJobSlipsForOrderAsync(order.Id, User.Identity?.Name);

            // Auto-inward or revert 3-piece ready product inventory based on lot status
            if (order.Status == OrderStatus.ReadyToDispatch || order.Status == OrderStatus.Dispatched)
            {
                if (!order.IsStockInwarded)
                {
                    await _readyInventoryService.InwardLotToReadyStockAsync(order.Id);
                }
            }
            else if ((oldStatus == OrderStatus.ReadyToDispatch || oldStatus == OrderStatus.Dispatched) && order.IsStockInwarded)
            {
                await _readyInventoryService.RevertLotFromReadyStockAsync(order.Id);
            }

            await _pmsSyncService.SyncOrderTrackingAsync(order.Id);

            TempData["Success"] = $"Order '{order.LotNo}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [PermissionAuthorize("ProductionOrder", "CanEdit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdvanceStage(int id)
        {
            var order = await _context.ProductionOrders
                .Include(p => p.Design)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (order == null) return NotFound();

            var steps = order.Design?.GetCreationSteps() ?? new List<string> { "Dying", "Handwork", "Stitching" };
            var completed = order.GetCompletedProcessesList();

            // Find current uncompleted step
            int curIdx = -1;
            for (int i = 0; i < steps.Count; i++)
            {
                if (!completed.Contains(steps[i]))
                {
                    curIdx = i;
                    break;
                }
            }

            if (curIdx >= 0 && curIdx < steps.Count)
            {
                // Mark current step as completed
                completed.Add(steps[curIdx]);
                order.SetCompletedProcessesList(completed);

                if (curIdx + 1 < steps.Count)
                {
                    // Move to next configured process
                    order.CurrentProcess = steps[curIdx + 1];
                    order.Status = MapProcessToOrderStatus(order.CurrentProcess);
                }
                else
                {
                    // All configured processes complete -> Ready to Dispatch!
                    order.CurrentProcess = "Ready to Dispatch";
                    order.Status = OrderStatus.ReadyToDispatch;
                }
            }
            else if (order.Status == OrderStatus.ReadyToDispatch)
            {
                // ReadyToDispatch -> Dispatched
                order.CurrentProcess = "Dispatched";
                order.Status = OrderStatus.Dispatched;
            }

            var entityStatus = order.Status switch
            {
                OrderStatus.ReadyToDispatch => "Completed",
                OrderStatus.Dispatched => "Dispatched",
                _ => order.CurrentProcess ?? "Created"
            };
            var lotEntities = await _context.ProductionEntities.Where(e => e.ProductionOrderId == order.Id).ToListAsync();
            foreach (var ent in lotEntities)
            {
                ent.Status = entityStatus;
            }

            await _context.SaveChangesAsync();

            if (order.Status == OrderStatus.ReadyToDispatch || order.Status == OrderStatus.Dispatched)
            {
                if (!order.IsStockInwarded)
                {
                    await _readyInventoryService.InwardLotToReadyStockAsync(order.Id);
                }
            }

            await _pmsSyncService.SyncOrderTrackingAsync(order.Id);

            TempData["Success"] = $"Stage advanced for Lot '{order.LotNo}'. Current Stage: {order.CurrentProcess}";
            return RedirectToAction(nameof(Edit), new { id = order.Id });
        }

        [PermissionAuthorize("ProductionOrder", "CanEdit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevertStage(int id)
        {
            var order = await _context.ProductionOrders
                .Include(p => p.Design)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (order == null) return NotFound();

            var steps = order.Design?.GetCreationSteps() ?? new List<string> { "Dying", "Handwork", "Stitching" };
            var completed = order.GetCompletedProcessesList();

            if (order.Status == OrderStatus.Dispatched)
            {
                order.Status = OrderStatus.ReadyToDispatch;
                order.CurrentProcess = "Ready to Dispatch";
            }
            else if (order.Status == OrderStatus.ReadyToDispatch)
            {
                if (order.IsStockInwarded)
                {
                    await _readyInventoryService.RevertLotFromReadyStockAsync(order.Id);
                }
                if (completed.Any())
                {
                    var last = completed.Last();
                    completed.RemoveAt(completed.Count - 1);
                    order.SetCompletedProcessesList(completed);
                    order.CurrentProcess = last;
                    order.Status = MapProcessToOrderStatus(last);
                }
                else
                {
                    order.CurrentProcess = steps.LastOrDefault() ?? "Stitching";
                    order.Status = MapProcessToOrderStatus(order.CurrentProcess);
                }
            }
            else if (completed.Any())
            {
                var lastCompleted = completed.Last();
                completed.RemoveAt(completed.Count - 1);
                order.SetCompletedProcessesList(completed);
                order.CurrentProcess = lastCompleted;
                order.Status = MapProcessToOrderStatus(lastCompleted);
            }

            var entityStatus = order.Status switch
            {
                OrderStatus.ReadyToDispatch => "Completed",
                OrderStatus.Dispatched => "Dispatched",
                _ => order.CurrentProcess ?? "Created"
            };
            var lotEntities = await _context.ProductionEntities.Where(e => e.ProductionOrderId == order.Id).ToListAsync();
            foreach (var ent in lotEntities)
            {
                ent.Status = entityStatus;
            }

            await _context.SaveChangesAsync();
            await _pmsSyncService.SyncOrderTrackingAsync(order.Id);

            TempData["Success"] = $"Stage reverted for Lot '{order.LotNo}'. Current Stage: {order.CurrentProcess}";
            return RedirectToAction(nameof(Edit), new { id = order.Id });
        }

        private static OrderStatus MapProcessToOrderStatus(string? processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return OrderStatus.RawMaterialArrived;
            var name = processName.Trim().ToLower();
            if (name.Contains("dying") || name.Contains("dye")) return OrderStatus.AtDying;
            if (name.Contains("handwork") || name.Contains("hand")) return OrderStatus.AtHandwork;
            if (name.Contains("stitching") || name.Contains("stitch")) return OrderStatus.AtStitching;
            if (name.Contains("ready") || name.Contains("dispatch")) return OrderStatus.ReadyToDispatch;
            return OrderStatus.RawMaterialArrived;
        }

        // Admin only — Delete
        [PermissionAuthorize("ProductionOrder", "CanDelete")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.ProductionOrders.FindAsync(id);
            if (order != null)
            {
                if (order.IsStockInwarded)
                {
                    await _readyInventoryService.RevertLotFromReadyStockAsync(order.Id);
                }
                _context.ProductionOrders.Remove(order);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        //private static int CalculateProgress(ProductionOrder o)
        //{
        //    var steps = o.Design?.GetCreationSteps() ?? new List<string>();
        //    if (!steps.Any()) return 0;

        //    int completed = 0;
        //    foreach (var step in steps)
        //    {
        //        switch (step.Trim().ToLower())
        //        {
        //            case "raw material":
        //                if (o.IsRawMaterialVerified) completed++;
        //                break;
        //            case "dying":
        //                if (o.IsDyingVerified) completed++;
        //                break;
        //            case "handwork":
        //                if (o.IsHandworkVerified) completed++;
        //                break;
        //            case "stitching":
        //                if (o.IsStitchingVerified) completed++;
        //                break;
        //        }
        //    }
        //    return (completed * 100) / steps.Count;
        //}
    }
}
