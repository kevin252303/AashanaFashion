using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AashanaFashion.Services
{
    public class EwayBillService : IEwayBillService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public EwayBillService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<string> GenerateInvoiceJsonAsync(int invoiceId, EwayBillTransportInput? transport = null)
        {
            var invoice = await _context.TaxInvoices
                .Include(i => i.Customer)
                .Include(i => i.Items)
                .ThenInclude(item => item.Design)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                throw new InvalidOperationException($"Tax Invoice #{invoiceId} not found.");
            }

            // Company (Supplier) Defaults
            string fromGstin = _config["Company:GSTIN"] ?? "24AABCA1234F1Z9";
            string fromTrdName = _config["Company:Name"] ?? "Aashana Fashion";
            string fromAddr1 = "Plot 14-16, Garment Industrial Zone";
            string fromAddr2 = "Pandesara";
            string fromPlace = "Surat";
            int fromPincode = 394221;
            int fromStateCode = 24; // Gujarat

            // Buyer (Recipient)
            string toGstin = !string.IsNullOrWhiteSpace(invoice.CustomerGstin) && invoice.CustomerGstin.Length == 15
                ? invoice.CustomerGstin.Trim().ToUpper()
                : "URP";

            string toTrdName = string.IsNullOrWhiteSpace(invoice.CustomerName) ? "Cash Buyer" : invoice.CustomerName;
            string toAddr1 = string.IsNullOrWhiteSpace(invoice.ShippingAddress) ? (invoice.BillingAddress ?? "Surat") : invoice.ShippingAddress;
            string toPlace = invoice.Customer?.City ?? "Surat";
            int toPincode = ExtractPincode(invoice.Customer?.PinCode ?? toAddr1, 395002);
            int toStateCode = GetStateCode(toGstin, invoice.PlaceOfSupply ?? invoice.Customer?.State);

            // Transport parameters
            string vehicleNo = (transport?.VehicleNumber ?? invoice.VehicleNumber ?? "").Replace(" ", "").Replace("-", "").ToUpper();
            string transporterName = transport?.TransporterName ?? invoice.TransporterName ?? "";
            string transporterId = (transport?.TransporterId ?? invoice.TransporterId ?? "").Trim().ToUpper();
            int distanceKm = transport?.DistanceKm ?? (invoice.DistanceKm.HasValue && invoice.DistanceKm.Value > 0 ? invoice.DistanceKm.Value : 50);
            string vehicleType = transport?.VehicleType ?? invoice.VehicleType ?? "R";
            string transMode = transport?.TransMode ?? invoice.TransMode ?? "1";
            string transDocNo = transport?.TransDocNo ?? "";
            string transDocDate = (transport?.TransDocDate ?? invoice.InvoiceDate).ToString("dd/MM/yyyy");

            var bill = new NicBillItem
            {
                UserGstin = fromGstin,
                SupplyType = "O", // Outward Supply
                SubSupplyType = "1", // 1 = Supply
                SubSupplyDesc = "",
                DocType = "INV", // Tax Invoice
                DocNo = invoice.InvoiceNumber,
                DocDate = invoice.InvoiceDate.ToString("dd/MM/yyyy"),

                FromGstin = fromGstin,
                FromTrdName = fromTrdName,
                FromAddr1 = fromAddr1,
                FromAddr2 = fromAddr2,
                FromPlace = fromPlace,
                FromPincode = fromPincode,
                ActFromStateCode = fromStateCode,
                FromStateCode = fromStateCode,

                ToGstin = toGstin,
                ToTrdName = toTrdName,
                ToAddr1 = toAddr1.Length > 100 ? toAddr1.Substring(0, 100) : toAddr1,
                ToAddr2 = "",
                ToPlace = toPlace,
                ToPincode = toPincode,
                ActToStateCode = toStateCode,
                ToStateCode = toStateCode,

                TransactionType = 1, // Regular
                OtherValue = 0.0,
                TotalValue = Math.Round((double)invoice.TaxableAmount, 2),
                CgstValue = Math.Round((double)invoice.CgstAmount, 2),
                SgstValue = Math.Round((double)invoice.SgstAmount, 2),
                IgstValue = Math.Round((double)invoice.IgstAmount, 2),
                CessValue = 0.0,
                CessNonAdvolValue = 0.0,
                TotInvValue = Math.Round((double)invoice.GrandTotal, 2),

                TransporterId = transporterId,
                TransporterName = transporterName,
                TransDocNo = transDocNo,
                TransMode = transMode,
                TransDistance = distanceKm.ToString(),
                TransDocDate = transDocDate,
                VehicleNo = vehicleNo,
                VehicleType = vehicleType
            };

            int itemIndex = 1;
            foreach (var item in invoice.Items)
            {
                int hsn = 6204;
                if (!string.IsNullOrWhiteSpace(item.HsnCode))
                {
                    var digitsOnly = Regex.Replace(item.HsnCode, @"[^\d]", "");
                    if (int.TryParse(digitsOnly, out int parsedHsn))
                    {
                        hsn = parsedHsn;
                    }
                }

                double gst = (double)item.GstRate;
                double cgst = invoice.IsInterState ? 0.0 : gst / 2.0;
                double sgst = invoice.IsInterState ? 0.0 : gst / 2.0;
                double igst = invoice.IsInterState ? gst : 0.0;

                bill.ItemList.Add(new NicItemDetail
                {
                    ItemNo = itemIndex++,
                    ProductName = string.IsNullOrWhiteSpace(item.Description) ? "Garments" : item.Description,
                    ProductDesc = $"{item.Colour} {item.Size}".Trim(),
                    HsnCode = hsn,
                    Quantity = (double)item.Quantity,
                    QtyUnit = "PCS",
                    CgstRate = cgst,
                    SgstRate = sgst,
                    IgstRate = igst,
                    CessRate = 0.0,
                    CessNonAdvol = 0.0,
                    TaxableAmount = Math.Round((double)item.TaxableValue, 2)
                });
            }

            var root = new NicEwayBillRoot
            {
                Version = "1.0.0421",
                BillLists = new List<NicBillItem> { bill }
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(root, options);
        }

        public async Task<string> GenerateChallanJsonAsync(int challanId, EwayBillTransportInput? transport = null)
        {
            var challan = await _context.DeliveryChallans
                .Include(c => c.Customer)
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == challanId);

            if (challan == null)
            {
                throw new InvalidOperationException($"Delivery Challan #{challanId} not found.");
            }

            string fromGstin = _config["Company:GSTIN"] ?? "24AABCA1234F1Z9";
            string fromTrdName = _config["Company:Name"] ?? "Aashana Fashion";
            string fromAddr1 = "Plot 14-16, Garment Industrial Zone";
            string fromPlace = "Surat";
            int fromPincode = 394221;
            int fromStateCode = 24;

            string toGstin = !string.IsNullOrWhiteSpace(challan.Customer?.GstNumber) && challan.Customer.GstNumber.Length == 15
                ? challan.Customer.GstNumber.Trim().ToUpper()
                : "URP";

            string toTrdName = challan.Customer?.CustomerName ?? "Consignee";
            string toAddr1 = challan.ShippingAddress ?? (challan.Customer?.Address ?? "Surat");
            string toPlace = challan.Customer?.City ?? "Surat";
            int toPincode = ExtractPincode(challan.Customer?.PinCode ?? toAddr1, 395002);
            int toStateCode = GetStateCode(toGstin, challan.Customer?.State);

            string vehicleNo = (transport?.VehicleNumber ?? challan.VehicleNumber ?? "").Replace(" ", "").Replace("-", "").ToUpper();
            string transporterName = transport?.TransporterName ?? challan.TransporterName ?? "";
            string transporterId = (transport?.TransporterId ?? challan.TransporterId ?? "").Trim().ToUpper();
            int distanceKm = transport?.DistanceKm ?? (challan.DistanceKm.HasValue && challan.DistanceKm.Value > 0 ? challan.DistanceKm.Value : 50);
            string vehicleType = transport?.VehicleType ?? challan.VehicleType ?? "R";
            string transMode = transport?.TransMode ?? challan.TransMode ?? "1";
            string transDocNo = transport?.TransDocNo ?? challan.LrNumber ?? "";
            string transDocDate = (transport?.TransDocDate ?? challan.ChallanDate).ToString("dd/MM/yyyy");

            // Compute estimated challan taxable value based on pieces (e.g. ₹500/pc default for garment jobwork)
            double totalPcs = challan.Items.Sum(i => i.QuantityDispatched);
            double estimatedValue = totalPcs * 500.0;
            double cgst = (fromStateCode == toStateCode) ? (estimatedValue * 0.025) : 0.0;
            double sgst = (fromStateCode == toStateCode) ? (estimatedValue * 0.025) : 0.0;
            double igst = (fromStateCode != toStateCode) ? (estimatedValue * 0.05) : 0.0;
            double grandTotal = estimatedValue + cgst + sgst + igst;

            var bill = new NicBillItem
            {
                UserGstin = fromGstin,
                SupplyType = "O", // Outward
                SubSupplyType = "4", // 4 = Job Work
                SubSupplyDesc = "Garment Manufacturing Job Work",
                DocType = "CHL", // Delivery Challan
                DocNo = challan.ChallanNumber,
                DocDate = challan.ChallanDate.ToString("dd/MM/yyyy"),

                FromGstin = fromGstin,
                FromTrdName = fromTrdName,
                FromAddr1 = fromAddr1,
                FromAddr2 = "Pandesara",
                FromPlace = fromPlace,
                FromPincode = fromPincode,
                ActFromStateCode = fromStateCode,
                FromStateCode = fromStateCode,

                ToGstin = toGstin,
                ToTrdName = toTrdName,
                ToAddr1 = toAddr1.Length > 100 ? toAddr1.Substring(0, 100) : toAddr1,
                ToAddr2 = "",
                ToPlace = toPlace,
                ToPincode = toPincode,
                ActToStateCode = toStateCode,
                ToStateCode = toStateCode,

                TransactionType = 1,
                OtherValue = 0.0,
                TotalValue = Math.Round(estimatedValue, 2),
                CgstValue = Math.Round(cgst, 2),
                SgstValue = Math.Round(sgst, 2),
                IgstValue = Math.Round(igst, 2),
                CessValue = 0.0,
                CessNonAdvolValue = 0.0,
                TotInvValue = Math.Round(grandTotal, 2),

                TransporterId = transporterId,
                TransporterName = transporterName,
                TransDocNo = transDocNo,
                TransMode = transMode,
                TransDistance = distanceKm.ToString(),
                TransDocDate = transDocDate,
                VehicleNo = vehicleNo,
                VehicleType = vehicleType
            };

            int itemNo = 1;
            foreach (var item in challan.Items)
            {
                double itemVal = item.QuantityDispatched * 500.0;
                bill.ItemList.Add(new NicItemDetail
                {
                    ItemNo = itemNo++,
                    ProductName = $"Design {item.DesignNumber}",
                    ProductDesc = $"{item.Colour} {item.Size}".Trim(),
                    HsnCode = 6204,
                    Quantity = item.QuantityDispatched,
                    QtyUnit = "PCS",
                    CgstRate = (fromStateCode == toStateCode) ? 2.5 : 0.0,
                    SgstRate = (fromStateCode == toStateCode) ? 2.5 : 0.0,
                    IgstRate = (fromStateCode != toStateCode) ? 5.0 : 0.0,
                    TaxableAmount = Math.Round(itemVal, 2)
                });
            }

            var root = new NicEwayBillRoot
            {
                Version = "1.0.0421",
                BillLists = new List<NicBillItem> { bill }
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(root, options);
        }

        public int GetStateCode(string? gstin, string? stateName)
        {
            if (!string.IsNullOrWhiteSpace(gstin) && gstin.Length >= 2)
            {
                if (int.TryParse(gstin.Substring(0, 2), out int code) && code >= 1 && code <= 38)
                {
                    return code;
                }
            }

            if (!string.IsNullOrWhiteSpace(stateName))
            {
                var s = stateName.ToLower();
                if (s.Contains("gujarat")) return 24;
                if (s.Contains("maharashtra")) return 27;
                if (s.Contains("delhi")) return 7;
                if (s.Contains("rajasthan")) return 8;
                if (s.Contains("uttar pradesh")) return 9;
                if (s.Contains("madhya pradesh")) return 23;
                if (s.Contains("karnataka")) return 29;
                if (s.Contains("tamil nadu")) return 33;
                if (s.Contains("west bengal")) return 19;
                if (s.Contains("punjab")) return 3;
                if (s.Contains("haryana")) return 6;
                if (s.Contains("telangana")) return 36;
                if (s.Contains("andhra")) return 37;
            }

            return 24; // Default to Gujarat
        }

        public int ExtractPincode(string? text, int defaultPin = 395002)
        {
            if (string.IsNullOrWhiteSpace(text)) return defaultPin;

            var match = Regex.Match(text, @"\b[1-9][0-9]{5}\b");
            if (match.Success && int.TryParse(match.Value, out int pin))
            {
                return pin;
            }

            return defaultPin;
        }
    }
}
