using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class VendorController : Controller
{
    private readonly AppDbContext _context;

    public VendorController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var vendors = await _context.Vendors.OrderBy(v => v.VendorName).ToListAsync();
        return View(vendors);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Create() => View(new VendorViewModel());

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VendorViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var vendor = new Vendor
        {
            VendorName = model.VendorName,
            GstNumber = model.GstNumber,
            ContactPerson = model.ContactPerson,
            Phone = model.Phone,
            Email = model.Email,
            Address = model.Address,
            City = model.City,
            State = model.State,
            PinCode = model.PinCode,
            BankName = model.BankName,
            AccountNumber = model.AccountNumber,
            IfscCode = model.IfscCode,
            PanNumber = model.PanNumber,
            IsActive = model.IsActive,
            CreatedDate = DateTime.Now
        };

        _context.Vendors.Add(vendor);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Vendor '{vendor.VendorName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vendor = await _context.Vendors.FindAsync(id);
        if (vendor == null) return NotFound();

        var model = new VendorViewModel
        {
            Id = vendor.Id,
            VendorName = vendor.VendorName,
            GstNumber = vendor.GstNumber,
            ContactPerson = vendor.ContactPerson,
            Phone = vendor.Phone,
            Email = vendor.Email,
            Address = vendor.Address,
            City = vendor.City,
            State = vendor.State,
            PinCode = vendor.PinCode,
            BankName = vendor.BankName,
            AccountNumber = vendor.AccountNumber,
            IfscCode = vendor.IfscCode,
            PanNumber = vendor.PanNumber,
            IsActive = vendor.IsActive
        };
        return View(model);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(VendorViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var vendor = await _context.Vendors.FindAsync(model.Id);
        if (vendor == null) return NotFound();

        vendor.VendorName = model.VendorName;
        vendor.GstNumber = model.GstNumber;
        vendor.ContactPerson = model.ContactPerson;
        vendor.Phone = model.Phone;
        vendor.Email = model.Email;
        vendor.Address = model.Address;
        vendor.City = model.City;
        vendor.State = model.State;
        vendor.PinCode = model.PinCode;
        vendor.BankName = model.BankName;
        vendor.AccountNumber = model.AccountNumber;
        vendor.IfscCode = model.IfscCode;
        vendor.PanNumber = model.PanNumber;
        vendor.IsActive = model.IsActive;

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Vendor '{vendor.VendorName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var vendor = await _context.Vendors.FindAsync(id);
        if (vendor != null)
        {
            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Vendor '{vendor.VendorName}' deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
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
}
