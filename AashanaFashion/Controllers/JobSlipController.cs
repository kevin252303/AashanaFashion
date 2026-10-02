using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Services;

namespace AashanaFashion.Controllers;

[Authorize]
public class JobSlipController : Controller
{
    private readonly AppDbContext _context;
    private readonly IJobSlipService _jobSlipService;

    public JobSlipController(AppDbContext context, IJobSlipService jobSlipService)
    {
        _context = context;
        _jobSlipService = jobSlipService;
    }

    // List all job slips with filters
    public async Task<IActionResult> Index(string? search, string? process, int? vendorId, string? status, int? lotId)
    {
        var query = _context.JobSlips
            .Include(js => js.Vendor)
            .Include(js => js.ProductionOrder)
                .ThenInclude(p => p!.Design)
            .Include(js => js.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(js => js.SlipNumber.ToLower().Contains(s) ||
                                      (js.ProductionOrder != null && js.ProductionOrder.LotNo.ToLower().Contains(s)) ||
                                      (js.ProductionOrder != null && js.ProductionOrder.Design != null && js.ProductionOrder.Design.DesignNumber.ToLower().Contains(s)) ||
                                      (js.Vendor != null && js.Vendor.VendorName.ToLower().Contains(s)) ||
                                      js.Components.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(process))
        {
            query = query.Where(js => js.ProcessName == process);
        }

        if (vendorId.HasValue && vendorId.Value > 0)
        {
            query = query.Where(js => js.VendorId == vendorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(js => js.Status == status);
        }

        if (lotId.HasValue && lotId.Value > 0)
        {
            query = query.Where(js => js.ProductionOrderId == lotId.Value);
        }

        var slips = await query.OrderByDescending(js => js.CreatedDate).ToListAsync();

        ViewBag.Search = search;
        ViewBag.SelectedProcess = process;
        ViewBag.SelectedVendorId = vendorId;
        ViewBag.SelectedStatus = status;
        ViewBag.SelectedLotId = lotId;

        ViewBag.Processes = await _context.ProcessMasters.Where(p => p.IsActive).OrderBy(p => p.DisplayOrder).Select(p => p.ProcessName).Distinct().ToListAsync();
        ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
        ViewBag.Lots = await _context.ProductionOrders.OrderByDescending(o => o.CreatedDate).Take(100).Select(o => new { o.Id, o.LotNo, DesignNumber = o.Design != null ? o.Design.DesignNumber : "" }).ToListAsync();

        ViewBag.TotalSlips = await _context.JobSlips.CountAsync();
        ViewBag.IssuedSlips = await _context.JobSlips.CountAsync(s => s.Status == "Issued");
        ViewBag.InProcessSlips = await _context.JobSlips.CountAsync(s => s.Status == "In Process");
        ViewBag.ReceivedSlips = await _context.JobSlips.CountAsync(s => s.Status == "Received");

        return View(slips);
    }

    // View Job Slip details
    public async Task<IActionResult> Details(int id)
    {
        var slip = await _jobSlipService.GetJobSlipByIdAsync(id);
        if (slip == null) return NotFound();

        return View(slip);
    }

    // Print single Job Slip
    public async Task<IActionResult> Print(int id)
    {
        var slip = await _jobSlipService.GetJobSlipByIdAsync(id);
        if (slip == null) return NotFound();

        return View(slip);
    }

    // Print all Job Slips for a Production Order Lot
    public async Task<IActionResult> PrintAll(int lotId)
    {
        var slips = await _jobSlipService.GetJobSlipsForOrderAsync(lotId);
        if (!slips.Any())
        {
            // Try generating first if not yet created
            slips = await _jobSlipService.GenerateJobSlipsForOrderAsync(lotId, User.Identity?.Name);
        }

        if (!slips.Any())
        {
            TempData["Error"] = "No external vendor job slips found for this lot. Job slips are only generated for processes assigned to outside vendors.";
            return RedirectToAction("Edit", "Production", new { id = lotId });
        }

        ViewBag.ProductionOrder = await _context.ProductionOrders
            .Include(p => p.Design)
            .FirstOrDefaultAsync(p => p.Id == lotId);

        return View("PrintAll", slips);
    }

    // Regenerate job slips for a lot
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Regenerate(int lotId, string? returnUrl = null)
    {
        var slips = await _jobSlipService.GenerateJobSlipsForOrderAsync(lotId, User.Identity?.Name);
        TempData["Success"] = $"Successfully generated/synchronized {slips.Count} job slip(s) for this lot.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Edit", "Production", new { id = lotId });
    }

    // Update job slip status (Issued -> In Process -> Received -> Cancelled)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, DateTime? receivedDate, string? remarks, string? returnUrl = null)
    {
        var success = await _jobSlipService.UpdateJobSlipStatusAsync(id, status, receivedDate, remarks);
        if (success)
        {
            TempData["Success"] = $"Job Slip status updated to '{status}'.";
        }
        else
        {
            TempData["Error"] = "Failed to update Job Slip.";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}
