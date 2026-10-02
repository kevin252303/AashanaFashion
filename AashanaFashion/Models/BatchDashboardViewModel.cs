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
        public List<string> CompletedProcesses { get; set; } = new();
        public string? CurrentProcess { get; set; }
        public Dictionary<string, bool> VerificationStatus { get; set; } = new();
        public string? HandworkWorkerName { get; set; }
        public string? StitchingWorkerName { get; set; }
        public string? HandworkParts { get; set; }
        public string? PhotoPath { get; set; }
        public List<ProductionOrderComponentAssignment> ComponentAssignments { get; set; } = new();
        public int JobSlipCount { get; set; }

        public string GetComponentAssignmentsSummary(string processName)
        {
            var forProcess = ComponentAssignments
                .Where(a => string.Equals(a.ProcessName, processName, StringComparison.OrdinalIgnoreCase) && a.Vendor != null)
                .ToList();
            if (!forProcess.Any()) return "";
            return string.Join(", ", forProcess.Select(a => $"{a.ComponentName} ➔ {a.Vendor?.VendorName}"));
        }
    }
}
