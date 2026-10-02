using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AashanaFashion.Data;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public class DoubleEntryService : IDoubleEntryService
{
    private readonly AppDbContext _context;

    public DoubleEntryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task EnsureDefaultChartOfAccountsAsync(int companyId)
    {
        bool hasAccounts = await _context.Accounts.AnyAsync(a => a.CompanyId == companyId);
        if (hasAccounts) return;

        // Standard Chart of Accounts tailored for Indian Garment Manufacturing (Surat)
        var defaultAccounts = new List<Account>
        {
            // Assets (100000 series)
            new() { CompanyId = companyId, Code = "101000", Name = "Cash on Hand", Type = AccountType.Asset, Classification = AccountClassification.BankAndCash, IsReconcilable = true, IsSystemAccount = true },
            new() { CompanyId = companyId, Code = "102000", Name = "Bank - Current Account", Type = AccountType.Asset, Classification = AccountClassification.BankAndCash, IsReconcilable = true, IsSystemAccount = true },
            new() { CompanyId = companyId, Code = "103000", Name = "Accounts Receivable (Debtors)", Type = AccountType.Asset, Classification = AccountClassification.AccountsReceivable, IsReconcilable = true, IsSystemAccount = true },
            new() { CompanyId = companyId, Code = "104000", Name = "Raw Material & Fabric Stock", Type = AccountType.Asset, Classification = AccountClassification.Inventory, IsReconcilable = false, IsSystemAccount = true },
            new() { CompanyId = companyId, Code = "105000", Name = "Finished Garments Stock", Type = AccountType.Asset, Classification = AccountClassification.Inventory, IsReconcilable = false, IsSystemAccount = true },
            new() { CompanyId = companyId, Code = "106100", Name = "Input Tax Credit - CGST", Type = AccountType.Asset, Classification = AccountClassification.CurrentAsset, IsReconcilable = false, IsSystemAccount = true },
            new() { CompanyId = companyId, Code = "106200", Name = "Input Tax Credit - SGST", Type = AccountType.Asset, Classification = AccountClassification.CurrentAsset, IsReconcilable = false, IsSystemAccount = true },
            new() { CompanyId = companyId, Code = "106300", Name = "Input Tax Credit - IGST", Type = AccountType.Asset, Classification = AccountClassification.CurrentAsset, IsReconcilable = false, IsSystemAccount = true },
            new() { CompanyId = companyId, Code = "108000", Name = "Plant & Stitching Machinery", Type = AccountType.Asset, Classification = AccountClassification.FixedAsset, IsReconcilable = false },

            // Liabilities (200000 series)
            new() { CompanyId = companyId, Code = "201000", Name = "Accounts Payable (Creditors / Vendors)", Type = AccountType.Liability, Classification = AccountClassification.AccountsPayable, IsReconcilable = true, IsSystemAccount = true },
            new() { CompanyId = companyId, Code = "202100", Name = "Output GST Liability - CGST", Type = AccountType.Liability, Classification = AccountClassification.DutiesAndTaxes, IsReconcilable = false, IsSystemAccount = true },
            new() { CompanyId = companyId, Code = "202200", Name = "Output GST Liability - SGST", Type = AccountType.Liability, Classification = AccountClassification.DutiesAndTaxes, IsReconcilable = false, IsSystemAccount = true },
            new() { CompanyId = companyId, Code = "202300", Name = "Output GST Liability - IGST", Type = AccountType.Liability, Classification = AccountClassification.DutiesAndTaxes, IsReconcilable = false, IsSystemAccount = true },
            new() { CompanyId = companyId, Code = "203000", Name = "TDS Payable", Type = AccountType.Liability, Classification = AccountClassification.DutiesAndTaxes, IsReconcilable = false },
            new() { CompanyId = companyId, Code = "204000", Name = "Salary & Wages Payable", Type = AccountType.Liability, Classification = AccountClassification.CurrentLiability, IsReconcilable = false },

            // Equity (300000 series)
            new() { CompanyId = companyId, Code = "301000", Name = "Owner's Capital", Type = AccountType.Equity, Classification = AccountClassification.Equity, IsReconcilable = false, IsSystemAccount = true },
            new() { CompanyId = companyId, Code = "302000", Name = "Retained Earnings", Type = AccountType.Equity, Classification = AccountClassification.Equity, IsReconcilable = false, IsSystemAccount = true },

            // Income (400000 series)
            new() { CompanyId = companyId, Code = "401000", Name = "Garment Sales Revenue", Type = AccountType.Income, Classification = AccountClassification.OperatingRevenue, IsReconcilable = false, IsSystemAccount = true },
            new() { CompanyId = companyId, Code = "402000", Name = "Job Work Income", Type = AccountType.Income, Classification = AccountClassification.OperatingRevenue, IsReconcilable = false },
            new() { CompanyId = companyId, Code = "403000", Name = "Discount & Rebates Received", Type = AccountType.Income, Classification = AccountClassification.OtherIncome, IsReconcilable = false },

            // Direct Costs / COGS (500000 series)
            new() { CompanyId = companyId, Code = "501000", Name = "Fabric & Raw Material Purchases", Type = AccountType.Expense, Classification = AccountClassification.CostOfGoodsSold, IsReconcilable = false, IsSystemAccount = true },
            new() { CompanyId = companyId, Code = "502000", Name = "Dying & Printing Charges", Type = AccountType.Expense, Classification = AccountClassification.DirectManufacturingExpense, IsReconcilable = false },
            new() { CompanyId = companyId, Code = "503000", Name = "Embroidery & Handwork Charges", Type = AccountType.Expense, Classification = AccountClassification.DirectManufacturingExpense, IsReconcilable = false },
            new() { CompanyId = companyId, Code = "504000", Name = "Stitching & Tailoring Labor", Type = AccountType.Expense, Classification = AccountClassification.DirectManufacturingExpense, IsReconcilable = false },
            new() { CompanyId = companyId, Code = "505000", Name = "Roll Press & Finishing Charges", Type = AccountType.Expense, Classification = AccountClassification.DirectManufacturingExpense, IsReconcilable = false },
            new() { CompanyId = companyId, Code = "506000", Name = "Packaging, Buttons & Trims", Type = AccountType.Expense, Classification = AccountClassification.CostOfGoodsSold, IsReconcilable = false },

            // Operating Expenses (600000 series)
            new() { CompanyId = companyId, Code = "601000", Name = "Staff Salaries & Factory Wages", Type = AccountType.Expense, Classification = AccountClassification.OperatingExpense, IsReconcilable = false },
            new() { CompanyId = companyId, Code = "602000", Name = "Factory & Office Rent", Type = AccountType.Expense, Classification = AccountClassification.OperatingExpense, IsReconcilable = false },
            new() { CompanyId = companyId, Code = "603000", Name = "Electricity & Power", Type = AccountType.Expense, Classification = AccountClassification.OperatingExpense, IsReconcilable = false },
            new() { CompanyId = companyId, Code = "604000", Name = "Freight & Transportation", Type = AccountType.Expense, Classification = AccountClassification.OperatingExpense, IsReconcilable = false },
            new() { CompanyId = companyId, Code = "605000", Name = "Salesman Commission Expense", Type = AccountType.Expense, Classification = AccountClassification.OperatingExpense, IsReconcilable = false },
            new() { CompanyId = companyId, Code = "606000", Name = "Bank Charges & Gateway Fees", Type = AccountType.Expense, Classification = AccountClassification.FinancialExpense, IsReconcilable = false },
            new() { CompanyId = companyId, Code = "607000", Name = "Round-off / Difference", Type = AccountType.Expense, Classification = AccountClassification.OperatingExpense, IsReconcilable = false, IsSystemAccount = true }
        };

        _context.Accounts.AddRange(defaultAccounts);
        await _context.SaveChangesAsync();

        // Default Journals
        var arAcc = defaultAccounts.First(a => a.Code == "103000");
        var revAcc = defaultAccounts.First(a => a.Code == "401000");
        var apAcc = defaultAccounts.First(a => a.Code == "201000");
        var purAcc = defaultAccounts.First(a => a.Code == "501000");
        var bnkAcc = defaultAccounts.First(a => a.Code == "102000");
        var cshAcc = defaultAccounts.First(a => a.Code == "101000");

        var defaultJournals = new List<Journal>
        {
            new() { CompanyId = companyId, Code = "INV", Name = "Customer Invoices", Type = JournalType.Sales, DefaultDebitAccountId = arAcc.Id, DefaultCreditAccountId = revAcc.Id },
            new() { CompanyId = companyId, Code = "BILL", Name = "Vendor Bills", Type = JournalType.Purchase, DefaultDebitAccountId = purAcc.Id, DefaultCreditAccountId = apAcc.Id },
            new() { CompanyId = companyId, Code = "BNK", Name = "Bank Operations", Type = JournalType.Bank, DefaultDebitAccountId = bnkAcc.Id, DefaultCreditAccountId = bnkAcc.Id },
            new() { CompanyId = companyId, Code = "CSH", Name = "Cash Operations", Type = JournalType.Cash, DefaultDebitAccountId = cshAcc.Id, DefaultCreditAccountId = cshAcc.Id },
            new() { CompanyId = companyId, Code = "MISC", Name = "Miscellaneous Operations", Type = JournalType.General }
        };

        _context.Journals.AddRange(defaultJournals);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Account>> GetAccountsAsync(int companyId, AccountType? type = null)
    {
        await EnsureDefaultChartOfAccountsAsync(companyId);
        var q = _context.Accounts.Where(a => a.CompanyId == companyId);
        if (type.HasValue) q = q.Where(a => a.Type == type.Value);
        return await q.OrderBy(a => a.Code).ToListAsync();
    }

    public async Task<List<Journal>> GetJournalsAsync(int companyId)
    {
        await EnsureDefaultChartOfAccountsAsync(companyId);
        return await _context.Journals
            .Where(j => j.CompanyId == companyId && j.IsActive)
            .OrderBy(j => j.Name)
            .ToListAsync();
    }

    public async Task<Account?> GetAccountByIdAsync(int id)
    {
        return await _context.Accounts
            .Include(a => a.ParentAccount)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Account> CreateAccountAsync(Account account)
    {
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task<bool> UpdateAccountAsync(Account account)
    {
        var existing = await _context.Accounts.FindAsync(account.Id);
        if (existing == null) return false;

        existing.Name = account.Name;
        existing.Type = account.Type;
        existing.Classification = account.Classification;
        existing.ParentAccountId = account.ParentAccountId;
        existing.Description = account.Description;
        existing.IsActive = account.IsActive;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<JournalEntry?> PostTaxInvoiceAsync(int invoiceId)
    {
        var invoice = await _context.TaxInvoices
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null || invoice.PaymentStatus == InvoicePaymentStatus.Cancelled)
            return null;

        await EnsureDefaultChartOfAccountsAsync(invoice.CompanyId);

        // Check if already posted
        string sourceDoc = $"TaxInvoice:{invoice.Id}";
        var existingEntry = await _context.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.CompanyId == invoice.CompanyId && e.SourceDocument == sourceDoc);

        if (existingEntry != null)
            return existingEntry;

        var journal = await _context.Journals.FirstOrDefaultAsync(j => j.CompanyId == invoice.CompanyId && j.Type == JournalType.Sales);
        if (journal == null) return null;

        var arAccount = await _context.Accounts.FirstAsync(a => a.CompanyId == invoice.CompanyId && a.Code == "103000");
        var salesAccount = await _context.Accounts.FirstAsync(a => a.CompanyId == invoice.CompanyId && a.Code == "401000");

        var entryNumber = await GenerateEntryNumberAsync(invoice.CompanyId, "INV", invoice.InvoiceDate);

        var entry = new JournalEntry
        {
            CompanyId = invoice.CompanyId,
            EntryNumber = entryNumber,
            Date = invoice.InvoiceDate,
            JournalId = journal.Id,
            Reference = invoice.InvoiceNumber,
            Narration = $"Sales invoice {invoice.InvoiceNumber} to {invoice.CustomerName}",
            Status = JournalEntryStatus.Posted,
            SourceDocument = sourceDoc,
            TotalAmount = invoice.GrandTotal,
            CreatedBy = "System"
        };

        // 1. Debit Accounts Receivable (Customer)
        entry.Lines.Add(new JournalEntryLine
        {
            AccountId = arAccount.Id,
            Debit = invoice.GrandTotal,
            Credit = 0m,
            CustomerId = invoice.CustomerId,
            Description = $"Invoice {invoice.InvoiceNumber}"
        });

        // 2. Credit Sales Revenue
        entry.Lines.Add(new JournalEntryLine
        {
            AccountId = salesAccount.Id,
            Debit = 0m,
            Credit = invoice.TaxableAmount,
            CustomerId = invoice.CustomerId,
            Description = $"Taxable sales for {invoice.InvoiceNumber}"
        });

        // 3. Credit Output Taxes
        if (invoice.IsInterState && invoice.IgstAmount > 0)
        {
            var igstAccount = await _context.Accounts.FirstAsync(a => a.CompanyId == invoice.CompanyId && a.Code == "202300");
            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = igstAccount.Id,
                Debit = 0m,
                Credit = invoice.IgstAmount,
                Description = $"IGST Output for {invoice.InvoiceNumber}"
            });
        }
        else
        {
            if (invoice.CgstAmount > 0)
            {
                var cgstAccount = await _context.Accounts.FirstAsync(a => a.CompanyId == invoice.CompanyId && a.Code == "202100");
                entry.Lines.Add(new JournalEntryLine
                {
                    AccountId = cgstAccount.Id,
                    Debit = 0m,
                    Credit = invoice.CgstAmount,
                    Description = $"CGST Output for {invoice.InvoiceNumber}"
                });
            }
            if (invoice.SgstAmount > 0)
            {
                var sgstAccount = await _context.Accounts.FirstAsync(a => a.CompanyId == invoice.CompanyId && a.Code == "202200");
                entry.Lines.Add(new JournalEntryLine
                {
                    AccountId = sgstAccount.Id,
                    Debit = 0m,
                    Credit = invoice.SgstAmount,
                    Description = $"SGST Output for {invoice.InvoiceNumber}"
                });
            }
        }

        // 4. Handle Round-off
        if (invoice.RoundOff != 0)
        {
            var roundAccount = await _context.Accounts.FirstAsync(a => a.CompanyId == invoice.CompanyId && a.Code == "607000");
            if (invoice.RoundOff < 0)
            {
                // Round down: extra debit
                entry.Lines.Add(new JournalEntryLine
                {
                    AccountId = roundAccount.Id,
                    Debit = Math.Abs(invoice.RoundOff),
                    Credit = 0m,
                    Description = "Invoice rounding discount"
                });
            }
            else
            {
                // Round up: extra credit
                entry.Lines.Add(new JournalEntryLine
                {
                    AccountId = roundAccount.Id,
                    Debit = 0m,
                    Credit = invoice.RoundOff,
                    Description = "Invoice rounding gain"
                });
            }
        }

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    public async Task<JournalEntry?> PostPaymentReceiptAsync(int receiptId)
    {
        var receipt = await _context.PaymentReceipts
            .Include(r => r.Customer)
            .Include(r => r.TaxInvoice)
            .FirstOrDefaultAsync(r => r.Id == receiptId);

        if (receipt == null) return null;

        await EnsureDefaultChartOfAccountsAsync(receipt.CompanyId);

        string sourceDoc = $"PaymentReceipt:{receipt.Id}";
        var existing = await _context.JournalEntries.FirstOrDefaultAsync(e => e.CompanyId == receipt.CompanyId && e.SourceDocument == sourceDoc);
        if (existing != null) return existing;

        var journalCode = receipt.PaymentMode == PaymentMode.Cash ? "CSH" : "BNK";
        var journal = await _context.Journals.FirstOrDefaultAsync(j => j.CompanyId == receipt.CompanyId && j.Code == journalCode);
        if (journal == null) return null;

        var debitAccountCode = receipt.PaymentMode == PaymentMode.Cash ? "101000" : "102000";
        var debitAccount = await _context.Accounts.FirstAsync(a => a.CompanyId == receipt.CompanyId && a.Code == debitAccountCode);
        var arAccount = await _context.Accounts.FirstAsync(a => a.CompanyId == receipt.CompanyId && a.Code == "103000");

        var entryNumber = await GenerateEntryNumberAsync(receipt.CompanyId, journalCode, receipt.PaymentDate);

        var entry = new JournalEntry
        {
            CompanyId = receipt.CompanyId,
            EntryNumber = entryNumber,
            Date = receipt.PaymentDate,
            JournalId = journal.Id,
            Reference = receipt.ReferenceNumber ?? receipt.ReceiptNumber,
            Narration = $"Payment received from {receipt.Customer?.CustomerName} against Inv #{receipt.TaxInvoice?.InvoiceNumber}",
            Status = JournalEntryStatus.Posted,
            SourceDocument = sourceDoc,
            TotalAmount = receipt.Amount,
            CreatedBy = "System"
        };

        // Dr Bank / Cash
        entry.Lines.Add(new JournalEntryLine
        {
            AccountId = debitAccount.Id,
            Debit = receipt.Amount,
            Credit = 0m,
            CustomerId = receipt.CustomerId,
            Description = $"Receipt {receipt.ReceiptNumber}"
        });

        // Cr Accounts Receivable (Customer)
        entry.Lines.Add(new JournalEntryLine
        {
            AccountId = arAccount.Id,
            Debit = 0m,
            Credit = receipt.Amount,
            CustomerId = receipt.CustomerId,
            Description = $"Clear AR for {receipt.ReceiptNumber}"
        });

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    public async Task<JournalEntry?> PostPurchaseOrderBillAsync(int purchaseOrderId)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Details)
            .Include(p => p.Vendor)
            .FirstOrDefaultAsync(p => p.Id == purchaseOrderId);

        if (po == null || po.Status == PurchaseOrderStatus.Cancelled) return null;

        await EnsureDefaultChartOfAccountsAsync(po.CompanyId);

        string sourceDoc = $"PurchaseOrder:{po.Id}";
        var existing = await _context.JournalEntries.FirstOrDefaultAsync(e => e.CompanyId == po.CompanyId && e.SourceDocument == sourceDoc);
        if (existing != null) return existing;

        var journal = await _context.Journals.FirstOrDefaultAsync(j => j.CompanyId == po.CompanyId && j.Type == JournalType.Purchase);
        if (journal == null) return null;

        var purchaseAccount = await _context.Accounts.FirstAsync(a => a.CompanyId == po.CompanyId && a.Code == "501000");
        var apAccount = await _context.Accounts.FirstAsync(a => a.CompanyId == po.CompanyId && a.Code == "201000");

        decimal totalTaxable = po.Details.Sum(d => d.TotalPrice - d.DiscountAmount);
        decimal totalTax = po.Details.Sum(d => d.GstAmount);
        decimal totalGrand = totalTaxable + totalTax;

        string vendorState = (po.Vendor?.State ?? "Gujarat").Trim();
        bool isInterState = !vendorState.Equals("Gujarat", StringComparison.OrdinalIgnoreCase);

        var entryNumber = await GenerateEntryNumberAsync(po.CompanyId, "BILL", po.OrderDate);

        var entry = new JournalEntry
        {
            CompanyId = po.CompanyId,
            EntryNumber = entryNumber,
            Date = po.OrderDate,
            JournalId = journal.Id,
            Reference = po.InvoiceNumber ?? po.PoNumber,
            Narration = $"Purchase bill from {po.Vendor?.VendorName} for PO #{po.PoNumber}",
            Status = JournalEntryStatus.Posted,
            SourceDocument = sourceDoc,
            TotalAmount = totalGrand,
            CreatedBy = "System"
        };

        // 1. Dr Fabric & Raw Material Purchase
        entry.Lines.Add(new JournalEntryLine
        {
            AccountId = purchaseAccount.Id,
            Debit = totalTaxable,
            Credit = 0m,
            VendorId = po.VendorId,
            Description = $"Purchases PO #{po.PoNumber}"
        });

        // 2. Dr Input GST
        if (isInterState && totalTax > 0)
        {
            var igstAccount = await _context.Accounts.FirstAsync(a => a.CompanyId == po.CompanyId && a.Code == "106300");
            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = igstAccount.Id,
                Debit = totalTax,
                Credit = 0m,
                VendorId = po.VendorId,
                Description = $"Input IGST for PO #{po.PoNumber}"
            });
        }
        else if (totalTax > 0)
        {
            var cgstAccount = await _context.Accounts.FirstAsync(a => a.CompanyId == po.CompanyId && a.Code == "106100");
            var sgstAccount = await _context.Accounts.FirstAsync(a => a.CompanyId == po.CompanyId && a.Code == "106200");
            decimal halfTax = Math.Round(totalTax / 2m, 2);

            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = cgstAccount.Id,
                Debit = halfTax,
                Credit = 0m,
                VendorId = po.VendorId,
                Description = $"Input CGST for PO #{po.PoNumber}"
            });
            entry.Lines.Add(new JournalEntryLine
            {
                AccountId = sgstAccount.Id,
                Debit = totalTax - halfTax,
                Credit = 0m,
                VendorId = po.VendorId,
                Description = $"Input SGST for PO #{po.PoNumber}"
            });
        }

        // 3. Cr Accounts Payable (Vendor)
        entry.Lines.Add(new JournalEntryLine
        {
            AccountId = apAccount.Id,
            Debit = 0m,
            Credit = totalGrand,
            VendorId = po.VendorId,
            Description = $"Accounts Payable for PO #{po.PoNumber}"
        });

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    public async Task<JournalEntry?> PostVendorPaymentAsync(int paymentId)
    {
        var payment = await _context.VendorPayments
            .Include(p => p.Vendor)
            .Include(p => p.PurchaseOrder)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null) return null;

        int companyId = payment.PurchaseOrder?.CompanyId ?? 1;
        await EnsureDefaultChartOfAccountsAsync(companyId);

        string sourceDoc = $"VendorPayment:{payment.Id}";
        var existing = await _context.JournalEntries.FirstOrDefaultAsync(e => e.CompanyId == companyId && e.SourceDocument == sourceDoc);
        if (existing != null) return existing;

        var journalCode = payment.PaymentMode == PaymentMode.Cash ? "CSH" : "BNK";
        var journal = await _context.Journals.FirstOrDefaultAsync(j => j.CompanyId == companyId && j.Code == journalCode);
        if (journal == null) return null;

        var creditAccountCode = payment.PaymentMode == PaymentMode.Cash ? "101000" : "102000";
        var creditAccount = await _context.Accounts.FirstAsync(a => a.CompanyId == companyId && a.Code == creditAccountCode);
        var apAccount = await _context.Accounts.FirstAsync(a => a.CompanyId == companyId && a.Code == "201000");

        var entryNumber = await GenerateEntryNumberAsync(companyId, journalCode, payment.PaymentDate);

        var entry = new JournalEntry
        {
            CompanyId = companyId,
            EntryNumber = entryNumber,
            Date = payment.PaymentDate,
            JournalId = journal.Id,
            Reference = payment.ReferenceNumber ?? payment.VoucherNumber,
            Narration = $"Payment made to {payment.Vendor?.VendorName} (Voucher #{payment.VoucherNumber})",
            Status = JournalEntryStatus.Posted,
            SourceDocument = sourceDoc,
            TotalAmount = payment.Amount,
            CreatedBy = "System"
        };

        // Dr Accounts Payable (Vendor)
        entry.Lines.Add(new JournalEntryLine
        {
            AccountId = apAccount.Id,
            Debit = payment.Amount,
            Credit = 0m,
            VendorId = payment.VendorId,
            Description = $"Payout voucher #{payment.VoucherNumber}"
        });

        // Cr Bank / Cash
        entry.Lines.Add(new JournalEntryLine
        {
            AccountId = creditAccount.Id,
            Debit = 0m,
            Credit = payment.Amount,
            VendorId = payment.VendorId,
            Description = $"Clear AP via {journalCode}"
        });

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    public async Task<(bool Success, string? ErrorMessage, JournalEntry? Entry)> CreateManualJournalEntryAsync(int companyId, CreateJournalEntryViewModel model, string? createdBy)
    {
        await EnsureDefaultChartOfAccountsAsync(companyId);

        decimal totalDebit = model.Lines.Sum(l => l.Debit);
        decimal totalCredit = model.Lines.Sum(l => l.Credit);

        if (Math.Abs(totalDebit - totalCredit) > 0.01m)
        {
            return (false, $"Double-entry imbalance! Total Debit (₹{totalDebit:N2}) must exactly equal Total Credit (₹{totalCredit:N2}).", null);
        }

        if (totalDebit <= 0)
        {
            return (false, "Journal entry amount must be greater than zero.", null);
        }

        var journal = await _context.Journals.FirstOrDefaultAsync(j => j.Id == model.JournalId && j.CompanyId == companyId);
        if (journal == null)
        {
            return (false, "Selected journal was not found.", null);
        }

        var entryNumber = await GenerateEntryNumberAsync(companyId, journal.Code, model.Date);

        var entry = new JournalEntry
        {
            CompanyId = companyId,
            EntryNumber = entryNumber,
            Date = model.Date,
            JournalId = journal.Id,
            Reference = model.Reference,
            Narration = model.Narration,
            Status = JournalEntryStatus.Posted,
            TotalAmount = totalDebit,
            CreatedBy = createdBy ?? "Admin"
        };

        foreach (var l in model.Lines)
        {
            if (l.Debit > 0 || l.Credit > 0)
            {
                entry.Lines.Add(new JournalEntryLine
                {
                    AccountId = l.AccountId,
                    Debit = l.Debit,
                    Credit = l.Credit,
                    CustomerId = l.CustomerId,
                    VendorId = l.VendorId,
                    Description = l.Description ?? model.Narration
                });
            }
        }

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
        return (true, null, entry);
    }

    public async Task<List<JournalEntry>> GetJournalEntriesAsync(int companyId, DateTime? startDate = null, DateTime? endDate = null, int? journalId = null)
    {
        await EnsureDefaultChartOfAccountsAsync(companyId);
        var q = _context.JournalEntries
            .Where(e => e.CompanyId == companyId)
            .Include(e => e.Journal)
            .Include(e => e.Lines)
                .ThenInclude(l => l.Account)
            .Include(e => e.Lines)
                .ThenInclude(l => l.Customer)
            .Include(e => e.Lines)
                .ThenInclude(l => l.Vendor)
            .AsQueryable();

        if (startDate.HasValue) q = q.Where(e => e.Date >= startDate.Value);
        if (endDate.HasValue) q = q.Where(e => e.Date <= endDate.Value.AddDays(1).AddSeconds(-1));
        if (journalId.HasValue) q = q.Where(e => e.JournalId == journalId.Value);

        return await q.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).ToListAsync();
    }

    public async Task<JournalEntry?> GetJournalEntryDetailsAsync(int id)
    {
        return await _context.JournalEntries
            .Include(e => e.Journal)
            .Include(e => e.Lines)
                .ThenInclude(l => l.Account)
            .Include(e => e.Lines)
                .ThenInclude(l => l.Customer)
            .Include(e => e.Lines)
                .ThenInclude(l => l.Vendor)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<TrialBalanceViewModel> GetTrialBalanceAsync(int companyId, DateTime asOfDate, DateTime? fromDate = null)
    {
        await EnsureDefaultChartOfAccountsAsync(companyId);
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId) ?? new Company { Id = companyId };

        var accounts = await _context.Accounts
            .Where(a => a.CompanyId == companyId && a.IsActive)
            .OrderBy(a => a.Code)
            .ToListAsync();

        var start = fromDate ?? new DateTime(asOfDate.Year, 1, 1);
        var end = asOfDate.Date.AddDays(1).AddSeconds(-1);

        var allLines = await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry!.CompanyId == companyId &&
                        l.JournalEntry.Status == JournalEntryStatus.Posted &&
                        l.JournalEntry.Date <= end)
            .ToListAsync();

        var model = new TrialBalanceViewModel
        {
            AsOfDate = asOfDate,
            FromDate = fromDate,
            Company = company
        };

        foreach (var acc in accounts)
        {
            var accLines = allLines.Where(l => l.AccountId == acc.Id).ToList();

            decimal openDr = 0m, openCr = 0m;
            decimal periodDr = 0m, periodCr = 0m;

            if (fromDate.HasValue)
            {
                var openingLines = accLines.Where(l => l.JournalEntry!.Date < fromDate.Value);
                openDr = openingLines.Sum(l => l.Debit);
                openCr = openingLines.Sum(l => l.Credit);

                var periodLines = accLines.Where(l => l.JournalEntry!.Date >= fromDate.Value && l.JournalEntry.Date <= end);
                periodDr = periodLines.Sum(l => l.Debit);
                periodCr = periodLines.Sum(l => l.Credit);
            }
            else
            {
                periodDr = accLines.Sum(l => l.Debit);
                periodCr = accLines.Sum(l => l.Credit);
            }

            decimal net = (openDr + periodDr) - (openCr + periodCr);
            decimal closeDr = 0m, closeCr = 0m;

            if (acc.Type == AccountType.Asset || acc.Type == AccountType.Expense)
            {
                if (net >= 0) closeDr = net;
                else closeCr = Math.Abs(net);
            }
            else
            {
                if (net <= 0) closeCr = Math.Abs(net);
                else closeDr = net;
            }

            model.Rows.Add(new TrialBalanceRow
            {
                AccountId = acc.Id,
                Code = acc.Code,
                Name = acc.Name,
                Type = acc.Type,
                Classification = acc.Classification,
                OpeningDebit = openDr,
                OpeningCredit = openCr,
                PeriodDebit = periodDr,
                PeriodCredit = periodCr,
                ClosingDebit = closeDr,
                ClosingCredit = closeCr
            });
        }

        return model;
    }

    public async Task<ProfitAndLossViewModel> GetProfitAndLossAsync(int companyId, DateTime fromDate, DateTime toDate)
    {
        await EnsureDefaultChartOfAccountsAsync(companyId);
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId) ?? new Company { Id = companyId };

        var end = toDate.Date.AddDays(1).AddSeconds(-1);

        var lines = await _context.JournalEntryLines
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry!.CompanyId == companyId &&
                        l.JournalEntry.Status == JournalEntryStatus.Posted &&
                        l.JournalEntry.Date >= fromDate.Date &&
                        l.JournalEntry.Date <= end &&
                        (l.Account!.Type == AccountType.Income || l.Account.Type == AccountType.Expense))
            .ToListAsync();

        var model = new ProfitAndLossViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            Company = company
        };

        model.OperatingRevenue.Title = "Operating Sales & Revenue";
        model.OtherIncome.Title = "Other Operating Income";
        model.CostOfGoodsSold.Title = "Cost of Goods Sold (Fabrics, Job Work & Production)";
        model.OperatingExpenses.Title = "General & Administrative Expenses";
        model.FinancialExpenses.Title = "Financial Expenses & Charges";

        var grouped = lines.GroupBy(l => l.Account!);
        foreach (var grp in grouped)
        {
            var acc = grp.Key;
            decimal balance = 0m;

            if (acc.Type == AccountType.Income)
            {
                balance = grp.Sum(l => l.Credit) - grp.Sum(l => l.Debit);
                if (acc.Classification == AccountClassification.OperatingRevenue)
                    model.OperatingRevenue.Lines.Add((acc.Name, acc.Code, balance));
                else
                    model.OtherIncome.Lines.Add((acc.Name, acc.Code, balance));
            }
            else
            {
                balance = grp.Sum(l => l.Debit) - grp.Sum(l => l.Credit);
                if (acc.Classification == AccountClassification.CostOfGoodsSold || acc.Classification == AccountClassification.DirectManufacturingExpense)
                    model.CostOfGoodsSold.Lines.Add((acc.Name, acc.Code, balance));
                else if (acc.Classification == AccountClassification.FinancialExpense)
                    model.FinancialExpenses.Lines.Add((acc.Name, acc.Code, balance));
                else
                    model.OperatingExpenses.Lines.Add((acc.Name, acc.Code, balance));
            }
        }

        return model;
    }

    public async Task<BalanceSheetViewModel> GetBalanceSheetAsync(int companyId, DateTime asOfDate)
    {
        await EnsureDefaultChartOfAccountsAsync(companyId);
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId) ?? new Company { Id = companyId };

        var end = asOfDate.Date.AddDays(1).AddSeconds(-1);

        var lines = await _context.JournalEntryLines
            .Include(l => l.Account)
            .Include(l => l.JournalEntry)
            .Where(l => l.JournalEntry!.CompanyId == companyId &&
                        l.JournalEntry.Status == JournalEntryStatus.Posted &&
                        l.JournalEntry.Date <= end)
            .ToListAsync();

        var model = new BalanceSheetViewModel
        {
            AsOfDate = asOfDate,
            Company = company
        };

        model.BankAndCash.Title = "Cash and Cash Equivalents";
        model.AccountsReceivable.Title = "Accounts Receivable (Trade Debtors)";
        model.CurrentAssets.Title = "Input GST & Other Current Assets";
        model.Inventory.Title = "Inventories";
        model.FixedAssets.Title = "Property, Plant & Equipment";

        model.AccountsPayable.Title = "Accounts Payable (Trade Creditors)";
        model.DutiesAndTaxes.Title = "Output GST & Taxes Payable";
        model.CurrentLiabilities.Title = "Current Liabilities & Provisions";
        model.NonCurrentLiabilities.Title = "Non-Current Liabilities & Long-term Debt";
        model.CapitalAndReserves.Title = "Share Capital & Reserves";

        var accounts = await _context.Accounts
            .Where(a => a.CompanyId == companyId && a.IsActive)
            .ToListAsync();

        decimal totalIncome = 0m;
        decimal totalExpense = 0m;

        foreach (var acc in accounts)
        {
            var accLines = lines.Where(l => l.AccountId == acc.Id).ToList();
            if (!accLines.Any()) continue;

            if (acc.Type == AccountType.Asset)
            {
                decimal net = accLines.Sum(l => l.Debit) - accLines.Sum(l => l.Credit);
                if (net != 0)
                {
                    switch (acc.Classification)
                    {
                        case AccountClassification.BankAndCash: model.BankAndCash.Lines.Add((acc.Name, acc.Code, net)); break;
                        case AccountClassification.AccountsReceivable: model.AccountsReceivable.Lines.Add((acc.Name, acc.Code, net)); break;
                        case AccountClassification.Inventory: model.Inventory.Lines.Add((acc.Name, acc.Code, net)); break;
                        case AccountClassification.FixedAsset: model.FixedAssets.Lines.Add((acc.Name, acc.Code, net)); break;
                        default: model.CurrentAssets.Lines.Add((acc.Name, acc.Code, net)); break;
                    }
                }
            }
            else if (acc.Type == AccountType.Liability)
            {
                decimal net = accLines.Sum(l => l.Credit) - accLines.Sum(l => l.Debit);
                if (net != 0)
                {
                    switch (acc.Classification)
                    {
                        case AccountClassification.AccountsPayable: model.AccountsPayable.Lines.Add((acc.Name, acc.Code, net)); break;
                        case AccountClassification.DutiesAndTaxes: model.DutiesAndTaxes.Lines.Add((acc.Name, acc.Code, net)); break;
                        case AccountClassification.NonCurrentLiability: model.NonCurrentLiabilities.Lines.Add((acc.Name, acc.Code, net)); break;
                        default: model.CurrentLiabilities.Lines.Add((acc.Name, acc.Code, net)); break;
                    }
                }
            }
            else if (acc.Type == AccountType.Equity)
            {
                decimal net = accLines.Sum(l => l.Credit) - accLines.Sum(l => l.Debit);
                if (net != 0)
                {
                    model.CapitalAndReserves.Lines.Add((acc.Name, acc.Code, net));
                }
            }
            else if (acc.Type == AccountType.Income)
            {
                totalIncome += accLines.Sum(l => l.Credit) - accLines.Sum(l => l.Debit);
            }
            else if (acc.Type == AccountType.Expense)
            {
                totalExpense += accLines.Sum(l => l.Debit) - accLines.Sum(l => l.Credit);
            }
        }

        // Net profit of the period flows into Balance Sheet Equity
        model.CurrentPeriodProfitOrLoss = totalIncome - totalExpense;

        return model;
    }

    public async Task<GeneralLedgerAccountViewModel> GetGeneralLedgerAccountAsync(int companyId, int accountId, DateTime fromDate, DateTime toDate)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId && a.CompanyId == companyId);
        if (account == null) throw new InvalidOperationException("Account not found.");

        var end = toDate.Date.AddDays(1).AddSeconds(-1);

        var priorLines = await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.AccountId == accountId &&
                        l.JournalEntry!.CompanyId == companyId &&
                        l.JournalEntry.Status == JournalEntryStatus.Posted &&
                        l.JournalEntry.Date < fromDate.Date)
            .ToListAsync();

        decimal openingBalance = 0m;
        if (account.Type == AccountType.Asset || account.Type == AccountType.Expense)
            openingBalance = priorLines.Sum(l => l.Debit) - priorLines.Sum(l => l.Credit);
        else
            openingBalance = priorLines.Sum(l => l.Credit) - priorLines.Sum(l => l.Debit);

        var periodLines = await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
                .ThenInclude(e => e!.Journal)
            .Include(l => l.Customer)
            .Include(l => l.Vendor)
            .Where(l => l.AccountId == accountId &&
                        l.JournalEntry!.CompanyId == companyId &&
                        l.JournalEntry.Status == JournalEntryStatus.Posted &&
                        l.JournalEntry.Date >= fromDate.Date &&
                        l.JournalEntry.Date <= end)
            .OrderBy(l => l.JournalEntry!.Date)
            .ThenBy(l => l.Id)
            .ToListAsync();

        var model = new GeneralLedgerAccountViewModel
        {
            Account = account,
            FromDate = fromDate,
            ToDate = toDate,
            OpeningBalance = openingBalance
        };

        decimal currentBalance = openingBalance;
        foreach (var l in periodLines)
        {
            if (account.Type == AccountType.Asset || account.Type == AccountType.Expense)
                currentBalance += (l.Debit - l.Credit);
            else
                currentBalance += (l.Credit - l.Debit);

            model.Rows.Add(new GeneralLedgerRow
            {
                Date = l.JournalEntry!.Date,
                EntryNumber = l.JournalEntry.EntryNumber,
                EntryId = l.JournalEntry.Id,
                JournalCode = l.JournalEntry.Journal?.Code ?? "GL",
                Reference = l.JournalEntry.Reference,
                PartnerName = l.Customer?.CustomerName ?? l.Vendor?.VendorName,
                Narration = l.Description ?? l.JournalEntry.Narration,
                Debit = l.Debit,
                Credit = l.Credit,
                RunningBalance = currentBalance
            });
        }

        return model;
    }

    private async Task<string> GenerateEntryNumberAsync(int companyId, string prefix, DateTime date)
    {
        string yearMonth = date.ToString("yyyyMM");
        int count = await _context.JournalEntries
            .CountAsync(e => e.CompanyId == companyId && e.Date.Year == date.Year && e.Date.Month == date.Month);

        return $"{prefix}-{yearMonth}-{(count + 1):D4}";
    }
}
