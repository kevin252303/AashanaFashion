using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using AashanaFashion.Services;
using System.Text;

namespace AashanaFashion.Controllers;

[Authorize]
public class InvoiceController : Controller
{
    private readonly AppDbContext _context;
    private readonly IEwayBillService _ewayBillService;
    private readonly IEInvoiceService _eInvoiceService;
    private readonly IConfiguration _config;
    private readonly ICompanyContext _companyContext;

    public InvoiceController(AppDbContext context, IEwayBillService ewayBillService, IEInvoiceService eInvoiceService, IConfiguration config, ICompanyContext companyContext)
    {
        _context = context;
        _ewayBillService = ewayBillService;
        _eInvoiceService = eInvoiceService;
        _config = config;
        _companyContext = companyContext;
    }

    public async Task<IActionResult> Index(string? search, InvoicePaymentStatus? status, string? eInvoiceStatus, bool? overdueOnly = false)
    {
        var activeCompany = await _companyContext.GetActiveCompanyAsync();
        ViewBag.ActiveCompany = activeCompany;

        var query = _context.TaxInvoices
            .Include(i => i.Customer)
            .Include(i => i.SalesOrder)
            .Include(i => i.Receipts)
            .Include(i => i.Company)
            .Where(i => i.CompanyId == activeCompany.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var sLower = search.Trim().ToLower();
            query = query.Where(i => i.InvoiceNumber.ToLower().Contains(sLower) ||
                                     i.CustomerName.ToLower().Contains(sLower) ||
                                     (i.CustomerGstin != null && i.CustomerGstin.ToLower().Contains(sLower)) ||
                                     (i.Irn != null && i.Irn.ToLower().Contains(sLower)));
        }

        if (status.HasValue)
        {
            query = query.Where(i => i.PaymentStatus == status.Value);
        }

        if (overdueOnly == true)
        {
            query = query.Where(i => i.DueDate < DateTime.Today && i.PaymentStatus != InvoicePaymentStatus.Paid && i.GrandTotal > i.PaidAmount);
        }

        if (!string.IsNullOrWhiteSpace(eInvoiceStatus))
        {
            if (eInvoiceStatus == "Generated")
                query = query.Where(i => i.EInvoiceStatus == "Generated");
            else if (eInvoiceStatus == "Pending")
                query = query.Where(i => i.EInvoiceStatus == "Not Generated" || string.IsNullOrEmpty(i.EInvoiceStatus));
            else if (eInvoiceStatus == "Cancelled")
                query = query.Where(i => i.EInvoiceStatus == "Cancelled");
        }

        var allInvoices = await _context.TaxInvoices.Where(i => i.CompanyId == activeCompany.Id).ToListAsync();
        var totalInvoiced = allInvoices.Sum(i => i.GrandTotal);
        var totalCollected = allInvoices.Sum(i => i.PaidAmount);
        var outstandingReceivables = allInvoices.Sum(i => i.BalanceDue);
        var overdueInvoices = allInvoices.Where(i => i.IsOverdue).ToList();
        var overdueCount = overdueInvoices.Count;
        var maxOverdueDays = overdueInvoices.Any() ? overdueInvoices.Max(i => i.OverdueDays) : 0;
        var avgOverdueDays = overdueInvoices.Any() ? (int)Math.Round(overdueInvoices.Average(i => i.OverdueDays)) : 0;
        var eInvoiceGeneratedCount = allInvoices.Count(i => i.EInvoiceStatus == "Generated");

        ViewBag.TotalInvoiced = totalInvoiced;
        ViewBag.TotalCollected = totalCollected;
        ViewBag.OutstandingReceivables = outstandingReceivables;
        ViewBag.OverdueCount = overdueCount;
        ViewBag.MaxOverdueDays = maxOverdueDays;
        ViewBag.AvgOverdueDays = avgOverdueDays;
        ViewBag.EInvoiceGeneratedCount = eInvoiceGeneratedCount;
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.OverdueOnly = overdueOnly == true;
        ViewBag.EInvoiceStatus = eInvoiceStatus;

        var invoices = await query.OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.Id).ToListAsync();
        return View(invoices);
    }

    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    [HttpGet]
    public async Task<IActionResult> Create(int? salesOrderId, int? deliveryChallanId)
    {
        var activeCompany = await _companyContext.GetActiveCompanyAsync();
        var model = new CreateInvoiceViewModel
        {
            InvoiceNumber = await GenerateNextInvoiceNumberAsync(activeCompany),
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(15),
            SalesOrderId = salesOrderId,
            DeliveryChallanId = deliveryChallanId,
            BankName = activeCompany.BankName ?? "HDFC Bank",
            BankAccountNumber = activeCompany.BankAccountNumber ?? "50200012345678",
            BankIfsc = activeCompany.BankIfsc ?? "HDFC0001234",
            BankBranch = activeCompany.BankBranch ?? "Surat"
        };

        DeliveryChallan? selectedChallan = null;
        if (deliveryChallanId.HasValue)
        {
            selectedChallan = await _context.DeliveryChallans
                .Include(c => c.Customer)
                .Include(c => c.Items)
                .Include(c => c.SalesOrder)
                .ThenInclude(s => s.Details)
                .ThenInclude(d => d.Design)
                .FirstOrDefaultAsync(c => c.Id == deliveryChallanId.Value);

            if (selectedChallan != null)
            {
                salesOrderId = selectedChallan.SalesOrderId;
                model.SalesOrderId = salesOrderId;
                model.DeliveryChallanId = selectedChallan.Id;
                model.DeliveryChallanNumber = selectedChallan.ChallanNumber;
            }
        }

        if (salesOrderId.HasValue)
        {
            var so = await _context.SalesOrders
                .Include(s => s.Customer)
                .Include(s => s.Details)
                .ThenInclude(d => d.Design)
                .FirstOrDefaultAsync(s => s.Id == salesOrderId.Value);

            if (so != null && so.Customer != null)
            {
                model.CustomerId = so.CustomerId;
                model.CustomerName = so.Customer.CustomerName;
                model.CustomerGstin = so.Customer.GstNumber;
                model.CustomerPan = so.Customer.PanNumber;
                model.BillingAddress = so.Customer.Address;
                model.ShippingAddress = so.ShippingAddress ?? so.Customer.Address;

                string custState = so.Customer.State ?? "";
                bool isInterState = !string.IsNullOrEmpty(custState) && 
                                    !custState.ToLower().Contains("gujarat") && 
                                    !custState.Contains("24");

                model.IsInterState = isInterState;
                model.PlaceOfSupply = !string.IsNullOrEmpty(custState) ? custState : "Gujarat (24)";

                if (isInterState)
                {
                    model.IgstRate = 5.0m;
                    model.CgstRate = 0m;
                    model.SgstRate = 0m;
                }
                else
                {
                    model.CgstRate = 2.5m;
                    model.SgstRate = 2.5m;
                    model.IgstRate = 5.0m;
                }

                // Query all delivery challans for this order
                var orderChallans = await _context.DeliveryChallans
                    .Include(c => c.Items)
                    .Where(c => c.SalesOrderId == so.Id)
                    .OrderByDescending(c => c.ChallanDate)
                    .ToListAsync();

                // Query existing invoices to calculate already invoiced quantities
                var existingInvoices = await _context.TaxInvoices
                    .Include(i => i.Items)
                    .Where(i => i.SalesOrderId == so.Id && i.PaymentStatus != InvoicePaymentStatus.Cancelled)
                    .ToListAsync();


                var activeChallan = selectedChallan ?? orderChallans.FirstOrDefault();
                if (activeChallan != null)
                {
                    model.EwayBillNumber = activeChallan.EwayBillNumber;
                    model.TransporterName = activeChallan.TransporterName;
                    model.VehicleNumber = activeChallan.VehicleNumber;
                    model.LrNumber = activeChallan.LrNumber;
                    if (!string.IsNullOrWhiteSpace(activeChallan.ShippingAddress))
                    {
                        model.ShippingAddress = activeChallan.ShippingAddress;
                    }
                }

                if (selectedChallan != null)
                {
                    // Specific Delivery Challan billing
                    model.Notes = $"Billed against Delivery Challan #{selectedChallan.ChallanNumber} (Dated: {selectedChallan.ChallanDate:dd/MM/yyyy}) for SO #{so.SoNumber}";
                    foreach (var chItem in selectedChallan.Items)
                    {
                        var d = so.Details.FirstOrDefault(x => 
                            (chItem.SalesOrderDetailId.HasValue && x.Id == chItem.SalesOrderDetailId.Value) ||
                            (x.DesignNumber == chItem.DesignNumber && x.Colour == chItem.Colour && x.Size == chItem.Size));

                        int orderedQty = d?.Quantity ?? chItem.QuantityDispatched;
                        int deliveredQty = chItem.QuantityDispatched;
                        decimal unitPrice = d?.UnitPrice ?? 0m;
                        decimal discPct = d?.DiscountPercentage ?? 0m;
                        decimal gstPct = d?.GstPercentage > 0 ? d.GstPercentage : 5.0m;

                        model.Items.Add(new InvoiceItemInputModel
                        {
                            DesignId = d?.DesignId,
                            SalesOrderDetailId = d?.Id,
                            Description = $"{chItem.DesignNumber} ({chItem.Colour} - {chItem.Size})",
                            HsnCode = "6204",
                            Colour = chItem.Colour,
                            Size = chItem.Size,
                            OrderedQuantity = orderedQty,
                            DeliveredQuantity = deliveredQty,
                            AlreadyInvoicedQuantity = 0,
                            Quantity = deliveredQty, // AUTO-FETCHED DELIVERED QUANTITY!
                            UnitPrice = unitPrice,
                            DiscountAmount = Math.Round((deliveredQty * unitPrice) * (discPct / 100m), 2),
                            GstRate = gstPct
                        });
                    }
                    model.TotalOrderedQuantity = model.Items.Sum(i => i.OrderedQuantity);
                    model.TotalDeliveredQuantity = selectedChallan.TotalQuantity;
                    model.InvoicingBasisMessage = $"Auto-fetched delivered items from Delivery Challan #{selectedChallan.ChallanNumber} ({selectedChallan.TotalQuantity} pcs).";
                }
                else
                {
                    // Full/Partial Sales Order billing auto-fetching delivered quantities
                    if (orderChallans.Any())
                    {
                        model.Notes = $"Billed against Sales Order #{so.SoNumber} (Challan(s): {string.Join(", ", orderChallans.Select(c => c.ChallanNumber))})";
                    }

                    foreach (var d in so.Details)
                    {
                        int challanDelivered = orderChallans.SelectMany(c => c.Items)
                            .Where(i => (i.SalesOrderDetailId.HasValue && i.SalesOrderDetailId.Value == d.Id) ||
                                        (i.DesignNumber == d.DesignNumber && i.Colour == d.Colour && i.Size == d.Size))
                            .Sum(i => i.QuantityDispatched);

                        int totalDelivered = Math.Max(challanDelivered, d.DispatchedQuantity);

                        int alreadyInvoiced = existingInvoices.SelectMany(inv => inv.Items)
                            .Where(i => (d.DesignId.HasValue && i.DesignId == d.DesignId) ||
                                        (i.Colour == d.Colour && i.Size == d.Size))
                            .Sum(i => i.Quantity);

                        int unbilledDelivered = Math.Max(0, totalDelivered - alreadyInvoiced);

                        // If goods were delivered, auto-fetch the delivered / unbilled delivered quantity
                        int billableQty = (totalDelivered > 0) ? unbilledDelivered : (orderChallans.Any() ? 0 : d.Quantity);

                        // If delivery challans exist for this order, skip items that were completely un-dispatched (delivered = 0)
                        if (orderChallans.Any() && totalDelivered == 0)
                        {
                            continue;
                        }

                        decimal discPct = d.DiscountPercentage;
                        decimal gstPct = d.GstPercentage > 0 ? d.GstPercentage : 5.0m;

                        model.Items.Add(new InvoiceItemInputModel
                        {
                            DesignId = d.DesignId,
                            SalesOrderDetailId = d.Id,
                            Description = $"{d.Design?.DesignNumber ?? d.DesignNumber} ({d.Colour} - {d.Size})",
                            HsnCode = "6204",
                            Colour = d.Colour,
                            Size = d.Size,
                            OrderedQuantity = d.Quantity,
                            DeliveredQuantity = totalDelivered,
                            AlreadyInvoicedQuantity = alreadyInvoiced,
                            Quantity = billableQty, // AUTO-FETCHED DELIVERED QUANTITY!
                            UnitPrice = d.UnitPrice,
                            DiscountAmount = Math.Round((billableQty * d.UnitPrice) * (discPct / 100m), 2),
                            GstRate = gstPct
                        });
                    }

                    model.TotalOrderedQuantity = so.Details.Sum(d => d.Quantity);
                    model.TotalDeliveredQuantity = orderChallans.Sum(c => c.TotalQuantity);

                    if (orderChallans.Any())
                    {
                        model.InvoicingBasisMessage = $"Delivered quantity auto-fetched: {model.Items.Sum(i => i.Quantity)} pcs across {orderChallans.Count} Delivery Challan(s) (Ordered: {model.TotalOrderedQuantity} pcs).";
                    }
                    else
                    {
                        model.InvoicingBasisMessage = $"Notice: No delivery challan found for this order (0 pcs delivered). Quantities reflect ordered amount.";
                    }
                }

                ViewBag.OrderChallans = orderChallans;
            }
        }

        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.SalesOrders = await _context.SalesOrders
            .Where(s => s.CompanyId == activeCompany.Id)
            .Include(s => s.Customer)
            .Include(s => s.Details)
            .Include(s => s.Challans)
            .OrderByDescending(s => s.Id)
            .ToListAsync();

        return View(model);
    }

    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateInvoiceViewModel model)
    {
        var activeCompany = await _companyContext.GetActiveCompanyAsync();

        if (model.CustomerId <= 0)
        {
            ModelState.AddModelError("CustomerId", "Please select a valid customer.");
        }

        if (model.Items == null || !model.Items.Any(i => i.Quantity > 0 && i.UnitPrice > 0))
        {
            ModelState.AddModelError("", "At least one valid line item with quantity and unit price is required.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
            ViewBag.SalesOrders = await _context.SalesOrders.Where(s => s.CompanyId == activeCompany.Id).Include(s => s.Customer).OrderByDescending(s => s.Id).ToListAsync();
            return View(model);
        }

        decimal subTotal = 0m;
        decimal totalDiscount = 0m;

        var invoice = new TaxInvoice
        {
            CompanyId = activeCompany.Id,
            InvoiceNumber = string.IsNullOrWhiteSpace(model.InvoiceNumber) ? await GenerateNextInvoiceNumberAsync(activeCompany) : model.InvoiceNumber.Trim(),
            InvoiceDate = model.InvoiceDate,
            DueDate = model.DueDate,
            SalesOrderId = model.SalesOrderId > 0 ? model.SalesOrderId : null,
            CustomerId = model.CustomerId,
            CustomerName = model.CustomerName.Trim(),
            CustomerGstin = model.CustomerGstin?.Trim(),
            CustomerPan = model.CustomerPan?.Trim(),
            BillingAddress = model.BillingAddress?.Trim(),
            ShippingAddress = model.ShippingAddress?.Trim(),
            PlaceOfSupply = model.PlaceOfSupply.Trim(),
            IsInterState = model.IsInterState,
            CgstRate = model.IsInterState ? 0m : model.CgstRate,
            SgstRate = model.IsInterState ? 0m : model.SgstRate,
            IgstRate = model.IsInterState ? model.IgstRate : 0m,
            BankName = model.BankName,
            BankAccountNumber = model.BankAccountNumber,
            BankIfsc = model.BankIfsc,
            BankBranch = model.BankBranch,
            TermsAndConditions = model.TermsAndConditions,
            Notes = model.Notes?.Trim(),
            EwayBillNumber = model.EwayBillNumber?.Trim(),
            TransporterName = model.TransporterName?.Trim(),
            VehicleNumber = model.VehicleNumber?.Trim(),
            PaymentStatus = InvoicePaymentStatus.Unpaid,
            CreatedDate = DateTime.Now
        };

        foreach (var item in (model.Items ?? new()).Where(i => i.Quantity > 0 && i.UnitPrice > 0))
        {
            decimal itemGross = item.Quantity * item.UnitPrice;
            decimal itemNet = Math.Max(0m, itemGross - item.DiscountAmount);
            decimal itemTax = Math.Round(itemNet * (item.GstRate / 100m), 2);
            decimal itemTotal = itemNet + itemTax;

            subTotal += itemGross;
            totalDiscount += item.DiscountAmount;

            invoice.Items.Add(new TaxInvoiceItem
            {
                DesignId = item.DesignId > 0 ? item.DesignId : null,
                Description = item.Description.Trim(),
                HsnCode = string.IsNullOrWhiteSpace(item.HsnCode) ? "6204" : item.HsnCode.Trim(),
                Colour = item.Colour?.Trim(),
                Size = item.Size?.Trim(),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                TaxableValue = itemNet,
                GstRate = item.GstRate,
                TotalAmount = itemTotal
            });
        }

        invoice.SubTotal = subTotal;
        invoice.DiscountAmount = totalDiscount;
        invoice.TaxableAmount = Math.Max(0m, subTotal - totalDiscount);

        if (invoice.IsInterState)
        {
            invoice.IgstAmount = Math.Round(invoice.TaxableAmount * (invoice.IgstRate / 100m), 2);
            invoice.CgstAmount = 0m;
            invoice.SgstAmount = 0m;
        }
        else
        {
            invoice.CgstAmount = Math.Round(invoice.TaxableAmount * (invoice.CgstRate / 100m), 2);
            invoice.SgstAmount = Math.Round(invoice.TaxableAmount * (invoice.SgstRate / 100m), 2);
            invoice.IgstAmount = 0m;
        }

        decimal rawTotal = invoice.TaxableAmount + invoice.CgstAmount + invoice.SgstAmount + invoice.IgstAmount;
        invoice.GrandTotal = Math.Round(rawTotal, 2);
        invoice.RoundOff = invoice.GrandTotal - rawTotal;

        _context.TaxInvoices.Add(invoice);

        // Record entry in Accounting Transactions ledger
        _context.AccountingTransactions.Add(new AccountingTransaction
        {
            CompanyId = invoice.CompanyId,
            Date = invoice.InvoiceDate,
            Type = TransactionType.Income,
            Amount = invoice.GrandTotal,
            Category = "Sales Invoice",
            Description = $"Tax Invoice {invoice.InvoiceNumber} generated for {invoice.CustomerName}",
            Reference = invoice.InvoiceNumber,
            CustomerId = invoice.CustomerId
        });

        // Automatically calculate & post Salesman Commission entries and expense transactions
        await ProcessSalesmanCommissionsAsync(invoice);

        await _context.SaveChangesAsync();

        // Automated Chatter & Activity Tracking
        var loggedUser = User.Identity?.Name ?? "User";
        _context.CommunicationLogs.Add(new CommunicationLog
        {
            DocumentType = "TaxInvoice",
            DocumentId = invoice.Id,
            DocumentReference = invoice.InvoiceNumber,
            Channel = CommunicationChannel.InternalNote,
            Recipient = "Accounts & Billing",
            RecipientName = invoice.CustomerName,
            Subject = "Tax Invoice Generated",
            Body = $"Generated Tax Invoice #{invoice.InvoiceNumber} for ₹{invoice.GrandTotal:N2} (Taxable: ₹{invoice.TaxableAmount:N2}, GST: ₹{(invoice.CgstAmount + invoice.SgstAmount + invoice.IgstAmount):N2}). Credit terms: {invoice.CreditPeriodDays} days, Due date: {invoice.DueDate:dd MMM yyyy}. Total days (age): {invoice.TotalDays} · Overdue days: {invoice.OverdueDays}.",
            Status = CommunicationStatus.Sent,
            SentAt = DateTime.Now,
            SentBy = loggedUser
        });

        if (invoice.SalesOrderId.HasValue)
        {
            _context.CommunicationLogs.Add(new CommunicationLog
            {
                DocumentType = "SalesOrder",
                DocumentId = invoice.SalesOrderId.Value,
                DocumentReference = invoice.SalesOrder?.SoNumber ?? $"SO-{invoice.SalesOrderId.Value}",
                Channel = CommunicationChannel.InternalNote,
                Recipient = "Accounts & Billing",
                RecipientName = invoice.CustomerName,
                Subject = $"Billed Tax Invoice #{invoice.InvoiceNumber}",
                Body = $"Billed Tax Invoice #{invoice.InvoiceNumber} totaling ₹{invoice.GrandTotal:N2} against this order. Total days: {invoice.TotalDays}, Overdue days: {invoice.OverdueDays}.",
                Status = CommunicationStatus.Sent,
                SentAt = DateTime.Now,
                SentBy = loggedUser
            });
        }

        await _context.SaveChangesAsync();

        // Credit Limit Check & Chatter Warning
        var customer = await _context.Customers.FindAsync(invoice.CustomerId);
        if (customer != null && customer.PartnerLimit.HasValue && customer.PartnerLimit.Value > 0)
        {
            var unpaidTotal = await _context.TaxInvoices
                .Where(i => i.CustomerId == customer.Id && i.Id != invoice.Id && i.PaymentStatus != InvoicePaymentStatus.Paid && i.PaymentStatus != InvoicePaymentStatus.Cancelled)
                .SumAsync(i => (decimal?)(i.GrandTotal - i.PaidAmount)) ?? 0m;
            decimal totalExposure = unpaidTotal + (customer.TotalReceivable ?? 0m) + invoice.GrandTotal;
            if (totalExposure > customer.PartnerLimit.Value)
            {
                decimal excess = totalExposure - customer.PartnerLimit.Value;
                _context.CommunicationLogs.Add(new CommunicationLog
                {
                    DocumentType = "TaxInvoice",
                    DocumentId = invoice.Id,
                    DocumentReference = invoice.InvoiceNumber,
                    Channel = CommunicationChannel.InternalNote,
                    Recipient = "Credit & Accounts",
                    RecipientName = customer.CustomerName,
                    Subject = "Credit Limit Exceeded Warning",
                    Body = $"⚠ Credit Limit Warning: Invoice #{invoice.InvoiceNumber} (₹{invoice.GrandTotal:N2}) brings customer total exposure to ₹{totalExposure:N2}, exceeding credit limit of ₹{customer.PartnerLimit.Value:N2} by ₹{excess:N2}.",
                    Status = CommunicationStatus.Sent,
                    SentAt = DateTime.Now,
                    SentBy = loggedUser
                });
                await _context.SaveChangesAsync();
                TempData["Warning"] = $"Tax Invoice {invoice.InvoiceNumber} generated, but Customer Credit Limit is exceeded by ₹{excess:N2} (Total Exposure: ₹{totalExposure:N2} / Limit: ₹{customer.PartnerLimit.Value:N2}).";
            }
        }

        if (TempData["Warning"] == null)
        {
            TempData["Success"] = $"Tax Invoice {invoice.InvoiceNumber} generated successfully. Total credit terms: {invoice.CreditPeriodDays} days · Due date: {invoice.DueDate:dd/MM/yyyy}. Total days: {invoice.TotalDays} · Overdue days: {invoice.OverdueDays}.";
        }
        return RedirectToAction(nameof(Details), new { id = invoice.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var invoice = await _context.TaxInvoices
            .Include(i => i.Company)
            .Include(i => i.Customer)
            .Include(i => i.SalesOrder)
            .Include(i => i.Items)
            .ThenInclude(item => item.Design)
            .Include(i => i.Receipts)
            .Include(i => i.Returns)
            .ThenInclude(r => r.Items)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return NotFound();

        ViewBag.CommissionEntries = await _context.SalesmanCommissionEntries
            .Where(e => e.TaxInvoiceId == id)
            .ToListAsync();

        if (!string.IsNullOrEmpty(invoice.EInvoiceSignedQrCode) || !string.IsNullOrEmpty(invoice.Irn))
        {
            ViewBag.EInvoiceQrSvg = _eInvoiceService.GenerateQrCodeSvg(invoice.EInvoiceSignedQrCode ?? invoice.Irn!, 130);
        }

        return View(invoice);
    }

    public async Task<IActionResult> Print(int id)
    {
        var invoice = await _context.TaxInvoices
            .Include(i => i.Company)
            .Include(i => i.Customer)
            .Include(i => i.SalesOrder)
            .Include(i => i.Items)
            .ThenInclude(item => item.Design)
            .Include(i => i.Receipts)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return NotFound();

        if (!string.IsNullOrEmpty(invoice.EInvoiceSignedQrCode) || !string.IsNullOrEmpty(invoice.Irn))
        {
            ViewBag.EInvoiceQrSvg = _eInvoiceService.GenerateQrCodeSvg(invoice.EInvoiceSignedQrCode ?? invoice.Irn!, 110);
        }

        return View(invoice);
    }

    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordPayment(RecordPaymentInputModel model)
    {
        var invoice = await _context.TaxInvoices
            .Include(i => i.Receipts)
            .FirstOrDefaultAsync(i => i.Id == model.InvoiceId);

        if (invoice == null) return NotFound();

        if (model.Amount <= 0)
        {
            TempData["Error"] = "Payment amount must be greater than zero.";
            return RedirectToAction(nameof(Details), new { id = model.InvoiceId });
        }

        string receiptNo = await GenerateNextReceiptNumberAsync();

        var receipt = new PaymentReceipt
        {
            CompanyId = invoice.CompanyId,
            ReceiptNumber = receiptNo,
            PaymentDate = model.PaymentDate,
            TaxInvoiceId = invoice.Id,
            CustomerId = invoice.CustomerId,
            Amount = model.Amount,
            PaymentMode = model.PaymentMode,
            ReferenceNumber = model.ReferenceNumber?.Trim(),
            Notes = model.Notes?.Trim(),
            CreatedDate = DateTime.Now
        };

        _context.PaymentReceipts.Add(receipt);

        invoice.PaidAmount += model.Amount;
        if (invoice.PaidAmount >= invoice.GrandTotal)
        {
            invoice.PaymentStatus = InvoicePaymentStatus.Paid;
        }
        else
        {
            invoice.PaymentStatus = InvoicePaymentStatus.PartiallyPaid;
        }

        // Record receipt in Accounting Transactions
        _context.AccountingTransactions.Add(new AccountingTransaction
        {
            CompanyId = invoice.CompanyId,
            Date = model.PaymentDate,
            Type = TransactionType.Income,
            Amount = model.Amount,
            Category = "Customer Payment",
            Description = $"Payment received against {invoice.InvoiceNumber} (Receipt: {receiptNo}) via {model.PaymentMode}",
            Reference = string.IsNullOrWhiteSpace(model.ReferenceNumber) ? receiptNo : model.ReferenceNumber.Trim(),
            CustomerId = invoice.CustomerId
        });

        await _context.SaveChangesAsync();

        // Automated Chatter & Activity Tracking
        var payUser = User.Identity?.Name ?? "User";
        _context.CommunicationLogs.Add(new CommunicationLog
        {
            DocumentType = "TaxInvoice",
            DocumentId = invoice.Id,
            DocumentReference = invoice.InvoiceNumber,
            Channel = CommunicationChannel.InternalNote,
            Recipient = "Accounts & Billing",
            RecipientName = invoice.CustomerName,
            Subject = $"Payment Received: ₹{model.Amount:N2}",
            Body = $"Recorded payment of ₹{model.Amount:N2} via {model.PaymentMode} (Receipt #{receiptNo}). Ref/UTR: {model.ReferenceNumber ?? "N/A"}. Remaining Due: ₹{invoice.BalanceDue:N2}.",
            Status = CommunicationStatus.Sent,
            SentAt = DateTime.Now,
            SentBy = payUser
        });

        _context.CommunicationLogs.Add(new CommunicationLog
        {
            DocumentType = "PaymentReceipt",
            DocumentId = receipt.Id,
            DocumentReference = receiptNo,
            Channel = CommunicationChannel.InternalNote,
            Recipient = "Accounts & Billing",
            RecipientName = invoice.CustomerName,
            Subject = "Payment Receipt Voucher Created",
            Body = $"Receipt #{receiptNo} issued for ₹{model.Amount:N2} against Invoice #{invoice.InvoiceNumber}.",
            Status = CommunicationStatus.Sent,
            SentAt = DateTime.Now,
            SentBy = payUser
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Payment of ₹{model.Amount:N2} recorded successfully (Receipt #{receiptNo}).";
        return RedirectToAction(nameof(Details), new { id = model.InvoiceId });
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomerDetails(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) return NotFound();

        string state = customer.State ?? "";
        bool isInterState = !string.IsNullOrEmpty(state) && !state.ToLower().Contains("gujarat") && !state.Contains("24");

        // Credit Limits & Outstanding Exposure
        var unpaidInvoices = await _context.TaxInvoices
            .Where(i => i.CustomerId == customerId && i.PaymentStatus != InvoicePaymentStatus.Paid && i.PaymentStatus != InvoicePaymentStatus.Cancelled)
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
            customerName = customer.CustomerName,
            gstin = customer.GstNumber ?? "",
            pan = customer.PanNumber ?? "",
            billingAddress = customer.Address ?? "",
            shippingAddress = customer.Address ?? "",
            state = state,
            isInterState = isInterState,
            placeOfSupply = !string.IsNullOrEmpty(state) ? state : "Gujarat (24)",
            creditLimit = creditLimit,
            currentOutstanding = totalOutstanding,
            availableCredit = availableCredit,
            hasCreditLimit = hasCreditLimit,
            isOverLimit = isOverLimit,
            daysSalesOutstanding = customer.DaysSalesOutstanding ?? 15
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetOrderDeliveryDetails(int? salesOrderId, int? deliveryChallanId)
    {
        if (!salesOrderId.HasValue && !deliveryChallanId.HasValue)
            return BadRequest("salesOrderId or deliveryChallanId is required.");

        DeliveryChallan? selectedChallan = null;
        if (deliveryChallanId.HasValue)
        {
            selectedChallan = await _context.DeliveryChallans
                .Include(c => c.Customer)
                .Include(c => c.Items)
                .Include(c => c.SalesOrder)
                .ThenInclude(s => s.Details)
                .ThenInclude(d => d.Design)
                .FirstOrDefaultAsync(c => c.Id == deliveryChallanId.Value);

            if (selectedChallan != null)
            {
                salesOrderId = selectedChallan.SalesOrderId;
            }
        }

        if (!salesOrderId.HasValue)
            return NotFound("Sales order not found.");

        var so = await _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.Details)
            .ThenInclude(d => d.Design)
            .FirstOrDefaultAsync(s => s.Id == salesOrderId.Value);

        if (so == null || so.Customer == null)
            return NotFound("Sales order or customer not found.");

        var challans = await _context.DeliveryChallans
            .Include(c => c.Items)
            .Where(c => c.SalesOrderId == salesOrderId.Value)
            .OrderByDescending(c => c.ChallanDate)
            .ToListAsync();

        var existingInvoices = await _context.TaxInvoices
            .Include(i => i.Items)
            .Where(i => i.SalesOrderId == salesOrderId.Value && i.PaymentStatus != InvoicePaymentStatus.Cancelled)
            .ToListAsync();

        string state = so.Customer.State ?? "";
        bool isInterState = !string.IsNullOrEmpty(state) && !state.ToLower().Contains("gujarat") && !state.Contains("24");

        var activeChallan = selectedChallan ?? challans.FirstOrDefault();

        var challanSummaries = challans.Select(c => new
        {
            id = c.Id,
            challanNumber = c.ChallanNumber,
            challanDate = c.ChallanDate.ToString("dd/MM/yyyy"),
            totalQuantity = c.TotalQuantity
        }).ToList();

        var items = new List<object>();

        if (selectedChallan != null)
        {
            foreach (var chItem in selectedChallan.Items)
            {
                var soDetail = so.Details.FirstOrDefault(d => 
                    (chItem.SalesOrderDetailId.HasValue && d.Id == chItem.SalesOrderDetailId.Value) ||
                    (d.DesignNumber == chItem.DesignNumber && d.Colour == chItem.Colour && d.Size == chItem.Size));

                int orderedQty = soDetail?.Quantity ?? chItem.QuantityDispatched;
                int deliveredQty = chItem.QuantityDispatched;
                decimal unitPrice = soDetail?.UnitPrice ?? 0m;
                decimal discPct = soDetail?.DiscountPercentage ?? 0m;
                decimal gstPct = soDetail?.GstPercentage > 0 ? soDetail.GstPercentage : 5.0m;

                items.Add(new
                {
                    designId = soDetail?.DesignId,
                    salesOrderDetailId = soDetail?.Id,
                    description = $"{chItem.DesignNumber} ({chItem.Colour} - {chItem.Size})",
                    hsnCode = "6204",
                    colour = chItem.Colour,
                    size = chItem.Size,
                    orderedQuantity = orderedQty,
                    deliveredQuantity = deliveredQty,
                    alreadyInvoicedQuantity = 0,
                    quantity = deliveredQty, // AUTO-FETCHED DELIVERED QUANTITY!
                    unitPrice = unitPrice,
                    discountAmount = Math.Round((deliveredQty * unitPrice) * (discPct / 100m), 2),
                    gstRate = gstPct
                });
            }
        }
        else
        {
            foreach (var d in so.Details)
            {
                int challanDelivered = challans.SelectMany(c => c.Items)
                    .Where(i => (i.SalesOrderDetailId.HasValue && i.SalesOrderDetailId.Value == d.Id) ||
                                (i.DesignNumber == d.DesignNumber && i.Colour == d.Colour && i.Size == d.Size))
                    .Sum(i => i.QuantityDispatched);

                int totalDelivered = Math.Max(challanDelivered, d.DispatchedQuantity);

                int alreadyInvoiced = existingInvoices.SelectMany(inv => inv.Items)
                    .Where(i => (d.DesignId.HasValue && i.DesignId == d.DesignId) ||
                                (i.Colour == d.Colour && i.Size == d.Size))
                    .Sum(i => i.Quantity);

                int unbilledDelivered = Math.Max(0, totalDelivered - alreadyInvoiced);

                int billableQty = (totalDelivered > 0) ? unbilledDelivered : (challans.Any() ? 0 : d.Quantity);

                if (challans.Any() && totalDelivered == 0 && billableQty == 0)
                {
                    continue;
                }

                decimal discPct = d.DiscountPercentage;
                decimal gstPct = d.GstPercentage > 0 ? d.GstPercentage : 5.0m;

                items.Add(new
                {
                    designId = d.DesignId,
                    salesOrderDetailId = d.Id,
                    description = $"{d.Design?.DesignNumber ?? d.DesignNumber} ({d.Colour} - {d.Size})",
                    hsnCode = "6204",
                    colour = d.Colour,
                    size = d.Size,
                    orderedQuantity = d.Quantity,
                    deliveredQuantity = totalDelivered,
                    alreadyInvoicedQuantity = alreadyInvoiced,
                    quantity = billableQty, // AUTO-FETCHED DELIVERED QUANTITY!
                    unitPrice = d.UnitPrice,
                    discountAmount = Math.Round((billableQty * d.UnitPrice) * (discPct / 100m), 2),
                    gstRate = gstPct
                });
            }
        }

        int totalOrdered = so.Details.Sum(d => d.Quantity);
        int totalDeliveredSum = challans.Sum(c => c.TotalQuantity);

        string basisMsg;
        if (selectedChallan != null)
        {
            basisMsg = $"Auto-fetched delivered items from Delivery Challan #{selectedChallan.ChallanNumber} ({selectedChallan.TotalQuantity} pcs).";
        }
        else if (challans.Any())
        {
            basisMsg = $"Auto-fetched delivered quantity across {challans.Count} Delivery Challan(s) ({totalDeliveredSum} of {totalOrdered} pcs delivered).";
        }
        else
        {
            basisMsg = $"No delivery challan found for this order (0 pcs delivered). Quantities reflect ordered amount.";
        }

        // Credit Limits & Outstanding Exposure
        var unpaidInvoices = await _context.TaxInvoices
            .Where(i => i.CustomerId == so.CustomerId && i.PaymentStatus != InvoicePaymentStatus.Paid && i.PaymentStatus != InvoicePaymentStatus.Cancelled)
            .ToListAsync();
        decimal invoiceOutstanding = unpaidInvoices.Sum(i => i.BalanceDue);
        decimal openingReceivable = so.Customer.TotalReceivable ?? 0m;
        decimal totalOutstanding = invoiceOutstanding + openingReceivable;
        decimal creditLimit = so.Customer.PartnerLimit ?? 0m;
        decimal availableCredit = creditLimit > 0 ? (creditLimit - totalOutstanding) : 0m;
        bool hasCreditLimit = creditLimit > 0;
        bool isOverLimit = hasCreditLimit && totalOutstanding > creditLimit;

        return Json(new
        {
            salesOrderId = so.Id,
            soNumber = so.SoNumber,
            deliveryChallanId = selectedChallan?.Id,
            deliveryChallanNumber = selectedChallan?.ChallanNumber,
            customerId = so.CustomerId,
            customerName = so.Customer.CustomerName,
            gstin = so.Customer.GstNumber ?? "",
            pan = so.Customer.PanNumber ?? "",
            billingAddress = so.Customer.Address ?? "",
            shippingAddress = activeChallan?.ShippingAddress ?? so.ShippingAddress ?? so.Customer.Address ?? "",
            state = state,
            isInterState = isInterState,
            placeOfSupply = !string.IsNullOrEmpty(state) ? state : "Gujarat (24)",
            ewayBillNumber = activeChallan?.EwayBillNumber ?? "",
            transporterName = activeChallan?.TransporterName ?? "",
            vehicleNumber = activeChallan?.VehicleNumber ?? "",
            lrNumber = activeChallan?.LrNumber ?? "",
            totalOrderedQuantity = totalOrdered,
            totalDeliveredQuantity = totalDeliveredSum,
            invoicingBasisMessage = basisMsg,
            creditLimit = creditLimit,
            currentOutstanding = totalOutstanding,
            availableCredit = availableCredit,
            hasCreditLimit = hasCreditLimit,
            isOverLimit = isOverLimit,
            daysSalesOutstanding = so.Customer.DaysSalesOutstanding ?? 15,
            hasAgentDiscount = so.HasAgentDiscount,
            agentDiscountRate = so.AgentDiscountRate,
            agentDiscountAmount = so.AgentDiscountAmount,
            challans = challanSummaries,
            items = items
        });
    }

    private async Task ProcessSalesmanCommissionsAsync(TaxInvoice invoice)
    {
        if (invoice.CustomerId <= 0) return;

        var customer = await _context.Customers
            .Include(c => c.Commissions)
            .FirstOrDefaultAsync(c => c.Id == invoice.CustomerId);

        if (customer == null) return;

        // Collect active commission rules
        var rules = customer.Commissions?
            .Where(c => c.IsActive && c.CommissionRate > 0 &&
                       (!c.StartDate.HasValue || c.StartDate.Value.Date <= invoice.InvoiceDate.Date) &&
                       (!c.EndDate.HasValue || c.EndDate.Value.Date >= invoice.InvoiceDate.Date))
            .ToList() ?? new List<CustomerSalesmanCommission>();

        // Fallback to legacy SM1, SM2, SM3 if no rules configured
        if (!rules.Any())
        {
            if (!string.IsNullOrWhiteSpace(customer.SM1Name) && customer.SM1CommissionPct > 0)
            {
                rules.Add(new CustomerSalesmanCommission
                {
                    SalesmanName = customer.SM1Name.Trim(),
                    Basis = CommissionBasis.AllProducts,
                    CalcType = CommissionCalcType.Percentage,
                    CommissionRate = customer.SM1CommissionPct.Value,
                    IsActive = true
                });
            }
            if (!string.IsNullOrWhiteSpace(customer.SM2Name) && customer.SM2CommissionPct > 0)
            {
                rules.Add(new CustomerSalesmanCommission
                {
                    SalesmanName = customer.SM2Name.Trim(),
                    Basis = CommissionBasis.AllProducts,
                    CalcType = CommissionCalcType.Percentage,
                    CommissionRate = customer.SM2CommissionPct.Value,
                    IsActive = true
                });
            }
            if (!string.IsNullOrWhiteSpace(customer.SM3Name) && customer.SM3CommissionPct > 0)
            {
                rules.Add(new CustomerSalesmanCommission
                {
                    SalesmanName = customer.SM3Name.Trim(),
                    Basis = CommissionBasis.AllProducts,
                    CalcType = CommissionCalcType.Percentage,
                    CommissionRate = customer.SM3CommissionPct.Value,
                    IsActive = true
                });
            }
        }

        if (!rules.Any()) return;

        // Preload designs for items to check category / design match
        var designIds = invoice.Items.Where(i => i.DesignId.HasValue).Select(i => i.DesignId!.Value).Distinct().ToList();
        var designs = await _context.Designs
            .Include(d => d.ProductCategory)
            .Where(d => designIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id);

        foreach (var rule in rules)
        {
            decimal salesAmount = 0m;
            int quantity = 0;
            decimal commissionAmount = 0m;
            string targetDisplay = rule.TargetValue ?? "";

            switch (rule.Basis)
            {
                case CommissionBasis.Category:
                    var matchedCatItems = invoice.Items.Where(i =>
                    {
                        if (string.IsNullOrWhiteSpace(rule.TargetValue) || rule.TargetValue == "All" || rule.TargetValue == "-- All Categories --")
                            return true;

                        if (i.DesignId.HasValue && designs.TryGetValue(i.DesignId.Value, out var des))
                        {
                            return string.Equals(des.ProductCategory?.CategoryName, rule.TargetValue, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(des.Category, rule.TargetValue, StringComparison.OrdinalIgnoreCase);
                        }
                        return false;
                    }).ToList();

                    salesAmount = matchedCatItems.Sum(i => i.TaxableValue);
                    quantity = matchedCatItems.Sum(i => i.Quantity);
                    targetDisplay = string.IsNullOrWhiteSpace(rule.TargetValue) ? "All Categories" : rule.TargetValue;

                    if (rule.CalcType == CommissionCalcType.Percentage)
                        commissionAmount = Math.Round(salesAmount * (rule.CommissionRate / 100m), 2);
                    else
                        commissionAmount = Math.Round(quantity * rule.CommissionRate, 2);
                    break;

                case CommissionBasis.AllProducts:
                    salesAmount = invoice.TaxableAmount;
                    quantity = invoice.Items.Sum(i => i.Quantity);
                    targetDisplay = "All Products";

                    if (rule.CalcType == CommissionCalcType.Percentage)
                        commissionAmount = Math.Round(salesAmount * (rule.CommissionRate / 100m), 2);
                    else
                        commissionAmount = Math.Round(quantity * rule.CommissionRate, 2);
                    break;

                case CommissionBasis.Design:
                    var matchedDesItems = invoice.Items.Where(i =>
                        (rule.DesignId.HasValue && i.DesignId == rule.DesignId.Value) ||
                        (!string.IsNullOrWhiteSpace(rule.TargetValue) && i.Description.Contains(rule.TargetValue, StringComparison.OrdinalIgnoreCase))
                    ).ToList();

                    salesAmount = matchedDesItems.Sum(i => i.TaxableValue);
                    quantity = matchedDesItems.Sum(i => i.Quantity);
                    targetDisplay = rule.TargetValue ?? (rule.DesignId.HasValue ? $"Design #{rule.DesignId}" : "Design");

                    if (rule.CalcType == CommissionCalcType.Percentage)
                        commissionAmount = Math.Round(salesAmount * (rule.CommissionRate / 100m), 2);
                    else
                        commissionAmount = Math.Round(quantity * rule.CommissionRate, 2);
                    break;

                case CommissionBasis.FixedPerPiece:
                    var matchedPieceItems = invoice.Items.Where(i =>
                    {
                        if (string.IsNullOrWhiteSpace(rule.TargetValue) || rule.TargetValue == "All" || rule.TargetValue == "-- All Categories --")
                            return true;

                        if (i.DesignId.HasValue && designs.TryGetValue(i.DesignId.Value, out var des))
                        {
                            return string.Equals(des.ProductCategory?.CategoryName, rule.TargetValue, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(des.Category, rule.TargetValue, StringComparison.OrdinalIgnoreCase);
                        }
                        return false;
                    }).ToList();

                    salesAmount = matchedPieceItems.Sum(i => i.TaxableValue);
                    quantity = matchedPieceItems.Sum(i => i.Quantity);
                    targetDisplay = string.IsNullOrWhiteSpace(rule.TargetValue) ? "All Pieces" : $"{rule.TargetValue} (Fixed/Pc)";
                    commissionAmount = Math.Round(quantity * rule.CommissionRate, 2);
                    break;
            }

            if (commissionAmount > 0)
            {
                var rateUnit = rule.CalcType == CommissionCalcType.Percentage ? "%" : "₹";

                // 1. Expense in Accounting Ledger
                var acctTx = new AccountingTransaction
                {
                    Date = invoice.InvoiceDate,
                    Type = TransactionType.Expense,
                    Amount = commissionAmount,
                    Category = "Salesman Commission",
                    Description = $"Sales Commission for {rule.SalesmanName} ({rule.CommissionRate}{rateUnit} on {targetDisplay}) on Invoice {invoice.InvoiceNumber}",
                    Reference = invoice.InvoiceNumber,
                    CustomerId = invoice.CustomerId
                };
                _context.AccountingTransactions.Add(acctTx);

                // 2. Salesman Commission Entry
                var entry = new SalesmanCommissionEntry
                {
                    TaxInvoice = invoice,
                    CustomerId = invoice.CustomerId,
                    SalesmanName = rule.SalesmanName,
                    EntryDate = invoice.InvoiceDate,
                    Basis = rule.Basis,
                    CategoryOrTarget = targetDisplay,
                    SalesAmount = salesAmount,
                    Quantity = quantity,
                    CommissionRate = rule.CommissionRate,
                    CalcType = rule.CalcType,
                    CommissionAmount = commissionAmount,
                    IsPaid = false,
                    AccountingTransaction = acctTx
                };
                _context.SalesmanCommissionEntries.Add(entry);
            }
        }
    }

    [HttpGet]
    public async Task<IActionResult> ReturnGoods(int id)
    {
        var invoice = await _context.TaxInvoices
            .Include(i => i.Items)
            .Include(i => i.Returns)
            .ThenInclude(r => r.Items)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return NotFound();

        var model = new SalesReturnCreateViewModel
        {
            TaxInvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.CustomerName,
            ReturnDate = DateTime.Today,
            Reason = ReturnReason.Defective,
            RestockInventory = true,
            Items = invoice.Items.Select(item =>
            {
                var alreadyReturned = invoice.Returns
                    .SelectMany(r => r.Items)
                    .Where(ri => ri.TaxInvoiceItemId == item.Id)
                    .Sum(ri => ri.Quantity);

                return new SalesReturnItemInput
                {
                    TaxInvoiceItemId = item.Id,
                    DesignId = item.DesignId,
                    Description = item.Description,
                    Colour = item.Colour,
                    Size = item.Size,
                    OriginalQuantity = item.Quantity,
                    AlreadyReturnedQuantity = alreadyReturned,
                    ReturnQuantity = 0,
                    UnitPrice = item.UnitPrice,
                    GstRate = item.GstRate
                };
            }).Where(i => i.OriginalQuantity > i.AlreadyReturnedQuantity).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnGoods(SalesReturnCreateViewModel model)
    {
        var invoice = await _context.TaxInvoices
            .Include(i => i.Items)
            .Include(i => i.Returns)
            .ThenInclude(r => r.Items)
            .FirstOrDefaultAsync(i => i.Id == model.TaxInvoiceId);

        if (invoice == null) return NotFound();

        var validItems = model.Items.Where(i => i.ReturnQuantity > 0).ToList();
        if (!validItems.Any())
        {
            ModelState.AddModelError("", "Please specify at least one item with a return quantity greater than 0.");
            return View(model);
        }

        foreach (var vi in validItems)
        {
            var invoiceItem = invoice.Items.FirstOrDefault(i => i.Id == vi.TaxInvoiceItemId);
            if (invoiceItem == null) continue;

            var alreadyReturned = invoice.Returns
                .SelectMany(r => r.Items)
                .Where(ri => ri.TaxInvoiceItemId == vi.TaxInvoiceItemId)
                .Sum(ri => ri.Quantity);

            int remainingEligible = invoiceItem.Quantity - alreadyReturned;
            if (vi.ReturnQuantity > remainingEligible)
            {
                ModelState.AddModelError("", $"Return quantity for '{vi.Description}' cannot exceed {remainingEligible}.");
                return View(model);
            }
        }

        string returnNo = await GenerateNextReturnNumberAsync();

        var salesReturn = new SalesReturn
        {
            CompanyId = invoice.CompanyId,
            ReturnNumber = returnNo,
            ReturnDate = model.ReturnDate,
            TaxInvoiceId = invoice.Id,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.CustomerName,
            Reason = model.Reason,
            Remarks = model.Remarks?.Trim(),
            RestockInventory = model.RestockInventory,
            CreatedDate = DateTime.Now
        };

        decimal returnSubTotal = 0m;
        decimal returnTaxAmount = 0m;

        foreach (var vi in validItems)
        {
            decimal taxable = Math.Round(vi.ReturnQuantity * vi.UnitPrice, 2);
            decimal tax = Math.Round(taxable * (vi.GstRate / 100m), 2);
            decimal total = taxable + tax;

            salesReturn.Items.Add(new SalesReturnItem
            {
                TaxInvoiceItemId = vi.TaxInvoiceItemId,
                DesignId = vi.DesignId,
                Description = vi.Description,
                Colour = vi.Colour,
                Size = vi.Size,
                Quantity = vi.ReturnQuantity,
                UnitPrice = vi.UnitPrice,
                TaxableAmount = taxable,
                GstRate = vi.GstRate,
                TotalAmount = total
            });

            returnSubTotal += taxable;
            returnTaxAmount += tax;

            // Restock to ReadyProduct inventory if enabled
            if (model.RestockInventory && vi.DesignId.HasValue && !string.IsNullOrWhiteSpace(vi.Colour) && !string.IsNullOrWhiteSpace(vi.Size))
            {
                var readyProduct = await _context.ReadyProducts
                    .FirstOrDefaultAsync(r => r.CompanyId == invoice.CompanyId && r.DesignId == vi.DesignId.Value && r.Colour == vi.Colour && r.Size == vi.Size);

                if (readyProduct != null)
                {
                    readyProduct.QuantityOnHand += vi.ReturnQuantity;
                    _context.ReadyProductTransactions.Add(new ReadyProductTransaction
                    {
                        CompanyId = invoice.CompanyId,
                        ReadyProductId = readyProduct.Id,
                        CreatedDate = model.ReturnDate,
                        TransactionType = ReadyProductTransactionType.CustomerSalesReturn,
                        Quantity = vi.ReturnQuantity,
                        BalanceAfter = readyProduct.QuantityOnHand,
                        ReferenceType = "Customer Sales Return",
                        ReferenceNumber = returnNo,
                        Notes = $"Customer return from {invoice.CustomerName} (Invoice #{invoice.InvoiceNumber})"
                    });
                }
            }
        }

        salesReturn.SubTotal = returnSubTotal;
        salesReturn.TaxAmount = returnTaxAmount;
        salesReturn.GrandTotal = returnSubTotal + returnTaxAmount;

        _context.SalesReturns.Add(salesReturn);

        // General Ledger Entry for Credit Note / Return
        _context.AccountingTransactions.Add(new AccountingTransaction
        {
            CompanyId = invoice.CompanyId,
            Date = model.ReturnDate,
            Type = TransactionType.Expense,
            Amount = salesReturn.GrandTotal,
            Category = "Sales Return / Credit Note",
            Description = $"Credit Note / Sales Return {returnNo} against Invoice {invoice.InvoiceNumber} ({model.Reason})",
            Reference = returnNo,
            CustomerId = invoice.CustomerId
        });

        // Commission Deduction calculation
        await ProcessReturnCommissionDeductionsAsync(salesReturn, invoice);

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Goods Return {returnNo} recorded successfully! ₹{salesReturn.TotalCommissionDeducted:N2} salesman commission deducted.";
        return RedirectToAction(nameof(Details), new { id = invoice.Id });
    }

    private async Task ProcessReturnCommissionDeductionsAsync(SalesReturn salesReturn, TaxInvoice invoice)
    {
        var originalCommissions = await _context.SalesmanCommissionEntries
            .Where(e => e.TaxInvoiceId == invoice.Id && e.CommissionAmount > 0)
            .ToListAsync();

        if (!originalCommissions.Any()) return;

        var designIds = salesReturn.Items.Where(i => i.DesignId.HasValue).Select(i => i.DesignId!.Value).Distinct().ToList();
        var designs = await _context.Designs
            .Include(d => d.ProductCategory)
            .Where(d => designIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id);

        decimal totalDeducted = 0m;

        foreach (var original in originalCommissions)
        {
            decimal returnedSales = 0m;
            int returnedQty = 0;
            decimal deductAmount = 0m;

            switch (original.Basis)
            {
                case CommissionBasis.Category:
                    var matchedItems = salesReturn.Items.Where(i =>
                    {
                        if (string.IsNullOrWhiteSpace(original.CategoryOrTarget) || original.CategoryOrTarget == "All" || original.CategoryOrTarget == "All Categories" || original.CategoryOrTarget == "-- All Categories --")
                            return true;

                        if (i.DesignId.HasValue && designs.TryGetValue(i.DesignId.Value, out var des))
                        {
                            return string.Equals(des.ProductCategory?.CategoryName, original.CategoryOrTarget, StringComparison.OrdinalIgnoreCase)
                                || string.Equals(des.Category, original.CategoryOrTarget, StringComparison.OrdinalIgnoreCase);
                        }
                        return false;
                    }).ToList();

                    returnedSales = matchedItems.Sum(i => i.TaxableAmount);
                    returnedQty = matchedItems.Sum(i => i.Quantity);

                    if (original.CalcType == CommissionCalcType.Percentage)
                        deductAmount = Math.Round(returnedSales * (original.CommissionRate / 100m), 2);
                    else
                        deductAmount = Math.Round(returnedQty * original.CommissionRate, 2);
                    break;

                case CommissionBasis.AllProducts:
                    returnedSales = salesReturn.SubTotal;
                    returnedQty = salesReturn.Items.Sum(i => i.Quantity);

                    if (original.CalcType == CommissionCalcType.Percentage)
                        deductAmount = Math.Round(returnedSales * (original.CommissionRate / 100m), 2);
                    else
                        deductAmount = Math.Round(returnedQty * original.CommissionRate, 2);
                    break;

                case CommissionBasis.Design:
                    var matchedDesItems = salesReturn.Items.Where(i =>
                        (original.CategoryOrTarget != null && i.Description.Contains(original.CategoryOrTarget, StringComparison.OrdinalIgnoreCase))
                    ).ToList();

                    returnedSales = matchedDesItems.Sum(i => i.TaxableAmount);
                    returnedQty = matchedDesItems.Sum(i => i.Quantity);

                    if (original.CalcType == CommissionCalcType.Percentage)
                        deductAmount = Math.Round(returnedSales * (original.CommissionRate / 100m), 2);
                    else
                        deductAmount = Math.Round(returnedQty * original.CommissionRate, 2);
                    break;

                case CommissionBasis.FixedPerPiece:
                    returnedQty = salesReturn.Items.Sum(i => i.Quantity);
                    returnedSales = salesReturn.SubTotal;
                    deductAmount = Math.Round(returnedQty * original.CommissionRate, 2);
                    break;
            }

            if (deductAmount > 0)
            {
                var rateUnit = original.CalcType == CommissionCalcType.Percentage ? "%" : "₹";

                // 1. Post reversal / deduction transaction to General Ledger
                var acctTx = new AccountingTransaction
                {
                    Date = salesReturn.ReturnDate,
                    Type = TransactionType.Income, // Reversing commission expense
                    Amount = deductAmount,
                    Category = "Salesman Commission Deduction",
                    Description = $"Commission deduction for {original.SalesmanName} ({original.CommissionRate}{rateUnit}) on Return {salesReturn.ReturnNumber} (Invoice #{invoice.InvoiceNumber})",
                    Reference = salesReturn.ReturnNumber,
                    CustomerId = invoice.CustomerId
                };
                _context.AccountingTransactions.Add(acctTx);

                // 2. Post negative entry in SalesmanCommissionEntries
                var returnEntry = new SalesmanCommissionEntry
                {
                    TaxInvoice = invoice,
                    CustomerId = invoice.CustomerId,
                    SalesmanName = original.SalesmanName,
                    EntryDate = salesReturn.ReturnDate,
                    Basis = original.Basis,
                    CategoryOrTarget = $"{original.CategoryOrTarget} (Return: {salesReturn.ReturnNumber})",
                    SalesAmount = -returnedSales,
                    Quantity = -returnedQty,
                    CommissionRate = original.CommissionRate,
                    CalcType = original.CalcType,
                    CommissionAmount = -deductAmount, // Negative amount
                    IsPaid = false,
                    PaymentReference = $"CLAWBACK-{salesReturn.ReturnNumber}",
                    AccountingTransaction = acctTx
                };
                _context.SalesmanCommissionEntries.Add(returnEntry);

                totalDeducted += deductAmount;
            }
        }

        salesReturn.TotalCommissionDeducted = totalDeducted;
    }

    private async Task<string> GenerateNextReturnNumberAsync()
    {
        var prefix = $"RET-{DateTime.Now:yyyyMM}-";
        var last = await _context.SalesReturns
            .Where(r => r.ReturnNumber.StartsWith(prefix))
            .OrderByDescending(r => r.ReturnNumber)
            .FirstOrDefaultAsync();

        int nextSeq = 1;
        if (last != null && last.ReturnNumber.Length >= prefix.Length + 4)
        {
            var seqStr = last.ReturnNumber.Substring(prefix.Length);
            if (int.TryParse(seqStr, out int cur)) nextSeq = cur + 1;
        }

        return $"{prefix}{nextSeq:D4}";
    }

    private async Task<string> GenerateNextInvoiceNumberAsync(Company? company = null)
    {
        company ??= await _companyContext.GetActiveCompanyAsync();
        var prefix = $"{company.InvoicePrefix}{DateTime.Now:yyyyMM}-";
        var last = await _context.TaxInvoices
            .Where(i => i.CompanyId == company.Id && i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        int nextSeq = 1;
        if (last != null && last.InvoiceNumber.Length >= prefix.Length + 4)
        {
            var seqStr = last.InvoiceNumber.Substring(prefix.Length);
            if (int.TryParse(seqStr, out int cur)) nextSeq = cur + 1;
        }

        return $"{prefix}{nextSeq:D4}";
    }

    private async Task<string> GenerateNextReceiptNumberAsync(int? companyId = null)
    {
        var cid = companyId ?? await _companyContext.GetActiveCompanyIdAsync();
        var prefix = $"REC-{DateTime.Now:yyyyMM}-";
        var last = await _context.PaymentReceipts
            .Where(r => r.CompanyId == cid && r.ReceiptNumber.StartsWith(prefix))
            .OrderByDescending(r => r.ReceiptNumber)
            .FirstOrDefaultAsync();

        int nextSeq = 1;
        if (last != null && last.ReceiptNumber.Length >= prefix.Length + 4)
        {
            var seqStr = last.ReceiptNumber.Substring(prefix.Length);
            if (int.TryParse(seqStr, out int cur)) nextSeq = cur + 1;
        }

        return $"{prefix}{nextSeq:D4}";
    }

    [HttpGet]
    public async Task<IActionResult> DownloadEwayBillJson(int id, [FromQuery] EwayBillTransportInput transport)
    {
        var invoice = await _context.TaxInvoices.FindAsync(id);
        if (invoice == null) return NotFound();

        // Update transport info if provided
        if (!string.IsNullOrWhiteSpace(transport.VehicleNumber)) invoice.VehicleNumber = transport.VehicleNumber.Trim();
        if (!string.IsNullOrWhiteSpace(transport.TransporterName)) invoice.TransporterName = transport.TransporterName.Trim();
        if (!string.IsNullOrWhiteSpace(transport.TransporterId)) invoice.TransporterId = transport.TransporterId.Trim();
        if (transport.DistanceKm > 0) invoice.DistanceKm = transport.DistanceKm;
        if (!string.IsNullOrWhiteSpace(transport.VehicleType)) invoice.VehicleType = transport.VehicleType;
        if (!string.IsNullOrWhiteSpace(transport.TransMode)) invoice.TransMode = transport.TransMode;

        await _context.SaveChangesAsync();

        var json = await _ewayBillService.GenerateInvoiceJsonAsync(id, transport);
        var fileName = $"EWB_INV_{invoice.InvoiceNumber}.json";
        return File(Encoding.UTF8.GetBytes(json), "application/json", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEwayBill(SaveEwayBillInput model)
    {
        var invoice = await _context.TaxInvoices.FindAsync(model.Id);
        if (invoice == null) return NotFound();

        invoice.EwayBillNumber = model.EwayBillNumber.Trim();
        invoice.EwayBillDate = model.EwayBillDate ?? DateTime.Today;
        if (!string.IsNullOrWhiteSpace(model.VehicleNumber)) invoice.VehicleNumber = model.VehicleNumber.Trim();
        if (!string.IsNullOrWhiteSpace(model.TransporterName)) invoice.TransporterName = model.TransporterName.Trim();

        await _context.SaveChangesAsync();
        TempData["Success"] = $"E-Way Bill #{invoice.EwayBillNumber} saved against Invoice {invoice.InvoiceNumber}.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadEInvoiceJson(int id)
    {
        var invoice = await _context.TaxInvoices.FindAsync(id);
        if (invoice == null) return NotFound();

        try
        {
            var json = await _eInvoiceService.GenerateStandardInv01JsonAsync(id);
            var fileName = $"Govt_EInvoice_INV01_{invoice.InvoiceNumber}.json";
            return File(Encoding.UTF8.GetBytes(json), "application/json", fileName);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error generating e-invoice JSON: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateEInvoice(int id, string? supplyType, bool generateEwayBill, string? vehicleNumber, string? transporterId, string? transporterName, int? distanceKm)
    {
        var request = new EInvoiceGenerateRequest
        {
            SupplyType = supplyType ?? "B2B",
            GenerateEwayBill = generateEwayBill,
            VehicleNumber = vehicleNumber,
            TransporterId = transporterId,
            TransporterName = transporterName,
            DistanceKm = distanceKm
        };

        var result = await _eInvoiceService.GenerateEInvoiceAsync(id, request);
        if (result.Success)
        {
            TempData["Success"] = $"Government E-Invoice generated successfully! IRN: {result.Irn}";
        }
        else
        {
            TempData["Error"] = $"E-Invoice Generation Failed: {result.Message}";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelEInvoice(int id, string cancelReason, string cancelRemarks)
    {
        var request = new EInvoiceCancelRequest
        {
            CancelReason = cancelReason,
            CancelRemarks = cancelRemarks
        };

        var result = await _eInvoiceService.CancelEInvoiceAsync(id, request);
        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordManualEInvoice(int id, string irn, string ackNo, DateTime ackDate, string? signedQr)
    {
        var request = new EInvoiceManualRequest
        {
            Irn = irn,
            AckNo = ackNo,
            AckDate = ackDate,
            SignedQrCode = signedQr
        };

        var result = await _eInvoiceService.RecordManualEInvoiceAsync(id, request);
        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> PrintEwayBill(int id)
    {
        var invoice = await _context.TaxInvoices
            .Include(i => i.Company)
            .Include(i => i.Customer)
            .Include(i => i.Items)
                .ThenInclude(it => it.Design)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return NotFound();

        var company = invoice.Company ?? await _companyContext.GetActiveCompanyAsync();

        string fromGstin = company.Gstin ?? _config["Company:GSTIN"] ?? "24AABCA1234F1Z9";
        string fromName = company.CompanyName ?? _config["Company:Name"] ?? "AASHANA FASHION";
        string fromAddr1 = company.Address1 ?? _config["Company:Address1"] ?? "Plot 14-16, Garment Industrial Zone";
        string fromAddr2 = company.Address2 ?? _config["Company:Address2"] ?? "Pandesara";
        string fromPlace = company.City ?? _config["Company:City"] ?? "Surat";
        string fromState = company.State ?? _config["Company:State"] ?? "Gujarat";
        int fromPin = int.TryParse(company.PinCode ?? _config["Company:Pincode"], out var pinVal) ? pinVal : 394221;
        int fromStateCode = company.StateCode > 0 ? company.StateCode : (int.TryParse(_config["Company:StateCode"], out var scVal) ? scVal : 24);
        string fromAddr = $"{fromAddr1}, {fromAddr2}, {fromPlace}, {fromState} - {fromPin}";

        string toGstin = !string.IsNullOrWhiteSpace(invoice.CustomerGstin) && invoice.CustomerGstin.Length == 15
            ? invoice.CustomerGstin.Trim().ToUpper()
            : "URP";
        string toName = string.IsNullOrWhiteSpace(invoice.CustomerName) ? "Cash Buyer" : invoice.CustomerName;
        string toAddr = string.IsNullOrWhiteSpace(invoice.ShippingAddress) ? (invoice.BillingAddress ?? "Surat, Gujarat") : invoice.ShippingAddress;
        string toPlace = invoice.Customer?.City ?? "Surat";
        int toPin = _ewayBillService.ExtractPincode(invoice.Customer?.PinCode ?? toAddr, 395002);
        int toState = _ewayBillService.GetStateCode(toGstin, invoice.PlaceOfSupply ?? invoice.Customer?.State);

        var ewbNo = !string.IsNullOrWhiteSpace(invoice.EwayBillNumber)
            ? invoice.EwayBillNumber
            : $"2410{DateTime.Now:yyMMdd}{invoice.Id:D4}";

        var genDate = invoice.EwayBillDate ?? invoice.InvoiceDate;
        var dist = invoice.DistanceKm ?? 50;
        var days = Math.Max(1, (int)Math.Ceiling(dist / 200.0));
        var validUntil = genDate.AddDays(days).Date.AddHours(23).AddMinutes(59);

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

            DocType = "Tax Invoice",
            DocCode = "INV",
            DocNumber = invoice.InvoiceNumber,
            DocDate = invoice.InvoiceDate,
            SupplyType = "Outward - Supply",
            TransactionType = "Regular",
            ReasonForTransportation = "Supply",

            TaxableAmount = invoice.TaxableAmount,
            CgstAmount = invoice.CgstAmount,
            SgstAmount = invoice.SgstAmount,
            IgstAmount = invoice.IgstAmount,
            CessAmount = 0m,
            TotalInvoiceValue = invoice.GrandTotal,

            TransMode = invoice.TransMode switch { "2" => "Rail", "3" => "Air", "4" => "Ship", _ => "Road" },
            VehicleNumber = !string.IsNullOrWhiteSpace(invoice.VehicleNumber) ? invoice.VehicleNumber : "GJ-05-BX-1234",
            VehicleType = invoice.VehicleType == "O" ? "Over Dimensional Cargo" : "Regular",
            TransporterName = !string.IsNullOrWhiteSpace(invoice.TransporterName) ? invoice.TransporterName : "Direct Transport",
            TransporterId = invoice.TransporterId ?? "",
            TransDocNo = "",
            TransDocDate = invoice.InvoiceDate,
            DistanceKm = dist,
            FromPlace = "Surat, Gujarat",

            SourceId = invoice.Id,
            SourceType = "Invoice"
        };

        int sr = 1;
        foreach (var it in invoice.Items)
        {
            vm.Items.Add(new EwayBillPrintItemViewModel
            {
                ItemNo = sr++,
                HsnCode = string.IsNullOrWhiteSpace(it.HsnCode) ? "6204" : it.HsnCode,
                ProductName = string.IsNullOrWhiteSpace(it.Description) ? "Garments" : it.Description,
                Description = $"{it.Colour} {it.Size}".Trim(),
                Quantity = it.Quantity,
                Unit = "PCS",
                TaxableValue = it.TaxableValue,
                CgstRate = invoice.IsInterState ? 0 : it.GstRate / 2m,
                SgstRate = invoice.IsInterState ? 0 : it.GstRate / 2m,
                IgstRate = invoice.IsInterState ? it.GstRate : 0
            });
        }

        return View("~/Views/Shared/PrintEwayBill.cshtml", vm);
    }
}
