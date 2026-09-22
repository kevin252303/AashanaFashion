namespace AashanaFashion.Models;

public class PrintTagsViewModel
{
    public ProductionOrder? Order { get; set; }
    public List<EntityBarcodeTagItem> Tags { get; set; } = new();
    public string Format { get; set; } = "ThermalRoll"; // ThermalRoll or A4Sheet
    public string? FilterEntityType { get; set; }
}

public class EntityBarcodeTagItem
{
    public int EntityId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string DesignNumber { get; set; } = string.Empty;
    public string LotNo { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string Colour { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public int SlNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string BarcodeSvg { get; set; } = string.Empty;
    public string QrCodeSvg { get; set; } = string.Empty;
}

public class ScanLookupResultViewModel
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int EntityId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string LotNo { get; set; } = string.Empty;
    public string DesignNumber { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string Colour { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public int SlNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CurrentProcess { get; set; }
    public string? CurrentVendor { get; set; }
    public int? CurrentTrackingId { get; set; }
    public DateTime? ExpectedReturn { get; set; }
    public List<ScanProcessHistoryItem> History { get; set; } = new();
}

public class ScanProcessHistoryItem
{
    public string ProcessName { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public string? GivenDate { get; set; }
    public string? ActualReturnDate { get; set; }
    public bool IsComplete { get; set; }
}

public class ScanActionRequest
{
    public string Barcode { get; set; } = string.Empty;
    public string ActionType { get; set; } = "Return"; // Return, SendProcess, Complete
    public string? ProcessName { get; set; }
    public int? VendorId { get; set; }
    public DateTime? ExpectedReturn { get; set; }
    public string? Notes { get; set; }
}
