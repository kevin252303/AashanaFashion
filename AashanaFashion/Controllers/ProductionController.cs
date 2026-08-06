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
        public async Task<IActionResult> Index()
        {
            var orders = await _context.ProductionOrders
                .Include(p => p.Design)
                .ToListAsync();

            ViewBag.Total = orders.Count;
            ViewBag.ReadyToDispatch = orders.Count(o => o.Status == OrderStatus.ReadyToDispatch);
            ViewBag.Dispatched = orders.Count(o => o.Status == OrderStatus.Dispatched);
            ViewBag.InProgress = orders.Count(o => o.Status != OrderStatus.ReadyToDispatch && o.Status != OrderStatus.Dispatched);

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
