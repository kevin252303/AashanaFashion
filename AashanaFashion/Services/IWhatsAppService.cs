using AashanaFashion.Models;

namespace AashanaFashion.Services;

public interface IWhatsAppService
{
    string NormalizePhoneNumber(string? phone);
    string GenerateWhatsAppUrl(string? phone, string message);
    string BuildJobSlipMessage(JobSlip slip, Company? company);
    string BuildDispatchChallanMessage(DeliveryChallan challan, Company? company);
    string BuildPaymentReceiptMessage(PaymentReceipt receipt, TaxInvoice? invoice, Company? company);
    string BuildInvoiceSummaryMessage(TaxInvoice invoice, Company? company);
    string BuildSalesOrderMessage(SalesOrder order, Company? company);
    string BuildLeadFollowUpMessage(Lead lead, Company? company);
    Task<(bool Success, string Message)> SendDirectMessageAsync(string phone, string message);
    Task LogWhatsAppSentAsync(string docType, int docId, string docRef, string phone, string? recipientName, string message, bool isDirectApi, bool success, string? error = null, string? sentBy = null);
}
