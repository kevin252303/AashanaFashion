using System;
using System.IO;
using System.Threading.Tasks;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public interface IGstReturnService
{
    Task<Gstr1PeriodViewModel> GetGstr1DataAsync(int companyId, int year, int month);
    Task<byte[]> GenerateGstr1JsonAsync(int companyId, int year, int month);
    Task<byte[]> GenerateGstr1CsvAsync(int companyId, int year, int month, string section);
    Task<Gstr3bViewModel> GetGstr3bDataAsync(int companyId, int year, int month);
    Task<Gstr2bReconciliationViewModel> ReconcileGstr2bAsync(int companyId, int year, int month, Stream gstr2bJsonStream, string fileName);
}
