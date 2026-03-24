using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            var orders = await _context.ProductionOrders.ToListAsync();

            ViewBag.Total = orders.Count;
            ViewBag.ReadyToDispatch = orders.Count(o => o.Status == OrderStatus.ReadyToDispatch);
            ViewBag.Dispatched = orders.Count(o => o.Status == OrderStatus.Dispatched);
            ViewBag.InProgress = orders.Count(o => o.Status != OrderStatus.ReadyToDispatch && o.Status != OrderStatus.Dispatched);

            var batches = orders.Select(b => new BatchDashboardViewModel
            {
                Id = b.Id,
                DesignNumber = b.DesignNumber,
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
        public IActionResult Create() => View(new ProductionOrder());

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductionOrder order)
        {
            if (!ModelState.IsValid) return View(order);
            order.CreatedDate = DateTime.Now;
            _context.ProductionOrders.Add(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Admin + Manager — Update Status
        [Authorize(Roles = "Admin,Manager")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var order = await _context.ProductionOrders.FindAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductionOrder order)
        {
            if (!ModelState.IsValid) return View(order);
            _context.ProductionOrders.Update(order);
            await _context.SaveChangesAsync();
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
