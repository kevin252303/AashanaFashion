using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AashanaFashion.Data;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public class GstReturnService : IGstReturnService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public GstReturnService(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<Gstr1PeriodViewModel> GetGstr1DataAsync(int companyId, int year, int month)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
        if (company == null)
        {
            company = new Company
            {
                Id = companyId,
                CompanyName = _config["Company:Name"] ?? "Aashana Fashion",
                Gstin = _config["Company:GSTIN"] ?? "24AABCA1234F1Z9",
                State = _config["Company:State"] ?? "Gujarat",
                StateCode = int.TryParse(_config["Company:StateCode"], out var sc) ? sc : 24
            };
        }

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        var invoices = await _context.TaxInvoices
            .Where(i => i.CompanyId == companyId && i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
            .Include(i => i.Items)
            .Include(i => i.Customer)
            .Include(i => i.Returns)
            .OrderBy(i => i.InvoiceNumber)
            .ToListAsync();

        var salesReturns = await _context.SalesReturns
            .Where(r => r.CompanyId == companyId && r.ReturnDate >= startDate && r.ReturnDate <= endDate)
            .Include(r => r.TaxInvoice)
            .Include(r => r.Customer)
            .OrderBy(r => r.ReturnNumber)
            .ToListAsync();

        var model = new Gstr1PeriodViewModel
        {
            Year = year,
            Month = month,
            MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
            Company = company,
            TotalInvoiceCount = invoices.Count
        };

        var companyStateCode = company.StateCode.ToString("D2");

        // Process invoices
        foreach (var inv in invoices)
        {
            if (inv.PaymentStatus == InvoicePaymentStatus.Cancelled)
                continue; // Handled in document summary

            model.TotalTurnover += inv.GrandTotal;
            model.TotalTaxableValue += inv.TaxableAmount;
            model.TotalCgst += inv.CgstAmount;
            model.TotalSgst += inv.SgstAmount;
            model.TotalIgst += inv.IgstAmount;

            var rawGstin = (inv.CustomerGstin ?? inv.Customer?.GstNumber ?? "").Trim().ToUpper();
            bool isB2b = !string.IsNullOrWhiteSpace(rawGstin) && rawGstin.Length == 15 && rawGstin != "URP";

            string posCode = ExtractStateCode(inv.PlaceOfSupply, rawGstin, companyStateCode);

            // Audit validations
            ValidateInvoice(inv, rawGstin, companyStateCode, posCode, model.ValidationIssues);

            if (isB2b)
            {
                model.B2bInvoiceCount++;
                model.B2bInvoices.Add(new Gstr1B2bInvoiceRow
                {
                    InvoiceId = inv.Id,
                    InvoiceNumber = inv.InvoiceNumber,
                    InvoiceDate = inv.InvoiceDate,
                    CustomerName = inv.CustomerName,
                    CustomerGstin = rawGstin,
                    PlaceOfSupply = inv.PlaceOfSupply,
                    PosStateCode = posCode,
                    IsInterState = inv.IsInterState,
                    InvoiceValue = inv.GrandTotal,
                    TaxableValue = inv.TaxableAmount,
                    GstRate = inv.IsInterState ? inv.IgstRate : (inv.CgstRate + inv.SgstRate),
                    CgstAmount = inv.CgstAmount,
                    SgstAmount = inv.SgstAmount,
                    IgstAmount = inv.IgstAmount,
                    ReverseCharge = "N",
                    InvoiceType = "Regular",
                    EInvoiceStatus = inv.EInvoiceStatus,
                    Irn = inv.Irn
                });
            }
            else
            {
                model.B2cInvoiceCount++;
                // B2C Large: Inter-state invoice with GrandTotal > 2,50,000
                if (inv.IsInterState && inv.GrandTotal > 250000m)
                {
                    model.B2clInvoices.Add(new Gstr1B2bInvoiceRow
                    {
                        InvoiceId = inv.Id,
                        InvoiceNumber = inv.InvoiceNumber,
                        InvoiceDate = inv.InvoiceDate,
                        CustomerName = inv.CustomerName,
                        CustomerGstin = "URP",
                        PlaceOfSupply = inv.PlaceOfSupply,
                        PosStateCode = posCode,
                        IsInterState = true,
                        InvoiceValue = inv.GrandTotal,
                        TaxableValue = inv.TaxableAmount,
                        GstRate = inv.IgstRate,
                        CgstAmount = 0,
                        SgstAmount = 0,
                        IgstAmount = inv.IgstAmount,
                        InvoiceType = "B2CL"
                    });
                }
                else
                {
                    // B2C Small: Grouped later
                }
            }
        }

        // B2C Small Summaries
        var b2csInvoices = invoices
            .Where(i => i.PaymentStatus != InvoicePaymentStatus.Cancelled &&
                        (string.IsNullOrWhiteSpace(i.CustomerGstin) || i.CustomerGstin.Trim().Length != 15 || i.CustomerGstin == "URP") &&
                        (!i.IsInterState || i.GrandTotal <= 250000m))
            .ToList();

        var b2csGroups = b2csInvoices
            .GroupBy(i => new
            {
                Pos = ExtractStateCode(i.PlaceOfSupply, "", companyStateCode),
                SupplyType = i.IsInterState ? "INTER" : "INTRA",
                Rate = i.IsInterState ? i.IgstRate : (i.CgstRate + i.SgstRate)
            });

        foreach (var grp in b2csGroups)
        {
            model.B2csSummaries.Add(new Gstr1B2csSummaryRow
            {
                PlaceOfSupply = grp.Key.Pos,
                SupplyType = grp.Key.SupplyType,
                GstRate = grp.Key.Rate,
                TaxableValue = grp.Sum(x => x.TaxableAmount),
                CgstAmount = grp.Sum(x => x.CgstAmount),
                SgstAmount = grp.Sum(x => x.SgstAmount),
                IgstAmount = grp.Sum(x => x.IgstAmount),
                InvoiceCount = grp.Count(),
                StateName = GetStateNameFromCode(grp.Key.Pos)
            });
        }

        // Credit Notes from Sales Returns
        foreach (var ret in salesReturns)
        {
            var origGstin = (ret.Customer?.GstNumber ?? ret.TaxInvoice?.CustomerGstin ?? "").Trim().ToUpper();
            decimal taxRate = 5.0m;
            if (ret.TaxInvoice != null)
            {
                taxRate = ret.TaxInvoice.IsInterState ? ret.TaxInvoice.IgstRate : (ret.TaxInvoice.CgstRate + ret.TaxInvoice.SgstRate);
            }

            model.CreditNotes.Add(new Gstr1CreditNoteRow
            {
                ReturnId = ret.Id,
                NoteNumber = ret.ReturnNumber,
                NoteDate = ret.ReturnDate,
                OriginalInvoiceNumber = ret.TaxInvoice?.InvoiceNumber ?? "N/A",
                OriginalInvoiceDate = ret.TaxInvoice?.InvoiceDate ?? ret.ReturnDate,
                CustomerName = ret.CustomerName,
                CustomerGstin = origGstin,
                NoteType = "C",
                NoteValue = ret.GrandTotal,
                TaxableValue = ret.SubTotal,
                GstRate = taxRate,
                CgstAmount = (ret.TaxInvoice?.IsInterState == true) ? 0 : (ret.TaxAmount / 2m),
                SgstAmount = (ret.TaxInvoice?.IsInterState == true) ? 0 : (ret.TaxAmount / 2m),
                IgstAmount = (ret.TaxInvoice?.IsInterState == true) ? ret.TaxAmount : 0,
                Reason = ret.Reason.ToString()
            });
        }

        // HSN Summary (Table 12)
        var allItems = invoices
            .Where(i => i.PaymentStatus != InvoicePaymentStatus.Cancelled)
            .SelectMany(i => i.Items.Select(item => new { Item = item, Invoice = i }))
            .ToList();

        var hsnGroups = allItems
            .GroupBy(x => new
            {
                Hsn = !string.IsNullOrWhiteSpace(x.Item.HsnCode) ? x.Item.HsnCode.Trim() : "6204",
                Rate = x.Item.GstRate
            });

        foreach (var grp in hsnGroups)
        {
            var hsnCode = grp.Key.Hsn;
            decimal totalTaxable = grp.Sum(x => x.Item.TaxableValue);
            decimal totalQty = grp.Sum(x => x.Item.Quantity);
            decimal totalVal = grp.Sum(x => x.Item.TotalAmount);

            decimal igst = grp.Where(x => x.Invoice.IsInterState).Sum(x => (x.Item.TaxableValue * grp.Key.Rate) / 100m);
            decimal cgst = grp.Where(x => !x.Invoice.IsInterState).Sum(x => (x.Item.TaxableValue * (grp.Key.Rate / 2m)) / 100m);
            decimal sgst = cgst;

            model.HsnSummaries.Add(new Gstr1HsnSummaryRow
            {
                HsnCode = hsnCode,
                Description = GetHsnDescription(hsnCode),
                Uqc = "PCS",
                TotalQuantity = totalQty,
                TotalValue = totalVal,
                TaxableValue = totalTaxable,
                GstRate = grp.Key.Rate,
                IgstAmount = Math.Round(igst, 2),
                CgstAmount = Math.Round(cgst, 2),
                SgstAmount = Math.Round(sgst, 2)
            });
        }

        // Document Issued Summary (Table 13)
        if (invoices.Any())
        {
            var ordered = invoices.OrderBy(i => i.InvoiceNumber).ToList();
            int cancelled = invoices.Count(i => i.PaymentStatus == InvoicePaymentStatus.Cancelled);
            model.DocumentSummaries.Add(new Gstr1DocSummaryRow
            {
                DocumentType = "Invoices for outward supply",
                FromSerial = ordered.First().InvoiceNumber,
                ToSerial = ordered.Last().InvoiceNumber,
                TotalCount = ordered.Count,
                CancelledCount = cancelled
            });
        }

        if (salesReturns.Any())
        {
            var ordered = salesReturns.OrderBy(r => r.ReturnNumber).ToList();
            model.DocumentSummaries.Add(new Gstr1DocSummaryRow
            {
                DocumentType = "Credit Notes",
                FromSerial = ordered.First().ReturnNumber,
                ToSerial = ordered.Last().ReturnNumber,
                TotalCount = ordered.Count,
                CancelledCount = 0
            });
        }

        return model;
    }

    public async Task<byte[]> GenerateGstr1JsonAsync(int companyId, int year, int month)
    {
        var data = await GetGstr1DataAsync(companyId, year, month);

        var payload = new Gstr1JsonRoot
        {
            Gstin = data.Company.Gstin ?? "24AABCA1234F1Z9",
            Fp = data.ReturnPeriodCode,
            Gt = 0.0m,
            CurGt = 0.0m
        };

        // B2B Grouping
        var b2bByParty = data.B2bInvoices.GroupBy(i => i.CustomerGstin);
        foreach (var party in b2bByParty)
        {
            var group = new Gstr1JsonB2bGroup
            {
                Ctin = party.Key
            };

            foreach (var inv in party)
            {
                var jsonInv = new Gstr1JsonInvoice
                {
                    Inum = inv.InvoiceNumber,
                    Idt = inv.InvoiceDate.ToString("dd-MM-yyyy"),
                    Val = inv.InvoiceValue,
                    Pos = inv.PosStateCode,
                    Rchrg = "N",
                    InvTyp = "R"
                };

                jsonInv.Itms.Add(new Gstr1JsonItemContainer
                {
                    Num = 1,
                    ItmDet = new Gstr1JsonItemDetail
                    {
                        Txval = inv.TaxableValue,
                        Rt = inv.GstRate,
                        Iamt = inv.IgstAmount,
                        Camt = inv.CgstAmount,
                        Samt = inv.SgstAmount,
                        Csamt = 0m
                    }
                });

                group.Inv.Add(jsonInv);
            }
            payload.B2b.Add(group);
        }

        // B2CL Grouping
        var b2clByPos = data.B2clInvoices.GroupBy(i => i.PosStateCode);
        foreach (var posGrp in b2clByPos)
        {
            var group = new Gstr1JsonB2clGroup
            {
                Pos = posGrp.Key
            };
            foreach (var inv in posGrp)
            {
                var jsonInv = new Gstr1JsonInvoice
                {
                    Inum = inv.InvoiceNumber,
                    Idt = inv.InvoiceDate.ToString("dd-MM-yyyy"),
                    Val = inv.InvoiceValue,
                    Pos = inv.PosStateCode
                };
                jsonInv.Itms.Add(new Gstr1JsonItemContainer
                {
                    Num = 1,
                    ItmDet = new Gstr1JsonItemDetail
                    {
                        Txval = inv.TaxableValue,
                        Rt = inv.GstRate,
                        Iamt = inv.IgstAmount,
                        Camt = 0,
                        Samt = 0,
                        Csamt = 0
                    }
                });
                group.Inv.Add(jsonInv);
            }
            payload.B2cl.Add(group);
        }

        // B2CS Items
        foreach (var b2cs in data.B2csSummaries)
        {
            payload.B2cs.Add(new Gstr1JsonB2csItem
            {
                SplyTy = b2cs.SupplyType,
                Pos = b2cs.PlaceOfSupply,
                Rt = b2cs.GstRate,
                Typ = "OE",
                Txval = b2cs.TaxableValue,
                Iamt = b2cs.IgstAmount,
                Camt = b2cs.CgstAmount,
                Samt = b2cs.SgstAmount,
                Csamt = 0m
            });
        }

        // CDNR Grouping
        var cdnrByParty = data.CreditNotes.Where(c => !string.IsNullOrWhiteSpace(c.CustomerGstin) && c.CustomerGstin.Length == 15).GroupBy(c => c.CustomerGstin!);
        foreach (var party in cdnrByParty)
        {
            var group = new Gstr1JsonCdnrGroup
            {
                Ctin = party.Key
            };
            foreach (var note in party)
            {
                var jsonNote = new Gstr1JsonCreditNote
                {
                    NtNum = note.NoteNumber,
                    NtDt = note.NoteDate.ToString("dd-MM-yyyy"),
                    Ntty = "C",
                    Inum = note.OriginalInvoiceNumber,
                    Idt = note.OriginalInvoiceDate.ToString("dd-MM-yyyy"),
                    Val = note.NoteValue,
                    Pos = party.Key.Substring(0, 2),
                    Rchrg = "N"
                };
                jsonNote.Itms.Add(new Gstr1JsonItemContainer
                {
                    Num = 1,
                    ItmDet = new Gstr1JsonItemDetail
                    {
                        Txval = note.TaxableValue,
                        Rt = note.GstRate,
                        Iamt = note.IgstAmount,
                        Camt = note.CgstAmount,
                        Samt = note.SgstAmount,
                        Csamt = 0
                    }
                });
                group.Nt.Add(jsonNote);
            }
            payload.Cdnr.Add(group);
        }

        // HSN Summary
        int hsnIndex = 1;
        foreach (var hsn in data.HsnSummaries)
        {
            payload.Hsn.Data.Add(new Gstr1JsonHsnItem
            {
                Num = hsnIndex++,
                HsnSc = hsn.HsnCode,
                Desc = hsn.Description,
                Uqc = hsn.Uqc,
                Qty = hsn.TotalQuantity,
                Val = hsn.TotalValue,
                Txval = hsn.TaxableValue,
                Iamt = hsn.IgstAmount,
                Camt = hsn.CgstAmount,
                Samt = hsn.SgstAmount,
                Csamt = 0
            });
        }

        // Doc Issues
        if (data.DocumentSummaries.Any())
        {
            var cat = new Gstr1JsonDocCategory
            {
                DocNum = 1
            };
            int docIdx = 1;
            foreach (var doc in data.DocumentSummaries)
            {
                cat.Docs.Add(new Gstr1JsonDocRange
                {
                    Num = docIdx++,
                    From = doc.FromSerial,
                    To = doc.ToSerial,
                    Totnum = doc.TotalCount,
                    Canc = doc.CancelledCount,
                    NetIssue = doc.NetIssuedCount
                });
            }
            payload.DocIssue.DocDet.Add(cat);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var jsonString = JsonSerializer.Serialize(payload, options);
        return Encoding.UTF8.GetBytes(jsonString);
    }

    public async Task<byte[]> GenerateGstr1CsvAsync(int companyId, int year, int month, string section)
    {
        var data = await GetGstr1DataAsync(companyId, year, month);
        var sb = new StringBuilder();

        if (string.Equals(section, "b2b", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("GSTIN/UIN of Recipient,Receiver Name,Invoice Number,Invoice date,Invoice Value,Place Of Supply,Reverse Charge,Applicable % of Tax Rate,Invoice Type,E-Commerce GSTIN,Rate,Taxable Value,Cess Amount");
            foreach (var inv in data.B2bInvoices)
            {
                sb.AppendLine($"\"{inv.CustomerGstin}\",\"{inv.CustomerName}\",\"{inv.InvoiceNumber}\",\"{inv.InvoiceDate:dd-MMM-yyyy}\",{inv.InvoiceValue:F2},\"{inv.PosStateCode}-{inv.PlaceOfSupply}\",\"N\",,{inv.InvoiceType},,{inv.GstRate:F2},{inv.TaxableValue:F2},0.00");
            }
        }
        else if (string.Equals(section, "hsn", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("HSN,Description,UQC,Total Quantity,Total Value,Taxable Value,Integrated Tax Amount,Central Tax Amount,State/UT Tax Amount,Cess Amount");
            foreach (var h in data.HsnSummaries)
            {
                sb.AppendLine($"\"{h.HsnCode}\",\"{h.Description}\",\"{h.Uqc}\",{h.TotalQuantity:F2},{h.TotalValue:F2},{h.TaxableValue:F2},{h.IgstAmount:F2},{h.CgstAmount:F2},{h.SgstAmount:F2},0.00");
            }
        }
        else // b2cs
        {
            sb.AppendLine("Type,Place Of Supply,Applicable % of Tax Rate,Rate,Taxable Value,Cess Amount,E-Commerce GSTIN");
            foreach (var b in data.B2csSummaries)
            {
                sb.AppendLine($"\"{b.SupplyType}\",\"{b.PlaceOfSupply}-{b.StateName}\",,{b.GstRate:F2},{b.TaxableValue:F2},0.00,");
            }
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<Gstr3bViewModel> GetGstr3bDataAsync(int companyId, int year, int month)
    {
        var gstr1 = await GetGstr1DataAsync(companyId, year, month);
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        // Fetch Purchases for Input Tax Credit (ITC)
        var purchases = await _context.PurchaseOrders
            .Where(p => p.CompanyId == companyId && p.OrderDate >= startDate && p.OrderDate <= endDate && p.Status != PurchaseOrderStatus.Cancelled)
            .Include(p => p.Details)
            .Include(p => p.Vendor)
            .ToListAsync();

        var model = new Gstr3bViewModel
        {
            Year = year,
            Month = month,
            MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
            Company = gstr1.Company
        };

        // Table 3.1 (a) Outward Taxable Supplies
        model.OutwardTaxableSupplies = new Gstr3bSectionRow
        {
            Description = "(a) Outward taxable supplies (other than zero rated, nil rated and exempted)",
            TotalTaxableValue = gstr1.TotalTaxableValue,
            IntegratedTax = gstr1.TotalIgst,
            CentralTax = gstr1.TotalCgst,
            StateTax = gstr1.TotalSgst,
            Cess = 0m
        };

        // Table 4 (A)(5) All Other ITC (From Raw Materials & Fabric Purchases)
        decimal totalPurchaseTaxable = 0m;
        decimal purchaseIgst = 0m;
        decimal purchaseCgst = 0m;
        decimal purchaseSgst = 0m;

        foreach (var po in purchases)
        {
            string vendorState = (po.Vendor?.State ?? "Gujarat").Trim();
            bool isInterState = !vendorState.Equals("Gujarat", StringComparison.OrdinalIgnoreCase);

            foreach (var item in po.Details)
            {
                decimal taxable = item.TotalPrice - item.DiscountAmount;
                totalPurchaseTaxable += taxable;

                if (isInterState)
                {
                    purchaseIgst += item.GstAmount;
                }
                else
                {
                    purchaseCgst += (item.GstAmount / 2m);
                    purchaseSgst += (item.GstAmount / 2m);
                }
            }
        }

        model.ItcAllOtherPurchases = new Gstr3bSectionRow
        {
            Description = "(5) All other ITC (Domestic purchases of fabrics, trims & services)",
            TotalTaxableValue = totalPurchaseTaxable,
            IntegratedTax = Math.Round(purchaseIgst, 2),
            CentralTax = Math.Round(purchaseCgst, 2),
            StateTax = Math.Round(purchaseSgst, 2),
            Cess = 0m
        };

        model.NetItcAvailable = new Gstr3bSectionRow
        {
            Description = "(C) Net ITC Available (A - B)",
            TotalTaxableValue = totalPurchaseTaxable,
            IntegratedTax = model.ItcAllOtherPurchases.IntegratedTax,
            CentralTax = model.ItcAllOtherPurchases.CentralTax,
            StateTax = model.ItcAllOtherPurchases.StateTax,
            Cess = 0m
        };

        return model;
    }

    public async Task<Gstr2bReconciliationViewModel> ReconcileGstr2bAsync(int companyId, int year, int month, Stream gstr2bJsonStream, string fileName)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        var model = new Gstr2bReconciliationViewModel
        {
            Year = year,
            Month = month,
            MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
            HasUploadedFile = true,
            FileName = fileName
        };

        // Fetch ERP Purchase Orders / Bills for that period
        var erpPurchases = await _context.PurchaseOrders
            .Where(p => p.CompanyId == companyId && p.OrderDate >= startDate && p.OrderDate <= endDate && p.Status != PurchaseOrderStatus.Cancelled)
            .Include(p => p.Details)
            .Include(p => p.Vendor)
            .Include(p => p.Bills)
            .ToListAsync();

        var erpBillMap = new Dictionary<string, (PurchaseOrder Po, decimal Taxable, decimal Tax)>();

        foreach (var po in erpPurchases)
        {
            string vendorGstin = (po.Vendor?.GstNumber ?? "").Trim().ToUpper();
            string billNo = (!string.IsNullOrWhiteSpace(po.InvoiceNumber) ? po.InvoiceNumber : (po.Bills.FirstOrDefault()?.BillNumber ?? po.PoNumber)).Trim().ToUpper();

            decimal taxable = po.Details.Sum(d => d.TotalPrice - d.DiscountAmount);
            decimal tax = po.Details.Sum(d => d.GstAmount);

            model.TotalErpBillsCount++;
            model.TotalErpItc += tax;

            string key = $"{vendorGstin}_{billNo}";
            erpBillMap[key] = (po, taxable, tax);
        }

        // Parse GSTR-2B JSON
        var gstr2bBills = new List<(string Gstin, string InvNum, DateTime? Date, decimal Taxable, decimal Tax)>();
        try
        {
            using var reader = new StreamReader(gstr2bJsonStream);
            string jsonContent = await reader.ReadToEndAsync();
            using var doc = JsonDocument.Parse(jsonContent);

            var root = doc.RootElement;
            // Handle standard GSTN GSTR-2B or custom sandbox output
            if (root.TryGetProperty("data", out var dataElem))
                root = dataElem;

            if (root.TryGetProperty("b2b", out var b2bElem) && b2bElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var party in b2bElem.EnumerateArray())
                {
                    string ctin = party.TryGetProperty("ctin", out var ctinElem) ? ctinElem.GetString() ?? "" : "";
                    if (party.TryGetProperty("inv", out var invList) && invList.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var inv in invList.EnumerateArray())
                        {
                            string inum = inv.TryGetProperty("inum", out var inumElem) ? inumElem.GetString() ?? "" : "";
                            DateTime? idt = null;
                            if (inv.TryGetProperty("idt", out var idtElem) && DateTime.TryParse(idtElem.GetString(), out var d))
                            {
                                idt = d;
                            }

                            decimal txval = 0;
                            decimal tax = 0;
                            if (inv.TryGetProperty("items", out var itmList) && itmList.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var itm in itmList.EnumerateArray())
                                {
                                    if (itm.TryGetProperty("txval", out var tv)) txval += tv.GetDecimal();
                                    if (itm.TryGetProperty("iamt", out var ia)) tax += ia.GetDecimal();
                                    if (itm.TryGetProperty("camt", out var ca)) tax += ca.GetDecimal();
                                    if (itm.TryGetProperty("samt", out var sa)) tax += sa.GetDecimal();
                                }
                            }
                            gstr2bBills.Add((ctin.Trim().ToUpper(), inum.Trim().ToUpper(), idt, txval, tax));
                        }
                    }
                }
            }
        }
        catch
        {
            // Fallback: If uploaded file was corrupt or different structure, we still reconcile against ERP
        }

        model.TotalGstr2bBillsCount = gstr2bBills.Count;
        model.TotalGstr2bItc = gstr2bBills.Sum(x => x.Tax);

        var matchedErpKeys = new HashSet<string>();

        // Reconcile 2B bills
        foreach (var b in gstr2bBills)
        {
            string key = $"{b.Gstin}_{b.InvNum}";
            if (erpBillMap.TryGetValue(key, out var erp))
            {
                matchedErpKeys.Add(key);
                decimal diff = Math.Abs((erp.Taxable + erp.Tax) - (b.Taxable + b.Tax));
                bool isExactMatch = diff <= 2.0m; // allow rounding variance up to Rs. 2

                if (isExactMatch)
                {
                    model.MatchedCount++;
                    model.SafeEligibleItc += b.Tax;
                    model.Items.Add(new Gstr2bReconcileItem
                    {
                        Source = "Both",
                        SupplierGstin = b.Gstin,
                        SupplierName = erp.Po.Vendor?.VendorName ?? "Supplier",
                        InvoiceNumber = b.InvNum,
                        InvoiceDate = b.Date ?? erp.Po.OrderDate,
                        ErpTaxableValue = erp.Taxable,
                        ErpTaxAmount = erp.Tax,
                        Gstr2bTaxableValue = b.Taxable,
                        Gstr2bTaxAmount = b.Tax,
                        MatchStatus = "Matched",
                        ActionRequired = "Ready to Claim ITC"
                    });
                }
                else
                {
                    model.MismatchedCount++;
                    model.AtRiskItc += Math.Abs(erp.Tax - b.Tax);
                    model.Items.Add(new Gstr2bReconcileItem
                    {
                        Source = "Both",
                        SupplierGstin = b.Gstin,
                        SupplierName = erp.Po.Vendor?.VendorName ?? "Supplier",
                        InvoiceNumber = b.InvNum,
                        InvoiceDate = b.Date ?? erp.Po.OrderDate,
                        ErpTaxableValue = erp.Taxable,
                        ErpTaxAmount = erp.Tax,
                        Gstr2bTaxableValue = b.Taxable,
                        Gstr2bTaxAmount = b.Tax,
                        MatchStatus = "Value Mismatch",
                        ActionRequired = "Check discount/tax rate difference with supplier"
                    });
                }
            }
            else
            {
                // In GSTR-2B but not found in ERP
                model.MissingInErpCount++;
                model.Items.Add(new Gstr2bReconcileItem
                {
                    Source = "GSTR-2B Only",
                    SupplierGstin = b.Gstin,
                    SupplierName = "Unknown in ERP",
                    InvoiceNumber = b.InvNum,
                    InvoiceDate = b.Date,
                    ErpTaxableValue = 0,
                    ErpTaxAmount = 0,
                    Gstr2bTaxableValue = b.Taxable,
                    Gstr2bTaxAmount = b.Tax,
                    MatchStatus = "Missing in ERP",
                    ActionRequired = "Record Purchase Bill in ERP to claim ITC"
                });
            }
        }

        // Check ERP bills that were missing in GSTR-2B
        foreach (var kvp in erpBillMap)
        {
            if (!matchedErpKeys.Contains(kvp.Key))
            {
                model.MissingIn2bCount++;
                model.AtRiskItc += kvp.Value.Tax;
                model.Items.Add(new Gstr2bReconcileItem
                {
                    Source = "ERP Only",
                    SupplierGstin = kvp.Key.Split('_').FirstOrDefault() ?? "",
                    SupplierName = kvp.Value.Po.Vendor?.VendorName ?? "Supplier",
                    InvoiceNumber = kvp.Value.Po.InvoiceNumber ?? kvp.Value.Po.PoNumber,
                    InvoiceDate = kvp.Value.Po.OrderDate,
                    ErpTaxableValue = kvp.Value.Taxable,
                    ErpTaxAmount = kvp.Value.Tax,
                    Gstr2bTaxableValue = 0,
                    Gstr2bTaxAmount = 0,
                    MatchStatus = "Missing in GSTR-2B",
                    ActionRequired = "Contact vendor: Invoice not yet uploaded to GST portal"
                });
            }
        }

        return model;
    }

    private void ValidateInvoice(TaxInvoice inv, string rawGstin, string companyStateCode, string posCode, List<GstValidationIssue> issues)
    {
        // 1. Missing GSTIN on B2B
        if (!string.IsNullOrWhiteSpace(rawGstin) && rawGstin.Length != 15)
        {
            issues.Add(new GstValidationIssue
            {
                DocumentNumber = inv.InvoiceNumber,
                DocumentDate = inv.InvoiceDate,
                PartyName = inv.CustomerName,
                Issue = $"Invalid GSTIN format: '{rawGstin}'. Must be exactly 15 characters.",
                Recommendation = "Update customer GSTIN before filing GSTR-1.",
                Severity = GstValidationSeverity.Error
            });
        }

        // 2. State mismatch vs Tax type
        bool isInterStatePos = posCode != companyStateCode;
        if (isInterStatePos && !inv.IsInterState)
        {
            issues.Add(new GstValidationIssue
            {
                DocumentNumber = inv.InvoiceNumber,
                DocumentDate = inv.InvoiceDate,
                PartyName = inv.CustomerName,
                Issue = $"POS is {posCode} (Inter-State) but invoice was charged CGST/SGST instead of IGST.",
                Recommendation = "Verify Place of Supply and recheck tax rates.",
                Severity = GstValidationSeverity.Warning
            });
        }
        else if (!isInterStatePos && inv.IsInterState)
        {
            issues.Add(new GstValidationIssue
            {
                DocumentNumber = inv.InvoiceNumber,
                DocumentDate = inv.InvoiceDate,
                PartyName = inv.CustomerName,
                Issue = $"POS is {posCode} (Intra-State) but invoice was charged IGST instead of CGST/SGST.",
                Recommendation = "Verify Place of Supply and recheck tax rates.",
                Severity = GstValidationSeverity.Warning
            });
        }

        // 3. Item HSN Code length
        foreach (var item in inv.Items)
        {
            if (string.IsNullOrWhiteSpace(item.HsnCode) || item.HsnCode.Length < 4)
            {
                issues.Add(new GstValidationIssue
                {
                    DocumentNumber = inv.InvoiceNumber,
                    DocumentDate = inv.InvoiceDate,
                    PartyName = inv.CustomerName,
                    Issue = $"Item '{item.Description}' has missing or short HSN code '{item.HsnCode}'. Minimum 4 digits required.",
                    Recommendation = "Use standard garment HSN code (e.g. 6204 / 6211).",
                    Severity = GstValidationSeverity.Warning
                });
                break;
            }
        }
    }

    private static string ExtractStateCode(string pos, string gstin, string defaultState)
    {
        if (!string.IsNullOrWhiteSpace(gstin) && gstin.Length >= 2 && char.IsDigit(gstin[0]) && char.IsDigit(gstin[1]))
        {
            return gstin.Substring(0, 2);
        }

        if (!string.IsNullOrWhiteSpace(pos))
        {
            // e.g. "Gujarat (24)" or "Maharashtra (27)" or just "24"
            var match = System.Text.RegularExpressions.Regex.Match(pos, @"\b(\d{2})\b");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return defaultState;
    }

    private static string GetStateNameFromCode(string code) => code switch
    {
        "24" => "Gujarat",
        "27" => "Maharashtra",
        "08" => "Rajasthan",
        "07" => "Delhi",
        "29" => "Karnataka",
        "33" => "Tamil Nadu",
        "23" => "Madhya Pradesh",
        "09" => "Uttar Pradesh",
        "19" => "West Bengal",
        "36" => "Telangana",
        "37" => "Andhra Pradesh",
        _ => $"State ({code})"
    };

    private static string GetHsnDescription(string hsn) => hsn switch
    {
        "6204" => "Women's or girls' suits, ensembles, jackets, dresses, skirts",
        "6211" => "Track suits, ski suits and swimwear; other garments",
        "5208" => "Woven fabrics of cotton, containing 85% or more by weight of cotton",
        "5407" => "Woven fabrics of synthetic filament yarn",
        "6104" => "Women's or girls' suits, dresses, skirts (knitted or crocheted)",
        _ => "Garments and textile articles"
    };
}
