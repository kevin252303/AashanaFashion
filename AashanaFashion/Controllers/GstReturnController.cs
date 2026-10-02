using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AashanaFashion.Models;
using AashanaFashion.Services;

namespace AashanaFashion.Controllers;

[Authorize(Roles = "Admin,SuperAdmin,System Admin,Manager")]
public class GstReturnController : Controller
{
    private readonly IGstReturnService _gstReturnService;
    private readonly ICompanyContext _companyContext;

    public GstReturnController(IGstReturnService gstReturnService, ICompanyContext companyContext)
    {
        _gstReturnService = gstReturnService;
        _companyContext = companyContext;
    }

    // GET: /GstReturn
    public async Task<IActionResult> Index(int? year, int? month)
    {
        int y = year ?? DateTime.Today.Year;
        int m = month ?? DateTime.Today.Month;

        int companyId = await _companyContext.GetActiveCompanyIdAsync();

        var gstr1Model = await _gstReturnService.GetGstr1DataAsync(companyId, y, m);
        var gstr3bModel = await _gstReturnService.GetGstr3bDataAsync(companyId, y, m);

        ViewBag.Gstr3b = gstr3bModel;
        ViewBag.SelectedYear = y;
        ViewBag.SelectedMonth = m;

        return View(gstr1Model);
    }

    // GET: /GstReturn/DownloadGstr1Json
    public async Task<IActionResult> DownloadGstr1Json(int year, int month)
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        var bytes = await _gstReturnService.GenerateGstr1JsonAsync(companyId, year, month);
        var gstr1Model = await _gstReturnService.GetGstr1DataAsync(companyId, year, month);

        string gstin = gstr1Model.Company.Gstin ?? "COMPANY";
        string fileName = $"GSTR1_{gstin}_{month:D2}{year}.json";

        return File(bytes, "application/json", fileName);
    }

    // GET: /GstReturn/DownloadGstr1Csv
    public async Task<IActionResult> DownloadGstr1Csv(int year, int month, string section = "b2b")
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();
        var bytes = await _gstReturnService.GenerateGstr1CsvAsync(companyId, year, month, section);
        var gstr1Model = await _gstReturnService.GetGstr1DataAsync(companyId, year, month);

        string gstin = gstr1Model.Company.Gstin ?? "COMPANY";
        string fileName = $"GSTR1_{section.ToUpper()}_{gstin}_{month:D2}{year}.csv";

        return File(bytes, "text/csv", fileName);
    }

    // GET: /GstReturn/Gstr3b
    public async Task<IActionResult> Gstr3b(int? year, int? month)
    {
        int y = year ?? DateTime.Today.Year;
        int m = month ?? DateTime.Today.Month;
        int companyId = await _companyContext.GetActiveCompanyIdAsync();

        var gstr3bModel = await _gstReturnService.GetGstr3bDataAsync(companyId, y, m);
        ViewBag.SelectedYear = y;
        ViewBag.SelectedMonth = m;

        return View(gstr3bModel);
    }

    // GET: /GstReturn/Reconciliation
    public async Task<IActionResult> Reconciliation(int? year, int? month)
    {
        int y = year ?? DateTime.Today.Year;
        int m = month ?? DateTime.Today.Month;
        int companyId = await _companyContext.GetActiveCompanyIdAsync();

        // Return empty upload state with ERP bill stats
        using var emptyStream = new MemoryStream();
        var model = await _gstReturnService.ReconcileGstr2bAsync(companyId, y, m, emptyStream, "");
        model.HasUploadedFile = false;

        ViewBag.SelectedYear = y;
        ViewBag.SelectedMonth = m;

        return View(model);
    }

    // POST: /GstReturn/ReconcileGstr2b
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReconcileGstr2b(int year, int month, IFormFile? gstr2bFile)
    {
        int companyId = await _companyContext.GetActiveCompanyIdAsync();

        if (gstr2bFile == null || gstr2bFile.Length == 0)
        {
            TempData["Error"] = "Please select a valid GSTR-2B JSON file downloaded from the GST Portal.";
            return RedirectToAction(nameof(Reconciliation), new { year, month });
        }

        using var stream = gstr2bFile.OpenReadStream();
        var model = await _gstReturnService.ReconcileGstr2bAsync(companyId, year, month, stream, gstr2bFile.FileName);

        ViewBag.SelectedYear = year;
        ViewBag.SelectedMonth = month;
        TempData["Success"] = $"Successfully processed GSTR-2B file: {gstr2bFile.FileName}. Found {model.TotalGstr2bBillsCount} bills from portal.";

        return View("Reconciliation", model);
    }
}
