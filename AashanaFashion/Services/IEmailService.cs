using AashanaFashion.Models;

namespace AashanaFashion.Services;

public interface IEmailService
{
    Task<(bool Success, string Message, string? MessageId)> SendEmailAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        string? docType = null,
        int? docId = null,
        string? docRef = null,
        string? sentBy = null,
        byte[]? attachmentBytes = null,
        string? attachmentFileName = null);

    string BuildInvoiceEmailHtml(TaxInvoice invoice, Company? company);
    string BuildSalesOrderEmailHtml(SalesOrder order, Company? company);
    string BuildDispatchChallanEmailHtml(DeliveryChallan challan, Company? company);
    string BuildPaymentReceiptEmailHtml(PaymentReceipt receipt, TaxInvoice? invoice, Company? company);
}
