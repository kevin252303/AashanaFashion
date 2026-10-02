using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize(Roles = "Admin,SuperAdmin,System Admin,Manager")]
public class AccountingController : Controller
{
    private readonly AppDbContext _context;
    private readonly IDoubleEntryService _doubleEntryService;
    private readonly ICompanyContext _companyContext;
    private readonly IAgingAndMatchingService _agingService;

    public AccountingController(AppDbContext context, IDoubleEntryService doubleEntryService, ICompanyContext companyContext, IAgingAndMatchingService agingService)
    {
        _context = context;
        _doubleEntryService = doubleEntryService;
        _companyContext = companyContext;
        _agingService = agingService;
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

    // GET: /Accounting/Commissions
    public async Task<IActionResult> Commissions(string? salesman, string? category, bool? pendingOnly, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.SalesmanCommissionEntries
            .Include(e => e.TaxInvoice)
            .Include(e => e.Customer)
            .Include(e => e.AccountingTransaction)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(salesman))
        {
            var s = salesman.Trim().ToLower();
            query = query.Where(e => e.SalesmanName.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var c = category.Trim().ToLower();
            query = query.Where(e => e.CategoryOrTarget != null && e.CategoryOrTarget.ToLower().Contains(c));
        }

        if (pendingOnly == true)
        {
            query = query.Where(e => !e.IsPaid);
        }

        if (startDate.HasValue)
        {
            query = query.Where(e => e.EntryDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.EntryDate <= endDate.Value.AddDays(1).AddSeconds(-1));
        }

        var entries = await query.OrderByDescending(e => e.EntryDate).ToListAsync();

        // Calculate summaries
        var allEntries = await _context.SalesmanCommissionEntries.ToListAsync();
        var model = new SalesCommissionDashboardViewModel
        {
            TotalCommissionIncurred = allEntries.Sum(e => e.CommissionAmount),
            TotalCommissionPaid = allEntries.Where(e => e.IsPaid).Sum(e => e.CommissionAmount),
            TotalCommissionPending = allEntries.Where(e => !e.IsPaid).Sum(e => e.CommissionAmount),
            TotalInvoicesWithCommission = allEntries.Select(e => e.TaxInvoiceId).Distinct().Count(),
            RecentEntries = entries,
            SalesmanSummaries = allEntries
                .GroupBy(e => e.SalesmanName)
                .Select(g => new SalesmanSummaryItem
                {
                    SalesmanName = g.Key,
                    TotalSales = g.Sum(e => e.SalesAmount),
                    TotalCommission = g.Sum(e => e.CommissionAmount),
                    PaidCommission = g.Where(e => e.IsPaid).Sum(e => e.CommissionAmount),
                    PendingCommission = g.Where(e => !e.IsPaid).Sum(e => e.CommissionAmount),
                    InvoiceCount = g.Select(e => e.TaxInvoiceId).Distinct().Count()
                })
                .OrderByDescending(s => s.TotalCommission)
                .ToList()
        };

        ViewBag.Salesman = salesman;
        ViewBag.Category = category;
        ViewBag.PendingOnly = pendingOnly;
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

        ViewBag.SalesmenList = await _context.SalesmanCommissionEntries
            .Select(e => e.SalesmanName)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        return View(model);
    }

    // POST: /Accounting/SettleCommission
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SettleCommission(int id, string? paymentReference)
    {
        var entry = await _context.SalesmanCommissionEntries
            .Include(e => e.TaxInvoice)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entry == null) return NotFound();

        entry.IsPaid = true;
        entry.PaidDate = DateTime.Now;
        entry.PaymentReference = paymentReference?.Trim() ?? $"PAID-{DateTime.Now:yyyyMMddHHmmss}";

        // Record payout in accounting ledger
        _context.AccountingTransactions.Add(new AccountingTransaction
        {
            Date = DateTime.Now,
            Type = TransactionType.Expense,
            Amount = entry.CommissionAmount,
            Category = "Salesman Commission Paid",
            Description = $"Commission payout to {entry.SalesmanName} for Invoice #{entry.TaxInvoice?.InvoiceNumber} (Ref: {entry.PaymentReference})",
            Reference = entry.PaymentReference,
            CustomerId = entry.CustomerId
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Commission of ₹{entry.CommissionAmount:N2} marked as paid to {entry.SalesmanName}.";
        return RedirectToAction(nameof(Commissions));
    }

    // ——— DOUBLE-ENTRY GENERAL LEDGER & CHART OF ACCOUNTS (OPTION B) ———

    // GET: /Accounting/ChartOfAccounts
    public async Task<IActionResult> ChartOfAccounts(AccountType? type)
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        var accounts = await _doubleEntryService.GetAccountsAsync(companyId, type);

        // Precompute current balances from JournalEntryLines
        var accountIds = accounts.Select(a => a.Id).ToList();
        var lines = await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => accountIds.Contains(l.AccountId) &&
                        l.JournalEntry!.CompanyId == companyId &&
                        l.JournalEntry.Status == JournalEntryStatus.Posted)
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                TotalDebit = g.Sum(x => x.Debit),
                TotalCredit = g.Sum(x => x.Credit)
            })
            .ToDictionaryAsync(x => x.AccountId, x => (x.TotalDebit, x.TotalCredit));

        ViewBag.Balances = lines;
        ViewBag.SelectedType = type;
        return View(accounts);
    }

    // POST: /Accounting/CreateAccount
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAccount(Account model)
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        model.CompanyId = companyId;

        if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrWhiteSpace(model.Name))
        {
            TempData["Error"] = "Account Code and Name are required.";
            return RedirectToAction(nameof(ChartOfAccounts));
        }

        bool codeExists = await _context.Accounts.AnyAsync(a => a.CompanyId == companyId && a.Code == model.Code.Trim());
        if (codeExists)
        {
            TempData["Error"] = $"Account code '{model.Code}' already exists.";
            return RedirectToAction(nameof(ChartOfAccounts));
        }

        model.Code = model.Code.Trim();
        model.Name = model.Name.Trim();
        await _doubleEntryService.CreateAccountAsync(model);

        TempData["Success"] = $"Account '{model.Code} - {model.Name}' created successfully.";
        return RedirectToAction(nameof(ChartOfAccounts));
    }

    // GET: /Accounting/JournalEntries
    public async Task<IActionResult> JournalEntries(DateTime? startDate, DateTime? endDate, int? journalId)
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        var entries = await _doubleEntryService.GetJournalEntriesAsync(companyId, startDate, endDate, journalId);
        ViewBag.Journals = await _doubleEntryService.GetJournalsAsync(companyId);
        ViewBag.SelectedJournalId = journalId;
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

        return View(entries);
    }

    // GET: /Accounting/JournalEntryDetails/5
    public async Task<IActionResult> JournalEntryDetails(int id)
    {
        var entry = await _doubleEntryService.GetJournalEntryDetailsAsync(id);
        if (entry == null) return NotFound();
        return View(entry);
    }

    // GET: /Accounting/CreateJournalEntry
    public async Task<IActionResult> CreateJournalEntry()
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        ViewBag.Journals = await _doubleEntryService.GetJournalsAsync(companyId);
        ViewBag.Accounts = await _doubleEntryService.GetAccountsAsync(companyId);
        ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();

        var model = new CreateJournalEntryViewModel
        {
            Date = DateTime.Today
        };
        // Add two default lines
        model.Lines.Add(new CreateJournalLineItem());
        model.Lines.Add(new CreateJournalLineItem());

        return View(model);
    }

    // POST: /Accounting/CreateJournalEntry
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateJournalEntry(CreateJournalEntryViewModel model)
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();

        var user = User.Identity?.Name ?? "Admin";
        var result = await _doubleEntryService.CreateManualJournalEntryAsync(companyId, model, user);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            ViewBag.Journals = await _doubleEntryService.GetJournalsAsync(companyId);
            ViewBag.Accounts = await _doubleEntryService.GetAccountsAsync(companyId);
            ViewBag.Customers = await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync();
            ViewBag.Vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
            return View(model);
        }

        TempData["Success"] = $"Journal Entry {result.Entry?.EntryNumber} posted successfully.";
        return RedirectToAction(nameof(JournalEntries));
    }

    // GET: /Accounting/TrialBalance
    public async Task<IActionResult> TrialBalance(DateTime? asOfDate, DateTime? fromDate)
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        DateTime targetDate = asOfDate ?? DateTime.Today;
        var model = await _doubleEntryService.GetTrialBalanceAsync(companyId, targetDate, fromDate);
        return View(model);
    }

    // GET: /Accounting/ProfitAndLoss
    public async Task<IActionResult> ProfitAndLoss(DateTime? fromDate, DateTime? toDate)
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        DateTime start = fromDate ?? new DateTime(DateTime.Today.Year, 1, 1);
        DateTime end = toDate ?? DateTime.Today;

        var model = await _doubleEntryService.GetProfitAndLossAsync(companyId, start, end);
        return View(model);
    }

    // GET: /Accounting/BalanceSheet
    public async Task<IActionResult> BalanceSheet(DateTime? asOfDate)
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        DateTime targetDate = asOfDate ?? DateTime.Today;
        var model = await _doubleEntryService.GetBalanceSheetAsync(companyId, targetDate);
        return View(model);
    }

    // GET: /Accounting/GeneralLedger
    public async Task<IActionResult> GeneralLedger(int? accountId, DateTime? fromDate, DateTime? toDate)
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        var accounts = await _doubleEntryService.GetAccountsAsync(companyId);
        ViewBag.Accounts = accounts;

        DateTime start = fromDate ?? new DateTime(DateTime.Today.Year, 1, 1);
        DateTime end = toDate ?? DateTime.Today;

        ViewBag.FromDate = start.ToString("yyyy-MM-dd");
        ViewBag.ToDate = end.ToString("yyyy-MM-dd");
        ViewBag.SelectedAccountId = accountId;

        if (!accountId.HasValue && accounts.Any())
        {
            // Default to Accounts Receivable or Cash
            var defaultAcc = accounts.FirstOrDefault(a => a.Code == "103000") ?? accounts.First();
            accountId = defaultAcc.Id;
            ViewBag.SelectedAccountId = accountId;
        }

        if (accountId.HasValue)
        {
            var model = await _doubleEntryService.GetGeneralLedgerAccountAsync(companyId, accountId.Value, start, end);
            return View(model);
        }

        return View(null);
    }

    // POST: /Accounting/SyncHistoricalEntries
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncHistoricalEntries()
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        await _doubleEntryService.EnsureDefaultChartOfAccountsAsync(companyId);

        int invoiceCount = 0;
        int receiptCount = 0;
        int poCount = 0;
        int paymentCount = 0;

        // 1. Sync Tax Invoices
        var invoices = await _context.TaxInvoices
            .Where(i => i.CompanyId == companyId && i.PaymentStatus != InvoicePaymentStatus.Cancelled)
            .Select(i => i.Id)
            .ToListAsync();

        foreach (var invId in invoices)
        {
            var entry = await _doubleEntryService.PostTaxInvoiceAsync(invId);
            if (entry != null) invoiceCount++;
        }

        // 2. Sync Payment Receipts
        var receipts = await _context.PaymentReceipts
            .Where(r => r.CompanyId == companyId)
            .Select(r => r.Id)
            .ToListAsync();

        foreach (var recId in receipts)
        {
            var entry = await _doubleEntryService.PostPaymentReceiptAsync(recId);
            if (entry != null) receiptCount++;
        }

        // 3. Sync Purchase Orders
        var pos = await _context.PurchaseOrders
            .Where(p => p.CompanyId == companyId && p.Status != PurchaseOrderStatus.Cancelled)
            .Select(p => p.Id)
            .ToListAsync();

        foreach (var poId in pos)
        {
            var entry = await _doubleEntryService.PostPurchaseOrderBillAsync(poId);
            if (entry != null) poCount++;
        }

        // 4. Sync Vendor Payments
        var payments = await _context.VendorPayments
            .Select(p => p.Id)
            .ToListAsync();

        foreach (var payId in payments)
        {
            var entry = await _doubleEntryService.PostVendorPaymentAsync(payId);
            if (entry != null) paymentCount++;
        }

        TempData["Success"] = $"Double-entry synchronization complete! Synchronized: {invoiceCount} Sales Invoices, {receiptCount} Receipts, {poCount} Purchase Bills, {paymentCount} Vendor Payouts.";
        return RedirectToAction(nameof(JournalEntries));
    }

    // ——— AGING ANALYSIS & 3-WAY MATCHING (OPTION C) ———

    // GET: /Accounting/AgedReceivables
    public async Task<IActionResult> AgedReceivables(DateTime? asOfDate)
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        DateTime target = asOfDate ?? DateTime.Today;
        var model = await _agingService.GetAgedReceivablesAsync(companyId, target);
        return View(model);
    }

    // GET: /Accounting/CustomerStatement?customerId=5
    public async Task<IActionResult> CustomerStatement(int customerId, DateTime? fromDate, DateTime? toDate)
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        DateTime start = fromDate ?? new DateTime(DateTime.Today.Year, 1, 1);
        DateTime end = toDate ?? DateTime.Today;

        var model = await _agingService.GetCustomerStatementAsync(companyId, customerId, start, end);
        return View(model);
    }

    // GET: /Accounting/AgedPayables
    public async Task<IActionResult> AgedPayables(DateTime? asOfDate)
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        DateTime target = asOfDate ?? DateTime.Today;
        var model = await _agingService.GetAgedPayablesAsync(companyId, target);
        return View(model);
    }

    // GET: /Accounting/VendorStatement?vendorId=3
    public async Task<IActionResult> VendorStatement(int vendorId, DateTime? fromDate, DateTime? toDate)
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        DateTime start = fromDate ?? new DateTime(DateTime.Today.Year, 1, 1);
        DateTime end = toDate ?? DateTime.Today;

        var model = await _agingService.GetVendorStatementAsync(companyId, vendorId, start, end);
        return View(model);
    }

    // GET: /Accounting/ThreeWayMatching
    public async Task<IActionResult> ThreeWayMatching(string? status)
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        var model = await _agingService.GetThreeWayMatchingAsync(companyId, status);
        return View(model);
    }
}

