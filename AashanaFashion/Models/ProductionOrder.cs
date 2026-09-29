namespace AashanaFashion.Models
{
    public class ProductionOrder
    {
        public int Id { get; set; }
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
        public DateTime CreatedDate { get; set; } = DateTime.Now;

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

        public List<ProductionOrderDetail> Details { get; set; } = new();
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
