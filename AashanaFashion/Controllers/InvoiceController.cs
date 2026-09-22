using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class InvoiceController : Controller
{
    private readonly AppDbContext _context;

    public InvoiceController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? search, InvoicePaymentStatus? status)
    {
        var query = _context.TaxInvoices
            .Include(i => i.Customer)
            .Include(i => i.SalesOrder)
            .Include(i => i.Receipts)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var sLower = search.Trim().ToLower();
            query = query.Where(i => i.InvoiceNumber.ToLower().Contains(sLower) ||
                                     i.CustomerName.ToLower().Contains(sLower) ||
                                     (i.CustomerGstin != null && i.CustomerGstin.ToLower().Contains(sLower)));
        }

        if (status.HasValue)
        {
            query = query.Where(i => i.PaymentStatus == status.Value);
        }

        var allInvoices = await _context.TaxInvoices.ToListAsync();
        var totalInvoiced = allInvoices.Sum(i => i.GrandTotal);
        var totalCollected = allInvoices.Sum(i => i.PaidAmount);
        var outstandingReceivables = allInvoices.Sum(i => i.BalanceDue);
        var overdueCount = allInvoices.Count(i => i.DueDate < DateTime.Today && i.PaymentStatus != InvoicePaymentStatus.Paid);

        ViewBag.TotalInvoiced = totalInvoiced;
        ViewBag.TotalCollected = totalCollected;
        ViewBag.OutstandingReceivables = outstandingReceivables;
        ViewBag.OverdueCount = overdueCount;
        ViewBag.Search = search;
        ViewBag.Status = status;

        var invoices = await query.OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.Id).ToListAsync();
        return View(invoices);
    }

    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    [HttpGet]
    public async Task<IActionResult> Create(int? salesOrderId)
    {
        var model = new CreateInvoiceViewModel
        {
            InvoiceNumber = await GenerateNextInvoiceNumberAsync(),
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(15),
            SalesOrderId = salesOrderId
        };

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

                foreach (var d in so.Details)
                {
                    model.Items.Add(new InvoiceItemInputModel
                    {
                        DesignId = d.DesignId,
                        Description = $"{d.Design?.DesignNumber ?? "Garment"} ({d.Colour} - {d.Size})",
                        HsnCode = "6204",
                        Colour = d.Colour,
                        Size = d.Size,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                        DiscountAmount = 0m,
                        GstRate = 5.0m
                    });
                }
            }
        }

        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.SalesOrders = await _context.SalesOrders.Include(s => s.Customer).OrderByDescending(s => s.Id).ToListAsync();

        return View(model);
    }

    [Authorize(Roles = "Admin,SuperAdmin,Manager")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateInvoiceViewModel model)
    {
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
            ViewBag.SalesOrders = await _context.SalesOrders.Include(s => s.Customer).OrderByDescending(s => s.Id).ToListAsync();
            return View(model);
        }

        decimal subTotal = 0m;
        decimal totalDiscount = 0m;

        var invoice = new TaxInvoice
        {
            InvoiceNumber = string.IsNullOrWhiteSpace(model.InvoiceNumber) ? await GenerateNextInvoiceNumberAsync() : model.InvoiceNumber.Trim(),
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
            Date = invoice.InvoiceDate,
            Type = TransactionType.Income,
            Amount = invoice.GrandTotal,
            Category = "Sales Invoice",
            Description = $"Tax Invoice {invoice.InvoiceNumber} generated for {invoice.CustomerName}",
            Reference = invoice.InvoiceNumber,
            CustomerId = invoice.CustomerId
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Tax Invoice {invoice.InvoiceNumber} created successfully.";
        return RedirectToAction(nameof(Details), new { id = invoice.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var invoice = await _context.TaxInvoices
            .Include(i => i.Customer)
            .Include(i => i.SalesOrder)
            .Include(i => i.Items)
            .ThenInclude(item => item.Design)
            .Include(i => i.Receipts)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return NotFound();

        return View(invoice);
    }

    public async Task<IActionResult> Print(int id)
    {
        var invoice = await _context.TaxInvoices
            .Include(i => i.Customer)
            .Include(i => i.SalesOrder)
            .Include(i => i.Items)
            .ThenInclude(item => item.Design)
            .Include(i => i.Receipts)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return NotFound();

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
            Date = model.PaymentDate,
            Type = TransactionType.Income,
            Amount = model.Amount,
            Category = "Customer Payment",
            Description = $"Payment received against {invoice.InvoiceNumber} (Receipt: {receiptNo}) via {model.PaymentMode}",
            Reference = string.IsNullOrWhiteSpace(model.ReferenceNumber) ? receiptNo : model.ReferenceNumber.Trim(),
            CustomerId = invoice.CustomerId
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

        return Json(new
        {
            customerName = customer.CustomerName,
            gstin = customer.GstNumber ?? "",
            pan = customer.PanNumber ?? "",
            billingAddress = customer.Address ?? "",
            shippingAddress = customer.Address ?? "",
            state = state,
            isInterState = isInterState,
            placeOfSupply = !string.IsNullOrEmpty(state) ? state : "Gujarat (24)"
        });
    }

    private async Task<string> GenerateNextInvoiceNumberAsync()
    {
        var prefix = $"INV-{DateTime.Now:yyyyMM}-";
        var last = await _context.TaxInvoices
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
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

    private async Task<string> GenerateNextReceiptNumberAsync()
    {
        var prefix = $"REC-{DateTime.Now:yyyyMM}-";
        var last = await _context.PaymentReceipts
            .Where(r => r.ReceiptNumber.StartsWith(prefix))
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
}
