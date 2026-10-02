using System;
using System.Collections.Generic;
using System.Linq;

namespace AashanaFashion.Models;

public class AgedReceivableCustomerRow
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Gstin { get; set; }
    public int InvoiceCount { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal CurrentNotDue { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Days90Plus { get; set; }
    public DateTime? OldestDueDate { get; set; }
}

public class AgedReceivablesViewModel
{
    public DateTime AsOfDate { get; set; } = DateTime.Today;
    public Company Company { get; set; } = new();
    public List<AgedReceivableCustomerRow> Customers { get; set; } = new();

    public decimal TotalOutstanding => Customers.Sum(c => c.TotalOutstanding);
    public decimal TotalCurrent => Customers.Sum(c => c.CurrentNotDue);
    public decimal Total1To30 => Customers.Sum(c => c.Days1To30);
    public decimal Total31To60 => Customers.Sum(c => c.Days31To60);
    public decimal Total61To90 => Customers.Sum(c => c.Days61To90);
    public decimal Total90Plus => Customers.Sum(c => c.Days90Plus);
}

public class StatementTransactionRow
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty; // "Invoice", "Payment", "Return"
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }  // Invoice amount
    public decimal Credit { get; set; } // Payment / Return amount
    public decimal RunningBalance { get; set; }
    public string? PaymentMode { get; set; }
}

public class CustomerStatementViewModel
{
    public Customer Customer { get; set; } = new();
    public Company Company { get; set; } = new();
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public decimal OpeningBalance { get; set; }
    public List<StatementTransactionRow> Transactions { get; set; } = new();

    public decimal TotalInvoiced => Transactions.Where(t => t.Type == "Invoice").Sum(t => t.Debit);
    public decimal TotalPaid => Transactions.Where(t => t.Type == "Payment").Sum(t => t.Credit);
    public decimal TotalReturns => Transactions.Where(t => t.Type == "Return").Sum(t => t.Credit);
    public decimal ClosingBalance => Transactions.Any() ? Transactions.Last().RunningBalance : OpeningBalance;

    // Aging breakdown for statement footer
    public decimal CurrentNotDue { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Days90Plus { get; set; }
}

public class AgedPayableVendorRow
{
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Gstin { get; set; }
    public int BillCount { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal CurrentNotDue { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Days90Plus { get; set; }
    public DateTime? NextDueDate { get; set; }
}

public class AgedPayablesViewModel
{
    public DateTime AsOfDate { get; set; } = DateTime.Today;
    public Company Company { get; set; } = new();
    public List<AgedPayableVendorRow> Vendors { get; set; } = new();

    public decimal TotalOutstanding => Vendors.Sum(v => v.TotalOutstanding);
    public decimal TotalCurrent => Vendors.Sum(v => v.CurrentNotDue);
    public decimal Total1To30 => Vendors.Sum(v => v.Days1To30);
    public decimal Total31To60 => Vendors.Sum(v => v.Days31To60);
    public decimal Total61To90 => Vendors.Sum(v => v.Days61To90);
    public decimal Total90Plus => Vendors.Sum(v => v.Days90Plus);
}

public class VendorStatementViewModel
{
    public Vendor Vendor { get; set; } = new();
    public Company Company { get; set; } = new();
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public decimal OpeningBalance { get; set; }
    public List<StatementTransactionRow> Transactions { get; set; } = new();

    public decimal TotalBilled => Transactions.Where(t => t.Type == "Bill").Sum(t => t.Debit);
    public decimal TotalPaid => Transactions.Where(t => t.Type == "Payment").Sum(t => t.Credit);
    public decimal ClosingBalance => Transactions.Any() ? Transactions.Last().RunningBalance : OpeningBalance;
}

public class ThreeWayMatchItem
{
    public int PurchaseOrderId { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? VendorGstin { get; set; }

    // 1. PO Order details
    public int OrderedQuantity { get; set; }
    public decimal PoAmount { get; set; }

    // 2. Receipt / GRN details
    public int ReceivedQuantity { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public string GoodsReceiptStatus { get; set; } = "Pending"; // "Fully Received", "Partially Received", "Pending Delivery"

    // 3. Vendor Bill details
    public string? BillNumber { get; set; }
    public decimal BilledAmount { get; set; }
    public bool HasBill { get; set; }

    // Match analysis
    public string QuantityMatchStatus { get; set; } = "Matched"; // "Matched", "Short Delivery", "Pending Receipt"
    public string PriceMatchStatus { get; set; } = "Matched"; // "Matched", "Price Variance"
    public decimal AmountVariance => BilledAmount - PoAmount;

    public string OverallMatchStatus { get; set; } = "Ready for Payment"; // "Ready for Payment", "Quantity Discrepancy", "Pending Delivery", "Price Discrepancy"
    public bool CanAuthorizePayment => OverallMatchStatus == "Ready for Payment";
}

public class ThreeWayMatchingViewModel
{
    public Company Company { get; set; } = new();
    public string? SelectedStatus { get; set; }
    public List<ThreeWayMatchItem> Items { get; set; } = new();

    public int TotalOrders => Items.Count;
    public int PerfectMatchCount => Items.Count(i => i.OverallMatchStatus == "Ready for Payment");
    public int DiscrepancyCount => Items.Count(i => i.OverallMatchStatus == "Quantity Discrepancy" || i.OverallMatchStatus == "Price Discrepancy");
    public int AwaitingDeliveryCount => Items.Count(i => i.OverallMatchStatus == "Pending Delivery");
}
