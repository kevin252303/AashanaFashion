using System;
using System.Collections.Generic;

namespace AashanaFashion.Models;

public class EInvoiceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Irn { get; set; }
    public string? AckNo { get; set; }
    public DateTime? AckDate { get; set; }
    public string? SignedQrCode { get; set; }
    public string? QrCodeSvg { get; set; }
    public string? EwayBillNumber { get; set; }
    public DateTime? EwayBillValidUntil { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class EInvoiceGenerateRequest
{
    public string SupplyType { get; set; } = "B2B"; // B2B, SEZWP, SEZWOP, EXPWP, EXPWOP, DEXP
    public bool GenerateEwayBill { get; set; } = false;
    public string? TransporterId { get; set; }
    public string? TransporterName { get; set; }
    public string? VehicleNumber { get; set; }
    public int? DistanceKm { get; set; }
}

public class EInvoiceCancelRequest
{
    public string CancelReason { get; set; } = "1"; // 1=Duplicate, 2=Data Entry Mistake, 3=Order Cancelled, 4=Other
    public string CancelRemarks { get; set; } = string.Empty;
}

public class EInvoiceManualRequest
{
    public string Irn { get; set; } = string.Empty;
    public string AckNo { get; set; } = string.Empty;
    public DateTime AckDate { get; set; } = DateTime.Now;
    public string? SignedQrCode { get; set; }
}
