using System.Threading.Tasks;

namespace AashanaFashion.Services;

public class BankDetailsResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Ifsc { get; set; }
    public string? BankName { get; set; }
    public string? Branch { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? State { get; set; }
    public string? Micr { get; set; }
    public string? Contact { get; set; }
    public bool? Rtgs { get; set; }
    public bool? Neft { get; set; }
    public bool? Imps { get; set; }
    public bool? Upi { get; set; }
}

public interface IBankLookupService
{
    Task<BankDetailsResult> LookupIfscAsync(string ifsc);
    bool ValidateIfsc(string? ifsc, out string? errorMessage);
}
