namespace AashanaFashion.Models;

public class ProductionOrderViewModel
{
    public int Id { get; set; }
    public int DesignId { get; set; }
    public string? DesignNumber { get; set; }
    public string LotNo { get; set; } = string.Empty;
    public string FabricType { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public bool IsRawMaterialVerified { get; set; }
    public bool IsDyingVerified { get; set; }
    public bool IsHandworkVerified { get; set; }
    public bool IsStitchingVerified { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<ProductionOrderDetail> Details { get; set; } = new();
    public Design? Design { get; set; }
}
