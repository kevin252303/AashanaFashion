using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize(Roles = "Admin,SuperAdmin,System Admin,Manager")]
public class AccountingController : Controller
{
    private readonly AppDbContext _context;

    public AccountingController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Accounting
    public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, TransactionType? type, string? category, string? search)
    {
        var query = _context.AccountingTransactions
            .Include(t => t.Vendor)
            .Include(t => t.Customer)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(t =>
                (t.Reference != null && t.Reference.ToLower().Contains(s)) ||
                (t.Description != null && t.Description.ToLower().Contains(s)) ||
                (t.Category != null && t.Category.ToLower().Contains(s)) ||
                (t.Vendor != null && t.Vendor.VendorName.ToLower().Contains(s)) ||
                (t.Customer != null && t.Customer.CustomerName.ToLower().Contains(s)));
        }
        if (startDate.HasValue)
        {
            query = query.Where(t => t.Date >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(t => t.Date <= endDate.Value.AddDays(1).AddSeconds(-1));
        }
        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(t => t.Category == category);
        }

        var transactions = await query.OrderByDescending(t => t.Date).ToListAsync();

        // Calculate totals
        var allTransactions = await _context.AccountingTransactions.ToListAsync();
        var totalIncome = allTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var totalExpense = allTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        // Calculate Receivables & Payables & GST
        var allInvoices = await _context.TaxInvoices.ToListAsync();
        var totalReceivables = allInvoices.Sum(i => i.BalanceDue);
        var totalOutputGst = allInvoices.Sum(i => i.CgstAmount + i.SgstAmount + i.IgstAmount);

        var vendorPayments = await _context.VendorPayments.ToListAsync();
        var totalVendorPaid = vendorPayments.Sum(p => p.Amount);

        ViewBag.TotalIncome = totalIncome;
        ViewBag.TotalExpense = totalExpense;
        ViewBag.NetBalance = totalIncome - totalExpense;
        ViewBag.TotalReceivables = totalReceivables;
        ViewBag.TotalOutputGst = totalOutputGst;
        ViewBag.TotalVendorPaid = totalVendorPaid;

        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
        ViewBag.Type = type;
        ViewBag.Category = category;
        ViewBag.Search = search;

        // Categories list for filter dropdown
        ViewBag.Categories = await _context.AccountingTransactions
            .Select(t => t.Category)
            .Distinct()
            .ToListAsync();

        ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
        ViewBag.PurchaseOrders = await _context.PurchaseOrders.Include(p => p.Vendor).OrderByDescending(p => p.Id).ToListAsync();

        return View(transactions);
    }

    // GET: /Accounting/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Vendors = new SelectList(await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync(), "Id", "VendorName");
        ViewBag.Customers = new SelectList(await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync(), "Id", "CustomerName");
        return View(new AccountingTransaction());
    }

    // POST: /Accounting/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountingTransaction transaction)
    {
        if (ModelState.IsValid)
        {
            _context.AccountingTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Transaction recorded successfully.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Vendors = new SelectList(await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync(), "Id", "VendorName", transaction.VendorId);
        ViewBag.Customers = new SelectList(await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync(), "Id", "CustomerName", transaction.CustomerId);
        return View(transaction);
    }

    // POST: /Accounting/RecordVendorPayment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordVendorPayment(RecordVendorPaymentInputModel model)
    {
        if (model.VendorId <= 0 || model.Amount <= 0)
        {
            TempData["Error"] = "Vendor and valid payment amount are required.";
            return RedirectToAction(nameof(Index));
        }

        var vendor = await _context.Vendors.FindAsync(model.VendorId);
        if (vendor == null) return NotFound();

        var prefix = $"PAY-{DateTime.Now:yyyyMM}-";
        var last = await _context.VendorPayments
            .Where(p => p.VoucherNumber.StartsWith(prefix))
            .OrderByDescending(p => p.VoucherNumber)
            .FirstOrDefaultAsync();

        int nextSeq = 1;
        if (last != null && last.VoucherNumber.Length >= prefix.Length + 4)
        {
            var seqStr = last.VoucherNumber.Substring(prefix.Length);
            if (int.TryParse(seqStr, out int cur)) nextSeq = cur + 1;
        }
        string voucherNo = $"{prefix}{nextSeq:D4}";

        var voucher = new VendorPayment
        {
            VoucherNumber = voucherNo,
            PaymentDate = model.PaymentDate,
            VendorId = model.VendorId,
            PurchaseOrderId = model.PurchaseOrderId > 0 ? model.PurchaseOrderId : null,
            Amount = model.Amount,
            PaymentMode = model.PaymentMode,
            ReferenceNumber = model.ReferenceNumber?.Trim(),
            Notes = model.Notes?.Trim(),
            CreatedDate = DateTime.Now
        };

        _context.VendorPayments.Add(voucher);

        // Record expense transaction in general ledger
        _context.AccountingTransactions.Add(new AccountingTransaction
        {
            Date = model.PaymentDate,
            Type = TransactionType.Expense,
            Amount = model.Amount,
            Category = "Vendor Payout",
            Description = $"Payment made to {vendor.VendorName} (Voucher: {voucherNo}) via {model.PaymentMode}",
            Reference = string.IsNullOrWhiteSpace(model.ReferenceNumber) ? voucherNo : model.ReferenceNumber.Trim(),
            VendorId = model.VendorId
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Vendor payment of ₹{model.Amount:N2} recorded to {vendor.VendorName} (Voucher #{voucherNo}).";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Accounting/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var transaction = await _context.AccountingTransactions.FindAsync(id);
        if (transaction == null)
        {
            return NotFound();
        }

        ViewBag.Vendors = new SelectList(await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync(), "Id", "VendorName", transaction.VendorId);
        ViewBag.Customers = new SelectList(await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync(), "Id", "CustomerName", transaction.CustomerId);
        return View(transaction);
    }

    // POST: /Accounting/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AccountingTransaction transaction)
    {
        if (id != transaction.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(transaction);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Transaction updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TransactionExists(transaction.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Vendors = new SelectList(await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync(), "Id", "VendorName", transaction.VendorId);
        ViewBag.Customers = new SelectList(await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync(), "Id", "CustomerName", transaction.CustomerId);
        return View(transaction);
    }

    // POST: /Accounting/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var transaction = await _context.AccountingTransactions.FindAsync(id);
        if (transaction != null)
        {
            _context.AccountingTransactions.Remove(transaction);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Transaction deleted successfully.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> TransactionExists(int id)
    {
        return await _context.AccountingTransactions.AnyAsync(e => e.Id == id);
    }
}
