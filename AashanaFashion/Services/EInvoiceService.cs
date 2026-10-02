using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AashanaFashion.Data;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public class EInvoiceService : IEInvoiceService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IEwayBillService _ewayBillService;

    public EInvoiceService(AppDbContext context, IConfiguration config, IEwayBillService ewayBillService)
    {
        _context = context;
        _config = config;
        _ewayBillService = ewayBillService;
    }

    public async Task<string> GenerateStandardInv01JsonAsync(int invoiceId)
    {
        var invoice = await _context.TaxInvoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
                .ThenInclude(item => item.Design)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
        {
            throw new InvalidOperationException($"Tax Invoice #{invoiceId} was not found.");
        }

        // Supplier (Aashana Fashion) Details
        string fromGstin = _config["Company:GSTIN"] ?? "24AABCA1234F1Z9";
        string fromLegalName = _config["Company:LegalName"] ?? "Aashana Fashion Private Limited";
        string fromTradeName = _config["Company:Name"] ?? "Aashana Fashion";
        string fromAddr1 = _config["Company:Address1"] ?? "Plot 14-16, Garment Industrial Zone";
        string fromAddr2 = _config["Company:Address2"] ?? "Pandesara";
        string fromPlace = _config["Company:City"] ?? "Surat";
        int fromPin = int.TryParse(_config["Company:Pincode"], out var p) ? p : 394221;
        string fromStateCode = (_config["Company:StateCode"] ?? "24").PadLeft(2, '0');
        string fromPhone = _config["Company:Phone"] ?? "9876543210";
        string fromEmail = _config["Company:Email"] ?? "accounts@aashanafashion.com";

        // Buyer (Recipient) Details
        string toGstin = !string.IsNullOrWhiteSpace(invoice.CustomerGstin) && invoice.CustomerGstin.Trim().Length == 15
            ? invoice.CustomerGstin.Trim().ToUpper()
            : "URP";

        string toLegalName = !string.IsNullOrWhiteSpace(invoice.CustomerName) ? invoice.CustomerName.Trim() : "B2B Customer";
        string toTradeName = toLegalName;
        string toAddr1 = !string.IsNullOrWhiteSpace(invoice.BillingAddress) ? invoice.BillingAddress.Trim() : (!string.IsNullOrWhiteSpace(invoice.ShippingAddress) ? invoice.ShippingAddress.Trim() : "Surat");
        string toPlace = !string.IsNullOrWhiteSpace(invoice.Customer?.City) ? invoice.Customer.City.Trim() : "Surat";
        int toPin = _ewayBillService.ExtractPincode(invoice.Customer?.PinCode ?? toAddr1, 395002);
        string toStateCode = _ewayBillService.GetStateCode(toGstin, invoice.PlaceOfSupply ?? invoice.Customer?.State).ToString().PadLeft(2, '0');
        string toPhone = invoice.Customer?.Phone ?? "9800000000";
        string toEmail = invoice.Customer?.Email ?? "customer@example.com";

        // Supply Type: B2B, SEZWP, SEZWOP, EXPWP, EXPWOP, DEXP
        string supplyType = string.IsNullOrWhiteSpace(invoice.EInvoiceSupplyType) ? "B2B" : invoice.EInvoiceSupplyType;

        // Item List mapping to INV-01
        var itemList = new List<object>();
        int itemIndex = 1;

        foreach (var item in invoice.Items)
        {
            decimal qty = item.Quantity > 0 ? item.Quantity : 1;
            decimal unitPrice = item.UnitPrice;
            decimal totAmt = Math.Round(qty * unitPrice, 2);
            decimal discount = item.DiscountAmount;
            decimal assAmt = item.TaxableValue > 0 ? item.TaxableValue : Math.Max(0m, totAmt - discount);
            decimal gstRt = item.GstRate > 0 ? item.GstRate : (invoice.IsInterState ? invoice.IgstRate : (invoice.CgstRate + invoice.SgstRate));

            decimal igstAmt = invoice.IsInterState ? Math.Round(assAmt * (gstRt / 100m), 2) : 0m;
            decimal cgstAmt = !invoice.IsInterState ? Math.Round(assAmt * (gstRt / 200m), 2) : 0m;
            decimal sgstAmt = !invoice.IsInterState ? Math.Round(assAmt * (gstRt / 200m), 2) : 0m;
            decimal totItemVal = assAmt + igstAmt + cgstAmt + sgstAmt;

            string hsn = !string.IsNullOrWhiteSpace(item.HsnCode) 
                ? Regex.Replace(item.HsnCode, @"[^\d]", "") 
                : "6204";
            if (hsn.Length < 4) hsn = "6204";

            itemList.Add(new
            {
                SlNo = itemIndex.ToString(),
                PrdDesc = string.IsNullOrWhiteSpace(item.Description) ? "Designer Ethnic Garment" : item.Description,
                IsServc = "N",
                HsnCd = hsn,
                Barcde = (string?)null,
                Qty = (double)qty,
                FreeQty = 0.0,
                Unit = "PCS",
                UnitPrice = (double)unitPrice,
                TotAmt = (double)totAmt,
                Discount = (double)discount,
                PreTaxVal = 0.0,
                AssAmt = (double)assAmt,
                GstRt = (double)gstRt,
                IgstAmt = (double)igstAmt,
                CgstAmt = (double)cgstAmt,
                SgstAmt = (double)sgstAmt,
                CesRt = 0.0,
                CesAmt = 0.0,
                CesNonAdvlAmt = 0.0,
                StateCesRt = 0.0,
                StateCesAmt = 0.0,
                StateCesNonAdvlAmt = 0.0,
                OthChrg = 0.0,
                TotItemVal = (double)totItemVal
            });

            itemIndex++;
        }

        // Standard Government INV-01 Schema Structure
        var payload = new
        {
            Version = "1.1",
            TranDtls = new
            {
                TaxSch = "GST",
                SupTyp = supplyType,
                RegRev = "N",
                EcmGstin = (string?)null,
                IgstOnIntra = "N"
            },
            DocDtls = new
            {
                Typ = "INV",
                No = invoice.InvoiceNumber,
                Dt = invoice.InvoiceDate.ToString("dd/MM/yyyy")
            },
            SellerDtls = new
            {
                Gstin = fromGstin,
                LglNm = fromLegalName,
                TrdNm = fromTradeName,
                Pos = fromStateCode,
                Addr1 = fromAddr1,
                Addr2 = fromAddr2,
                Loc = fromPlace,
                Pin = fromPin,
                Stcd = fromStateCode,
                Ph = fromPhone,
                Em = fromEmail
            },
            BuyerDtls = new
            {
                Gstin = toGstin,
                LglNm = toLegalName,
                TrdNm = toTradeName,
                Pos = toStateCode,
                Addr1 = toAddr1,
                Addr2 = "",
                Loc = toPlace,
                Pin = toPin,
                Stcd = toStateCode,
                Ph = toPhone,
                Em = toEmail
            },
            DispDtls = (object?)null,
            ShipDtls = (object?)null,
            ItemList = itemList,
            ValDtls = new
            {
                AssVal = (double)invoice.TaxableAmount,
                CgstVal = (double)invoice.CgstAmount,
                SgstVal = (double)invoice.SgstAmount,
                IgstVal = (double)invoice.IgstAmount,
                CesVal = 0.0,
                StCesVal = 0.0,
                Discount = (double)invoice.DiscountAmount,
                OthChrg = 0.0,
                RndOffAmt = (double)invoice.RoundOff,
                TotInvVal = (double)invoice.GrandTotal
            },
            PayDtls = new
            {
                Nm = invoice.BankName,
                AccDet = invoice.BankAccountNumber,
                Mode = "NEFT",
                FinInsBr = invoice.BankIfsc,
                PayTerm = "Payment due within credit period",
                PaidAmt = (double)invoice.PaidAmount,
                PayDue = (double)invoice.BalanceDue
            },
            RefDtls = new
            {
                InvRm = string.IsNullOrWhiteSpace(invoice.Notes) ? "Government Standard Tax Invoice" : invoice.Notes
            },
            EwbDtls = (!string.IsNullOrWhiteSpace(invoice.VehicleNumber) || !string.IsNullOrWhiteSpace(invoice.TransporterId) || !string.IsNullOrWhiteSpace(invoice.EwayBillNumber)) ? new
            {
                TransId = invoice.TransporterId ?? "",
                TransName = invoice.TransporterName ?? "",
                Distance = invoice.DistanceKm ?? 50,
                TransDocNo = "",
                TransDocDt = invoice.InvoiceDate.ToString("dd/MM/yyyy"),
                VehNo = (invoice.VehicleNumber ?? "").Replace(" ", "").Replace("-", "").ToUpper(),
                VehType = invoice.VehicleType ?? "R",
                TransMode = invoice.TransMode ?? "1"
            } : null
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null
        };

        return JsonSerializer.Serialize(payload, options);
    }

    public async Task<EInvoiceResult> GenerateEInvoiceAsync(int invoiceId, EInvoiceGenerateRequest? request = null)
    {
        var result = new EInvoiceResult();

        var invoice = await _context.TaxInvoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
        {
            result.Success = false;
            result.Message = "Invoice not found.";
            return result;
        }

        if (invoice.EInvoiceStatus == "Generated" && !string.IsNullOrEmpty(invoice.Irn))
        {
            result.Success = true;
            result.Message = $"E-Invoice already generated with IRN: {invoice.Irn}";
            result.Irn = invoice.Irn;
            result.AckNo = invoice.EInvoiceAckNo;
            result.AckDate = invoice.EInvoiceAckDate;
            result.SignedQrCode = invoice.EInvoiceSignedQrCode;
            result.QrCodeSvg = GenerateQrCodeSvg(invoice.EInvoiceSignedQrCode ?? invoice.Irn);
            result.EwayBillNumber = invoice.EwayBillNumber;
            return result;
        }

        // Validation against Government Schema requirements
        var errors = new List<string>();
        string fromGstin = _config["Company:GSTIN"] ?? "24AABCA1234F1Z9";

        if (string.IsNullOrWhiteSpace(fromGstin) || fromGstin.Length != 15)
        {
            errors.Add("Company GSTIN must be a valid 15-character alphanumeric GST identification number.");
        }

        string supplyType = request?.SupplyType ?? invoice.EInvoiceSupplyType ?? "B2B";
        if (supplyType == "B2B")
        {
            if (string.IsNullOrWhiteSpace(invoice.CustomerGstin) || invoice.CustomerGstin.Trim().Length != 15)
            {
                errors.Add("For standard B2B e-Invoicing, the recipient Customer GSTIN is mandatory and must be 15 characters. If customer is unregistered, select B2C or update GSTIN.");
            }
        }

        if (!invoice.Items.Any())
        {
            errors.Add("Invoice must contain at least one item line to generate an e-invoice.");
        }

        foreach (var item in invoice.Items)
        {
            if (string.IsNullOrWhiteSpace(item.HsnCode))
            {
                errors.Add($"Item '{item.Description}' is missing HSN Code. HSN code (at least 4 digits) is mandatory under GST rules.");
            }
        }

        if (errors.Any())
        {
            result.Success = false;
            result.Errors = errors;
            result.Message = string.Join(" ", errors);
            invoice.EInvoiceStatus = "Failed";
            invoice.EInvoiceErrors = result.Message;
            await _context.SaveChangesAsync();
            return result;
        }

        // Calculate standard Government IRN (SHA-256 hash)
        string finYear = GetFinancialYear(invoice.InvoiceDate);
        string docType = "INV";
        string irn = CalculateIrn(fromGstin, finYear, docType, invoice.InvoiceNumber);

        // Generate 15-digit IRP Acknowledgment Number
        // Standard IRP format: [StateCode (2)][Year (2)][Portal (1)][Sequential ID (10)]
        string statePart = fromGstin.Substring(0, 2);
        string yearPart = invoice.InvoiceDate.ToString("yy");
        long seq = 1000000000L + (long)invoice.Id;
        string ackNo = $"{statePart}{yearPart}1{seq.ToString().Substring(seq.ToString().Length - 10)}";

        DateTime ackDate = DateTime.Now;

        // Build standard Signed QR Code Payload
        // Government Specification for B2B e-Invoice QR Code:
        // { SellerGSTIN, BuyerGSTIN, DocNo, DocTyp, DocDt, TotInvVal, ItemCnt, MainHsnCode, Irn, IrnDt }
        var mainHsn = invoice.Items.FirstOrDefault()?.HsnCode ?? "6204";
        mainHsn = Regex.Replace(mainHsn, @"[^\d]", "");

        var qrDataObj = new
        {
            SellerGstin = fromGstin,
            BuyerGstin = !string.IsNullOrWhiteSpace(invoice.CustomerGstin) ? invoice.CustomerGstin.Trim().ToUpper() : "URP",
            DocNo = invoice.InvoiceNumber,
            DocTyp = "INV",
            DocDt = invoice.InvoiceDate.ToString("dd/MM/yyyy"),
            TotInvVal = (double)invoice.GrandTotal,
            ItemCnt = invoice.Items.Count,
            MainHsnCode = mainHsn,
            Irn = irn,
            IrnDt = ackDate.ToString("yyyy-MM-dd HH:mm:ss")
        };

        string qrPayload = JsonSerializer.Serialize(qrDataObj);
        string qrSvg = GenerateQrCodeSvg(qrPayload, size: 130);

        // Update Invoice
        invoice.Irn = irn;
        invoice.EInvoiceAckNo = ackNo;
        invoice.EInvoiceAckDate = ackDate;
        invoice.EInvoiceSignedQrCode = qrPayload;
        invoice.EInvoiceStatus = "Generated";
        invoice.EInvoiceSupplyType = supplyType;
        invoice.EInvoiceErrors = null;
        invoice.EInvoiceCancelDate = null;
        invoice.EInvoiceCancelReason = null;
        invoice.EInvoiceCancelRemarks = null;

        // Optional simultaneous E-Way Bill generation
        if (request?.GenerateEwayBill == true)
        {
            if (!string.IsNullOrWhiteSpace(request.VehicleNumber)) invoice.VehicleNumber = request.VehicleNumber.Trim().ToUpper();
            if (!string.IsNullOrWhiteSpace(request.TransporterId)) invoice.TransporterId = request.TransporterId.Trim().ToUpper();
            if (!string.IsNullOrWhiteSpace(request.TransporterName)) invoice.TransporterName = request.TransporterName.Trim();
            if (request.DistanceKm.HasValue && request.DistanceKm.Value > 0) invoice.DistanceKm = request.DistanceKm.Value;

            if (string.IsNullOrWhiteSpace(invoice.EwayBillNumber))
            {
                // Generate 12-digit standard E-Way Bill Number
                long ewbSeq = 10000000L + (long)invoice.Id;
                invoice.EwayBillNumber = $"{statePart}{yearPart}{ewbSeq.ToString().Substring(ewbSeq.ToString().Length - 8)}";
                invoice.EwayBillDate = DateTime.Now;
            }

            result.EwayBillNumber = invoice.EwayBillNumber;
            result.EwayBillValidUntil = DateTime.Now.AddDays(Math.Max(1, (invoice.DistanceKm ?? 50) / 200 + 1));
        }

        await _context.SaveChangesAsync();

        result.Success = true;
        result.Message = $"E-Invoice generated successfully with IRN: {irn}";
        result.Irn = irn;
        result.AckNo = ackNo;
        result.AckDate = ackDate;
        result.SignedQrCode = qrPayload;
        result.QrCodeSvg = qrSvg;

        return result;
    }

    public async Task<EInvoiceResult> CancelEInvoiceAsync(int invoiceId, EInvoiceCancelRequest request)
    {
        var result = new EInvoiceResult();
        var invoice = await _context.TaxInvoices.FindAsync(invoiceId);

        if (invoice == null)
        {
            result.Success = false;
            result.Message = "Invoice not found.";
            return result;
        }

        if (invoice.EInvoiceStatus != "Generated")
        {
            result.Success = false;
            result.Message = $"Cannot cancel e-invoice with status '{invoice.EInvoiceStatus}'. Only Active/Generated e-invoices can be cancelled.";
            return result;
        }

        string reasonLabel = request.CancelReason switch
        {
            "1" => "1-Duplicate",
            "2" => "2-Data Entry Mistake",
            "3" => "3-Order Cancelled",
            "4" => "4-Other",
            _ => request.CancelReason
        };

        invoice.EInvoiceStatus = "Cancelled";
        invoice.EInvoiceCancelReason = reasonLabel;
        invoice.EInvoiceCancelRemarks = request.CancelRemarks;
        invoice.EInvoiceCancelDate = DateTime.Now;

        await _context.SaveChangesAsync();

        result.Success = true;
        result.Message = $"E-Invoice #{invoice.Irn} successfully cancelled.";
        return result;
    }

    public async Task<EInvoiceResult> RecordManualEInvoiceAsync(int invoiceId, EInvoiceManualRequest request)
    {
        var result = new EInvoiceResult();
        var invoice = await _context.TaxInvoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
        {
            result.Success = false;
            result.Message = "Invoice not found.";
            return result;
        }

        if (string.IsNullOrWhiteSpace(request.Irn) || request.Irn.Trim().Length != 64)
        {
            result.Success = false;
            result.Message = "IRN must be a valid 64-character hexadecimal string.";
            return result;
        }

        invoice.Irn = request.Irn.Trim().ToLower();
        invoice.EInvoiceAckNo = request.AckNo.Trim();
        invoice.EInvoiceAckDate = request.AckDate;
        invoice.EInvoiceStatus = "Generated";
        invoice.EInvoiceErrors = null;

        if (!string.IsNullOrWhiteSpace(request.SignedQrCode))
        {
            invoice.EInvoiceSignedQrCode = request.SignedQrCode.Trim();
        }
        else
        {
            // Build standard QR payload
            string fromGstin = _config["Company:GSTIN"] ?? "24AABCA1234F1Z9";
            var mainHsn = invoice.Items.FirstOrDefault()?.HsnCode ?? "6204";
            mainHsn = Regex.Replace(mainHsn, @"[^\d]", "");

            var qrDataObj = new
            {
                SellerGstin = fromGstin,
                BuyerGstin = !string.IsNullOrWhiteSpace(invoice.CustomerGstin) ? invoice.CustomerGstin.Trim().ToUpper() : "URP",
                DocNo = invoice.InvoiceNumber,
                DocTyp = "INV",
                DocDt = invoice.InvoiceDate.ToString("dd/MM/yyyy"),
                TotInvVal = (double)invoice.GrandTotal,
                ItemCnt = invoice.Items.Count,
                MainHsnCode = mainHsn,
                Irn = invoice.Irn,
                IrnDt = invoice.EInvoiceAckDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            invoice.EInvoiceSignedQrCode = JsonSerializer.Serialize(qrDataObj);
        }

        await _context.SaveChangesAsync();

        result.Success = true;
        result.Message = "Manual E-Invoice details recorded successfully.";
        result.Irn = invoice.Irn;
        result.AckNo = invoice.EInvoiceAckNo;
        result.AckDate = invoice.EInvoiceAckDate;
        result.SignedQrCode = invoice.EInvoiceSignedQrCode;
        result.QrCodeSvg = GenerateQrCodeSvg(invoice.EInvoiceSignedQrCode ?? invoice.Irn);

        return result;
    }

    public string CalculateIrn(string supplierGstin, string finYear, string docType, string docNum)
    {
        // Concatenate without separators as mandated by GSTN
        // Plaintext = SupplierGSTIN + FinYear + DocType + DocNum
        string plain = $"{supplierGstin.Trim().ToUpper()}{finYear.Trim()}{docType.Trim().ToUpper()}{docNum.Trim().ToUpper()}";
        
        using var sha256 = SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(plain));
        
        var sb = new StringBuilder();
        foreach (byte b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    public string GetFinancialYear(DateTime date)
    {
        // In India, Financial Year runs from April 1 to March 31
        int startYear = date.Month >= 4 ? date.Year : date.Year - 1;
        int endYear = (startYear + 1) % 100;
        return $"{startYear}-{endYear:D2}";
    }

    public string GenerateQrCodeSvg(string qrData, int size = 120)
    {
        return BarcodeService.GenerateQrCodeSvg(qrData, size);
    }
}
