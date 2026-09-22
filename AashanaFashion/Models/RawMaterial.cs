namespace AashanaFashion.Models;

public class RawMaterial
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal Rate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}

public class RawMaterialRequirement
{
    public int Id { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Purchased
    public string? Remarks { get; set; }
    public DateTime RequiredDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}

public class RawMaterialTransaction
{
    public int Id { get; set; }

    public int RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    public string Type { get; set; } = "Inward";
    public decimal Quantity { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? ReferenceType { get; set; } // PurchaseOrder, ProductionOrder, ManualAdjustment
    public int? ReferenceId { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}

public class RawMaterialInOutLine
{
    public string Type { get; set; } = "Inward";
    public decimal Quantity { get; set; }
    public string? Remarks { get; set; }
}

public class RawMaterialInOutViewModel
{
    public int MaterialId { get; set; }
    public string? MaterialName { get; set; }
    public string? Unit { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public List<RawMaterialInOutLine> Lines { get; set; } = new();
    public List<RawMaterial> AvailableMaterials { get; set; } = new();
    public List<RawMaterialTransaction> RecentTransactions { get; set; } = new();
}

public class RawMaterialLedgerViewModel
{
    public int? MaterialId { get; set; }
    public string? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<RawMaterialTransaction> Transactions { get; set; } = new();
    public List<RawMaterial> Materials { get; set; } = new();
    public decimal TotalInward { get; set; }
    public decimal TotalOutward { get; set; }
    public decimal TotalAdjustment { get; set; }
}

public class RawMaterialAdjustmentViewModel
{
    public int MaterialId { get; set; }
    public string? MaterialName { get; set; }
    public string? Unit { get; set; }
    public decimal CurrentStock { get; set; }
    public string AdjustmentType { get; set; } = "Count"; // Count (Set new balance), Add (+), Subtract (-)
    public decimal NewQuantity { get; set; }
    public decimal Quantity { get; set; }
    public string? Reason { get; set; }
    public List<RawMaterial> AvailableMaterials { get; set; } = new();
}
