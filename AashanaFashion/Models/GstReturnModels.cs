using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AashanaFashion.Models;

public enum GstValidationSeverity
{
    Warning,
    Error
}

public class GstValidationIssue
{
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public GstValidationSeverity Severity { get; set; } = GstValidationSeverity.Warning;
}

public class Gstr1B2bInvoiceRow
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerGstin { get; set; } = string.Empty;
    public string PlaceOfSupply { get; set; } = string.Empty;
    public string PosStateCode { get; set; } = "24";
    public bool IsInterState { get; set; }
    public decimal InvoiceValue { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal GstRate { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public string ReverseCharge { get; set; } = "N";
    public string InvoiceType { get; set; } = "Regular";
    public string EInvoiceStatus { get; set; } = "Not Generated";
    public string? Irn { get; set; }
}

public class Gstr1B2csSummaryRow
{
    public string SupplyType { get; set; } = "INTRA"; // INTRA or INTER
    public string PlaceOfSupply { get; set; } = "24";
    public string StateName { get; set; } = "Gujarat";
    public decimal GstRate { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal CessAmount { get; set; }
    public int InvoiceCount { get; set; }
}

public class Gstr1CreditNoteRow
{
    public int ReturnId { get; set; }
    public string NoteNumber { get; set; } = string.Empty;
    public DateTime NoteDate { get; set; }
    public string OriginalInvoiceNumber { get; set; } = string.Empty;
    public DateTime OriginalInvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerGstin { get; set; }
    public string NoteType { get; set; } = "C"; // C = Credit Note
    public decimal NoteValue { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal GstRate { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class Gstr1HsnSummaryRow
{
    public string HsnCode { get; set; } = "6204";
    public string Description { get; set; } = "Women Garments";
    public string Uqc { get; set; } = "PCS";
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TaxableValue { get; set; }
    public decimal GstRate { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
}

public class Gstr1DocSummaryRow
{
    public string DocumentType { get; set; } = "Invoices for outward supply";
    public string FromSerial { get; set; } = string.Empty;
    public string ToSerial { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int CancelledCount { get; set; }
    public int NetIssuedCount => Math.Max(0, TotalCount - CancelledCount);
}

public class Gstr1PeriodViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public string ReturnPeriodCode => $"{Month:D2}{Year}"; // e.g. "092026"
    public Company Company { get; set; } = new();

    // Summary Totals
    public decimal TotalTurnover { get; set; }
    public decimal TotalTaxableValue { get; set; }
    public decimal TotalCgst { get; set; }
    public decimal TotalSgst { get; set; }
    public decimal TotalIgst { get; set; }
    public decimal TotalTaxLiability => TotalCgst + TotalSgst + TotalIgst;
    public int TotalInvoiceCount { get; set; }
    public int B2bInvoiceCount { get; set; }
    public int B2cInvoiceCount { get; set; }

    // Table Data
    public List<Gstr1B2bInvoiceRow> B2bInvoices { get; set; } = new();
    public List<Gstr1B2bInvoiceRow> B2clInvoices { get; set; } = new();
    public List<Gstr1B2csSummaryRow> B2csSummaries { get; set; } = new();
    public List<Gstr1CreditNoteRow> CreditNotes { get; set; } = new();
    public List<Gstr1HsnSummaryRow> HsnSummaries { get; set; } = new();
    public List<Gstr1DocSummaryRow> DocumentSummaries { get; set; } = new();

    // Pre-filing Audit
    public List<GstValidationIssue> ValidationIssues { get; set; } = new();
}

public class Gstr3bSectionRow
{
    public string Description { get; set; } = string.Empty;
    public decimal TotalTaxableValue { get; set; }
    public decimal IntegratedTax { get; set; }
    public decimal CentralTax { get; set; }
    public decimal StateTax { get; set; }
    public decimal Cess { get; set; }
    public decimal TotalTax => IntegratedTax + CentralTax + StateTax + Cess;
}

public class Gstr3bViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public Company Company { get; set; } = new();

    // Table 3.1 - Details of Outward Supplies
    public Gstr3bSectionRow OutwardTaxableSupplies { get; set; } = new();
    public Gstr3bSectionRow OutwardZeroRated { get; set; } = new();
    public Gstr3bSectionRow OtherOutwardSuppliesNilExempt { get; set; } = new();
    public Gstr3bSectionRow InwardSuppliesReverseCharge { get; set; } = new();
    public Gstr3bSectionRow NonGstOutwardSupplies { get; set; } = new();

    // Table 4 - Eligible Input Tax Credit (ITC)
    public Gstr3bSectionRow ItcAllOtherPurchases { get; set; } = new();
    public Gstr3bSectionRow ItcReversed { get; set; } = new();
    public Gstr3bSectionRow NetItcAvailable { get; set; } = new();

    // Payment / Net Tax Liability
    public decimal NetPayableIgst => Math.Max(0, OutwardTaxableSupplies.IntegratedTax - NetItcAvailable.IntegratedTax);
    public decimal NetPayableCgst => Math.Max(0, OutwardTaxableSupplies.CentralTax - NetItcAvailable.CentralTax);
    public decimal NetPayableSgst => Math.Max(0, OutwardTaxableSupplies.StateTax - NetItcAvailable.StateTax);
    public decimal TotalCashPayable => NetPayableIgst + NetPayableCgst + NetPayableSgst;
}

public class Gstr2bReconcileItem
{
    public string Source { get; set; } = string.Empty; // "Both", "ERP Only", "GSTR-2B Only"
    public string SupplierGstin { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime? InvoiceDate { get; set; }
    public decimal ErpTaxableValue { get; set; }
    public decimal ErpTaxAmount { get; set; }
    public decimal Gstr2bTaxableValue { get; set; }
    public decimal Gstr2bTaxAmount { get; set; }
    public decimal Difference => (ErpTaxableValue + ErpTaxAmount) - (Gstr2bTaxableValue + Gstr2bTaxAmount);
    public string MatchStatus { get; set; } = "Matched"; // "Matched", "Missing in GSTR-2B", "Missing in ERP", "Value Mismatch"
    public string ActionRequired { get; set; } = string.Empty;
}

public class Gstr2bReconciliationViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public bool HasUploadedFile { get; set; }
    public string? FileName { get; set; }

    public int TotalErpBillsCount { get; set; }
    public int TotalGstr2bBillsCount { get; set; }
    public int MatchedCount { get; set; }
    public int MissingIn2bCount { get; set; }
    public int MissingInErpCount { get; set; }
    public int MismatchedCount { get; set; }

    public decimal TotalErpItc { get; set; }
    public decimal TotalGstr2bItc { get; set; }
    public decimal SafeEligibleItc { get; set; }
    public decimal AtRiskItc { get; set; }

    public List<Gstr2bReconcileItem> Items { get; set; } = new();
}

// ——— GSTN Portal Official GSTR-1 JSON Schema DTOs ———
public class Gstr1JsonRoot
{
    [JsonPropertyName("gstin")]
    public string Gstin { get; set; } = string.Empty;

    [JsonPropertyName("fp")]
    public string Fp { get; set; } = string.Empty; // MMYYYY

    [JsonPropertyName("gt")]
    public decimal Gt { get; set; } = 0.0m;

    [JsonPropertyName("cur_gt")]
    public decimal CurGt { get; set; } = 0.0m;

    [JsonPropertyName("b2b")]
    public List<Gstr1JsonB2bGroup> B2b { get; set; } = new();

    [JsonPropertyName("b2cl")]
    public List<Gstr1JsonB2clGroup> B2cl { get; set; } = new();

    [JsonPropertyName("b2cs")]
    public List<Gstr1JsonB2csItem> B2cs { get; set; } = new();

    [JsonPropertyName("cdnr")]
    public List<Gstr1JsonCdnrGroup> Cdnr { get; set; } = new();

    [JsonPropertyName("hsn")]
    public Gstr1JsonHsnRoot Hsn { get; set; } = new();

    [JsonPropertyName("doc_issue")]
    public Gstr1JsonDocRoot DocIssue { get; set; } = new();
}

public class Gstr1JsonB2bGroup
{
    [JsonPropertyName("ctin")]
    public string Ctin { get; set; } = string.Empty;

    [JsonPropertyName("inv")]
    public List<Gstr1JsonInvoice> Inv { get; set; } = new();
}

public class Gstr1JsonB2clGroup
{
    [JsonPropertyName("pos")]
    public string Pos { get; set; } = string.Empty;

    [JsonPropertyName("inv")]
    public List<Gstr1JsonInvoice> Inv { get; set; } = new();
}

public class Gstr1JsonInvoice
{
    [JsonPropertyName("inum")]
    public string Inum { get; set; } = string.Empty;

    [JsonPropertyName("idt")]
    public string Idt { get; set; } = string.Empty; // DD-MM-YYYY

    [JsonPropertyName("val")]
    public decimal Val { get; set; }

    [JsonPropertyName("pos")]
    public string Pos { get; set; } = string.Empty;

    [JsonPropertyName("rchrg")]
    public string Rchrg { get; set; } = "N";

    [JsonPropertyName("inv_typ")]
    public string InvTyp { get; set; } = "R";

    [JsonPropertyName("itms")]
    public List<Gstr1JsonItemContainer> Itms { get; set; } = new();
}

public class Gstr1JsonItemContainer
{
    [JsonPropertyName("num")]
    public int Num { get; set; }

    [JsonPropertyName("itm_det")]
    public Gstr1JsonItemDetail ItmDet { get; set; } = new();
}

public class Gstr1JsonItemDetail
{
    [JsonPropertyName("txval")]
    public decimal Txval { get; set; }

    [JsonPropertyName("rt")]
    public decimal Rt { get; set; }

    [JsonPropertyName("iamt")]
    public decimal Iamt { get; set; }

    [JsonPropertyName("camt")]
    public decimal Camt { get; set; }

    [JsonPropertyName("samt")]
    public decimal Samt { get; set; }

    [JsonPropertyName("csamt")]
    public decimal Csamt { get; set; } = 0m;
}

public class Gstr1JsonB2csItem
{
    [JsonPropertyName("sply_ty")]
    public string SplyTy { get; set; } = "INTRA";

    [JsonPropertyName("pos")]
    public string Pos { get; set; } = string.Empty;

    [JsonPropertyName("rt")]
    public decimal Rt { get; set; }

    [JsonPropertyName("typ")]
    public string Typ { get; set; } = "OE";

    [JsonPropertyName("txval")]
    public decimal Txval { get; set; }

    [JsonPropertyName("iamt")]
    public decimal Iamt { get; set; }

    [JsonPropertyName("camt")]
    public decimal Camt { get; set; }

    [JsonPropertyName("samt")]
    public decimal Samt { get; set; }

    [JsonPropertyName("csamt")]
    public decimal Csamt { get; set; } = 0m;
}

public class Gstr1JsonCdnrGroup
{
    [JsonPropertyName("ctin")]
    public string Ctin { get; set; } = string.Empty;

    [JsonPropertyName("nt")]
    public List<Gstr1JsonCreditNote> Nt { get; set; } = new();
}

public class Gstr1JsonCreditNote
{
    [JsonPropertyName("nt_num")]
    public string NtNum { get; set; } = string.Empty;

    [JsonPropertyName("nt_dt")]
    public string NtDt { get; set; } = string.Empty;

    [JsonPropertyName("ntty")]
    public string Ntty { get; set; } = "C";

    [JsonPropertyName("inum")]
    public string Inum { get; set; } = string.Empty;

    [JsonPropertyName("idt")]
    public string Idt { get; set; } = string.Empty;

    [JsonPropertyName("val")]
    public decimal Val { get; set; }

    [JsonPropertyName("pos")]
    public string Pos { get; set; } = string.Empty;

    [JsonPropertyName("rchrg")]
    public string Rchrg { get; set; } = "N";

    [JsonPropertyName("itms")]
    public List<Gstr1JsonItemContainer> Itms { get; set; } = new();
}

public class Gstr1JsonHsnRoot
{
    [JsonPropertyName("data")]
    public List<Gstr1JsonHsnItem> Data { get; set; } = new();
}

public class Gstr1JsonHsnItem
{
    [JsonPropertyName("num")]
    public int Num { get; set; }

    [JsonPropertyName("hsn_sc")]
    public string HsnSc { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Desc { get; set; } = string.Empty;

    [JsonPropertyName("uqc")]
    public string Uqc { get; set; } = "PCS";

    [JsonPropertyName("qty")]
    public decimal Qty { get; set; }

    [JsonPropertyName("val")]
    public decimal Val { get; set; }

    [JsonPropertyName("txval")]
    public decimal Txval { get; set; }

    [JsonPropertyName("iamt")]
    public decimal Iamt { get; set; }

    [JsonPropertyName("camt")]
    public decimal Camt { get; set; }

    [JsonPropertyName("samt")]
    public decimal Samt { get; set; }

    [JsonPropertyName("csamt")]
    public decimal Csamt { get; set; } = 0m;
}

public class Gstr1JsonDocRoot
{
    [JsonPropertyName("doc_det")]
    public List<Gstr1JsonDocCategory> DocDet { get; set; } = new();
}

public class Gstr1JsonDocCategory
{
    [JsonPropertyName("doc_num")]
    public int DocNum { get; set; } = 1; // 1 for Outward Invoices

    [JsonPropertyName("docs")]
    public List<Gstr1JsonDocRange> Docs { get; set; } = new();
}

public class Gstr1JsonDocRange
{
    [JsonPropertyName("num")]
    public int Num { get; set; } = 1;

    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("totnum")]
    public int Totnum { get; set; }

    [JsonPropertyName("canc")]
    public int Canc { get; set; }

    [JsonPropertyName("net_issue")]
    public int NetIssue { get; set; }
}
