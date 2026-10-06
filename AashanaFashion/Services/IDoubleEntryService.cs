using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public interface IDoubleEntryService
{
    Task EnsureDefaultChartOfAccountsAsync(int companyId);
    Task<List<Account>> GetAccountsAsync(int companyId, AccountType? type = null);
    Task<List<Journal>> GetJournalsAsync(int companyId);
    Task<Account?> GetAccountByIdAsync(int id);
    Task<Account> CreateAccountAsync(Account account);
    Task<bool> UpdateAccountAsync(Account account);

    // Automated Posting Triggers
    Task<JournalEntry?> PostTaxInvoiceAsync(int invoiceId);
    Task<JournalEntry?> PostPaymentReceiptAsync(int receiptId);
    Task<JournalEntry?> PostPurchaseOrderBillAsync(int purchaseOrderId);
    Task<JournalEntry?> PostVendorPaymentAsync(int paymentId);

    // Manual Journal Entry
    Task<(bool Success, string? ErrorMessage, JournalEntry? Entry)> CreateManualJournalEntryAsync(int companyId, CreateJournalEntryViewModel model, string? createdBy);
    Task<List<JournalEntry>> GetJournalEntriesAsync(int companyId, DateTime? startDate = null, DateTime? endDate = null, int? journalId = null);
    Task<JournalEntry?> GetJournalEntryDetailsAsync(int id);

    // Financial Reports
    Task<TrialBalanceViewModel> GetTrialBalanceAsync(int companyId, DateTime asOfDate, DateTime? fromDate = null);
    Task<ProfitAndLossViewModel> GetProfitAndLossAsync(int companyId, DateTime fromDate, DateTime toDate);
    Task<BalanceSheetViewModel> GetBalanceSheetAsync(int companyId, DateTime asOfDate);
    Task<GeneralLedgerAccountViewModel> GetGeneralLedgerAccountAsync(int companyId, int accountId, DateTime fromDate, DateTime toDate);
    Task<PartnerLedgerViewModel> GetCustomerLedgerAsync(int companyId, int customerId, DateTime fromDate, DateTime toDate);
    Task<PartnerLedgerViewModel> GetVendorLedgerAsync(int companyId, int vendorId, DateTime fromDate, DateTime toDate);
    Task<PartnerLedgerSummaryViewModel> GetPartnerLedgerSummaryAsync(int companyId, string partnerType, DateTime fromDate, DateTime toDate);
}
