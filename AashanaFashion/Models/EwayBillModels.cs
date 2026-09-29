using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AashanaFashion.Models
{
    public class EwayBillTransportInput
    {
        [Display(Name = "Transporter ID (GSTIN / TRANSIN)")]
        [StringLength(15)]
        public string? TransporterId { get; set; }

        [Display(Name = "Transporter / Courier Name")]
        [StringLength(100)]
        public string? TransporterName { get; set; }

        [Display(Name = "Vehicle Number")]
        [StringLength(20)]
        public string? VehicleNumber { get; set; }

        [Display(Name = "Vehicle Type")]
        public string VehicleType { get; set; } = "R"; // R = Regular, O = Over Dimensional Cargo

        [Display(Name = "Transport Mode")]
        public string TransMode { get; set; } = "1"; // 1 = Road, 2 = Rail, 3 = Air, 4 = Ship

        [Display(Name = "Approximate Distance (in KM)")]
        [Range(1, 4000)]
        public int DistanceKm { get; set; } = 50;

        [Display(Name = "LR / Docket / Transporter Doc No")]
        [StringLength(50)]
        public string? TransDocNo { get; set; }

        [Display(Name = "Transporter Doc Date")]
        public DateTime? TransDocDate { get; set; }
    }

    public class SaveEwayBillInput
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 12, ErrorMessage = "E-Way Bill Number is typically 12 digits.")]
        public string EwayBillNumber { get; set; } = string.Empty;

        public DateTime? EwayBillDate { get; set; } = DateTime.Today;

        public string? VehicleNumber { get; set; }
        public string? TransporterName { get; set; }
    }

    public class EwayBillPrintViewModel
    {
        public string EwayBillNumber { get; set; } = string.Empty;
        public DateTime EwayBillDate { get; set; } = DateTime.Now;
        public DateTime ValidFrom { get; set; } = DateTime.Now;
        public DateTime ValidUntil { get; set; } = DateTime.Now.AddDays(1);
        public string GeneratedBy { get; set; } = string.Empty;

        // Part-A: Goods / Transaction Details
        public string SupplierGstin { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string DispatchAddress { get; set; } = string.Empty;
        public string DispatchPlace { get; set; } = string.Empty;
        public int DispatchPincode { get; set; }
        public int DispatchStateCode { get; set; } = 24;

        public string RecipientGstin { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string DeliveryPlace { get; set; } = string.Empty;
        public int DeliveryPincode { get; set; }
        public int DeliveryStateCode { get; set; } = 24;

        public string DocType { get; set; } = "Tax Invoice";
        public string DocCode { get; set; } = "INV";
        public string DocNumber { get; set; } = string.Empty;
        public DateTime DocDate { get; set; } = DateTime.Today;

        public string SupplyType { get; set; } = "Outward - Supply";
        public string TransactionType { get; set; } = "Regular";
        public string ReasonForTransportation { get; set; } = "Supply";

        public decimal TaxableAmount { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal IgstAmount { get; set; }
        public decimal CessAmount { get; set; }
        public decimal TotalInvoiceValue { get; set; }

        public List<EwayBillPrintItemViewModel> Items { get; set; } = new();

        // Part-B: Vehicle / Transporter Details
        public string TransMode { get; set; } = "Road";
        public string VehicleNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = "Regular";
        public string TransporterName { get; set; } = string.Empty;
        public string TransporterId { get; set; } = string.Empty;
        public string TransDocNo { get; set; } = string.Empty;
        public DateTime? TransDocDate { get; set; }
        public int DistanceKm { get; set; } = 50;
        public string FromPlace { get; set; } = "Surat, Gujarat";

        public int SourceId { get; set; }
        public string SourceType { get; set; } = "Invoice";
    }

    public class EwayBillPrintItemViewModel
    {
        public int ItemNo { get; set; }
        public string HsnCode { get; set; } = "6204";
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "PCS";
        public decimal TaxableValue { get; set; }
        public decimal CgstRate { get; set; }
        public decimal SgstRate { get; set; }
        public decimal IgstRate { get; set; }
        public decimal CessRate { get; set; }
    }

    // =========================================================================
    // Official Government NIC E-Way Bill JSON Schema (v1.0.0421)
    // Accepted on ewaybillgst.gov.in (Bulk Generation)
    // =========================================================================
    public class NicEwayBillRoot
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0421";

        [JsonPropertyName("billLists")]
        public List<NicBillItem> BillLists { get; set; } = new();
    }

    public class NicBillItem
    {
        [JsonPropertyName("userGstin")]
        public string UserGstin { get; set; } = string.Empty;

        [JsonPropertyName("supplyType")]
        public string SupplyType { get; set; } = "O"; // O = Outward, I = Inward

        [JsonPropertyName("subSupplyType")]
        public string SubSupplyType { get; set; } = "1"; // 1 = Supply, 3 = Export, 4 = Job Work, 8 = Others

        [JsonPropertyName("subSupplyDesc")]
        public string SubSupplyDesc { get; set; } = string.Empty;

        [JsonPropertyName("docType")]
        public string DocType { get; set; } = "INV"; // INV = Tax Invoice, CHL = Delivery Challan, BIL = Bill of Supply

        [JsonPropertyName("docNo")]
        public string DocNo { get; set; } = string.Empty;

        [JsonPropertyName("docDate")]
        public string DocDate { get; set; } = string.Empty; // Format: dd/MM/yyyy

        [JsonPropertyName("fromGstin")]
        public string FromGstin { get; set; } = string.Empty;

        [JsonPropertyName("fromTrdName")]
        public string FromTrdName { get; set; } = string.Empty;

        [JsonPropertyName("fromAddr1")]
        public string FromAddr1 { get; set; } = string.Empty;

        [JsonPropertyName("fromAddr2")]
        public string FromAddr2 { get; set; } = string.Empty;

        [JsonPropertyName("fromPlace")]
        public string FromPlace { get; set; } = string.Empty;

        [JsonPropertyName("fromPincode")]
        public int FromPincode { get; set; }

        [JsonPropertyName("actFromStateCode")]
        public int ActFromStateCode { get; set; }

        [JsonPropertyName("fromStateCode")]
        public int FromStateCode { get; set; }

        [JsonPropertyName("toGstin")]
        public string ToGstin { get; set; } = "URP"; // URP for unregistered buyers

        [JsonPropertyName("toTrdName")]
        public string ToTrdName { get; set; } = string.Empty;

        [JsonPropertyName("toAddr1")]
        public string ToAddr1 { get; set; } = string.Empty;

        [JsonPropertyName("toAddr2")]
        public string ToAddr2 { get; set; } = string.Empty;

        [JsonPropertyName("toPlace")]
        public string ToPlace { get; set; } = string.Empty;

        [JsonPropertyName("toPincode")]
        public int ToPincode { get; set; }

        [JsonPropertyName("actToStateCode")]
        public int ActToStateCode { get; set; }

        [JsonPropertyName("toStateCode")]
        public int ToStateCode { get; set; }

        [JsonPropertyName("transactionType")]
        public int TransactionType { get; set; } = 1; // 1 = Regular, 2 = Bill to - Ship to, etc.

        [JsonPropertyName("otherValue")]
        public double OtherValue { get; set; } = 0.0;

        [JsonPropertyName("totalValue")]
        public double TotalValue { get; set; } // Taxable amount

        [JsonPropertyName("cgstValue")]
        public double CgstValue { get; set; }

        [JsonPropertyName("sgstValue")]
        public double SgstValue { get; set; }

        [JsonPropertyName("igstValue")]
        public double IgstValue { get; set; }

        [JsonPropertyName("cessValue")]
        public double CessValue { get; set; } = 0.0;

        [JsonPropertyName("cessNonAdvolValue")]
        public double CessNonAdvolValue { get; set; } = 0.0;

        [JsonPropertyName("totInvValue")]
        public double TotInvValue { get; set; } // Grand Total

        [JsonPropertyName("transporterId")]
        public string TransporterId { get; set; } = string.Empty;

        [JsonPropertyName("transporterName")]
        public string TransporterName { get; set; } = string.Empty;

        [JsonPropertyName("transDocNo")]
        public string TransDocNo { get; set; } = string.Empty;

        [JsonPropertyName("transMode")]
        public string TransMode { get; set; } = "1";

        [JsonPropertyName("transDistance")]
        public string TransDistance { get; set; } = "50";

        [JsonPropertyName("transDocDate")]
        public string TransDocDate { get; set; } = string.Empty;

        [JsonPropertyName("vehicleNo")]
        public string VehicleNo { get; set; } = string.Empty;

        [JsonPropertyName("vehicleType")]
        public string VehicleType { get; set; } = "R";

        [JsonPropertyName("itemList")]
        public List<NicItemDetail> ItemList { get; set; } = new();
    }

    public class NicItemDetail
    {
        [JsonPropertyName("itemNo")]
        public int ItemNo { get; set; }

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("productDesc")]
        public string ProductDesc { get; set; } = string.Empty;

        [JsonPropertyName("hsnCode")]
        public int HsnCode { get; set; }

        [JsonPropertyName("quantity")]
        public double Quantity { get; set; }

        [JsonPropertyName("qtyUnit")]
        public string QtyUnit { get; set; } = "PCS";

        [JsonPropertyName("cgstRate")]
        public double CgstRate { get; set; }

        [JsonPropertyName("sgstRate")]
        public double SgstRate { get; set; }

        [JsonPropertyName("igstRate")]
        public double IgstRate { get; set; }

        [JsonPropertyName("cessRate")]
        public double CessRate { get; set; } = 0.0;

        [JsonPropertyName("cessNonAdvol")]
        public double CessNonAdvol { get; set; } = 0.0;

        [JsonPropertyName("taxableAmount")]
        public double TaxableAmount { get; set; }
    }
}
