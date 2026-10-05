using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AashanaFashion.Models;
using AashanaFashion.Services;

namespace AashanaFashion.Controllers;

[Authorize]
public class BankPaymentController : Controller
{
    private readonly IBankPayoutService _bankPayoutService;
    private readonly ICompanyContext _companyContext;

    public BankPaymentController(IBankPayoutService bankPayoutService, ICompanyContext companyContext)
    {
        _bankPayoutService = bankPayoutService;
        _companyContext = companyContext;
    }

    // GET: /BankPayment
    public async Task<IActionResult> Index()
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        var model = await _bankPayoutService.GetHubDashboardDataAsync(companyId);
        return View(model);
    }

    // POST: /BankPayment/CreateBatch
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBatch([FromForm] CreateBankPaymentBatchInput input)
    {
        if (input.SelectedKeys == null || input.SelectedKeys.Count == 0)
        {
            TempData["Error"] = "Please select at least one payable item (Vendor bill, Karigar slip, or Salary) to include in the bank payment batch.";
            return RedirectToAction(nameof(Index));
        }

        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        string user = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "Admin";

        try
        {
            var batch = await _bankPayoutService.CreateBatchAsync(companyId, input, user);
            TempData["Success"] = $"Bank Payment Batch #{batch.BatchNumber} created with {batch.TotalBeneficiaries} beneficiaries totaling ₹{batch.TotalAmount:N2}. You can now download the bank file.";
            return RedirectToAction(nameof(BatchDetails), new { id = batch.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to create payment batch: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: /BankPayment/BatchDetails/5
    public async Task<IActionResult> BatchDetails(int id)
    {
        var batch = await _bankPayoutService.GetBatchDetailsAsync(id);
        if (batch == null)
        {
            TempData["Error"] = "Bank payment batch not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(batch);
    }

    // GET: /BankPayment/DownloadFile?id=5&format=HdfcENet
    public async Task<IActionResult> DownloadFile(int id, BankFormat? format)
    {
        try
        {
            var (fileBytes, fileName, contentType) = await _bankPayoutService.GenerateBankExportFileAsync(id, format);
            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to generate bank file: {ex.Message}";
            return RedirectToAction(nameof(BatchDetails), new { id });
        }
    }

    // POST: /BankPayment/MarkProcessed
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkProcessed([FromForm] MarkBatchProcessedInput input)
    {
        if (string.IsNullOrWhiteSpace(input.BankReferenceUtr))
        {
            TempData["Error"] = "Bank Reference / UTR Number is required to reconcile this payment batch.";
            return RedirectToAction(nameof(BatchDetails), new { id = input.BatchId });
        }

        string user = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "Admin";
        bool success = await _bankPayoutService.MarkBatchProcessedAsync(
            input.BatchId,
            input.BankReferenceUtr,
            input.ProcessedDate,
            user,
            input.Notes);

        if (success)
        {
            TempData["Success"] = $"Batch marked as Processed! Bank UTR #{input.BankReferenceUtr} recorded. All associated vendor bills, job slips, and salary records have been updated to Paid and accounting transactions were posted.";
        }
        else
        {
            TempData["Error"] = "Unable to process the payment batch.";
        }

        return RedirectToAction(nameof(BatchDetails), new { id = input.BatchId });
    }

    // POST: /BankPayment/CancelBatch
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBatch(int id)
    {
        string user = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "Admin";
        bool success = await _bankPayoutService.CancelBatchAsync(id, user);

        if (success)
        {
            TempData["Success"] = "Payment batch was cancelled.";
        }
        else
        {
            TempData["Error"] = "Batch could not be cancelled (it may have already been processed).";
        }

        return RedirectToAction(nameof(Index));
    }
}
