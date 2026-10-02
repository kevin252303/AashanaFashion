using System;
using System.Threading.Tasks;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public interface IEInvoiceService
{
    Task<string> GenerateStandardInv01JsonAsync(int invoiceId);
    Task<EInvoiceResult> GenerateEInvoiceAsync(int invoiceId, EInvoiceGenerateRequest? request = null);
    Task<EInvoiceResult> CancelEInvoiceAsync(int invoiceId, EInvoiceCancelRequest request);
    Task<EInvoiceResult> RecordManualEInvoiceAsync(int invoiceId, EInvoiceManualRequest request);
    string CalculateIrn(string supplierGstin, string finYear, string docType, string docNum);
    string GetFinancialYear(DateTime date);
    string GenerateQrCodeSvg(string qrData, int size = 120);
}
