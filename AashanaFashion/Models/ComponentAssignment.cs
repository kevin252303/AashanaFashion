namespace AashanaFashion.Models;

public class DesignComponentAssignment
{
    public int Id { get; set; }

    public int DesignId { get; set; }
    public Design? Design { get; set; }

    /// <summary>e.g. "Chaniya", "Choli", "Dupatta", "Belt", "Jacket", "Inner / Lining"</summary>
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>e.g. "Handwork", "Stitching", "Dying", "Cutting", "Roll", etc.</summary>
    public string ProcessName { get; set; } = string.Empty;

    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public decimal EstimatedRate { get; set; }
    public string? Remarks { get; set; }
}

public class ProductionOrderComponentAssignment
{
    public int Id { get; set; }

    public int ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    /// <summary>e.g. "Chaniya", "Choli", "Dupatta", "Belt", "Jacket", "Inner / Lining"</summary>
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>e.g. "Handwork", "Stitching", "Dying", "Cutting", "Roll", etc.</summary>
    public string ProcessName { get; set; } = string.Empty;

    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public decimal Rate { get; set; }
    public string? Remarks { get; set; }
}
