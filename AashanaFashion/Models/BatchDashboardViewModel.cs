namespace AashanaFashion.Models
{
    public class BatchDashboardViewModel
    {
        public int Id { get; set; }
        public string DesignNumber { get; set; } = string.Empty;
        public string LotNo { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public bool IsGhagraDone { get; set; }
        public bool IsCholiDone { get; set; }
        public bool IsDupattaDone { get; set; }
        public string CurrentStage { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }
        public bool IsHandworkVerified { get; set; }
        public bool IsStitchingVerified { get; set; }
        public OrderStatus Status { get; set; }
    }
}
