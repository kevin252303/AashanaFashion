using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class PurchaseController : Controller
{
    private readonly AppDbContext _context;

    public PurchaseController(AppDbContext context) => _context = context;

    [PermissionAuthorize("Purchase", "CanView")]
    public async Task<IActionResult> Index()
    {
        var orders = await _context.PurchaseOrders
            .Include(p => p.Vendor)
            .Include(p => p.Details)
            .OrderByDescending(p => p.OrderDate)
            .ToListAsync();
        return View(orders);
    }

    [PermissionAuthorize("Purchase", "CanCreate")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
        ViewBag.NextPoNumber = await GeneratePoNumber();
        return View(new PurchaseOrderViewModel());
    }

    [PermissionAuthorize("Purchase", "CanCreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseOrderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
            ViewBag.NextPoNumber = await GeneratePoNumber();
            return View(model);
        }

        var order = new PurchaseOrder
        {
            PoNumber = model.PoNumber,
            VendorId = model.VendorId,
            AgentName = model.AgentName,
            CourierServiceName = model.CourierServiceName,
            InvoiceNumber = model.InvoiceNumber,
            OrderDate = model.OrderDate,
            ExpectedReceivingDate = model.ExpectedReceivingDate,
            Status = PurchaseOrderStatus.Pending,
            Notes = model.Notes,
            TransportCharge = model.TransportCharge,
            TransportChargeGST = model.TransportChargeGST,
            RoundOff = model.RoundOff,
            CreatedDate = DateTime.Now
        };

        int srNo = 1;
        foreach (var d in model.Details)
        {
            order.Details.Add(new PurchaseOrderDetail
            {
                SrNo = srNo++,
                ProductName = d.ProductName,
                ProductDesignNo = d.ProductDesignNo,
                HsnCode = d.HsnCode,
                Unit = d.Unit,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                GstPercentage = d.GstPercentage,
                DiscountPercentage = d.DiscountPercentage
            });
        }

        var effectiveTransport = model.TransportCharge + (model.TransportCharge * model.TransportChargeGST / 100m);
        order.TotalAmount = order.Details.Sum(d => d.NetAmount) + effectiveTransport + order.RoundOff;

        _context.PurchaseOrders.Add(order);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Purchase Order '{order.PoNumber}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("Purchase", "CanEdit")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var order = await _context.PurchaseOrders
            .Include(p => p.Vendor)
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (order == null) return NotFound();

        if (order.Status == PurchaseOrderStatus.Received)
        {
            TempData["Error"] = "Received Purchase Orders cannot be edited.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        var model = new PurchaseOrderViewModel
        {
            Id = order.Id,
            PoNumber = order.PoNumber,
            VendorId = order.VendorId,
            AgentName = order.AgentName,
            CourierServiceName = order.CourierServiceName,
            InvoiceNumber = order.InvoiceNumber,
            OrderDate = order.OrderDate,
            ExpectedReceivingDate = order.ExpectedReceivingDate,
            Status = order.Status,
            ReceivedOnDate = order.ReceivedOnDate,
            Notes = order.Notes,
            TransportCharge = order.TransportCharge,
            TransportChargeGST = order.TransportChargeGST,
            RoundOff = order.RoundOff,
            TotalAmount = order.TotalAmount,
            Details = order.Details.Select(d => new PurchaseOrderDetailViewModel
            {
                Id = d.Id,
                SrNo = d.SrNo,
                ProductName = d.ProductName,
                ProductDesignNo = d.ProductDesignNo,
                HsnCode = d.HsnCode,
                Unit = d.Unit,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                GstPercentage = d.GstPercentage,
                DiscountPercentage = d.DiscountPercentage,
                ReceivedQuantity = d.ReceivedQuantity
            }).ToList()
        };

        ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
        return View(model);
    }

    [PermissionAuthorize("Purchase", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PurchaseOrderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
            return View(model);
        }

        var order = await _context.PurchaseOrders
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.Id == model.Id);

        if (order == null) return NotFound();

        if (order.Status == PurchaseOrderStatus.Received)
        {
            TempData["Error"] = "Received Purchase Orders cannot be edited.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        order.PoNumber = model.PoNumber;
        order.VendorId = model.VendorId;
        order.AgentName = model.AgentName;
        order.CourierServiceName = model.CourierServiceName;
        order.InvoiceNumber = model.InvoiceNumber;
        order.OrderDate = model.OrderDate;
        order.ExpectedReceivingDate = model.ExpectedReceivingDate;
        order.Notes = model.Notes;
        order.TransportCharge = model.TransportCharge;
        order.TransportChargeGST = model.TransportChargeGST;
        order.RoundOff = model.RoundOff;

        var receivedQtyMap = order.Details.ToDictionary(d => d.Id, d => d.ReceivedQuantity);
        _context.PurchaseOrderDetails.RemoveRange(order.Details);
        order.Details.Clear();

        int srNo = 1;
        foreach (var d in model.Details)
        {
            var newDetail = new PurchaseOrderDetail
            {
                SrNo = srNo++,
                ProductName = d.ProductName,
                ProductDesignNo = d.ProductDesignNo,
                HsnCode = d.HsnCode,
                Unit = d.Unit,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                GstPercentage = d.GstPercentage,
                DiscountPercentage = d.DiscountPercentage
            };
            if (d.Id > 0 && receivedQtyMap.TryGetValue(d.Id, out var rq))
                newDetail.ReceivedQuantity = rq;
            order.Details.Add(newDetail);
        }

        var effectiveTransport = order.TransportCharge + (order.TransportCharge * order.TransportChargeGST / 100m);
        order.TotalAmount = order.Details.Sum(d => d.NetAmount) + effectiveTransport + order.RoundOff;

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Purchase Order '{order.PoNumber}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("Purchase", "CanView")]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.PurchaseOrders
            .Include(p => p.Vendor)
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (order == null) return NotFound();
        return View(order);
    }

    [PermissionAuthorize("Purchase", "CanView")]
    public async Task<IActionResult> Print(int id)
    {
        var order = await _context.PurchaseOrders
            .Include(p => p.Vendor)
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (order == null) return NotFound();
        return View(order);
    }

    [PermissionAuthorize("Purchase", "CanDelete")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _context.PurchaseOrders.Include(p => p.Details).FirstOrDefaultAsync(p => p.Id == id);
        if (order != null)
        {
            _context.PurchaseOrderDetails.RemoveRange(order.Details);
            _context.PurchaseOrders.Remove(order);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Purchase Order '{order.PoNumber}' deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("Purchase", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, PurchaseOrderStatus status, List<PurchaseOrderDetail>? details, DateTime? receivedOnDate)
    {
        var order = await _context.PurchaseOrders
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (order == null) return NotFound();

        if (order.Status == PurchaseOrderStatus.Received)
        {
            TempData["Error"] = "Received Purchase Orders cannot be modified.";
            return RedirectToAction(nameof(Details), new { id });
        }

        order.Status = status;

        if (status == PurchaseOrderStatus.Received)
        {
            foreach (var d in order.Details)
                d.ReceivedQuantity = d.Quantity;
            
            order.ReceivedOnDate = receivedOnDate ?? DateTime.Today;
        }
        else if (status == PurchaseOrderStatus.PartiallyReceived && details?.Any() == true)
        {
            foreach (var d in details)
            {
                var detail = order.Details.FirstOrDefault(x => x.Id == d.Id);
                if (detail != null)
                    detail.ReceivedQuantity = d.ReceivedQuantity;
            }

            // Auto-promote status to Received if all items are fully received
            if (order.Details.All(d => d.ReceivedQuantity >= d.Quantity))
            {
                order.Status = PurchaseOrderStatus.Received;
            }

            order.ReceivedOnDate = receivedOnDate ?? DateTime.Today;
        }
        else
        {
            order.ReceivedOnDate = null;
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Purchase Order '{order.PoNumber}' status updated to {order.Status}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetVendorDetails(int id)
    {
        var vendor = await _context.Vendors.FindAsync(id);
        if (vendor == null) return NotFound();
        return Json(new
        {
            vendorName = vendor.VendorName,
            gstNumber = vendor.GstNumber ?? "",
            phone = vendor.Phone ?? "",
            email = vendor.Email ?? "",
            address = vendor.Address ?? "",
            city = vendor.City ?? "",
            state = vendor.State ?? "",
            pinCode = vendor.PinCode ?? ""
        });
    }

    private async Task<string> GeneratePoNumber()
    {
        var lastPo = await _context.PurchaseOrders
            .OrderByDescending(p => p.Id)
            .Select(p => p.PoNumber)
            .FirstOrDefaultAsync();

        if (lastPo == null) return "PO-0001";

        if (int.TryParse(lastPo.Replace("PO-", ""), out int lastNum))
            return $"PO-{(lastNum + 1):D4}";

        return "PO-0001";
    }
}
