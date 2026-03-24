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
