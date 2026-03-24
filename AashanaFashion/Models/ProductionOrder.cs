namespace AashanaFashion.Models
{
    public class ProductionOrder
    {
        public int Id { get; set; }
        public string DesignNumber { get; set; } = string.Empty;
        public string FabricType { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public OrderStatus Status { get; set; }
        public bool IsRawMaterialVerified { get; set; }
        public bool IsDyingVerified { get; set; }
        public bool IsHandworkVerified { get; set; }
        public bool IsStitchingVerified { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    public enum OrderStatus
    {
        RawMaterialArrived,
        AtDying,
        AtHandwork,
        AtStitching,
        ReadyToDispatch,
        Dispatched
    }
}
