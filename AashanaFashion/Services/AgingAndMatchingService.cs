using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AashanaFashion.Data;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public class AgingAndMatchingService : IAgingAndMatchingService
{
    private readonly AppDbContext _context;

    public AgingAndMatchingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AgedReceivablesViewModel> GetAgedReceivablesAsync(int companyId, DateTime asOfDate)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId) ?? new Company { Id = companyId };

        var openInvoices = await _context.TaxInvoices
            .Where(i => i.CompanyId == companyId &&
                        i.PaymentStatus != InvoicePaymentStatus.Paid &&
                        i.PaymentStatus != InvoicePaymentStatus.Cancelled &&
                        i.InvoiceDate <= asOfDate)
            .Include(i => i.Customer)
            .ToListAsync();

        var model = new AgedReceivablesViewModel
        {
            AsOfDate = asOfDate,
            Company = company
        };

        var byCustomer = openInvoices.GroupBy(i => i.CustomerId);

        foreach (var grp in byCustomer)
        {
            var customer = grp.First().Customer;
            var row = new AgedReceivableCustomerRow
            {
                CustomerId = grp.Key,
                CustomerName = customer?.CustomerName ?? grp.First().CustomerName,
                Phone = customer?.Phone,
                City = customer?.City,
                Gstin = customer?.GstNumber ?? grp.First().CustomerGstin,
                InvoiceCount = grp.Count()
            };

            foreach (var inv in grp)
            {
                decimal unpaid = inv.BalanceDue;
                row.TotalOutstanding += unpaid;

                int overdueDays = (asOfDate.Date - inv.DueDate.Date).Days;

                if (overdueDays <= 0)
                {
                    row.CurrentNotDue += unpaid;
                }
                else if (overdueDays <= 30)
                {
                    row.Days1To30 += unpaid;
                }
                else if (overdueDays <= 60)
                {
                    row.Days31To60 += unpaid;
                }
                else if (overdueDays <= 90)
                {
                    row.Days61To90 += unpaid;
                }
                else
                {
                    row.Days90Plus += unpaid;
                }

                if (!row.OldestDueDate.HasValue || inv.DueDate < row.OldestDueDate.Value)
                {
                    row.OldestDueDate = inv.DueDate;
                }
            }

            if (row.TotalOutstanding > 0)
            {
                model.Customers.Add(row);
            }
        }

        model.Customers = model.Customers.OrderByDescending(c => c.TotalOutstanding).ToList();
        return model;
    }

    public async Task<CustomerStatementViewModel> GetCustomerStatementAsync(int companyId, int customerId, DateTime fromDate, DateTime toDate)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId) ?? new Company { Id = companyId };
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId)
            ?? throw new InvalidOperationException("Customer not found.");

        var model = new CustomerStatementViewModel
        {
            Customer = customer,
            Company = company,
            FromDate = fromDate,
            ToDate = toDate
        };

        var end = toDate.Date.AddDays(1).AddSeconds(-1);

        // Fetch all customer invoices, receipts, and returns up to 'end'
        var allInvoices = await _context.TaxInvoices
            .Where(i => i.CompanyId == companyId && i.CustomerId == customerId && i.InvoiceDate <= end && i.PaymentStatus != InvoicePaymentStatus.Cancelled)
            .ToListAsync();

        var allReceipts = await _context.PaymentReceipts
            .Where(r => r.CompanyId == companyId && r.CustomerId == customerId && r.PaymentDate <= end)
            .Include(r => r.TaxInvoice)
            .ToListAsync();

        var allReturns = await _context.SalesReturns
            .Where(r => r.CompanyId == companyId && r.CustomerId == customerId && r.ReturnDate <= end)
            .Include(r => r.TaxInvoice)
            .ToListAsync();

        // 1. Calculate opening balance (all prior to fromDate)
        decimal priorInvoices = allInvoices.Where(i => i.InvoiceDate < fromDate.Date).Sum(i => i.GrandTotal);
        decimal priorReceipts = allReceipts.Where(r => r.PaymentDate < fromDate.Date).Sum(r => r.Amount);
        decimal priorReturns = allReturns.Where(r => r.ReturnDate < fromDate.Date).Sum(r => r.GrandTotal);
        model.OpeningBalance = priorInvoices - (priorReceipts + priorReturns);

        // 2. Period Transactions
        var periodInvoices = allInvoices.Where(i => i.InvoiceDate >= fromDate.Date && i.InvoiceDate <= end);
        var periodReceipts = allReceipts.Where(r => r.PaymentDate >= fromDate.Date && r.PaymentDate <= end);
        var periodReturns = allReturns.Where(r => r.ReturnDate >= fromDate.Date && r.ReturnDate <= end);

        var txList = new List<StatementTransactionRow>();

        foreach (var inv in periodInvoices)
        {
            txList.Add(new StatementTransactionRow
            {
                Date = inv.InvoiceDate,
                Type = "Invoice",
                ReferenceNumber = inv.InvoiceNumber,
                Description = $"Tax Invoice #{inv.InvoiceNumber} (Due: {inv.DueDate:dd-MMM-yyyy})",
                Debit = inv.GrandTotal,
                Credit = 0m
            });
        }

        foreach (var rec in periodReceipts)
        {
            txList.Add(new StatementTransactionRow
            {
                Date = rec.PaymentDate,
                Type = "Payment",
                ReferenceNumber = rec.ReceiptNumber,
                Description = $"Payment Receipt #{rec.ReceiptNumber} ({rec.PaymentMode}) - Ref: {rec.ReferenceNumber ?? "N/A"}",
                Debit = 0m,
                Credit = rec.Amount,
                PaymentMode = rec.PaymentMode.ToString()
            });
        }

        foreach (var ret in periodReturns)
        {
            txList.Add(new StatementTransactionRow
            {
                Date = ret.ReturnDate,
                Type = "Return",
                ReferenceNumber = ret.ReturnNumber,
                Description = $"Sales Return #{ret.ReturnNumber} against Inv #{ret.TaxInvoice?.InvoiceNumber}",
                Debit = 0m,
                Credit = ret.GrandTotal
            });
        }

        // Sort chronologically and compute running balance
        decimal running = model.OpeningBalance;
        foreach (var tx in txList.OrderBy(t => t.Date).ThenBy(t => t.Type == "Invoice" ? 0 : 1))
        {
            running += (tx.Debit - tx.Credit);
            tx.RunningBalance = running;
            model.Transactions.Add(tx);
        }

        // 3. Compute Aging breakdown as of toDate
        foreach (var inv in allInvoices.Where(i => i.PaymentStatus != InvoicePaymentStatus.Paid))
        {
            decimal unpaid = inv.BalanceDue;
            int overdueDays = (toDate.Date - inv.DueDate.Date).Days;

            if (overdueDays <= 0) model.CurrentNotDue += unpaid;
            else if (overdueDays <= 30) model.Days1To30 += unpaid;
            else if (overdueDays <= 60) model.Days31To60 += unpaid;
            else if (overdueDays <= 90) model.Days61To90 += unpaid;
            else model.Days90Plus += unpaid;
        }

        return model;
    }

    public async Task<AgedPayablesViewModel> GetAgedPayablesAsync(int companyId, DateTime asOfDate)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId) ?? new Company { Id = companyId };

        var openPos = await _context.PurchaseOrders
            .Where(p => p.CompanyId == companyId &&
                        p.Status != PurchaseOrderStatus.Cancelled &&
                        p.OrderDate <= asOfDate)
            .Include(p => p.Vendor)
            .Include(p => p.Bills)
            .ToListAsync();

        var poIds = openPos.Select(p => p.Id).ToList();
        var allPayments = await _context.VendorPayments
            .Where(v => v.PurchaseOrderId.HasValue && poIds.Contains(v.PurchaseOrderId.Value))
            .ToListAsync();

        var model = new AgedPayablesViewModel
        {
            AsOfDate = asOfDate,
            Company = company
        };

        var byVendor = openPos.GroupBy(p => p.VendorId);

        foreach (var grp in byVendor)
        {
            var vendor = grp.First().Vendor;
            var row = new AgedPayableVendorRow
            {
                VendorId = grp.Key,
                VendorName = vendor?.VendorName ?? "Vendor",
                Phone = vendor?.Phone,
                City = vendor?.City,
                Gstin = vendor?.GstNumber,
                BillCount = grp.Count()
            };

            foreach (var po in grp)
            {
                decimal paid = allPayments.Where(v => v.PurchaseOrderId == po.Id).Sum(v => v.Amount);
                decimal due = Math.Max(0m, po.TotalAmount - paid);

                if (due <= 0) continue;

                row.TotalOutstanding += due;

                // Credit terms default: 30 days from order date
                DateTime dueDate = po.OrderDate.AddDays(30);
                int overdueDays = (asOfDate.Date - dueDate.Date).Days;

                if (overdueDays <= 0) row.CurrentNotDue += due;
                else if (overdueDays <= 30) row.Days1To30 += due;
                else if (overdueDays <= 60) row.Days31To60 += due;
                else if (overdueDays <= 90) row.Days61To90 += due;
                else row.Days90Plus += due;

                if (!row.NextDueDate.HasValue || dueDate < row.NextDueDate.Value)
                {
                    row.NextDueDate = dueDate;
                }
            }

            if (row.TotalOutstanding > 0)
            {
                model.Vendors.Add(row);
            }
        }

        model.Vendors = model.Vendors.OrderByDescending(v => v.TotalOutstanding).ToList();
        return model;
    }

    public async Task<VendorStatementViewModel> GetVendorStatementAsync(int companyId, int vendorId, DateTime fromDate, DateTime toDate)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId) ?? new Company { Id = companyId };
        var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId)
            ?? throw new InvalidOperationException("Vendor not found.");

        var model = new VendorStatementViewModel
        {
            Vendor = vendor,
            Company = company,
            FromDate = fromDate,
            ToDate = toDate
        };

        var end = toDate.Date.AddDays(1).AddSeconds(-1);

        var allPos = await _context.PurchaseOrders
            .Where(p => p.CompanyId == companyId && p.VendorId == vendorId && p.OrderDate <= end && p.Status != PurchaseOrderStatus.Cancelled)
            .Include(p => p.Bills)
            .ToListAsync();

        var poIds = allPos.Select(p => p.Id).ToList();
        var allPayments = await _context.VendorPayments
            .Where(v => v.VendorId == vendorId && v.PaymentDate <= end)
            .ToListAsync();

        // Opening balance
        decimal priorBilled = allPos.Where(p => p.OrderDate < fromDate.Date).Sum(p => p.TotalAmount);
        decimal priorPaid = allPayments.Where(p => p.PaymentDate < fromDate.Date).Sum(p => p.Amount);
        model.OpeningBalance = priorBilled - priorPaid;

        var txList = new List<StatementTransactionRow>();

        foreach (var po in allPos.Where(p => p.OrderDate >= fromDate.Date && p.OrderDate <= end))
        {
            string billRef = po.InvoiceNumber ?? (po.Bills.FirstOrDefault()?.BillNumber ?? po.PoNumber);
            txList.Add(new StatementTransactionRow
            {
                Date = po.OrderDate,
                Type = "Bill",
                ReferenceNumber = billRef,
                Description = $"Purchase Bill #{billRef} (PO: {po.PoNumber})",
                Debit = po.TotalAmount,
                Credit = 0m
            });
        }

        foreach (var pay in allPayments.Where(p => p.PaymentDate >= fromDate.Date && p.PaymentDate <= end))
        {
            txList.Add(new StatementTransactionRow
            {
                Date = pay.PaymentDate,
                Type = "Payment",
                ReferenceNumber = pay.VoucherNumber,
                Description = $"Payment Voucher #{pay.VoucherNumber} ({pay.PaymentMode})",
                Debit = 0m,
                Credit = pay.Amount,
                PaymentMode = pay.PaymentMode.ToString()
            });
        }

        decimal running = model.OpeningBalance;
        foreach (var tx in txList.OrderBy(t => t.Date).ThenBy(t => t.Type == "Bill" ? 0 : 1))
        {
            running += (tx.Debit - tx.Credit);
            tx.RunningBalance = running;
            model.Transactions.Add(tx);
        }

        return model;
    }

    public async Task<ThreeWayMatchingViewModel> GetThreeWayMatchingAsync(int companyId, string? statusFilter)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId) ?? new Company { Id = companyId };

        var pos = await _context.PurchaseOrders
            .Where(p => p.CompanyId == companyId && p.Status != PurchaseOrderStatus.Cancelled)
            .Include(p => p.Vendor)
            .Include(p => p.Details)
            .Include(p => p.Bills)
            .OrderByDescending(p => p.OrderDate)
            .ToListAsync();

        var model = new ThreeWayMatchingViewModel
        {
            Company = company,
            SelectedStatus = statusFilter
        };

        foreach (var po in pos)
        {
            int orderedQty = po.Details.Sum(d => d.Quantity);
            int receivedQty = po.Details.Sum(d => d.ReceivedQuantity);

            bool hasBill = !string.IsNullOrWhiteSpace(po.InvoiceNumber) || po.Bills.Any();
            string? billNumber = po.InvoiceNumber ?? po.Bills.FirstOrDefault()?.BillNumber;
            decimal billedAmount = hasBill ? po.TotalAmount : 0m;

            var item = new ThreeWayMatchItem
            {
                PurchaseOrderId = po.Id,
                PoNumber = po.PoNumber,
                OrderDate = po.OrderDate,
                VendorId = po.VendorId,
                VendorName = po.Vendor?.VendorName ?? "Vendor",
                VendorGstin = po.Vendor?.GstNumber,
                OrderedQuantity = orderedQty,
                ReceivedQuantity = receivedQty,
                ReceivedDate = po.ReceivedOnDate,
                PoAmount = po.TotalAmount,
                BilledAmount = billedAmount,
                BillNumber = billNumber,
                HasBill = hasBill
            };

            // 1. Goods Receipt Status
            if (receivedQty >= orderedQty && orderedQty > 0)
            {
                item.GoodsReceiptStatus = "Fully Received";
                item.QuantityMatchStatus = "Matched";
            }
            else if (receivedQty > 0 && receivedQty < orderedQty)
            {
                item.GoodsReceiptStatus = "Partially Received";
                item.QuantityMatchStatus = "Short Delivery";
            }
            else
            {
                item.GoodsReceiptStatus = "Pending Delivery";
                item.QuantityMatchStatus = "Pending Receipt";
            }

            // 2. Price / Value Match
            item.PriceMatchStatus = "Matched";

            // 3. Overall 3-Way Status
            if (!item.HasBill)
            {
                item.OverallMatchStatus = "Awaiting Vendor Bill";
            }
            else if (item.GoodsReceiptStatus == "Pending Delivery")
            {
                item.OverallMatchStatus = "Pending Delivery";
            }
            else if (item.QuantityMatchStatus == "Short Delivery")
            {
                item.OverallMatchStatus = "Quantity Discrepancy";
            }
            else
            {
                item.OverallMatchStatus = "Ready for Payment";
            }

            // Apply filter if specified
            if (string.IsNullOrEmpty(statusFilter) ||
                string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.OverallMatchStatus, statusFilter, StringComparison.OrdinalIgnoreCase))
            {
                model.Items.Add(item);
            }
        }

        return model;
    }
}
