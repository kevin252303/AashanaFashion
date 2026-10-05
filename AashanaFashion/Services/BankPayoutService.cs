using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AashanaFashion.Data;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public class BankPayoutService : IBankPayoutService
{
    private readonly AppDbContext _context;

    public BankPayoutService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BankBulkPaymentHubViewModel> GetHubDashboardDataAsync(int companyId)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId) ?? new Company
        {
            CompanyName = "Aashana Fashion",
            BankName = "HDFC Bank",
            BankAccountNumber = "50200012345678",
            BankIfsc = "HDFC0000240"
        };

        var model = new BankBulkPaymentHubViewModel
        {
            Company = company
        };

        // 1. Pending Vendor Bills from Purchase Orders
        var purchaseOrders = await _context.PurchaseOrders
            .Include(po => po.Vendor)
            .Include(po => po.Details)
            .Where(po => po.CompanyId == companyId && po.Status != PurchaseOrderStatus.Cancelled)
            .OrderByDescending(po => po.OrderDate)
            .ToListAsync();

        var vendorPayments = await _context.VendorPayments.ToListAsync();

        foreach (var po in purchaseOrders)
        {
            decimal totalPaid = vendorPayments.Where(vp => vp.PurchaseOrderId == po.Id).Sum(vp => vp.Amount);
            decimal balance = po.TotalAmount - totalPaid;

            if (balance > 0.01m)
            {
                var vendor = po.Vendor;
                bool validBank = ValidateBeneficiaryBank(vendor?.AccountNumber, vendor?.IfscCode, out string? bankErr);

                model.VendorBills.Add(new PendingPayableItemViewModel
                {
                    Key = $"po_{po.Id}",
                    BeneficiaryType = BeneficiaryType.Vendor,
                    BeneficiaryId = po.VendorId,
                    BeneficiaryName = vendor?.VendorName ?? "Unknown Vendor",
                    BankName = vendor?.BankName,
                    AccountNumber = vendor?.AccountNumber,
                    IfscCode = vendor?.IfscCode?.Trim().ToUpper(),
                    Email = vendor?.Email,
                    Phone = vendor?.Phone,
                    SourceType = "PurchaseOrder",
                    SourceId = po.Id,
                    ReferenceNumber = po.PoNumber,
                    ReferenceDate = po.OrderDate,
                    TotalAmount = po.TotalAmount,
                    PayableAmount = balance,
                    Description = $"PO #{po.PoNumber} (Ordered: {po.OrderDate:dd/MM/yyyy})",
                    HasValidBankDetails = validBank,
                    BankValidationError = bankErr
                });
            }
        }

        // 2. Pending Karigar / Job Worker Slips
        var jobSlips = await _context.JobSlips
            .Include(j => j.Vendor)
            .Where(j => j.Status == "Received" || j.Status == "Issued")
            .OrderByDescending(j => j.IssueDate)
            .ToListAsync();

        foreach (var js in jobSlips)
        {
            decimal slipAmount = js.TotalAmount ?? ((js.Rate ?? 0) * js.TotalQuantity);
            if (slipAmount > 0)
            {
                var karigar = js.Vendor;
                bool validBank = ValidateBeneficiaryBank(karigar?.AccountNumber, karigar?.IfscCode, out string? bankErr);

                model.JobSlips.Add(new PendingPayableItemViewModel
                {
                    Key = $"job_{js.Id}",
                    BeneficiaryType = BeneficiaryType.Karigar,
                    BeneficiaryId = js.VendorId,
                    BeneficiaryName = karigar?.VendorName ?? "Unknown Karigar",
                    BankName = karigar?.BankName,
                    AccountNumber = karigar?.AccountNumber,
                    IfscCode = karigar?.IfscCode?.Trim().ToUpper(),
                    Email = karigar?.Email,
                    Phone = karigar?.Phone,
                    SourceType = "JobSlip",
                    SourceId = js.Id,
                    ReferenceNumber = js.SlipNumber,
                    ReferenceDate = js.IssueDate,
                    TotalAmount = slipAmount,
                    PayableAmount = slipAmount,
                    Description = $"Job Slip #{js.SlipNumber} - {js.ProcessName} ({js.TotalQuantity} pcs)",
                    HasValidBankDetails = validBank,
                    BankValidationError = bankErr
                });
            }
        }

        // 3. Pending Staff Salaries (Payroll)
        var unpaidSalaries = await _context.SalaryRecords
            .Include(s => s.Employee)
            .Where(s => s.PaymentStatus != PayrollStatus.Paid && s.NetSalary > 0)
            .OrderByDescending(s => s.Year).ThenByDescending(s => s.Month)
            .ToListAsync();

        foreach (var sal in unpaidSalaries)
        {
            var emp = sal.Employee;
            bool validBank = ValidateBeneficiaryBank(emp?.BankAccountNumber, emp?.BankIFSC, out string? bankErr);

            model.Salaries.Add(new PendingPayableItemViewModel
            {
                Key = $"sal_{sal.Id}",
                BeneficiaryType = BeneficiaryType.Employee,
                BeneficiaryId = sal.EmployeeId,
                BeneficiaryName = emp?.FullName ?? "Unknown Employee",
                BankName = emp?.BankName,
                AccountNumber = emp?.BankAccountNumber,
                IfscCode = emp?.BankIFSC?.Trim().ToUpper(),
                Email = emp?.Email,
                Phone = emp?.ContactNumber,
                SourceType = "SalaryRecord",
                SourceId = sal.Id,
                ReferenceNumber = $"SAL-{sal.Year}-{sal.Month:D2}",
                ReferenceDate = sal.GeneratedDate,
                TotalAmount = sal.NetSalary,
                PayableAmount = sal.NetSalary,
                Description = $"Salary for {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(sal.Month)} {sal.Year} ({emp?.Designation})",
                HasValidBankDetails = validBank,
                BankValidationError = bankErr
            });
        }

        // Available active vendors for custom quick payout
        model.AvailableVendors = await _context.Vendors
            .Where(v => v.IsActive)
            .OrderBy(v => v.VendorName)
            .ToListAsync();

        // Calculate totals
        model.TotalPendingVendorBills = model.VendorBills.Sum(v => v.PayableAmount);
        model.TotalPendingJobSlips = model.JobSlips.Sum(j => j.PayableAmount);
        model.TotalPendingSalaries = model.Salaries.Sum(s => s.PayableAmount);
        model.TotalPendingPayable = model.TotalPendingVendorBills + model.TotalPendingJobSlips + model.TotalPendingSalaries;
        model.PendingCount = model.VendorBills.Count + model.JobSlips.Count + model.Salaries.Count;

        // Recent Batches
        model.RecentBatches = await _context.BankPaymentBatches
            .Include(b => b.Items)
            .Where(b => b.CompanyId == companyId)
            .OrderByDescending(b => b.CreatedAt)
            .Take(15)
            .ToListAsync();

        return model;
    }

    public async Task<BankPaymentBatch> CreateBatchAsync(int companyId, CreateBankPaymentBatchInput input, string createdBy)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId) ?? new Company
        {
            CompanyName = "Aashana Fashion",
            BankName = "HDFC Bank",
            BankAccountNumber = "50200012345678",
            BankIfsc = "HDFC0000240"
        };

        string debitAcc = !string.IsNullOrWhiteSpace(input.CustomDebitAccount)
            ? input.CustomDebitAccount.Trim()
            : (company.BankAccountNumber ?? "50200012345678");

        string debitIfsc = company.BankIfsc?.Trim().ToUpper() ?? "HDFC0000240";
        string debitBank = company.BankName ?? "HDFC Bank";

        // Generate sequential Batch Number: BATCH-yyyyMMdd-XXX
        string datePrefix = DateTime.Now.ToString("yyyyMMdd");
        int countToday = await _context.BankPaymentBatches
            .CountAsync(b => b.BatchNumber.StartsWith($"BATCH-{datePrefix}"));
        string batchNumber = $"BATCH-{datePrefix}-{(countToday + 1):D3}";

        var batch = new BankPaymentBatch
        {
            CompanyId = companyId,
            BatchNumber = batchNumber,
            BankFormat = input.BankFormat,
            DebitAccountNumber = debitAcc,
            DebitBankName = debitBank,
            DebitIfsc = debitIfsc,
            PaymentDate = input.PaymentDate,
            Status = BankPaymentBatchStatus.Draft,
            Notes = input.Notes,
            ExportedBy = createdBy,
            CreatedAt = DateTime.Now
        };

        var items = new List<BankPaymentBatchItem>();

        foreach (var key in input.SelectedKeys)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;

            var parts = key.Split('_');
            if (parts.Length < 2 || !int.TryParse(parts[1], out int id)) continue;

            string type = parts[0];

            if (type == "po")
            {
                var po = await _context.PurchaseOrders
                    .Include(p => p.Vendor)
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (po != null && po.Vendor != null)
                {
                    decimal totalPaid = await _context.VendorPayments
                        .Where(vp => vp.PurchaseOrderId == po.Id)
                        .SumAsync(vp => vp.Amount);
                    decimal balance = po.TotalAmount - totalPaid;
                    if (balance <= 0) balance = po.TotalAmount;

                    string ifsc = po.Vendor.IfscCode?.Trim().ToUpper() ?? "";
                    string txnType = DetermineTransactionType(debitIfsc, ifsc, balance);
                    bool isValid = ValidateBeneficiaryBank(po.Vendor.AccountNumber, ifsc, out string? err);

                    items.Add(new BankPaymentBatchItem
                    {
                        BeneficiaryType = BeneficiaryType.Vendor,
                        BeneficiaryId = po.VendorId,
                        BeneficiaryName = po.Vendor.VendorName,
                        AccountNumber = po.Vendor.AccountNumber ?? "",
                        IfscCode = ifsc,
                        BankName = po.Vendor.BankName,
                        Amount = balance,
                        TransactionType = txnType,
                        ReferenceNumber = po.PoNumber,
                        SourceType = "PurchaseOrder",
                        SourceId = po.Id,
                        BeneficiaryEmail = po.Vendor.Email,
                        BeneficiaryPhone = po.Vendor.Phone,
                        Narration = $"Inv/PO {po.PoNumber}",
                        Status = "Pending",
                        ValidationErrors = isValid ? null : err
                    });
                }
            }
            else if (type == "job")
            {
                var js = await _context.JobSlips
                    .Include(j => j.Vendor)
                    .FirstOrDefaultAsync(j => j.Id == id);
                if (js != null && js.Vendor != null)
                {
                    decimal amount = js.TotalAmount ?? ((js.Rate ?? 0) * js.TotalQuantity);
                    string ifsc = js.Vendor.IfscCode?.Trim().ToUpper() ?? "";
                    string txnType = DetermineTransactionType(debitIfsc, ifsc, amount);
                    bool isValid = ValidateBeneficiaryBank(js.Vendor.AccountNumber, ifsc, out string? err);

                    items.Add(new BankPaymentBatchItem
                    {
                        BeneficiaryType = BeneficiaryType.Karigar,
                        BeneficiaryId = js.VendorId,
                        BeneficiaryName = js.Vendor.VendorName,
                        AccountNumber = js.Vendor.AccountNumber ?? "",
                        IfscCode = ifsc,
                        BankName = js.Vendor.BankName,
                        Amount = amount,
                        TransactionType = txnType,
                        ReferenceNumber = js.SlipNumber,
                        SourceType = "JobSlip",
                        SourceId = js.Id,
                        BeneficiaryEmail = js.Vendor.Email,
                        BeneficiaryPhone = js.Vendor.Phone,
                        Narration = $"Slip {js.SlipNumber} {js.ProcessName}",
                        Status = "Pending",
                        ValidationErrors = isValid ? null : err
                    });
                }
            }
            else if (type == "sal")
            {
                var sal = await _context.SalaryRecords
                    .Include(s => s.Employee)
                    .FirstOrDefaultAsync(s => s.Id == id);
                if (sal != null && sal.Employee != null)
                {
                    string ifsc = sal.Employee.BankIFSC?.Trim().ToUpper() ?? "";
                    string txnType = DetermineTransactionType(debitIfsc, ifsc, sal.NetSalary);
                    bool isValid = ValidateBeneficiaryBank(sal.Employee.BankAccountNumber, ifsc, out string? err);

                    items.Add(new BankPaymentBatchItem
                    {
                        BeneficiaryType = BeneficiaryType.Employee,
                        BeneficiaryId = sal.EmployeeId,
                        BeneficiaryName = sal.Employee.FullName,
                        AccountNumber = sal.Employee.BankAccountNumber ?? "",
                        IfscCode = ifsc,
                        BankName = sal.Employee.BankName,
                        Amount = sal.NetSalary,
                        TransactionType = txnType,
                        ReferenceNumber = $"SAL-{sal.Year}-{sal.Month:D2}",
                        SourceType = "SalaryRecord",
                        SourceId = sal.Id,
                        BeneficiaryEmail = sal.Employee.Email,
                        BeneficiaryPhone = sal.Employee.ContactNumber,
                        Narration = $"Salary {sal.Month:D2}/{sal.Year}",
                        Status = "Pending",
                        ValidationErrors = isValid ? null : err
                    });
                }
            }
        }

        batch.TotalAmount = items.Sum(i => i.Amount);
        batch.TotalBeneficiaries = items.Count;
        batch.Items = items;

        _context.BankPaymentBatches.Add(batch);
        await _context.SaveChangesAsync();

        return batch;
    }

    public async Task<BankPaymentBatch?> GetBatchDetailsAsync(int batchId)
    {
        return await _context.BankPaymentBatches
            .Include(b => b.Company)
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == batchId);
    }

    public async Task<(byte[] FileBytes, string FileName, string ContentType)> GenerateBankExportFileAsync(int batchId, BankFormat? targetFormat = null)
    {
        var batch = await _context.BankPaymentBatches
            .Include(b => b.Company)
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch == null)
            throw new ArgumentException("Bank payment batch not found.", nameof(batchId));

        var format = targetFormat ?? batch.BankFormat;

        // Update status to Exported
        if (batch.Status == BankPaymentBatchStatus.Draft)
        {
            batch.Status = BankPaymentBatchStatus.Exported;
            batch.ExportedAt = DateTime.Now;
            foreach (var item in batch.Items)
            {
                if (item.Status == "Pending") item.Status = "Exported";
            }
            await _context.SaveChangesAsync();
        }

        string csvContent = format switch
        {
            BankFormat.HdfcENet => GenerateHdfcENetCsv(batch),
            BankFormat.IciciCib => GenerateIciciCibCsv(batch),
            BankFormat.SbiCmp => GenerateSbiCmpCsv(batch),
            BankFormat.AxisCms => GenerateAxisCmsCsv(batch),
            BankFormat.KotakCms => GenerateKotakCmsCsv(batch),
            _ => GenerateStandardNeftRtgsCsv(batch)
        };

        string prefix = format switch
        {
            BankFormat.HdfcENet => "HDFC_ENET",
            BankFormat.IciciCib => "ICICI_CIB",
            BankFormat.SbiCmp => "SBI_CMP",
            BankFormat.AxisCms => "AXIS_CMS",
            BankFormat.KotakCms => "KOTAK_CMS",
            _ => "BANK_BULK_PAYOUT"
        };

        string fileName = $"{prefix}_{batch.BatchNumber}_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csvContent)).ToArray();

        return (bytes, fileName, "text/csv");
    }

    public async Task<bool> MarkBatchProcessedAsync(int batchId, string bankUtr, DateTime processedDate, string processedBy, string? notes)
    {
        var batch = await _context.BankPaymentBatches
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch == null) return false;

        batch.Status = BankPaymentBatchStatus.Processed;
        batch.BankReferenceUtr = bankUtr.Trim();
        batch.ProcessedAt = processedDate;
        batch.ProcessedBy = processedBy;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            batch.Notes = string.IsNullOrWhiteSpace(batch.Notes) ? notes : $"{batch.Notes} | {notes}";
        }

        // Settle underlying documents
        foreach (var item in batch.Items)
        {
            item.Status = "Processed";
            item.UtrNumber = bankUtr.Trim();

            if (item.SourceType == "PurchaseOrder" && item.SourceId.HasValue)
            {
                // Create VendorPayment voucher
                string vPrefix = $"VP-{processedDate:yyyyMM}-";
                int vCount = await _context.VendorPayments.CountAsync(v => v.VoucherNumber.StartsWith(vPrefix));
                string vNo = $"{vPrefix}{(vCount + 1):D3}";

                var vp = new VendorPayment
                {
                    VoucherNumber = vNo,
                    PaymentDate = processedDate,
                    VendorId = item.BeneficiaryId ?? 0,
                    PurchaseOrderId = item.SourceId,
                    Amount = item.Amount,
                    PaymentMode = PaymentMode.BankTransfer,
                    ReferenceNumber = bankUtr.Trim(),
                    Notes = $"Bulk bank payout batch {batch.BatchNumber}. Ref: {item.ReferenceNumber}"
                };
                _context.VendorPayments.Add(vp);

                // Add general ledger accounting entry
                _context.AccountingTransactions.Add(new AccountingTransaction
                {
                    CompanyId = batch.CompanyId,
                    Date = processedDate,
                    Type = TransactionType.Expense,
                    Amount = item.Amount,
                    Category = "Purchase",
                    Description = $"Vendor payout to {item.BeneficiaryName} via Bulk Bank Payout ({batch.BankFormat}) - Ref: {item.ReferenceNumber}",
                    Reference = bankUtr.Trim(),
                    VendorId = item.BeneficiaryId
                });
            }
            else if (item.SourceType == "JobSlip" && item.SourceId.HasValue)
            {
                // Add job work expense entry
                _context.AccountingTransactions.Add(new AccountingTransaction
                {
                    CompanyId = batch.CompanyId,
                    Date = processedDate,
                    Type = TransactionType.Expense,
                    Amount = item.Amount,
                    Category = "JobWork",
                    Description = $"Karigar wage payout to {item.BeneficiaryName} for Job Slip {item.ReferenceNumber} via {batch.BankFormat}",
                    Reference = bankUtr.Trim(),
                    VendorId = item.BeneficiaryId
                });
            }
            else if (item.SourceType == "SalaryRecord" && item.SourceId.HasValue)
            {
                var sal = await _context.SalaryRecords.FirstOrDefaultAsync(s => s.Id == item.SourceId.Value);
                if (sal != null)
                {
                    sal.PaymentStatus = PayrollStatus.Paid;
                    sal.PaidDate = processedDate;
                    sal.PaymentMethod = "Bank Transfer";
                    sal.PaymentReference = bankUtr.Trim();
                    sal.Remarks = $"Paid via Bank Bulk Payout {batch.BatchNumber}";

                    // Add salary expense transaction
                    _context.AccountingTransactions.Add(new AccountingTransaction
                    {
                        CompanyId = batch.CompanyId,
                        Date = processedDate,
                        Type = TransactionType.Expense,
                        Amount = item.Amount,
                        Category = "Salary",
                        Description = $"Salary payment to {item.BeneficiaryName} for Month {sal.Month}/{sal.Year} (Batch: {batch.BatchNumber})",
                        Reference = bankUtr.Trim()
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelBatchAsync(int batchId, string cancelledBy)
    {
        var batch = await _context.BankPaymentBatches
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch == null || batch.Status == BankPaymentBatchStatus.Processed)
            return false;

        batch.Status = BankPaymentBatchStatus.Cancelled;
        batch.Notes = $"{batch.Notes} [Cancelled by {cancelledBy} on {DateTime.Now:dd/MM/yyyy HH:mm}]";
        foreach (var item in batch.Items)
        {
            item.Status = "Cancelled";
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public bool ValidateIfsc(string? ifsc, out string? errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(ifsc))
        {
            errorMessage = "IFSC code is required";
            return false;
        }

        string clean = ifsc.Trim().ToUpper();
        if (clean.Length != 11)
        {
            errorMessage = $"IFSC must be exactly 11 characters (given {clean.Length})";
            return false;
        }

        if (clean[4] != '0')
        {
            errorMessage = "5th character of IFSC must be '0' (Zero)";
            return false;
        }

        if (!Regex.IsMatch(clean, @"^[A-Z]{4}0[A-Z0-9]{6}$"))
        {
            errorMessage = "Invalid IFSC format. Expected 4 letters, 0, then 6 alphanumeric characters.";
            return false;
        }

        return true;
    }

    private bool ValidateBeneficiaryBank(string? accountNo, string? ifsc, out string? errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(accountNo))
        {
            errorMessage = "Missing bank account number";
            return false;
        }

        if (accountNo.Trim().Length < 6)
        {
            errorMessage = "Account number too short (minimum 6 digits)";
            return false;
        }

        return ValidateIfsc(ifsc, out errorMessage);
    }

    private string DetermineTransactionType(string debitIfsc, string beneIfsc, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(beneIfsc)) return "NEFT";

        beneIfsc = beneIfsc.Trim().ToUpper();
        debitIfsc = debitIfsc.Trim().ToUpper();

        // Check if both are same bank (first 4 characters)
        if (beneIfsc.Length >= 4 && debitIfsc.Length >= 4 && beneIfsc.Substring(0, 4) == debitIfsc.Substring(0, 4))
        {
            return "IFT"; // Internal Fund Transfer / Book Transfer
        }

        if (amount >= 200000m)
        {
            return "RTGS";
        }

        return "NEFT";
    }

    private string CleanCsv(string? val)
    {
        if (string.IsNullOrEmpty(val)) return "";
        string clean = val.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ").Trim();
        if (clean.Contains(",") || clean.Contains("\""))
        {
            return $"\"{clean}\"";
        }
        return clean;
    }

    // ——— Bank Template Generators ———

    private string GenerateHdfcENetCsv(BankPaymentBatch batch)
    {
        var sb = new StringBuilder();
        // HDFC ENet Standard Corporate Upload Template
        sb.AppendLine("Transaction Type,Beneficiary Code,Beneficiary Account Number,Transaction Amount,Beneficiary Name,Drawee Location,Print Location,Beneficiary Address 1,Beneficiary Address 2,Beneficiary Address 3,Beneficiary City,Beneficiary State,Beneficiary Pin,Beneficiary IFSC,Debit Account Number,Value Date,Customer Reference Number,Payment Details 1,Payment Details 2,Beneficiary Email,Beneficiary Mobile");

        foreach (var item in batch.Items)
        {
            string txnType = item.TransactionType == "IFT" ? "FT" : item.TransactionType;
            string beneCode = $"BENE-{item.BeneficiaryId ?? 0}";
            string accNo = item.AccountNumber.Trim();
            string amt = item.Amount.ToString("0.00", CultureInfo.InvariantCulture);
            string name = CleanCsv(item.BeneficiaryName);
            string ifsc = item.IfscCode.Trim().ToUpper();
            string debitAcc = batch.DebitAccountNumber.Trim();
            string valDate = batch.PaymentDate.ToString("dd/MM/yyyy");
            string refNo = CleanCsv(item.ReferenceNumber ?? batch.BatchNumber);
            string narration = CleanCsv(item.Narration ?? "KRIYEX PAYOUT");
            string email = CleanCsv(item.BeneficiaryEmail);
            string mobile = CleanCsv(item.BeneficiaryPhone);

            sb.AppendLine($"{txnType},{beneCode},{accNo},{amt},{name},Surat,,,Surat,,Surat,Gujarat,395002,{ifsc},{debitAcc},{valDate},{refNo},{narration},,{email},{mobile}");
        }

        return sb.ToString();
    }

    private string GenerateIciciCibCsv(BankPaymentBatch batch)
    {
        var sb = new StringBuilder();
        // ICICI CIB Corporate Net Banking Maker-Checker Template
        sb.AppendLine("PYMT_PROD_TYPE_CODE,PYMT_MODE,DEBIT_ACC_NO,BNF_NAME,BENE_ACC_NO,BENE_IFSC,AMOUNT,PAYMT_REF_NO,PAYMENT_DATE,INSTRUMENT_REF_NO,CR_NARRATION,DR_NARRATION,BENE_EMAIL_ID,BENE_MOBILE_NO");

        foreach (var item in batch.Items)
        {
            string mode = item.TransactionType;
            string debitAcc = batch.DebitAccountNumber.Trim();
            string name = CleanCsv(item.BeneficiaryName);
            string accNo = item.AccountNumber.Trim();
            string ifsc = item.IfscCode.Trim().ToUpper();
            string amt = item.Amount.ToString("0.00", CultureInfo.InvariantCulture);
            string refNo = CleanCsv(item.ReferenceNumber ?? batch.BatchNumber);
            string date = batch.PaymentDate.ToString("dd/MM/yyyy");
            string instNo = $"VCH-{item.Id}";
            string crNarr = CleanCsv(item.Narration ?? "Payment from Kriyex");
            string drNarr = CleanCsv($"Payout to {item.BeneficiaryName}");
            string email = CleanCsv(item.BeneficiaryEmail);
            string mobile = CleanCsv(item.BeneficiaryPhone);

            sb.AppendLine($"PA,{mode},{debitAcc},{name},{accNo},{ifsc},{amt},{refNo},{date},{instNo},{crNarr},{drNarr},{email},{mobile}");
        }

        return sb.ToString();
    }

    private string GenerateSbiCmpCsv(BankPaymentBatch batch)
    {
        var sb = new StringBuilder();
        // SBI CMP / Saral Corporate Template
        sb.AppendLine("Debit Account No,Beneficiary Name,Beneficiary Account No,IFSC Code,Amount,Payment Type,Narration / Remarks,Beneficiary Mobile,Beneficiary Email,Value Date");

        foreach (var item in batch.Items)
        {
            string debitAcc = batch.DebitAccountNumber.Trim();
            string name = CleanCsv(item.BeneficiaryName);
            string accNo = item.AccountNumber.Trim();
            string ifsc = item.IfscCode.Trim().ToUpper();
            string amt = item.Amount.ToString("0.00", CultureInfo.InvariantCulture);
            string mode = item.TransactionType == "IFT" ? "TRANSFER" : item.TransactionType;
            string narr = CleanCsv(item.Narration ?? item.ReferenceNumber ?? "Payout");
            string mobile = CleanCsv(item.BeneficiaryPhone);
            string email = CleanCsv(item.BeneficiaryEmail);
            string date = batch.PaymentDate.ToString("dd/MM/yyyy");

            sb.AppendLine($"{debitAcc},{name},{accNo},{ifsc},{amt},{mode},{narr},{mobile},{email},{date}");
        }

        return sb.ToString();
    }

    private string GenerateAxisCmsCsv(BankPaymentBatch batch)
    {
        var sb = new StringBuilder();
        // Axis Bank CMS / Enlite Template
        sb.AppendLine("Payment Mode,Corporate Account No,Beneficiary Name,Beneficiary Account No,Beneficiary Bank IFSC,Amount,Customer Reference No,Payment Details,Beneficiary Email,Beneficiary Mobile");

        foreach (var item in batch.Items)
        {
            string mode = item.TransactionType;
            string debitAcc = batch.DebitAccountNumber.Trim();
            string name = CleanCsv(item.BeneficiaryName);
            string accNo = item.AccountNumber.Trim();
            string ifsc = item.IfscCode.Trim().ToUpper();
            string amt = item.Amount.ToString("0.00", CultureInfo.InvariantCulture);
            string refNo = CleanCsv(item.ReferenceNumber ?? batch.BatchNumber);
            string details = CleanCsv(item.Narration ?? "Supplier Payment");
            string email = CleanCsv(item.BeneficiaryEmail);
            string mobile = CleanCsv(item.BeneficiaryPhone);

            sb.AppendLine($"{mode},{debitAcc},{name},{accNo},{ifsc},{amt},{refNo},{details},{email},{mobile}");
        }

        return sb.ToString();
    }

    private string GenerateKotakCmsCsv(BankPaymentBatch batch)
    {
        var sb = new StringBuilder();
        // Kotak Mahindra Bank CMS Template
        sb.AppendLine("Client Code,Debit Account No,Beneficiary Account No,Beneficiary Name,Amount,Payment Method,IFSC Code,Customer Ref No,Remarks,Beneficiary Email,Beneficiary Mobile,Value Date");

        foreach (var item in batch.Items)
        {
            string clientCode = batch.Company?.CompanyCode ?? "KRIYEX";
            string debitAcc = batch.DebitAccountNumber.Trim();
            string accNo = item.AccountNumber.Trim();
            string name = CleanCsv(item.BeneficiaryName);
            string amt = item.Amount.ToString("0.00", CultureInfo.InvariantCulture);
            string method = item.TransactionType;
            string ifsc = item.IfscCode.Trim().ToUpper();
            string refNo = CleanCsv(item.ReferenceNumber ?? batch.BatchNumber);
            string remarks = CleanCsv(item.Narration ?? "Vendor Payout");
            string email = CleanCsv(item.BeneficiaryEmail);
            string mobile = CleanCsv(item.BeneficiaryPhone);
            string date = batch.PaymentDate.ToString("dd/MM/yyyy");

            sb.AppendLine($"{clientCode},{debitAcc},{accNo},{name},{amt},{method},{ifsc},{refNo},{remarks},{email},{mobile},{date}");
        }

        return sb.ToString();
    }

    private string GenerateStandardNeftRtgsCsv(BankPaymentBatch batch)
    {
        var sb = new StringBuilder();
        // Universal RBI Standard NEFT/RTGS Template
        sb.AppendLine("Debit Account Number,Beneficiary Name,Beneficiary Account Number,IFSC Code,Amount,Transaction Type,Reference / Bill No,Narration,Value Date,Beneficiary Email,Beneficiary Mobile");

        foreach (var item in batch.Items)
        {
            string debitAcc = batch.DebitAccountNumber.Trim();
            string name = CleanCsv(item.BeneficiaryName);
            string accNo = item.AccountNumber.Trim();
            string ifsc = item.IfscCode.Trim().ToUpper();
            string amt = item.Amount.ToString("0.00", CultureInfo.InvariantCulture);
            string txnType = item.TransactionType;
            string refNo = CleanCsv(item.ReferenceNumber ?? batch.BatchNumber);
            string narr = CleanCsv(item.Narration ?? "Payout");
            string date = batch.PaymentDate.ToString("dd/MM/yyyy");
            string email = CleanCsv(item.BeneficiaryEmail);
            string mobile = CleanCsv(item.BeneficiaryPhone);

            sb.AppendLine($"{debitAcc},{name},{accNo},{ifsc},{amt},{txnType},{refNo},{narr},{date},{email},{mobile}");
        }

        return sb.ToString();
    }
}
