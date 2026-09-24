using AashanaFashion.Authorization;
using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using AashanaFashion.Services;
using System.Text;

namespace AashanaFashion.Controllers;

[Authorize]
public class SalesOrderController : Controller
{
    private readonly AppDbContext _context;
    private readonly IEwayBillService _ewayBillService;

    public SalesOrderController(AppDbContext context, IEwayBillService ewayBillService)
    {
        _context = context;
        _ewayBillService = ewayBillService;
    }

    public async Task<IActionResult> Index(string? search, SalesOrderStatus? status)
    {
        var query = _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.Details)
            .Include(s => s.Challans)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var sLower = search.Trim().ToLower();
            query = query.Where(s => s.SoNumber.ToLower().Contains(sLower) ||
                                     s.Customer!.CustomerName.ToLower().Contains(sLower) ||
                                     (s.CustomerPoReference != null && s.CustomerPoReference.ToLower().Contains(sLower)));
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var orders = await query.OrderByDescending(s => s.OrderDate).ToListAsync();
        ViewBag.Search = search;
        ViewBag.SelectedStatus = status;
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.Designs = await _context.Designs.Where(d => d.IsActive).OrderBy(d => d.DesignNumber).ToListAsync();
        ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
        ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
        ViewBag.NextSoNumber = await GenerateSoNumber();

        return View(new SalesOrderViewModel
        {
            SoNumber = await GenerateSoNumber(),
            OrderDate = DateTime.Today,
            ExpectedDeliveryDate = DateTime.Today.AddDays(14)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalesOrderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
            ViewBag.Designs = await _context.Designs.Where(d => d.IsActive).OrderBy(d => d.DesignNumber).ToListAsync();
            ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
            return View(model);
        }

        var order = new SalesOrder
        {
            SoNumber = model.SoNumber,
            CustomerId = model.CustomerId,
            CustomerPoReference = model.CustomerPoReference,
            OrderDate = model.OrderDate,
            ExpectedDeliveryDate = model.ExpectedDeliveryDate,
            Status = model.Status,
            ShippingAddress = model.ShippingAddress,
            TransporterName = model.TransporterName,
            PaymentTerms = model.PaymentTerms,
            Notes = model.Notes,
            TransportCharge = model.TransportCharge,
            TransportChargeGST = model.TransportChargeGST,
            RoundOff = model.RoundOff,
            CreatedDate = DateTime.Now
        };

        int sr = 1;
        foreach (var d in model.Details.Where(x => x.Quantity > 0))
        {
            order.Details.Add(new SalesOrderDetail
            {
                SrNo = sr++,
                DesignId = d.DesignId > 0 ? d.DesignId : null,
                DesignNumber = d.DesignNumber,
                Colour = d.Colour,
                Size = d.Size,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                DiscountPercentage = d.DiscountPercentage,
                GstPercentage = d.GstPercentage
            });
        }

        var effTransport = model.TransportCharge + (model.TransportCharge * model.TransportChargeGST / 100m);
        order.TotalAmount = order.Details.Sum(d => d.NetAmount) + effTransport + model.RoundOff;

        _context.SalesOrders.Add(order);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Sales Order '{order.SoNumber}' booked successfully.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var order = await _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.Details)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (order == null) return NotFound();

        if (order.Status == SalesOrderStatus.Dispatched)
        {
            TempData["Error"] = "Dispatched orders cannot be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var vm = new SalesOrderViewModel
        {
            Id = order.Id,
            SoNumber = order.SoNumber,
            CustomerId = order.CustomerId,
            CustomerPoReference = order.CustomerPoReference,
            OrderDate = order.OrderDate,
            ExpectedDeliveryDate = order.ExpectedDeliveryDate,
            Status = order.Status,
            ShippingAddress = order.ShippingAddress,
            TransporterName = order.TransporterName,
            PaymentTerms = order.PaymentTerms,
            Notes = order.Notes,
            TransportCharge = order.TransportCharge,
            TransportChargeGST = order.TransportChargeGST,
            RoundOff = order.RoundOff,
            TotalAmount = order.TotalAmount,
            Details = order.Details.OrderBy(d => d.SrNo).Select(d => new SalesOrderDetailViewModel
            {
                Id = d.Id,
                SrNo = d.SrNo,
                DesignId = d.DesignId,
                DesignNumber = d.DesignNumber,
                Colour = d.Colour,
                Size = d.Size,
                Quantity = d.Quantity,
                DispatchedQuantity = d.DispatchedQuantity,
                UnitPrice = d.UnitPrice,
                DiscountPercentage = d.DiscountPercentage,
                GstPercentage = d.GstPercentage
            }).ToList()
        };

        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.Designs = await _context.Designs.Where(d => d.IsActive).OrderBy(d => d.DesignNumber).ToListAsync();
        ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
        ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SalesOrderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
            ViewBag.Designs = await _context.Designs.Where(d => d.IsActive).OrderBy(d => d.DesignNumber).ToListAsync();
            ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
            return View(model);
        }

        var order = await _context.SalesOrders
            .Include(s => s.Details)
            .FirstOrDefaultAsync(s => s.Id == model.Id);

        if (order == null) return NotFound();

        order.CustomerId = model.CustomerId;
        order.CustomerPoReference = model.CustomerPoReference;
        order.OrderDate = model.OrderDate;
        order.ExpectedDeliveryDate = model.ExpectedDeliveryDate;
        order.Status = model.Status;
        order.ShippingAddress = model.ShippingAddress;
        order.TransporterName = model.TransporterName;
        order.PaymentTerms = model.PaymentTerms;
        order.Notes = model.Notes;
        order.TransportCharge = model.TransportCharge;
        order.TransportChargeGST = model.TransportChargeGST;
        order.RoundOff = model.RoundOff;

        var existingDispatchedMap = order.Details.ToDictionary(d => d.Id, d => d.DispatchedQuantity);
        _context.SalesOrderDetails.RemoveRange(order.Details);
        order.Details.Clear();

        int sr = 1;
        foreach (var d in model.Details.Where(x => x.Quantity > 0))
        {
            var newDetail = new SalesOrderDetail
            {
                SrNo = sr++,
                DesignId = d.DesignId > 0 ? d.DesignId : null,
                DesignNumber = d.DesignNumber,
                Colour = d.Colour,
                Size = d.Size,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                DiscountPercentage = d.DiscountPercentage,
                GstPercentage = d.GstPercentage
            };
            if (d.Id > 0 && existingDispatchedMap.TryGetValue(d.Id, out var dq))
            {
                newDetail.DispatchedQuantity = dq;
            }
            order.Details.Add(newDetail);
        }

        var effTransport = model.TransportCharge + (model.TransportCharge * model.TransportChargeGST / 100m);
        order.TotalAmount = order.Details.Sum(d => d.NetAmount) + effTransport + model.RoundOff;

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Sales Order '{order.SoNumber}' updated.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.Details)
            .Include(s => s.Challans)
                .ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (order == null) return NotFound();
        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> CreateChallan(int id)
    {
        var order = await _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.Details)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (order == null) return NotFound();

        var pendingItems = order.Details.Where(d => d.PendingQuantity > 0).ToList();
        if (!pendingItems.Any())
        {
            TempData["Error"] = "All items in this order have already been dispatched.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var nextChallanNumber = await GenerateChallanNumber();

        var vm = new CreateChallanViewModel
        {
            SalesOrderId = order.Id,
            SoNumber = order.SoNumber,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.CustomerName ?? "",
            ChallanNumber = nextChallanNumber,
            ChallanDate = DateTime.Now,
            TransporterName = order.TransporterName,
            ShippingAddress = order.ShippingAddress,
            Items = pendingItems.Select(d => new CreateChallanItemViewModel
            {
                SalesOrderDetailId = d.Id,
                DesignNumber = d.DesignNumber,
                Colour = d.Colour,
                Size = d.Size,
                OrderedQuantity = d.Quantity,
                PreviouslyDispatched = d.DispatchedQuantity,
                PendingQuantity = d.PendingQuantity,
                DispatchQuantity = d.PendingQuantity
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateChallan(CreateChallanViewModel model)
    {
        var order = await _context.SalesOrders
            .Include(s => s.Details)
            .FirstOrDefaultAsync(s => s.Id == model.SalesOrderId);

        if (order == null) return NotFound();

        var itemsToDispatch = model.Items.Where(i => i.DispatchQuantity > 0).ToList();
        if (!itemsToDispatch.Any())
        {
            ModelState.AddModelError("", "Please enter at least 1 item with dispatch quantity > 0.");
            return View(model);
        }

        var challan = new DeliveryChallan
        {
            ChallanNumber = model.ChallanNumber,
            SalesOrderId = order.Id,
            CustomerId = order.CustomerId,
            ChallanDate = model.ChallanDate,
            TransporterName = model.TransporterName,
            VehicleNumber = model.VehicleNumber,
            LrNumber = model.LrNumber,
            EwayBillNumber = model.EwayBillNumber,
            NumberOfBoxes = model.NumberOfBoxes,
            ShippingAddress = model.ShippingAddress,
            Notes = model.Notes,
            DispatchedBy = User.Identity?.Name ?? "Staff",
            CreatedDate = DateTime.Now
        };

        foreach (var item in itemsToDispatch)
        {
            var orderDetail = order.Details.FirstOrDefault(d => d.Id == item.SalesOrderDetailId);
            if (orderDetail != null)
            {
                orderDetail.DispatchedQuantity += item.DispatchQuantity;
                challan.Items.Add(new DeliveryChallanItem
                {
                    SalesOrderDetailId = orderDetail.Id,
                    DesignNumber = orderDetail.DesignNumber,
                    Colour = orderDetail.Colour,
                    Size = orderDetail.Size,
                    QuantityDispatched = item.DispatchQuantity,
                    Remarks = item.Remarks
                });
            }
        }

        // Update overall SalesOrder status
        if (order.Details.All(d => d.DispatchedQuantity >= d.Quantity))
        {
            order.Status = SalesOrderStatus.Dispatched;
        }
        else
        {
            order.Status = SalesOrderStatus.PartiallyDispatched;
        }

        _context.DeliveryChallans.Add(challan);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Delivery Challan '{challan.ChallanNumber}' generated successfully.";
        return RedirectToAction(nameof(PrintChallan), new { id = challan.Id });
    }

    [HttpGet]
    public async Task<IActionResult> PrintChallan(int id)
    {
        var challan = await _context.DeliveryChallans
            .Include(c => c.Customer)
            .Include(c => c.SalesOrder)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (challan == null) return NotFound();
        return View(challan);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _context.SalesOrders
            .Include(s => s.Details)
            .Include(s => s.Challans)
                .ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (order != null)
        {
            _context.SalesOrders.Remove(order);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Sales Order '{order.SoNumber}' deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomerDetails(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        var addressParts = new[] { customer.Address, customer.City, customer.State, customer.PinCode }
            .Where(p => !string.IsNullOrWhiteSpace(p));

        return Json(new
        {
            shippingAddress = string.Join(", ", addressParts),
            transporter = customer.Transporter ?? customer.DeliveryMethod ?? "",
            paymentTerms = customer.SalesPaymentTerms ?? "",
            gstNumber = customer.GstNumber ?? "",
            phone = customer.Phone ?? "",
            contactPerson = customer.ContactPerson ?? ""
        });
    }

    private async Task<string> GenerateSoNumber()
    {
        var year = DateTime.Now.Year;
        var prefix = $"SO-{year}-";
        var count = await _context.SalesOrders.CountAsync(s => s.SoNumber.StartsWith(prefix));
        return $"{prefix}{(count + 1):D4}";
    }

    private async Task<string> GenerateChallanNumber()
    {
        var year = DateTime.Now.Year;
        var prefix = $"DC-{year}-";
        var count = await _context.DeliveryChallans.CountAsync(c => c.ChallanNumber.StartsWith(prefix));
        return $"{prefix}{(count + 1):D4}";
    }

    [HttpGet]
    public async Task<IActionResult> DownloadChallanEwayJson(int id, [FromQuery] EwayBillTransportInput transport)
    {
        var challan = await _context.DeliveryChallans.FindAsync(id);
        if (challan == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(transport.VehicleNumber)) challan.VehicleNumber = transport.VehicleNumber.Trim();
        if (!string.IsNullOrWhiteSpace(transport.TransporterName)) challan.TransporterName = transport.TransporterName.Trim();
        if (!string.IsNullOrWhiteSpace(transport.TransporterId)) challan.TransporterId = transport.TransporterId.Trim();
        if (transport.DistanceKm > 0) challan.DistanceKm = transport.DistanceKm;
        if (!string.IsNullOrWhiteSpace(transport.VehicleType)) challan.VehicleType = transport.VehicleType;
        if (!string.IsNullOrWhiteSpace(transport.TransMode)) challan.TransMode = transport.TransMode;

        await _context.SaveChangesAsync();

        var json = await _ewayBillService.GenerateChallanJsonAsync(id, transport);
        var fileName = $"EWB_CHL_{challan.ChallanNumber}.json";
        return File(Encoding.UTF8.GetBytes(json), "application/json", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveChallanEwayBill(SaveEwayBillInput model)
    {
        var challan = await _context.DeliveryChallans.FindAsync(model.Id);
        if (challan == null) return NotFound();

        challan.EwayBillNumber = model.EwayBillNumber.Trim();
        challan.EwayBillDate = model.EwayBillDate ?? DateTime.Today;
        if (!string.IsNullOrWhiteSpace(model.VehicleNumber)) challan.VehicleNumber = model.VehicleNumber.Trim();
        if (!string.IsNullOrWhiteSpace(model.TransporterName)) challan.TransporterName = model.TransporterName.Trim();

        await _context.SaveChangesAsync();
        TempData["Success"] = $"E-Way Bill #{challan.EwayBillNumber} saved against Challan {challan.ChallanNumber}.";
        return RedirectToAction(nameof(PrintChallan), new { id = model.Id });
    }
}
