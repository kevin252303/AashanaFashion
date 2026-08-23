using System.Threading.Tasks;

namespace AashanaFashion.Services;

public class GstVerificationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? LegalName { get; set; }
    public string? TradeName { get; set; }
    public string? Status { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PinCode { get; set; }
    public string? PanNumber { get; set; }
}

public interface IGstVerificationService
{
    Task<GstVerificationResult> VerifyGstAsync(string gstin);
}
