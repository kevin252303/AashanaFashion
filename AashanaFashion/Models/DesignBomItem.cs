namespace AashanaFashion.Models;

public class DesignBomItem
{
    public int Id { get; set; }
    public int DesignId { get; set; }
    public Design? Design { get; set; }

    public int RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    /// <summary>e.g., Chaniya, Choli, Dupatta, Blouse, Lining, Trims & Accessories</summary>
    public string Component { get; set; } = "General";

    /// <summary>Consumption per single garment piece</summary>
    public decimal QuantityPerPiece { get; set; }

    /// <summary>Cutting / shrinkage wastage % (e.g. 5)</summary>
    public decimal WastagePercentage { get; set; }

    public string? Remarks { get; set; }

    public decimal EffectiveQuantity => QuantityPerPiece * (1 + (WastagePercentage / 100m));
}
