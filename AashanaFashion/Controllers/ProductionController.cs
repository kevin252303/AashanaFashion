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

        public ProductionController(AppDbContext context) => _context = context;

        // All roles can view
        public async Task<IActionResult> Index(string? search, OrderStatus? status)
        {
            var totalOrders = await _context.ProductionOrders.ToListAsync();
            ViewBag.Total = totalOrders.Count;
            ViewBag.ReadyToDispatch = totalOrders.Count(o => o.Status == OrderStatus.ReadyToDispatch);
            ViewBag.Dispatched = totalOrders.Count(o => o.Status == OrderStatus.Dispatched);
            ViewBag.InProgress = totalOrders.Count(o => o.Status != OrderStatus.ReadyToDispatch && o.Status != OrderStatus.Dispatched);

            var query = _context.ProductionOrders
                .Include(p => p.Design)
                    .ThenInclude(d => d!.HandworkWorker)
                .Include(p => p.Design)
                    .ThenInclude(d => d!.StitchingWorker)
                .Include(p => p.HandworkWorker)
                .Include(p => p.StitchingWorker)
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

            var batches = orders.Select(b => new BatchDashboardViewModel
            {
                Id = b.Id,
                DesignNumber = b.Design?.DesignNumber ?? "",
                LotNo = b.LotNo,
                TotalQuantity = b.TotalQuantity,
                CurrentStage = b.Status.ToString(),
                Status = b.Status,
                CreationSteps = b.Design?.GetCreationSteps() ?? new List<string>(),
                HandworkWorkerName = b.HandworkWorker?.VendorName ?? b.Design?.HandworkWorker?.VendorName,
                StitchingWorkerName = b.StitchingWorker?.VendorName ?? b.Design?.StitchingWorker?.VendorName,
                HandworkParts = b.HandworkComponentsSummary,
                VerificationStatus = new Dictionary<string, bool>
                {
                    { "RawMaterial", b.IsRawMaterialVerified },
                    { "Dying", b.IsDyingVerified },
                    { "Handwork", b.IsHandworkVerified },
                    { "Stitching", b.IsStitchingVerified }
                },
                //ProgressPercentage = CalculateProgress(b)
            }).ToList();

            return View(batches);
        }

        // Admin only — Create
        [PermissionAuthorize("ProductionOrder", "CanCreate")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var designs = await _context.Designs
                .Include(d => d.HandworkWorker)
                .Include(d => d.StitchingWorker)
                .OrderBy(d => d.DesignNumber)
                .ToListAsync();
            ViewBag.Designs = designs;
            ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
            var nextLotNo = await GenerateNextLotNumberAsync();
            return View(new ProductionOrder { LotNo = nextLotNo });
        }

        [PermissionAuthorize("ProductionOrder", "CanCreate")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductionOrder order, [FromForm] List<ProductionOrderDetail> Details)
        {
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

            if (!ModelState.IsValid || order.DesignId == 0)
            {
                ViewBag.Designs = await _context.Designs
                    .Include(d => d.HandworkWorker)
                    .Include(d => d.StitchingWorker)
                    .OrderBy(d => d.DesignNumber)
                    .ToListAsync();
                ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
                return View(order);
            }

            order.HandworkWorkerId = order.HandworkWorkerId > 0 ? order.HandworkWorkerId : null;
            order.StitchingWorkerId = order.StitchingWorkerId > 0 ? order.StitchingWorkerId : null;
            order.CreatedDate = DateTime.Now;
            
            if (Details != null && Details.Any(d => d.Quantity > 0))
            {
                order.Details = Details.Where(d => d.Quantity > 0).ToList();
                order.TotalQuantity = order.Details.Sum(d => d.Quantity);
            }

            _context.ProductionOrders.Add(order);
            await _context.SaveChangesAsync();

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
                .Include(p => p.Details)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (order == null) return NotFound();

            var designs = _context.Designs
                .Include(d => d.HandworkWorker)
                .Include(d => d.StitchingWorker)
                .OrderBy(d => d.DesignNumber).ToList();
            ViewBag.Designs = designs;
            ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
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
        public async Task<IActionResult> Edit(int id, [FromForm] List<ProductionOrderDetail> Details)
        {
            var order = await _context.ProductionOrders
                .Include(p => p.Details)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (order == null) return NotFound();

            if (Request.Form.ContainsKey("IsRawMaterialVerified"))
                order.IsRawMaterialVerified = true;
            if (Request.Form.ContainsKey("IsDyingVerified"))
                order.IsDyingVerified = true;
            if (Request.Form.ContainsKey("IsHandworkVerified"))
                order.IsHandworkVerified = true;
            if (Request.Form.ContainsKey("IsStitchingVerified"))
                order.IsStitchingVerified = true;

            if (Request.Form["Status"].Count > 0)
            {
                var newStatus = Enum.Parse<OrderStatus>(Request.Form["Status"]!);
                if (order.Status != newStatus)
                {
                    order.Status = newStatus;
                    var entityStatus = order.Status switch
                    {
                        OrderStatus.AtDying => "AtDying",
                        OrderStatus.AtHandwork => "AtHandwork",
                        OrderStatus.AtStitching => "AtStitching",
                        OrderStatus.ReadyToDispatch => "Completed",
                        OrderStatus.Dispatched => "Dispatched",
                        _ => "Created"
                    };
                    var lotEntities = await _context.ProductionEntities.Where(e => e.ProductionOrderId == order.Id).ToListAsync();
                    foreach (var ent in lotEntities)
                    {
                        ent.Status = entityStatus;
                    }
                }
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

            if (Request.Form.ContainsKey("StitchingWorkerId"))
            {
                if (int.TryParse(Request.Form["StitchingWorkerId"], out int swId) && swId > 0)
                    order.StitchingWorkerId = swId;
                else
                    order.StitchingWorkerId = null;
            }

            if (Request.Form.ContainsKey("HandworkWorkerId"))
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
            TempData["Success"] = $"Order '{order.LotNo}' updated successfully.";
            return RedirectToAction(nameof(Index));
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
