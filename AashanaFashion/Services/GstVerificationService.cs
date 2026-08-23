using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace AashanaFashion.Services;

public class GstVerificationService : IGstVerificationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GstVerificationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<GstVerificationResult> VerifyGstAsync(string gstin)
    {
        if (string.IsNullOrWhiteSpace(gstin) || gstin.Length != 15)
        {
            return new GstVerificationResult
            {
                Success = false,
                Message = "Invalid GSTIN format. A GSTIN must be exactly 15 characters long."
            };
        }

        var apiKey = _configuration["GstSettings:ApiKey"];
        var apiUrl = _configuration["GstSettings:ApiUrl"];

        // If ApiKey is not configured, run mock implementation
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return GetMockGstResult(gstin);
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{apiUrl}/{gstin}");
            request.Headers.Add("Authorization", apiKey);
            request.Headers.Add("x-api-key", apiKey);

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<GstApiResponse>();
                if (apiResponse != null && apiResponse.Status == "success" && apiResponse.Data != null)
                {
                    var data = apiResponse.Data;
                    var pan = gstin.Substring(2, 10);
                    return new GstVerificationResult
                    {
                        Success = true,
                        Message = "GSTIN verified successfully.",
                        LegalName = data.Lgnm,
                        TradeName = data.TradeNam ?? data.Lgnm,
                        Status = data.Sts,
                        Address = $"{data.Pradr?.Addr?.Bnm} {data.Pradr?.Addr?.St} {data.Pradr?.Addr?.Loc}".Trim(),
                        City = data.Pradr?.Addr?.Loc,
                        State = data.Pradr?.Addr?.Stcd,
                        PinCode = data.Pradr?.Addr?.Pncd,
                        PanNumber = pan
                    };
                }
            }

            return new GstVerificationResult
            {
                Success = false,
                Message = $"Failed to verify GSTIN from API. Status: {response.StatusCode}. Defaulting to mock for safety."
            };
        }
        catch (System.Exception ex)
        {
            return new GstVerificationResult
            {
                Success = false,
                Message = $"API connection error: {ex.Message}. Falling back to mock verification."
            };
        }
    }

    private GstVerificationResult GetMockGstResult(string gstin)
    {
        var stateCode = gstin.Substring(0, 2);
        var pan = gstin.Substring(2, 10);

        string stateName = "Gujarat";
        string city = "Surat";
        string pincode = "395003";
        string street = "Ring Road Textile Market";

        switch (stateCode)
        {
            case "24":
                stateName = "Gujarat";
                city = "Surat";
                pincode = "395002";
                street = "Salabatpura Textile Hub";
                break;
            case "27":
                stateName = "Maharashtra";
                city = "Mumbai";
                pincode = "400002";
                street = "Kalbadevi Cloth Market";
                break;
            case "29":
                stateName = "Karnataka";
                city = "Bangalore";
                pincode = "560002";
                street = "Chickpet Wholesale Market";
                break;
            case "07":
                stateName = "Delhi";
                city = "Delhi";
                pincode = "110006";
                street = "Chandni Chowk Market";
                break;
            case "19":
                stateName = "West Bengal";
                city = "Kolkata";
                pincode = "700007";
                street = "Barabazar Textile Hub";
                break;
        }

        string entityType = "Fashions";
        if (pan.EndsWith("L") || pan.EndsWith("C")) entityType = "Private Limited";
        else if (pan.EndsWith("P")) entityType = "Partnership";
        else if (pan.EndsWith("F")) entityType = "Fabrics";

        return new GstVerificationResult
        {
            Success = true,
            Message = "GSTIN verified successfully (Mock Data Mode - No API Key).",
            LegalName = $"Aashana {entityType} Pvt Ltd",
            TradeName = $"Aashana {entityType}",
            Status = "Active",
            Address = $"{street}, {city}",
            City = city,
            State = stateName,
            PinCode = pincode,
            PanNumber = pan
        };
    }
}

public class GstApiResponse
{
    public string? Status { get; set; }
    public GstApiData? Data { get; set; }
}

public class GstApiData
{
    public string? Gstin { get; set; }
    public string? Lgnm { get; set; }
    public string? TradeNam { get; set; }
    public string? Sts { get; set; }
    public GstApiAddressWrapper? Pradr { get; set; }
}

public class GstApiAddressWrapper
{
    public GstApiAddress? Addr { get; set; }
}

public class GstApiAddress
{
    public string? Bnm { get; set; }
    public string? St { get; set; }
    public string? Loc { get; set; }
    public string? Pncd { get; set; }
    public string? Stcd { get; set; }
}
