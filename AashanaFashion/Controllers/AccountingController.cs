using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AashanaFashion.Controllers
{
    [Authorize(Roles = "System Admin")]
    [PermissionAuthorize("Accounting", "CanView")]
    public class AccountingController : Controller
    {
        private readonly AppDbContext _context;

        public AccountingController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Accounting
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, TransactionType? type, string? category)
        {
            var query = _context.AccountingTransactions
                .Include(t => t.Vendor)
                .Include(t => t.Customer)
                .AsQueryable();

            // Apply filters
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

            ViewBag.TotalIncome = totalIncome;
            ViewBag.TotalExpense = totalExpense;
            ViewBag.NetBalance = totalIncome - totalExpense;

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Type = type;
            ViewBag.Category = category;

            // Categories list for filter dropdown
            ViewBag.Categories = await _context.AccountingTransactions
                .Select(t => t.Category)
                .Distinct()
                .ToListAsync();

            return View(transactions);
        }

        // GET: /Accounting/Create
        [PermissionAuthorize("Accounting", "CanCreate")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Vendors = new SelectList(await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync(), "Id", "VendorName");
            ViewBag.Customers = new SelectList(await _context.Customers.Where(c => c.IsActive).OrderBy(c => c.CustomerName).ToListAsync(), "Id", "CustomerName");
            return View(new AccountingTransaction());
        }

        // POST: /Accounting/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermissionAuthorize("Accounting", "CanCreate")]
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

        // GET: /Accounting/Edit/5
        [PermissionAuthorize("Accounting", "CanEdit")]
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
        [PermissionAuthorize("Accounting", "CanEdit")]
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
        [PermissionAuthorize("Accounting", "CanDelete")]
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
}
