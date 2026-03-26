using AashanaFashion.Data;
using AashanaFashion.Models;
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
                IsGhagraDone = b.IsHandworkVerified,
                IsCholiDone = b.IsStitchingVerified,
                CurrentStage = b.Status.ToString(),
                IsHandworkVerified = b.IsHandworkVerified,
                IsStitchingVerified = b.IsStitchingVerified,
                Status = b.Status,
                ProgressPercentage = CalculateProgress(b)
            }).ToList();

            return View(batches);
        }

        // Admin only — Create
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            var designs = _context.Designs.OrderBy(d => d.DesignNumber).ToList();
            ViewBag.Designs = designs;
            return View(new ProductionOrder());
        }

        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin,Manager")]
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

        [Authorize(Roles = "Admin,Manager")]
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
                if (Request.Form["FabricType"].Count > 0)
                    order.FabricType = Request.Form["FabricType"]!;
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
        [Authorize(Roles = "Admin")]
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

        private static int CalculateProgress(ProductionOrder o)
        {
            int steps = 0;
            if (o.IsRawMaterialVerified) steps++;
            if (o.IsDyingVerified) steps++;
            if (o.IsHandworkVerified) steps++;
            if (o.IsStitchingVerified) steps++;
            return (steps * 100) / 4;
        }
    }
}
