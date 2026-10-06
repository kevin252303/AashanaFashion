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
    private readonly IConfiguration _config;
    private readonly ICompanyContext _companyContext;
    private readonly IPricelistService _pricelistService;

    public SalesOrderController(
        AppDbContext context,
        IEwayBillService ewayBillService,
        IConfiguration config,
        ICompanyContext companyContext,
        IPricelistService pricelistService)
    {
        _context = context;
        _ewayBillService = ewayBillService;
        _config = config;
        _companyContext = companyContext;
        _pricelistService = pricelistService;
    }

    public async Task<IActionResult> Index(string? search, SalesOrderStatus? status)
    {
        var activeCompany = await _companyContext.GetActiveCompanyAsync();

        var query = _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.Details)
            .Include(s => s.Challans)
            .Where(s => s.CompanyId == activeCompany.Id)
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
        ViewBag.ActiveCompanyName = activeCompany.CompanyName;
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var activeCompany = await _companyContext.GetActiveCompanyAsync();

        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.Designs = await _context.Designs.Include(d => d.ColourImages).Include(d => d.DiscontinuedVariants).Where(d => d.IsActive && (d.CompanyId == null || d.CompanyId == activeCompany.Id)).OrderBy(d => d.DesignNumber).ToListAsync();
        ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
        ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
        ViewBag.Pricelists = await _context.Pricelists.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
        
        var nextSoNumber = await GenerateSoNumber(activeCompany);
        ViewBag.NextSoNumber = nextSoNumber;

        return View(new SalesOrderViewModel
        {
            SoNumber = nextSoNumber,
            OrderDate = DateTime.Today,
            ExpectedDeliveryDate = DateTime.Today.AddDays(14)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalesOrderViewModel model)
    {
        var activeCompany = await _companyContext.GetActiveCompanyAsync();

        await ValidateDiscontinuedStockAsync(model.Details);

        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
            ViewBag.Designs = await _context.Designs.Include(d => d.ColourImages).Include(d => d.DiscontinuedVariants).Where(d => d.IsActive && (d.CompanyId == null || d.CompanyId == activeCompany.Id)).OrderBy(d => d.DesignNumber).ToListAsync();
            ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
            ViewBag.Pricelists = await _context.Pricelists.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.SoNumber))
        {
            model.SoNumber = await GenerateSoNumber(activeCompany);
        }

        var order = new SalesOrder
        {
            CompanyId = activeCompany.Id,
            SoNumber = model.SoNumber,
            CustomerId = model.CustomerId,
            PricelistId = model.PricelistId,
            CustomerPoReference = model.CustomerPoReference,
            OrderDate = model.OrderDate,
            ExpectedDeliveryDate = model.ExpectedDeliveryDate,
            Status = model.Status,
            ShippingAddress = model.ShippingAddress,
            TransporterName = model.TransporterName,
            PaymentTerms = model.PaymentTerms,
            Notes = model.Notes,
            HasAgentDiscount = model.HasAgentDiscount,
            AgentDiscountType = model.AgentDiscountType,
            AgentDiscountRate = model.HasAgentDiscount ? Math.Max(0m, model.AgentDiscountRate) : 0m,
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

        decimal itemsSubtotal = order.Details.Sum(d => d.NetAmount);
        decimal agentDiscountAmt = 0m;
        if (order.HasAgentDiscount && order.AgentDiscountRate > 0)
        {
            if (order.AgentDiscountType == AgentDiscountType.Percentage)
            {
                agentDiscountAmt = Math.Round(itemsSubtotal * (order.AgentDiscountRate / 100m), 2);
            }
            else
            {
                agentDiscountAmt = Math.Min(itemsSubtotal, Math.Round(order.AgentDiscountRate, 2));
            }
        }
        order.AgentDiscountAmount = agentDiscountAmt;

        var effTransport = model.TransportCharge + (model.TransportCharge * model.TransportChargeGST / 100m);
        order.TotalAmount = (itemsSubtotal - agentDiscountAmt) + effTransport + model.RoundOff;

        _context.SalesOrders.Add(order);
        await _context.SaveChangesAsync();

        // Credit Limit Check & Chatter Logging
        var customer = await _context.Customers.FindAsync(order.CustomerId);
        if (customer != null && customer.PartnerLimit.HasValue && customer.PartnerLimit.Value > 0)
        {
            var unpaidTotal = await _context.TaxInvoices
                .Where(i => i.CustomerId == customer.Id && i.PaymentStatus != InvoicePaymentStatus.Paid && i.PaymentStatus != InvoicePaymentStatus.Cancelled)
                .SumAsync(i => (decimal?)(i.GrandTotal - i.PaidAmount)) ?? 0m;
            decimal totalExposure = unpaidTotal + (customer.TotalReceivable ?? 0m) + order.TotalAmount;
            if (totalExposure > customer.PartnerLimit.Value)
            {
                decimal excess = totalExposure - customer.PartnerLimit.Value;
                _context.CommunicationLogs.Add(new CommunicationLog
                {
                    DocumentType = "SalesOrder",
                    DocumentId = order.Id,
                    DocumentReference = order.SoNumber,
                    Channel = CommunicationChannel.InternalNote,
                    Recipient = "Credit & Accounts",
                    RecipientName = customer.CustomerName,
                    Subject = "Credit Limit Exceeded Warning",
                    Body = $"⚠ Credit Limit Warning: Order #{order.SoNumber} (₹{order.TotalAmount:N2}) brings customer exposure to ₹{totalExposure:N2}, exceeding the credit limit of ₹{customer.PartnerLimit.Value:N2} by ₹{excess:N2}.",
                    Status = CommunicationStatus.Sent,
                    SentAt = DateTime.Now,
                    SentBy = User.Identity?.Name ?? "System"
                });
                await _context.SaveChangesAsync();
                TempData["Warning"] = $"Sales Order '{order.SoNumber}' booked, but Customer Credit Limit is exceeded by ₹{excess:N2} (Total Exposure: ₹{totalExposure:N2} / Limit: ₹{customer.PartnerLimit.Value:N2}).";
            }
        }

        if (TempData["Warning"] == null)
        {
            TempData["Success"] = $"Sales Order '{order.SoNumber}' booked successfully.";
        }
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var order = await _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.Pricelist)
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
            PricelistId = order.PricelistId,
            PricelistName = order.Pricelist?.Name,
            CustomerPoReference = order.CustomerPoReference,
            OrderDate = order.OrderDate,
            ExpectedDeliveryDate = order.ExpectedDeliveryDate,
            Status = order.Status,
            ShippingAddress = order.ShippingAddress,
            TransporterName = order.TransporterName,
            PaymentTerms = order.PaymentTerms,
            Notes = order.Notes,
            HasAgentDiscount = order.HasAgentDiscount,
            AgentDiscountType = order.AgentDiscountType,
            AgentDiscountRate = order.AgentDiscountRate,
            AgentDiscountAmount = order.AgentDiscountAmount,
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

        var activeCompany = await _companyContext.GetActiveCompanyAsync();

        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.Designs = await _context.Designs.Include(d => d.ColourImages).Include(d => d.DiscontinuedVariants).Where(d => d.IsActive && (d.CompanyId == null || d.CompanyId == activeCompany.Id)).OrderBy(d => d.DesignNumber).ToListAsync();
        ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
        ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
        ViewBag.Pricelists = await _context.Pricelists.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SalesOrderViewModel model)
    {
        var activeCompany = await _companyContext.GetActiveCompanyAsync();

        await ValidateDiscontinuedStockAsync(model.Details);

        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
            ViewBag.Designs = await _context.Designs.Include(d => d.ColourImages).Include(d => d.DiscontinuedVariants).Where(d => d.IsActive && (d.CompanyId == null || d.CompanyId == activeCompany.Id)).OrderBy(d => d.DesignNumber).ToListAsync();
            ViewBag.Colours = await _context.Colours.Where(c => c.IsActive).OrderBy(c => c.ColourName).ToListAsync();
            ViewBag.Sizes = await _context.Sizes.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.SizeName).ToListAsync();
            ViewBag.Pricelists = await _context.Pricelists.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            return View(model);
        }

        var order = await _context.SalesOrders
            .Include(s => s.Details)
            .FirstOrDefaultAsync(s => s.Id == model.Id);

        if (order == null) return NotFound();

        order.CustomerId = model.CustomerId;
        order.PricelistId = model.PricelistId;
        order.CustomerPoReference = model.CustomerPoReference;
        order.OrderDate = model.OrderDate;
        order.ExpectedDeliveryDate = model.ExpectedDeliveryDate;
        order.Status = model.Status;
        order.ShippingAddress = model.ShippingAddress;
        order.TransporterName = model.TransporterName;
        order.PaymentTerms = model.PaymentTerms;
        order.Notes = model.Notes;
        order.HasAgentDiscount = model.HasAgentDiscount;
        order.AgentDiscountType = model.AgentDiscountType;
        order.AgentDiscountRate = model.HasAgentDiscount ? Math.Max(0m, model.AgentDiscountRate) : 0m;
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

        decimal itemsSubtotal = order.Details.Sum(d => d.NetAmount);
        decimal agentDiscountAmt = 0m;
        if (order.HasAgentDiscount && order.AgentDiscountRate > 0)
        {
            if (order.AgentDiscountType == AgentDiscountType.Percentage)
            {
                agentDiscountAmt = Math.Round(itemsSubtotal * (order.AgentDiscountRate / 100m), 2);
            }
            else
            {
                agentDiscountAmt = Math.Min(itemsSubtotal, Math.Round(order.AgentDiscountRate, 2));
            }
        }
        order.AgentDiscountAmount = agentDiscountAmt;

        var effTransport = model.TransportCharge + (model.TransportCharge * model.TransportChargeGST / 100m);
        order.TotalAmount = (itemsSubtotal - agentDiscountAmt) + effTransport + model.RoundOff;

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Sales Order '{order.SoNumber}' updated.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.Pricelist)
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

                // Deduct from Ready Product Inventory (Outward Sales Dispatch)
                var readyProduct = await _context.ReadyProducts
                    .FirstOrDefaultAsync(r => 
                        r.CompanyId == order.CompanyId &&
                        ((orderDetail.DesignId.HasValue && r.DesignId == orderDetail.DesignId.Value && r.Colour == orderDetail.Colour && r.Size == orderDetail.Size) ||
                        (r.DesignNumber == orderDetail.DesignNumber && r.Colour == orderDetail.Colour && r.Size == orderDetail.Size)));

                if (readyProduct != null)
                {
                    readyProduct.QuantityOnHand -= item.DispatchQuantity;
                    readyProduct.UpdatedDate = DateTime.Now;

                    var readyTx = new ReadyProductTransaction
                    {
                        CompanyId = order.CompanyId,
                        ReadyProductId = readyProduct.Id,
                        TransactionType = ReadyProductTransactionType.OutwardSales,
                        Quantity = -item.DispatchQuantity,
                        BalanceAfter = readyProduct.QuantityOnHand,
                        ReferenceType = "Delivery Challan",
                        ReferenceNumber = challan.ChallanNumber,
                        Notes = $"Dispatched {item.DispatchQuantity} ready set(s) against Sales Order #{order.SoNumber} via Challan #{challan.ChallanNumber}",
                        CreatedBy = User.Identity?.Name ?? "Staff",
                        CreatedDate = DateTime.Now
                    };
                    _context.ReadyProductTransactions.Add(readyTx);
                }
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

        // Automated Chatter & Activity Tracking
        var loggedUser = User.Identity?.Name ?? "User";
        var custName = challan.Customer?.CustomerName ?? order.Customer?.CustomerName ?? "Customer";
        _context.CommunicationLogs.Add(new CommunicationLog
        {
            DocumentType = "SalesOrder",
            DocumentId = order.Id,
            DocumentReference = order.SoNumber,
            Channel = CommunicationChannel.InternalNote,
            Recipient = "Dispatch Desk",
            RecipientName = custName,
            Subject = $"Delivery Challan #{challan.ChallanNumber} Generated",
            Body = $"Generated delivery challan #{challan.ChallanNumber} for {challan.Items.Sum(i => i.QuantityDispatched)} pcs. Transporter: {challan.TransporterName ?? "Direct Vehicle"}, Vehicle: {challan.VehicleNumber ?? "N/A"}, LR: {challan.LrNumber ?? "N/A"}.",
            Status = CommunicationStatus.Sent,
            SentAt = DateTime.Now,
            SentBy = loggedUser
        });

        _context.CommunicationLogs.Add(new CommunicationLog
        {
            DocumentType = "DeliveryChallan",
            DocumentId = challan.Id,
            DocumentReference = challan.ChallanNumber,
            Channel = CommunicationChannel.InternalNote,
            Recipient = "Dispatch Desk",
            RecipientName = custName,
            Subject = "Delivery Challan Issued",
            Body = $"Challan #{challan.ChallanNumber} created against {order.SoNumber}. Boxes: {challan.NumberOfBoxes}, Dispatched: {challan.Items.Sum(i => i.QuantityDispatched)} pcs.",
            Status = CommunicationStatus.Sent,
            SentAt = DateTime.Now,
            SentBy = loggedUser
        });

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

        // Credit Limits & Outstanding Exposure
        var unpaidInvoices = await _context.TaxInvoices
            .Where(i => i.CustomerId == id && i.PaymentStatus != InvoicePaymentStatus.Paid && i.PaymentStatus != InvoicePaymentStatus.Cancelled)
            .ToListAsync();
        decimal invoiceOutstanding = unpaidInvoices.Sum(i => i.BalanceDue);
        decimal openingReceivable = customer.TotalReceivable ?? 0m;
        decimal totalOutstanding = invoiceOutstanding + openingReceivable;
        decimal creditLimit = customer.PartnerLimit ?? 0m;
        decimal availableCredit = creditLimit > 0 ? (creditLimit - totalOutstanding) : 0m;
        bool hasCreditLimit = creditLimit > 0;
        bool isOverLimit = hasCreditLimit && totalOutstanding > creditLimit;

        return Json(new
        {
            shippingAddress = string.Join(", ", addressParts),
            transporter = customer.Transporter ?? customer.DeliveryMethod ?? "",
            paymentTerms = customer.SalesPaymentTerms ?? "",
            gstNumber = customer.GstNumber ?? "",
            phone = customer.Phone ?? "",
            contactPerson = customer.ContactPerson ?? "",
            pricelistId = customer.PricelistId,
            pricelistName = customer.Pricelist ?? "",
            creditLimit = creditLimit,
            currentOutstanding = totalOutstanding,
            availableCredit = availableCredit,
            hasCreditLimit = hasCreditLimit,
            isOverLimit = isOverLimit,
            creditPeriodDays = customer.DaysSalesOutstanding ?? 15
        });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ScanBarcode([FromBody] BarcodeScanOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Code))
        {
            return Json(new { success = false, message = "Please scan or enter a barcode / SKU." });
        }

        var code = request.Code.Trim();

        // 1. Check ReadyProducts (SKU or Barcode)
        var rp = await _context.ReadyProducts
            .Include(r => r.Design)
            .FirstOrDefaultAsync(r => r.Barcode == code || r.Sku == code);

        Design? design = rp?.Design;
        string? colour = rp?.Colour;
        string? size = rp?.Size;
        int stockAvailable = rp?.AvailableQuantity ?? 0;
        string foundType = "Ready Product";

        // 2. If not found, check ProductionEntities (Garment Piece Tag)
        if (design == null)
        {
            var entity = await _context.ProductionEntities
                .Include(e => e.ProductionOrder)
                    .ThenInclude(p => p!.Design)
                .FirstOrDefaultAsync(e => e.Barcode == code || e.Id.ToString() == code);

            if (entity?.ProductionOrder?.Design != null)
            {
                design = entity.ProductionOrder.Design;
                colour = entity.Colour;
                size = entity.Size;
                foundType = "Garment Piece Tag";
            }
        }

        // 3. If not found, check Designs directly (Design Number or CommonDNo)
        if (design == null)
        {
            design = await _context.Designs
                .FirstOrDefaultAsync(d => d.DesignNumber == code || (d.CommonDNo != null && d.CommonDNo == code));

            if (design != null)
            {
                foundType = "Design Master";
            }
        }

        // 4. If not found, check ProductionOrders by Lot No
        if (design == null)
        {
            var lotNo = code.StartsWith("LOT-", StringComparison.OrdinalIgnoreCase) ? code.Substring(4) : code;
            var po = await _context.ProductionOrders
                .Include(p => p.Design)
                .FirstOrDefaultAsync(p => p.LotNo.ToLower() == lotNo.ToLower());

            if (po?.Design != null)
            {
                design = po.Design;
                foundType = "Lot / Batch";
            }
        }

        if (design == null)
        {
            return Json(new { success = false, message = $"Barcode / SKU '{code}' not recognized in system." });
        }

        // Calculate rate & discount using active pricelist
        decimal unitPrice = design.SalesPrice;
        decimal discountPct = 0;
        string? ruleDesc = null;

        try
        {
            PricelistCalculationResult plResult;
            if (request.PricelistId.HasValue && request.PricelistId.Value > 0)
            {
                plResult = await _pricelistService.CalculatePriceAsync(
                    request.PricelistId.Value, design.Id, request.Quantity > 0 ? request.Quantity : 1, DateTime.Today, colour, size);
                unitPrice = plResult.UnitPrice;
                discountPct = plResult.DiscountPercentage;
                ruleDesc = plResult.AppliedRuleDescription;
            }
            else if (request.CustomerId.HasValue && request.CustomerId.Value > 0)
            {
                plResult = await _pricelistService.CalculateCustomerPriceAsync(
                    request.CustomerId.Value, design.Id, request.Quantity > 0 ? request.Quantity : 1, DateTime.Today, colour, size);
                unitPrice = plResult.UnitPrice;
                discountPct = plResult.DiscountPercentage;
                ruleDesc = plResult.AppliedRuleDescription;
            }
        }
        catch
        {
            unitPrice = design.SalesPrice;
        }

        // Parse default GST
        decimal gstPct = 12.0m;
        if (!string.IsNullOrWhiteSpace(design.SalesTaxes))
        {
            var match = System.Text.RegularExpressions.Regex.Match(design.SalesTaxes, @"\d+(\.\d+)?");
            if (match.Success && decimal.TryParse(match.Value, out var g))
            {
                gstPct = g;
            }
        }

        // If colour or size is null, pick first available from design
        var coloursList = (design.Colours ?? "")
            .Split(new[] { ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim())
            .ToList();

        var sizesList = (design.Sizes ?? "")
            .Split(new[] { ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();

        if (string.IsNullOrEmpty(colour) && coloursList.Any())
        {
            colour = coloursList.First();
        }

        if (string.IsNullOrEmpty(size) && sizesList.Any())
        {
            size = sizesList.First();
        }

        return Json(new
        {
            success = true,
            foundType = foundType,
            designId = design.Id,
            designNumber = design.DesignNumber,
            colour = colour ?? "",
            size = size ?? "",
            unitPrice = unitPrice,
            discountPercentage = discountPct,
            gstPercentage = gstPct,
            availableStock = stockAvailable,
            discontinued = design.Discontinued,
            appliedRule = ruleDesc,
            message = $"Found {foundType}: {design.DesignNumber}" +
                      (!string.IsNullOrEmpty(colour) ? $" ({colour} / {size})" : "") +
                      (stockAvailable > 0 ? $" · {stockAvailable} pcs in stock" : "")
        });
    }

    private async Task<string> GenerateSoNumber(Company? company = null)
    {
        company ??= await _companyContext.GetActiveCompanyAsync();
        var year = DateTime.Now.Year;
        var prefix = !string.IsNullOrWhiteSpace(company.SalesOrderPrefix) ? company.SalesOrderPrefix : $"SO-{year}-";
        var count = await _context.SalesOrders.CountAsync(s => s.CompanyId == company.Id && s.SoNumber.StartsWith(prefix));
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

    [HttpGet]
    public async Task<IActionResult> PrintChallanEwayBill(int id)
    {
        var challan = await _context.DeliveryChallans
            .Include(c => c.Customer)
            .Include(c => c.SalesOrder)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (challan == null) return NotFound();

        var company = challan.SalesOrder != null 
            ? await _context.Companies.FindAsync(challan.SalesOrder.CompanyId)
            : await _companyContext.GetActiveCompanyAsync();
        company ??= await _companyContext.GetActiveCompanyAsync();

        string fromGstin = !string.IsNullOrWhiteSpace(company.Gstin) ? company.Gstin : (_config["Company:GSTIN"] ?? "24AABCA1234F1Z9");
        string fromName = !string.IsNullOrWhiteSpace(company.CompanyName) ? company.CompanyName : (_config["Company:Name"] ?? "AASHANA FASHION");
        string fromAddr1 = !string.IsNullOrWhiteSpace(company.Address1) ? company.Address1 : (_config["Company:Address1"] ?? "Plot 14-16, Garment Industrial Zone");
        string fromAddr2 = company.Address2 ?? (_config["Company:Address2"] ?? "Pandesara");
        string fromPlace = !string.IsNullOrWhiteSpace(company.City) ? company.City : (_config["Company:City"] ?? "Surat");
        string fromState = !string.IsNullOrWhiteSpace(company.State) ? company.State : (_config["Company:State"] ?? "Gujarat");
        int fromPin = int.TryParse(company.PinCode, out var pinVal) ? pinVal : (int.TryParse(_config["Company:Pincode"], out var pVal) ? pVal : 394221);
        int fromStateCode = company.StateCode > 0 ? company.StateCode : (int.TryParse(_config["Company:StateCode"], out var scV) ? scV : 24);
        string fromAddr = $"{fromAddr1}, {fromAddr2}, {fromPlace}, {fromState} - {fromPin}";

        string toGstin = !string.IsNullOrWhiteSpace(challan.Customer?.GstNumber) && challan.Customer.GstNumber.Length == 15
            ? challan.Customer.GstNumber.Trim().ToUpper()
            : "URP";
        string toName = challan.Customer?.CustomerName ?? "Consignee";
        string toAddr = challan.ShippingAddress ?? (challan.Customer?.Address ?? "Surat, Gujarat");
        string toPlace = challan.Customer?.City ?? "Surat";
        int toPin = _ewayBillService.ExtractPincode(challan.Customer?.PinCode ?? toAddr, 395002);
        int toState = _ewayBillService.GetStateCode(toGstin, challan.Customer?.State);

        var ewbNo = !string.IsNullOrWhiteSpace(challan.EwayBillNumber)
            ? challan.EwayBillNumber
            : $"2410{DateTime.Now:yyMMdd}{challan.Id:D4}";

        var genDate = challan.EwayBillDate ?? challan.ChallanDate;
        var dist = challan.DistanceKm ?? 50;
        var days = Math.Max(1, (int)Math.Ceiling(dist / 200.0));
        var validUntil = genDate.AddDays(days).Date.AddHours(23).AddMinutes(59);

        // Estimate challan goods value for Job Work (e.g. ₹500/piece default)
        decimal totalPcs = challan.Items.Sum(i => i.QuantityDispatched);
        decimal taxableVal = totalPcs * 500m;
        bool isInter = (toState != 24);
        decimal cgst = isInter ? 0m : Math.Round(taxableVal * 0.025m, 2);
        decimal sgst = isInter ? 0m : Math.Round(taxableVal * 0.025m, 2);
        decimal igst = isInter ? Math.Round(taxableVal * 0.05m, 2) : 0m;
        decimal totalVal = taxableVal + cgst + sgst + igst;

        var vm = new EwayBillPrintViewModel
        {
            EwayBillNumber = ewbNo,
            EwayBillDate = genDate,
            ValidFrom = genDate,
            ValidUntil = validUntil,
            GeneratedBy = $"{fromGstin} - {fromName}",
            SupplierGstin = fromGstin,
            SupplierName = fromName,
            DispatchAddress = fromAddr,
            DispatchPlace = fromPlace,
            DispatchPincode = fromPin,
            DispatchStateCode = fromStateCode,

            RecipientGstin = toGstin,
            RecipientName = toName,
            DeliveryAddress = toAddr,
            DeliveryPlace = toPlace,
            DeliveryPincode = toPin,
            DeliveryStateCode = toState,

            DocType = "Delivery Challan",
            DocCode = "CHL",
            DocNumber = challan.ChallanNumber,
            DocDate = challan.ChallanDate,
            SupplyType = "Outward - Job Work",
            TransactionType = "Regular",
            ReasonForTransportation = "Job Work / Delivery Challan",

            TaxableAmount = taxableVal,
            CgstAmount = cgst,
            SgstAmount = sgst,
            IgstAmount = igst,
            CessAmount = 0m,
            TotalInvoiceValue = totalVal,

            TransMode = challan.TransMode switch { "2" => "Rail", "3" => "Air", "4" => "Ship", _ => "Road" },
            VehicleNumber = !string.IsNullOrWhiteSpace(challan.VehicleNumber) ? challan.VehicleNumber : "GJ-05-BX-1234",
            VehicleType = challan.VehicleType == "O" ? "Over Dimensional Cargo" : "Regular",
            TransporterName = !string.IsNullOrWhiteSpace(challan.TransporterName) ? challan.TransporterName : "Direct Transport",
            TransporterId = challan.TransporterId ?? "",
            TransDocNo = challan.LrNumber ?? "",
            TransDocDate = challan.ChallanDate,
            DistanceKm = dist,
            FromPlace = "Surat, Gujarat",

            SourceId = challan.Id,
            SourceType = "Challan"
        };

        int sr = 1;
        foreach (var it in challan.Items)
        {
            decimal itemVal = it.QuantityDispatched * 500m;
            vm.Items.Add(new EwayBillPrintItemViewModel
            {
                ItemNo = sr++,
                HsnCode = "6204",
                ProductName = $"Design {it.DesignNumber}",
                Description = $"{it.Colour} {it.Size}".Trim(),
                Quantity = it.QuantityDispatched,
                Unit = "PCS",
                TaxableValue = itemVal,
                CgstRate = isInter ? 0 : 2.5m,
                SgstRate = isInter ? 0 : 2.5m,
                IgstRate = isInter ? 5m : 0
            });
        }

        return View("~/Views/Shared/PrintEwayBill.cshtml", vm);
    }

    private async Task ValidateDiscontinuedStockAsync(List<SalesOrderDetailViewModel> details)
    {
        if (details == null || !details.Any()) return;

        var activeDetails = details.Where(x => x.Quantity > 0).ToList();
        var designIds = activeDetails.Where(x => x.DesignId.HasValue && x.DesignId.Value > 0).Select(x => x.DesignId!.Value).Distinct().ToList();
        var designNumbers = activeDetails.Where(x => !x.DesignId.HasValue || x.DesignId.Value == 0).Select(x => x.DesignNumber).Distinct().ToList();

        var designs = await _context.Designs
            .Include(d => d.DiscontinuedVariants)
            .Where(d => designIds.Contains(d.Id) || designNumbers.Contains(d.DesignNumber))
            .ToListAsync();

        foreach (var d in activeDetails)
        {
            var design = designs.FirstOrDefault(x => (d.DesignId.HasValue && x.Id == d.DesignId.Value) || x.DesignNumber.Equals(d.DesignNumber, StringComparison.OrdinalIgnoreCase));
            if (design == null) continue;

            bool isWholeDiscontinued = design.Discontinued;
            bool isVariantDiscontinued = design.IsVariantDiscontinued(d.Colour, d.Size);

            if (isWholeDiscontinued || isVariantDiscontinued)
            {
                var col = d.Colour?.Trim() ?? "";
                var sz = d.Size?.Trim() ?? "";

                // Find ready product stock
                var readyProduct = await _context.ReadyProducts
                    .FirstOrDefaultAsync(rp => rp.DesignId == design.Id
                        && rp.Colour.ToLower() == col.ToLower()
                        && rp.Size.ToLower() == sz.ToLower()
                        && rp.IsActive);

                int stockOnHand = readyProduct?.QuantityOnHand ?? 0;
                string label = $"{design.DesignNumber} ({col} / {sz})";

                if (stockOnHand <= 0)
                {
                    ModelState.AddModelError("", $"'{label}' is discontinued and out of stock (0 pcs on hand). No new orders can be created for discontinued items with zero stock.");
                }
                else if (d.Quantity > stockOnHand)
                {
                    ModelState.AddModelError("", $"'{label}' is discontinued. Only {stockOnHand} pcs available in stock. Order quantity ({d.Quantity}) cannot exceed stock on hand.");
                }
            }
        }
    }

    [HttpGet]
    public async Task<IActionResult> CheckItemStockAndDiscontinued(int designId, string? colour, string? size)
    {
        var design = await _context.Designs
            .Include(d => d.DiscontinuedVariants)
            .FirstOrDefaultAsync(d => d.Id == designId);

        if (design == null) return Json(new { success = false, message = "Design not found" });

        var col = colour?.Trim() ?? "";
        var sz = size?.Trim() ?? "";

        bool isDiscontinued = design.Discontinued || design.IsVariantDiscontinued(col, sz);

        var readyProduct = await _context.ReadyProducts
            .FirstOrDefaultAsync(rp => rp.DesignId == designId
                && rp.Colour.ToLower() == col.ToLower()
                && rp.Size.ToLower() == sz.ToLower()
                && rp.IsActive);

        int stockOnHand = readyProduct?.QuantityOnHand ?? 0;
        int availableStock = readyProduct?.AvailableQuantity ?? 0;

        string message = "";
        bool canSell = true;

        if (isDiscontinued)
        {
            if (stockOnHand <= 0)
            {
                canSell = false;
                message = $"Item is discontinued and out of stock (0 pcs). No new orders can be created.";
            }
            else
            {
                canSell = true;
                message = $"Item is discontinued. Only {stockOnHand} pcs in stock available to sell.";
            }
        }

        return Json(new
        {
            success = true,
            isDiscontinued,
            isWholeDiscontinued = design.Discontinued,
            stockOnHand,
            availableStock,
            canSell,
            message
        });
    }
}
