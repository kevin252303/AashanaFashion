using System;
using System.Threading.Tasks;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public interface IDocumentNumberService
{
    Task<string> GenerateInvoiceNumberAsync(Company? company = null, DateTime? date = null);
    Task<string> GenerateSalesOrderNumberAsync(Company? company = null, DateTime? date = null);
    Task<string> GeneratePurchaseOrderNumberAsync(Company? company = null, DateTime? date = null);
    string PreviewFormat(string? format, string? prefix, string? companyCode, DateTime? date = null, int sampleSequence = 1);
}
