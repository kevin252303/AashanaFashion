using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Authorization;
using AashanaFashion.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class VendorController : Controller
{
    private readonly AppDbContext _context;

    public VendorController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> VerifyGSTIN(string gstin)
    {
        if (string.IsNullOrWhiteSpace(gstin))
            return Json(new { success = false, message = "GSTIN is required." });

        var verificationService = HttpContext.RequestServices.GetRequiredService<IGstVerificationService>();
        var result = await verificationService.VerifyGstAsync(gstin.ToUpper().Trim());
        return Json(result);
    }

    public async Task<IActionResult> Index()
    {
        var vendors = await _context.Vendors.OrderBy(v => v.VendorName).ToListAsync();
        return View(vendors);
    }

    [PermissionAuthorize("VendorMaster", "CanCreate")]
    [HttpGet]
    public IActionResult Create() => View(new VendorViewModel());

    [PermissionAuthorize("VendorMaster", "CanCreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VendorViewModel model, List<VendorContact>? Contacts)
    {
        if (!ModelState.IsValid) return View(model);

        var vendor = MapToVendor(model);
        vendor.CreatedDate = DateTime.Now;
        _context.Vendors.Add(vendor);
        await _context.SaveChangesAsync();

        if (Contacts?.Any() == true)
            foreach (var c in Contacts.Where(c => !string.IsNullOrWhiteSpace(c.ContactName)))
            {
                c.VendorId = vendor.Id;
                _context.VendorContacts.Add(c);
            }
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Vendor '{vendor.VendorName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("VendorMaster", "CanEdit")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vendor = await _context.Vendors.Include(v => v.Contacts).FirstOrDefaultAsync(v => v.Id == id);
        if (vendor == null) return NotFound();
        return View(MapToViewModel(vendor));
    }

    [PermissionAuthorize("VendorMaster", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(VendorViewModel model, List<VendorContact>? Contacts)
    {
        if (!ModelState.IsValid) return View(model);

        var vendor = await _context.Vendors.Include(v => v.Contacts).FirstOrDefaultAsync(v => v.Id == model.Id);
        if (vendor == null) return NotFound();

        MapToVendor(model, vendor);

        // Replace contacts
        _context.VendorContacts.RemoveRange(vendor.Contacts);
        if (Contacts?.Any() == true)
            foreach (var c in Contacts.Where(c => !string.IsNullOrWhiteSpace(c.ContactName)))
            {
                c.Id = 0;
                c.VendorId = vendor.Id;
                _context.VendorContacts.Add(c);
            }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Vendor '{vendor.VendorName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("VendorMaster", "CanDelete")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var vendor = await _context.Vendors.Include(v => v.Contacts).FirstOrDefaultAsync(v => v.Id == id);
        if (vendor != null)
        {
            _context.VendorContacts.RemoveRange(vendor.Contacts);
            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Vendor '{vendor.VendorName}' deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("VendorMaster", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var vendor = await _context.Vendors.FindAsync(id);
        if (vendor != null)
        {
            vendor.IsActive = !vendor.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Vendor '{vendor.VendorName}' {(vendor.IsActive ? "activated" : "deactivated")}.";
        }
        return RedirectToAction(nameof(Index));
    }

    private static Vendor MapToVendor(VendorViewModel model, Vendor? existing = null)
    {
        var v = existing ?? new Vendor();
        v.VendorName = model.VendorName;
        v.GstNumber = model.GstNumber;
        v.ContactPerson = model.ContactPerson;
        v.Phone = model.Phone;
        v.Email = model.Email;
        v.Address = model.Address;
        v.City = model.City;
        v.State = model.State;
        v.PinCode = model.PinCode;
        v.PanNumber = model.PanNumber;
        v.IsActive = model.IsActive;

        v.GroupRFQ = model.GroupRFQ;
        v.Buyer = model.Buyer;
        v.PurchasePaymentTerms = model.PurchasePaymentTerms;
        v.PurchasePaymentMethod = model.PurchasePaymentMethod;
        v.Box1099 = model.Box1099;
        v.ReceiptReminder = model.ReceiptReminder;
        v.FiscalPosition = model.FiscalPosition;
        v.CompanyId = model.CompanyId;
        v.Reference = model.Reference;
        v.VendorCompany = model.VendorCompany;
        v.Website = model.Website;
        v.Industry = model.Industry;
        v.PartnerId = model.PartnerId;

        v.AccountPayable = model.AccountPayable;
        v.AutoPostBills = model.AutoPostBills;
        v.InvoiceReport = model.InvoiceReport;
        v.PeppolId = model.PeppolId;
        v.FollowUpLevel = model.FollowUpLevel;
        v.FollowUpStatus = model.FollowUpStatus;
        v.Reminders = model.Reminders;
        v.NextReminder = model.NextReminder;
        v.AccountingResponsible = model.AccountingResponsible;
        v.JournalItems = model.JournalItems;
        v.Send = model.Send;
        v.PartnerLimit = model.PartnerLimit;
        v.AnalyticDistribution = model.AnalyticDistribution;

        v.BankName = model.BankName;
        v.AccountNumber = model.AccountNumber;
        v.IfscCode = model.IfscCode;

        v.SM1Name = model.SM1Name;
        v.SM1CommissionPct = model.SM1CommissionPct;
        v.SM2Name = model.SM2Name;
        v.SM2CommissionPct = model.SM2CommissionPct;
        v.SM3Name = model.SM3Name;
        v.SM3CommissionPct = model.SM3CommissionPct;
        v.CommissionStartDate = model.CommissionStartDate;
        v.CommissionEndDate = model.CommissionEndDate;

        v.Activation = model.Activation;
        v.LevelWeight = model.LevelWeight;
        v.LatestReview = model.LatestReview;
        v.NextReview = model.NextReview;
        v.PartnershipDate = model.PartnershipDate;
        v.GeoLatitude = model.GeoLatitude;
        v.GeoLongitude = model.GeoLongitude;
        v.ComputeBasedOnAddress = model.ComputeBasedOnAddress;

        return v;
    }

    private static VendorViewModel MapToViewModel(Vendor v) => new()
    {
        Id = v.Id,
        VendorName = v.VendorName,
        GstNumber = v.GstNumber,
        ContactPerson = v.ContactPerson,
        Phone = v.Phone,
        Email = v.Email,
        Address = v.Address,
        City = v.City,
        State = v.State,
        PinCode = v.PinCode,
        PanNumber = v.PanNumber,
        IsActive = v.IsActive,

        GroupRFQ = v.GroupRFQ,
        Buyer = v.Buyer,
        PurchasePaymentTerms = v.PurchasePaymentTerms,
        PurchasePaymentMethod = v.PurchasePaymentMethod,
        Box1099 = v.Box1099,
        ReceiptReminder = v.ReceiptReminder,
        FiscalPosition = v.FiscalPosition,
        CompanyId = v.CompanyId,
        Reference = v.Reference,
        VendorCompany = v.VendorCompany,
        Website = v.Website,
        Industry = v.Industry,
        PartnerId = v.PartnerId,

        AccountPayable = v.AccountPayable,
        AutoPostBills = v.AutoPostBills,
        InvoiceReport = v.InvoiceReport,
        PeppolId = v.PeppolId,
        FollowUpLevel = v.FollowUpLevel,
        FollowUpStatus = v.FollowUpStatus,
        Reminders = v.Reminders,
        NextReminder = v.NextReminder,
        AccountingResponsible = v.AccountingResponsible,
        JournalItems = v.JournalItems,
        Send = v.Send,
        PartnerLimit = v.PartnerLimit,
        AnalyticDistribution = v.AnalyticDistribution,

        BankName = v.BankName,
        AccountNumber = v.AccountNumber,
        IfscCode = v.IfscCode,

        SM1Name = v.SM1Name,
        SM1CommissionPct = v.SM1CommissionPct,
        SM2Name = v.SM2Name,
        SM2CommissionPct = v.SM2CommissionPct,
        SM3Name = v.SM3Name,
        SM3CommissionPct = v.SM3CommissionPct,
        CommissionStartDate = v.CommissionStartDate,
        CommissionEndDate = v.CommissionEndDate,

        Activation = v.Activation,
        LevelWeight = v.LevelWeight,
        LatestReview = v.LatestReview,
        NextReview = v.NextReview,
        PartnershipDate = v.PartnershipDate,
        GeoLatitude = v.GeoLatitude,
        GeoLongitude = v.GeoLongitude,
        ComputeBasedOnAddress = v.ComputeBasedOnAddress,
        Contacts = v.Contacts?.ToList() ?? new()
    };
}
