using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Authorization;
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
        public IActionResult Create()
        {
            var designs = _context.Designs.OrderBy(d => d.DesignNumber).ToList();
            ViewBag.Designs = designs;
            return View(new ProductionOrder());
        }

        [PermissionAuthorize("ProductionOrder", "CanCreate")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductionOrder order, [FromForm] List<ProductionOrderDetail> Details)
        {
            if (!ModelState.IsValid || order.DesignId == 0)
            {
                ViewBag.Designs = _context.Designs.OrderBy(d => d.DesignNumber).ToList();
                return View(order);
            }

            order.CreatedDate = DateTime.Now;
            
            if (Details != null && Details.Any(d => d.Quantity > 0))
            {
                order.Details = Details.Where(d => d.Quantity > 0).ToList();
                order.TotalQuantity = order.Details.Sum(d => d.Quantity);
            }

            _context.ProductionOrders.Add(order);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Order '{order.LotNo}' created successfully.";
            return RedirectToAction(nameof(Index));
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

            var designs = _context.Designs.OrderBy(d => d.DesignNumber).ToList();
            ViewBag.Designs = designs;
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
                order.Status = Enum.Parse<OrderStatus>(Request.Form["Status"]!);

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
