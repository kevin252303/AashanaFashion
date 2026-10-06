using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models;

public enum AccountType
{
    Asset,
    Liability,
    Equity,
    Income,
    Expense
}

public enum AccountClassification
{
    CurrentAsset,
    BankAndCash,
    AccountsReceivable,
    Inventory,
    FixedAsset,
    CurrentLiability,
    AccountsPayable,
    DutiesAndTaxes,
    NonCurrentLiability,
    Equity,
    OperatingRevenue,
    OtherIncome,
    CostOfGoodsSold,
    DirectManufacturingExpense,
    OperatingExpense,
    FinancialExpense
}

public class Account : IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; } = 1;
    public int CompanyId { get; set; } = 1;
    public Company? Company { get; set; }

    [Required]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty; // e.g. "101000"

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; }

    public AccountClassification Classification { get; set; }

    public int? ParentAccountId { get; set; }
    public Account? ParentAccount { get; set; }

    public bool IsReconcilable { get; set; }

    public bool IsSystemAccount { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(300)]
    public string? Description { get; set; }

    public List<JournalEntryLine> Lines { get; set; } = new();
}

public enum JournalType
{
    Sales,
    Purchase,
    Bank,
    Cash,
    General
}

public class Journal : IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; } = 1;
    public int CompanyId { get; set; } = 1;
    public Company? Company { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty; // "INV", "BILL", "BNK", "CSH", "MISC"

    public JournalType Type { get; set; }

    public int? DefaultDebitAccountId { get; set; }
    public Account? DefaultDebitAccount { get; set; }

    public int? DefaultCreditAccountId { get; set; }
    public Account? DefaultCreditAccount { get; set; }

    public bool IsActive { get; set; } = true;
}

public enum JournalEntryStatus
{
    Draft,
    Posted,
    Cancelled
}

public class JournalEntry : IMustHaveTenant
{
    public int Id { get; set; }

    public int TenantId { get; set; } = 1;
    public int CompanyId { get; set; } = 1;
    public Company? Company { get; set; }

    [Required]
    [StringLength(50)]
    public string EntryNumber { get; set; } = string.Empty; // e.g. "JE-2026-0001"

    public DateTime Date { get; set; } = DateTime.Today;

    public int JournalId { get; set; }
    public Journal? Journal { get; set; }

    [StringLength(100)]
    public string? Reference { get; set; }

    [StringLength(500)]
    public string? Narration { get; set; }

    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Posted;

    [StringLength(100)]
    public string? SourceDocument { get; set; } // e.g. "TaxInvoice:12"

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    public List<JournalEntryLine> Lines { get; set; } = new();
}

public class JournalEntryLine
{
    public int Id { get; set; }

    public int JournalEntryId { get; set; }
    public JournalEntry? JournalEntry { get; set; }

    public int AccountId { get; set; }
    public Account? Account { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Debit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Credit { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    [StringLength(300)]
    public string? Description { get; set; }

    public bool IsReconciled { get; set; }
}

// ——— Financial Statements & Report ViewModels ———

public class TrialBalanceRow
{
    public int AccountId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public AccountClassification Classification { get; set; }
    public decimal OpeningDebit { get; set; }
    public decimal OpeningCredit { get; set; }
    public decimal PeriodDebit { get; set; }
    public decimal PeriodCredit { get; set; }
    public decimal ClosingDebit { get; set; }
    public decimal ClosingCredit { get; set; }
}

public class TrialBalanceViewModel
{
    public DateTime AsOfDate { get; set; } = DateTime.Today;
    public DateTime? FromDate { get; set; }
    public Company Company { get; set; } = new();
    public List<TrialBalanceRow> Rows { get; set; } = new();
    public decimal TotalDebit => Rows.Sum(r => r.ClosingDebit);
    public decimal TotalCredit => Rows.Sum(r => r.ClosingCredit);
    public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.01m;
}

public class FinancialReportSection
{
    public string Title { get; set; } = string.Empty;
    public List<(string Name, string Code, decimal Amount)> Lines { get; set; } = new();
    public decimal TotalAmount => Lines.Sum(l => l.Amount);
}

public class ProfitAndLossViewModel
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Company Company { get; set; } = new();

    public FinancialReportSection OperatingRevenue { get; set; } = new();
    public FinancialReportSection OtherIncome { get; set; } = new();
    public decimal TotalIncome => OperatingRevenue.TotalAmount + OtherIncome.TotalAmount;

    public FinancialReportSection CostOfGoodsSold { get; set; } = new();
    public decimal GrossProfit => TotalIncome - CostOfGoodsSold.TotalAmount;
    public decimal GrossMarginPercentage => TotalIncome > 0 ? (GrossProfit / TotalIncome) * 100m : 0m;

    public FinancialReportSection OperatingExpenses { get; set; } = new();
    public FinancialReportSection FinancialExpenses { get; set; } = new();
    public decimal TotalExpenses => CostOfGoodsSold.TotalAmount + OperatingExpenses.TotalAmount + FinancialExpenses.TotalAmount;

    public decimal NetProfit => TotalIncome - TotalExpenses;
    public decimal NetMarginPercentage => TotalIncome > 0 ? (NetProfit / TotalIncome) * 100m : 0m;
}

public class BalanceSheetViewModel
{
    public DateTime AsOfDate { get; set; } = DateTime.Today;
    public Company Company { get; set; } = new();

    // Assets
    public FinancialReportSection BankAndCash { get; set; } = new();
    public FinancialReportSection AccountsReceivable { get; set; } = new();
    public FinancialReportSection CurrentAssets { get; set; } = new();
    public FinancialReportSection Inventory { get; set; } = new();
    public FinancialReportSection FixedAssets { get; set; } = new();
    public decimal TotalAssets => BankAndCash.TotalAmount + AccountsReceivable.TotalAmount + CurrentAssets.TotalAmount + Inventory.TotalAmount + FixedAssets.TotalAmount;

    // Liabilities
    public FinancialReportSection AccountsPayable { get; set; } = new();
    public FinancialReportSection DutiesAndTaxes { get; set; } = new();
    public FinancialReportSection CurrentLiabilities { get; set; } = new();
    public FinancialReportSection NonCurrentLiabilities { get; set; } = new();
    public decimal TotalLiabilities => AccountsPayable.TotalAmount + DutiesAndTaxes.TotalAmount + CurrentLiabilities.TotalAmount + NonCurrentLiabilities.TotalAmount;

    // Equity
    public FinancialReportSection CapitalAndReserves { get; set; } = new();
    public decimal CurrentPeriodProfitOrLoss { get; set; }
    public decimal TotalEquity => CapitalAndReserves.TotalAmount + CurrentPeriodProfitOrLoss;

    public decimal TotalLiabilitiesAndEquity => TotalLiabilities + TotalEquity;
    public bool IsBalanced => Math.Abs(TotalAssets - TotalLiabilitiesAndEquity) < 0.05m;
}

public class GeneralLedgerRow
{
    public DateTime Date { get; set; }
    public string EntryNumber { get; set; } = string.Empty;
    public int EntryId { get; set; }
    public string JournalCode { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? PartnerName { get; set; }
    public string? Narration { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}

public class GeneralLedgerAccountViewModel
{
    public Account Account { get; set; } = new();
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public List<GeneralLedgerRow> Rows { get; set; } = new();
    public decimal TotalDebit => Rows.Sum(r => r.Debit);
    public decimal TotalCredit => Rows.Sum(r => r.Credit);
    public decimal ClosingBalance => Rows.Any() ? Rows.Last().RunningBalance : OpeningBalance;
}

public class PartnerLedgerViewModel
{
    public string PartnerType { get; set; } = "Customer"; // "Customer" or "Vendor"
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string? Gstin { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public decimal? CreditLimit { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public List<GeneralLedgerRow> Rows { get; set; } = new();
    public decimal TotalDebit => Rows.Sum(r => r.Debit);
    public decimal TotalCredit => Rows.Sum(r => r.Credit);
    public decimal ClosingBalance => Rows.Any() ? Rows.Last().RunningBalance : OpeningBalance;
}

public class PartnerLedgerSummaryRow
{
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Phone { get; set; }
    public string? Gstin { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal ClosingBalance { get; set; }
}

public class PartnerLedgerSummaryViewModel
{
    public string PartnerType { get; set; } = "Customer"; // "Customer" or "Vendor"
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<PartnerLedgerSummaryRow> Rows { get; set; } = new();
    public decimal TotalOpening => Rows.Sum(r => r.OpeningBalance);
    public decimal TotalDebit => Rows.Sum(r => r.TotalDebit);
    public decimal TotalCredit => Rows.Sum(r => r.TotalCredit);
    public decimal TotalClosing => Rows.Sum(r => r.ClosingBalance);
}

public class CreateJournalEntryViewModel
{
    [Required]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required]
    public int JournalId { get; set; }

    [StringLength(100)]
    public string? Reference { get; set; }

    [Required]
    [StringLength(500)]
    public string Narration { get; set; } = string.Empty;

    public string EntryMode { get; set; } = "Single";

    public List<CreateJournalLineItem> Lines { get; set; } = new();
}

public class CreateJournalLineItem
{
    public int AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public int? CustomerId { get; set; }
    public int? VendorId { get; set; }
    public string? Description { get; set; }
}
