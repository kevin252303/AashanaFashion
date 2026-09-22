namespace AashanaFashion.Models;

public class DesignOperationCost
{
    public int Id { get; set; }
    public int DesignId { get; set; }
    public Design? Design { get; set; }

    /// <summary>Process step e.g., Dying, Roll Press, Handwork, Stitching, Finishing, Packing</summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>Estimated labor / job-work cost in ₹ per piece</summary>
    public decimal EstimatedCost { get; set; }

    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public string? Remarks { get; set; }
}
