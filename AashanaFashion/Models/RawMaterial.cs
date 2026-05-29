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
