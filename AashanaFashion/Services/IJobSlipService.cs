using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public interface IJobSlipService
{
    Task<List<JobSlip>> GenerateJobSlipsForOrderAsync(int productionOrderId, string? createdBy = null);
    Task<List<JobSlip>> GetJobSlipsForOrderAsync(int productionOrderId);
    Task<JobSlip?> GetJobSlipByIdAsync(int id);
    Task<string> GenerateNextSlipNumberAsync(string lotNo, string processName, int vendorId);
    Task<bool> UpdateJobSlipStatusAsync(int id, string status, DateTime? receivedDate, string? remarks);
}
