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
