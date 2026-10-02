namespace AashanaFashion.Models
{
    public class ProductionOrder : IMustHaveTenant
    {
        public int Id { get; set; }
        public int TenantId { get; set; } = 1;
        public int CompanyId { get; set; } = 1;
        public Company? Company { get; set; }
        public int DesignId { get; set; }
        public Design? Design { get; set; }
        public string LotNo { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public OrderStatus Status { get; set; }
        public bool IsRawMaterialVerified { get; set; }
        public bool IsMaterialIssued { get; set; }
        public DateTime? MaterialIssuedDate { get; set; }
        public bool IsDyingVerified { get; set; }
        public bool IsHandworkVerified { get; set; }
        public bool IsStitchingVerified { get; set; }
        public bool IsStockInwarded { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // ——— Dynamic Process Master Stage Tracking ———
        public string? CurrentProcess { get; set; }
        public string? CompletedProcesses { get; set; }

        public List<string> GetCompletedProcessesList()
        {
            if (string.IsNullOrWhiteSpace(CompletedProcesses)) return new List<string>();
            return CompletedProcesses.Split(new[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim())
                                     .Where(s => !string.IsNullOrEmpty(s))
                                     .ToList();
        }

        public void SetCompletedProcessesList(IEnumerable<string> list)
        {
            CompletedProcesses = string.Join(",", list.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));
        }

        // ——— Assigned Workers / Karigars for this Lot ———
        public int? HandworkWorkerId { get; set; }
        public Vendor? HandworkWorker { get; set; }

        public int? StitchingWorkerId { get; set; }
        public Vendor? StitchingWorker { get; set; }

        // ——— Handwork Garment Parts for this Lot ———
        public bool HandworkCholi { get; set; } = true;
        public bool HandworkChaniya { get; set; } = true;
        public bool HandworkDupatta { get; set; } = false;

        public string HandworkComponentsSummary
        {
            get
            {
                var parts = new List<string>();
                if (HandworkCholi) parts.Add("Choli");
                if (HandworkChaniya) parts.Add("Chaniya");
                if (HandworkDupatta) parts.Add("Dupatta");
                return parts.Any() ? string.Join(", ", parts) : "None";
            }
        }

        // ——— Dynamic Multi-Component & Multi-Process Vendor Assignments ———
        public string Components { get; set; } = "Chaniya, Choli, Dupatta";
        public List<ProductionOrderComponentAssignment> ComponentAssignments { get; set; } = new();

        public List<string> GetComponentsList()
        {
            if (!string.IsNullOrWhiteSpace(Components))
            {
                return Components
                    .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            if (ComponentAssignments != null && ComponentAssignments.Any())
            {
                return ComponentAssignments
                    .Select(a => a.ComponentName.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            return Design?.GetComponentsList() ?? new List<string> { "Chaniya", "Choli", "Dupatta" };
        }

        public void SetComponentsList(IEnumerable<string> list)
        {
            Components = string.Join(", ", list.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));
        }

        public int? GetAssignedVendorId(string component, string process)
        {
            return ComponentAssignments?
                .FirstOrDefault(a => string.Equals(a.ComponentName, component, StringComparison.OrdinalIgnoreCase) &&
                                     string.Equals(a.ProcessName, process, StringComparison.OrdinalIgnoreCase))?.VendorId;
        }

        public Vendor? GetAssignedVendor(string component, string process)
        {
            return ComponentAssignments?
                .FirstOrDefault(a => string.Equals(a.ComponentName, component, StringComparison.OrdinalIgnoreCase) &&
                                     string.Equals(a.ProcessName, process, StringComparison.OrdinalIgnoreCase))?.Vendor;
        }

        public List<ProductionOrderDetail> Details { get; set; } = new();
        public List<JobSlip> JobSlips { get; set; } = new();
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
