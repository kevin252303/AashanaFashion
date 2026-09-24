using AashanaFashion.Models;

namespace AashanaFashion.Services
{
    public interface IEwayBillService
    {
        Task<string> GenerateInvoiceJsonAsync(int invoiceId, EwayBillTransportInput? transport = null);
        Task<string> GenerateChallanJsonAsync(int challanId, EwayBillTransportInput? transport = null);
        int GetStateCode(string? gstin, string? stateName);
        int ExtractPincode(string? text, int defaultPin = 395002);
    }
}
