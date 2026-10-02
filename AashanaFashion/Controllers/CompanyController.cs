using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class CompanyController : Controller
{
    private readonly AppDbContext _context;
    private readonly ICompanyContext _companyContext;

    public CompanyController(AppDbContext context, ICompanyContext companyContext)
    {
        _context = context;
        _companyContext = companyContext;
    }

    [HttpGet]
    public async Task<IActionResult> Switch(int companyId, string? returnUrl)
    {
        var allowed = await _companyContext.GetAllowedCompaniesAsync();
        var target = allowed.FirstOrDefault(c => c.Id == companyId);

        if (target != null)
        {
            _companyContext.SetActiveCompanyId(target.Id);
            TempData["Success"] = $"Switched active company to \"{target.CompanyName}\".";
        }
        else
        {
            TempData["Error"] = "Selected company not found or you do not have permission to access it.";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Index()
    {
        var companies = await _context.Companies
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.CompanyName)
            .ToListAsync();

        ViewBag.ActiveCompanyId = await _companyContext.GetActiveCompanyIdAsync();
        return View(companies);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public IActionResult Create()
    {
        var model = new Company
        {
            StateCode = 24,
            State = "Gujarat",
            City = "Surat",
            InvoicePrefix = "INV-",
            SalesOrderPrefix = "SO-",
            PurchaseOrderPrefix = "PO-"
        };
        return View(model);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Company model)
    {
        if (string.IsNullOrWhiteSpace(model.CompanyName))
            ModelState.AddModelError("CompanyName", "Company name is required.");

        if (string.IsNullOrWhiteSpace(model.CompanyCode))
            ModelState.AddModelError("CompanyCode", "Company code is required.");

        if (await _context.Companies.AnyAsync(c => c.CompanyCode == model.CompanyCode.Trim()))
            ModelState.AddModelError("CompanyCode", "A company with this code already exists.");

        if (!ModelState.IsValid)
            return View(model);

        model.CompanyName = model.CompanyName.Trim();
        model.CompanyCode = model.CompanyCode.Trim().ToUpper();
        model.Gstin = model.Gstin?.Trim().ToUpper();
        model.Pan = model.Pan?.Trim().ToUpper();
        model.Email = model.Email?.Trim();
        model.Phone = model.Phone?.Trim();
        model.Address1 = model.Address1?.Trim();
        model.Address2 = model.Address2?.Trim();
        model.City = model.City?.Trim() ?? "Surat";
        model.State = model.State?.Trim() ?? "Gujarat";
        model.PinCode = model.PinCode?.Trim() ?? "395002";
        model.BankName = model.BankName?.Trim();
        model.BankAccountNumber = model.BankAccountNumber?.Trim();
        model.BankIfsc = model.BankIfsc?.Trim().ToUpper();
        model.BankBranch = model.BankBranch?.Trim();
        model.InvoicePrefix = string.IsNullOrWhiteSpace(model.InvoicePrefix) ? $"INV-{model.CompanyCode}-" : model.InvoicePrefix.Trim();
        model.SalesOrderPrefix = string.IsNullOrWhiteSpace(model.SalesOrderPrefix) ? $"SO-{model.CompanyCode}-" : model.SalesOrderPrefix.Trim();
        model.PurchaseOrderPrefix = string.IsNullOrWhiteSpace(model.PurchaseOrderPrefix) ? $"PO-{model.CompanyCode}-" : model.PurchaseOrderPrefix.Trim();
        model.CreatedDate = DateTime.Now;

        if (model.IsDefault)
        {
            var otherDefaults = await _context.Companies.Where(c => c.IsDefault).ToListAsync();
            foreach (var od in otherDefaults) od.IsDefault = false;
        }

        _context.Companies.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Company \"{model.CompanyName}\" created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company == null) return NotFound();
        return View(company);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Company model)
    {
        if (id != model.Id) return NotFound();

        if (string.IsNullOrWhiteSpace(model.CompanyName))
            ModelState.AddModelError("CompanyName", "Company name is required.");

        if (string.IsNullOrWhiteSpace(model.CompanyCode))
            ModelState.AddModelError("CompanyCode", "Company code is required.");

        if (await _context.Companies.AnyAsync(c => c.CompanyCode == model.CompanyCode.Trim() && c.Id != id))
            ModelState.AddModelError("CompanyCode", "Another company with this code already exists.");

        if (!ModelState.IsValid)
            return View(model);

        var existing = await _context.Companies.FindAsync(id);
        if (existing == null) return NotFound();

        existing.CompanyName = model.CompanyName.Trim();
        existing.CompanyCode = model.CompanyCode.Trim().ToUpper();
        existing.Gstin = model.Gstin?.Trim().ToUpper();
        existing.Pan = model.Pan?.Trim().ToUpper();
        existing.Email = model.Email?.Trim();
        existing.Phone = model.Phone?.Trim();
        existing.Address1 = model.Address1?.Trim();
        existing.Address2 = model.Address2?.Trim();
        existing.City = model.City?.Trim() ?? "Surat";
        existing.State = model.State?.Trim() ?? "Gujarat";
        existing.StateCode = model.StateCode;
        existing.PinCode = model.PinCode?.Trim() ?? "395002";
        existing.BankName = model.BankName?.Trim();
        existing.BankAccountNumber = model.BankAccountNumber?.Trim();
        existing.BankIfsc = model.BankIfsc?.Trim().ToUpper();
        existing.BankBranch = model.BankBranch?.Trim();
        existing.InvoicePrefix = model.InvoicePrefix?.Trim() ?? "INV-";
        existing.SalesOrderPrefix = model.SalesOrderPrefix?.Trim() ?? "SO-";
        existing.PurchaseOrderPrefix = model.PurchaseOrderPrefix?.Trim() ?? "PO-";
        existing.IsActive = model.IsActive;

        if (model.IsDefault && !existing.IsDefault)
        {
            var otherDefaults = await _context.Companies.Where(c => c.IsDefault && c.Id != id).ToListAsync();
            foreach (var od in otherDefaults) od.IsDefault = false;
            existing.IsDefault = true;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Company \"{existing.CompanyName}\" updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault(int id)
    {
        var target = await _context.Companies.FindAsync(id);
        if (target == null) return NotFound();

        var others = await _context.Companies.Where(c => c.IsDefault).ToListAsync();
        foreach (var o in others) o.IsDefault = false;

        target.IsDefault = true;
        target.IsActive = true;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"\"{target.CompanyName}\" is now the default company.";
        return RedirectToAction(nameof(Index));
    }
}
