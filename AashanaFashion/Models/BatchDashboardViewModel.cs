namespace AashanaFashion.Models
{
    public class BatchDashboardViewModel
    {
        public int Id { get; set; }
        public string DesignNumber { get; set; } = string.Empty;
        public string LotNo { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public string CurrentStage { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }
        public OrderStatus Status { get; set; }
        public List<string> CreationSteps { get; set; } = new();
        public Dictionary<string, bool> VerificationStatus { get; set; } = new();
    }
}
