using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public interface IBankPayoutService
{
    Task<BankBulkPaymentHubViewModel> GetHubDashboardDataAsync(int companyId);
    Task<BankPaymentBatch> CreateBatchAsync(int companyId, CreateBankPaymentBatchInput input, string createdBy);
    Task<BankPaymentBatch?> GetBatchDetailsAsync(int batchId);
    Task<(byte[] FileBytes, string FileName, string ContentType)> GenerateBankExportFileAsync(int batchId, BankFormat? targetFormat = null);
    Task<bool> MarkBatchProcessedAsync(int batchId, string bankUtr, DateTime processedDate, string processedBy, string? notes);
    Task<bool> CancelBatchAsync(int batchId, string cancelledBy);
    bool ValidateIfsc(string? ifsc, out string? errorMessage);
}
