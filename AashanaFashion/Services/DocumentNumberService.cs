using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AashanaFashion.Data;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public class DocumentNumberService : IDocumentNumberService
{
    private readonly AppDbContext _context;
    private readonly ICompanyContext _companyContext;

    public DocumentNumberService(AppDbContext context, ICompanyContext companyContext)
    {
        _context = context;
        _companyContext = companyContext;
    }

    public async Task<string> GenerateInvoiceNumberAsync(Company? company = null, DateTime? date = null)
    {
        company ??= await _companyContext.GetActiveCompanyAsync();
        var dt = date ?? DateTime.Now;

        var format = !string.IsNullOrWhiteSpace(company.InvoiceNumberFormat)
            ? company.InvoiceNumberFormat
            : "{PREFIX}{YYYY}{MM}-{0000}";

        var prefix = !string.IsNullOrWhiteSpace(company.InvoicePrefix)
            ? company.InvoicePrefix
            : "INV-";

        var (prefixStem, suffixStem, paddingFormat) = DeconstructFormat(format, prefix, company.CompanyCode, dt);

        var existing = await _context.TaxInvoices
            .Where(i => i.CompanyId == company.Id && i.InvoiceNumber.StartsWith(prefixStem))
            .Select(i => i.InvoiceNumber)
            .ToListAsync();

        int nextSeq = GetNextSequenceFromExisting(existing, prefixStem, suffixStem);
        return $"{prefixStem}{nextSeq.ToString(paddingFormat)}{suffixStem}";
    }

    public async Task<string> GenerateSalesOrderNumberAsync(Company? company = null, DateTime? date = null)
    {
        company ??= await _companyContext.GetActiveCompanyAsync();
        var dt = date ?? DateTime.Now;

        var format = !string.IsNullOrWhiteSpace(company.SalesOrderNumberFormat)
            ? company.SalesOrderNumberFormat
            : "{PREFIX}{YYYY}-{0000}";

        var prefix = !string.IsNullOrWhiteSpace(company.SalesOrderPrefix)
            ? company.SalesOrderPrefix
            : "SO-";

        var (prefixStem, suffixStem, paddingFormat) = DeconstructFormat(format, prefix, company.CompanyCode, dt);

        var existing = await _context.SalesOrders
            .Where(s => s.CompanyId == company.Id && s.SoNumber.StartsWith(prefixStem))
            .Select(s => s.SoNumber)
            .ToListAsync();

        int nextSeq = GetNextSequenceFromExisting(existing, prefixStem, suffixStem);
        return $"{prefixStem}{nextSeq.ToString(paddingFormat)}{suffixStem}";
    }

    public async Task<string> GeneratePurchaseOrderNumberAsync(Company? company = null, DateTime? date = null)
    {
        company ??= await _companyContext.GetActiveCompanyAsync();
        var dt = date ?? DateTime.Now;

        var format = !string.IsNullOrWhiteSpace(company.PurchaseOrderNumberFormat)
            ? company.PurchaseOrderNumberFormat
            : "{PREFIX}{0000}";

        var prefix = !string.IsNullOrWhiteSpace(company.PurchaseOrderPrefix)
            ? company.PurchaseOrderPrefix
            : "PO-";

        var (prefixStem, suffixStem, paddingFormat) = DeconstructFormat(format, prefix, company.CompanyCode, dt);

        var existing = await _context.PurchaseOrders
            .Where(p => p.CompanyId == company.Id && p.PoNumber.StartsWith(prefixStem))
            .Select(p => p.PoNumber)
            .ToListAsync();

        int nextSeq = GetNextSequenceFromExisting(existing, prefixStem, suffixStem);
        return $"{prefixStem}{nextSeq.ToString(paddingFormat)}{suffixStem}";
    }

    public string PreviewFormat(string? format, string? prefix, string? companyCode, DateTime? date = null, int sampleSequence = 1)
    {
        var dt = date ?? DateTime.Now;
        var rawFormat = string.IsNullOrWhiteSpace(format) ? "{PREFIX}{0000}" : format.Trim();
        var rawPrefix = prefix ?? "";
        var code = companyCode ?? "";

        var (prefixStem, suffixStem, paddingFormat) = DeconstructFormat(rawFormat, rawPrefix, code, dt);
        return $"{prefixStem}{sampleSequence.ToString(paddingFormat)}{suffixStem}";
    }

    private static (string PrefixStem, string SuffixStem, string PaddingFormat) DeconstructFormat(
        string format, string prefix, string? companyCode, DateTime date)
    {
        var pattern = @"\{(0+|SEQ(?::?\d*)?)\}";
        var match = Regex.Match(format, pattern, RegexOptions.IgnoreCase);

        string prefixTemplate;
        string suffixTemplate;
        string paddingFormat = "D4";

        if (match.Success)
        {
            prefixTemplate = format.Substring(0, match.Index);
            suffixTemplate = format.Substring(match.Index + match.Length);

            var token = match.Groups[1].Value.ToUpperInvariant();
            if (token.StartsWith("0"))
            {
                paddingFormat = $"D{token.Length}";
            }
            else if (token.StartsWith("SEQ"))
            {
                var numPart = new string(token.Where(char.IsDigit).ToArray());
                if (int.TryParse(numPart, out int pad) && pad > 0 && pad <= 10)
                {
                    paddingFormat = $"D{pad}";
                }
            }
        }
        else
        {
            prefixTemplate = format;
            suffixTemplate = string.Empty;
            paddingFormat = "D4";
        }

        string prefixStem = EvaluateDateAndCompanyTokens(prefixTemplate, prefix, companyCode ?? "", date);
        string suffixStem = EvaluateDateAndCompanyTokens(suffixTemplate, prefix, companyCode ?? "", date);

        return (prefixStem, suffixStem, paddingFormat);
    }

    private static string EvaluateDateAndCompanyTokens(string template, string prefix, string companyCode, DateTime date)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;

        int startYear = date.Month >= 4 ? date.Year : date.Year - 1;
        int endYear = startYear + 1;
        string fy2 = $"{startYear % 100:D2}-{(endYear % 100):D2}"; // e.g. 26-27
        string fy4 = $"{startYear}-{endYear}";                     // e.g. 2026-2027

        return template
            .Replace("{PREFIX}", prefix, StringComparison.OrdinalIgnoreCase)
            .Replace("{COMPANY}", companyCode, StringComparison.OrdinalIgnoreCase)
            .Replace("{COMP}", companyCode, StringComparison.OrdinalIgnoreCase)
            .Replace("{FYFULL}", fy4, StringComparison.OrdinalIgnoreCase)
            .Replace("{FY4}", fy4, StringComparison.OrdinalIgnoreCase)
            .Replace("{FY}", fy2, StringComparison.OrdinalIgnoreCase)
            .Replace("{YYYY}", date.ToString("yyyy"), StringComparison.OrdinalIgnoreCase)
            .Replace("{YY}", date.ToString("yy"), StringComparison.OrdinalIgnoreCase)
            .Replace("{MM}", date.ToString("MM"), StringComparison.OrdinalIgnoreCase)
            .Replace("{DD}", date.ToString("dd"), StringComparison.OrdinalIgnoreCase);
    }

    private static int GetNextSequenceFromExisting(IEnumerable<string> existingNumbers, string prefixStem, string suffixStem)
    {
        int maxSeq = 0;
        foreach (var num in existingNumbers)
        {
            if (string.IsNullOrEmpty(num) || !num.StartsWith(prefixStem, StringComparison.OrdinalIgnoreCase))
                continue;

            var middle = num.Substring(prefixStem.Length);
            if (!string.IsNullOrEmpty(suffixStem) && middle.EndsWith(suffixStem, StringComparison.OrdinalIgnoreCase))
            {
                middle = middle.Substring(0, middle.Length - suffixStem.Length);
            }

            var digits = new string(middle.TakeWhile(char.IsDigit).ToArray());
            if (int.TryParse(digits, out int val) && val > maxSeq)
            {
                maxSeq = val;
            }
        }
        return maxSeq + 1;
    }
}
