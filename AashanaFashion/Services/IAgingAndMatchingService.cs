using System;
using System.Threading.Tasks;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public interface IAgingAndMatchingService
{
    Task<AgedReceivablesViewModel> GetAgedReceivablesAsync(int companyId, DateTime asOfDate);
    Task<CustomerStatementViewModel> GetCustomerStatementAsync(int companyId, int customerId, DateTime fromDate, DateTime toDate);
    Task<AgedPayablesViewModel> GetAgedPayablesAsync(int companyId, DateTime asOfDate);
    Task<VendorStatementViewModel> GetVendorStatementAsync(int companyId, int vendorId, DateTime fromDate, DateTime toDate);
    Task<ThreeWayMatchingViewModel> GetThreeWayMatchingAsync(int companyId, string? statusFilter);
}
