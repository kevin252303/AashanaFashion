using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AashanaFashion.Services;

public class BankLookupService : IBankLookupService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BankLookupService> _logger;

    // Standard RBI 4-letter bank prefix mapping for instant identification & offline resiliency
    private static readonly Dictionary<string, string> KnownBankPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "SBIN", "State Bank of India" },
        { "HDFC", "HDFC Bank" },
        { "ICIC", "ICICI Bank" },
        { "UTIB", "Axis Bank" },
        { "KKBK", "Kotak Mahindra Bank" },
        { "BARB", "Bank of Baroda" },
        { "PUNB", "Punjab National Bank" },
        { "CNRB", "Canara Bank" },
        { "UBIN", "Union Bank of India" },
        { "IDIB", "Indian Bank" },
        { "BKID", "Bank of India" },
        { "IOBA", "Indian Overseas Bank" },
        { "CBIN", "Central Bank of India" },
        { "YESB", "Yes Bank" },
        { "IDFB", "IDFC First Bank" },
        { "INDB", "IndusInd Bank" },
        { "FDRL", "Federal Bank" },
        { "AUBL", "AU Small Finance Bank" },
        { "BDBL", "Bandhan Bank" },
        { "RBLN", "RBL Bank" },
        { "CSBK", "CSB Bank" },
        { "KARB", "Karnataka Bank" },
        { "KVBL", "Karur Vysya Bank" },
        { "SIBL", "South Indian Bank" },
        { "TMBL", "Tamilnad Mercantile Bank" },
        { "UJVN", "Ujjivan Small Finance Bank" },
        { "ESFB", "Equitas Small Finance Bank" },
        { "JAKA", "Jammu & Kashmir Bank" },
        { "PSIB", "Punjab & Sind Bank" },
        { "MAHB", "Bank of Maharashtra" },
        { "UCBA", "UCO Bank" },
        { "AIRP", "Airtel Payments Bank" },
        { "IPOS", "India Post Payments Bank" },
        { "PYTM", "Paytm Payments Bank" },
        { "FINO", "Fino Payments Bank" }
    };

    public BankLookupService(HttpClient httpClient, IMemoryCache cache, ILogger<BankLookupService> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        _cache = cache;
        _logger = logger;
    }

    public bool ValidateIfsc(string? ifsc, out string? errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(ifsc))
        {
            errorMessage = "IFSC code is required.";
            return false;
        }

        string clean = ifsc.Trim().ToUpper();
        if (clean.Length != 11)
        {
            errorMessage = $"IFSC must be exactly 11 characters (provided {clean.Length}).";
            return false;
        }

        if (clean[4] != '0')
        {
            errorMessage = "5th character of IFSC must always be '0' (Zero).";
            return false;
        }

        if (!Regex.IsMatch(clean, @"^[A-Z]{4}0[A-Z0-9]{6}$"))
        {
            errorMessage = "Invalid IFSC format. Expected 4 letters, 0, then 6 alphanumeric characters (e.g. SBIN0001234, HDFC0000240).";
            return false;
        }

        return true;
    }

    public async Task<BankDetailsResult> LookupIfscAsync(string ifsc)
    {
        if (string.IsNullOrWhiteSpace(ifsc))
        {
            return new BankDetailsResult
            {
                Success = false,
                Message = "Please provide an IFSC code."
            };
        }

        string clean = ifsc.Trim().ToUpper();

        if (!ValidateIfsc(clean, out var validationError))
        {
            return new BankDetailsResult
            {
                Success = false,
                Ifsc = clean,
                Message = validationError
            };
        }

        string cacheKey = $"ifsc_lookup_{clean}";
        if (_cache.TryGetValue(cacheKey, out BankDetailsResult? cached) && cached != null)
        {
            return cached;
        }

        string prefix = clean.Substring(0, 4);
        string? fallbackBankName = KnownBankPrefixes.TryGetValue(prefix, out var knownName) ? knownName : null;

        try
        {
            var response = await _httpClient.GetAsync($"https://ifsc.razorpay.com/{clean}");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<RazorpayIfscApiResponse>();
                if (data != null)
                {
                    var result = new BankDetailsResult
                    {
                        Success = true,
                        Message = "Bank details fetched successfully.",
                        Ifsc = clean,
                        BankName = !string.IsNullOrWhiteSpace(data.Bank) ? data.Bank : fallbackBankName,
                        Branch = data.Branch,
                        Address = data.Address,
                        City = data.City,
                        District = data.District,
                        State = data.State,
                        Micr = data.Micr,
                        Contact = data.Contact,
                        Rtgs = data.Rtgs,
                        Neft = data.Neft,
                        Imps = data.Imps,
                        Upi = data.Upi
                    };

                    _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
                    return result;
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // IFSC not found in official active registry
                return new BankDetailsResult
                {
                    Success = false,
                    Ifsc = clean,
                    BankName = fallbackBankName,
                    Message = fallbackBankName != null
                        ? $"Recognized bank as {fallbackBankName}, but the branch IFSC code was not found in the national registry."
                        : "IFSC code not found in the national bank registry. Please verify the code."
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query online IFSC directory for {Ifsc}. Checking local bank prefix.", clean);

            // If network is offline or service unreachable, fall back to prefix
            if (fallbackBankName != null)
            {
                var fallbackResult = new BankDetailsResult
                {
                    Success = true,
                    Ifsc = clean,
                    BankName = fallbackBankName,
                    Branch = "Branch details unavailable (offline lookup)",
                    Message = $"Bank identified as {fallbackBankName}. Branch details could not be retrieved from the online registry."
                };
                return fallbackResult;
            }
        }

        return new BankDetailsResult
        {
            Success = false,
            Ifsc = clean,
            Message = "Unable to fetch bank details for this IFSC code. Please enter the bank name and branch manually."
        };
    }

    private class RazorpayIfscApiResponse
    {
        [JsonPropertyName("BANK")]
        public string? Bank { get; set; }

        [JsonPropertyName("BRANCH")]
        public string? Branch { get; set; }

        [JsonPropertyName("ADDRESS")]
        public string? Address { get; set; }

        [JsonPropertyName("CITY")]
        public string? City { get; set; }

        [JsonPropertyName("DISTRICT")]
        public string? District { get; set; }

        [JsonPropertyName("STATE")]
        public string? State { get; set; }

        [JsonPropertyName("MICR")]
        public string? Micr { get; set; }

        [JsonPropertyName("CONTACT")]
        public string? Contact { get; set; }

        [JsonPropertyName("UPI")]
        public bool? Upi { get; set; }

        [JsonPropertyName("RTGS")]
        public bool? Rtgs { get; set; }

        [JsonPropertyName("NEFT")]
        public bool? Neft { get; set; }

        [JsonPropertyName("IMPS")]
        public bool? Imps { get; set; }
    }
}
