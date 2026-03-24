using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace AashanaFashion.Controllers
{
    public class ProductionController : Controller
    {
        private readonly AppDbContext _context;

        public ProductionController(AppDbContext context) => _context = context;
        public async Task<IActionResult> Index()
        {
            var batches = await _context.ProductionOrders
            .Select(b => new BatchDashboardViewModel
            {
                Id = b.Id,
                DesignNumber = b.DesignNumber,
                TotalQuantity = b.TotalQuantity,
                // Logic to distinguish pieces visually
                IsGhagraDone = b.IsHandworkVerified,
                IsCholiDone = b.IsStitchingVerified,
                CurrentStage = b.Status.ToString()
            }).ToListAsync();

            return View(batches);
        }
    }
}
