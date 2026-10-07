using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Authorization;
using AashanaFashion.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class CustomerController : Controller
{
    private readonly AppDbContext _context;

    public CustomerController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> VerifyGSTIN(string gstin)
    {
        if (string.IsNullOrWhiteSpace(gstin))
            return Json(new { success = false, message = "GSTIN is required." });

        var verificationService = HttpContext.RequestServices.GetRequiredService<IGstVerificationService>();
        var result = await verificationService.VerifyGstAsync(gstin.ToUpper().Trim());
        return Json(result);
    }

    public async Task<IActionResult> Index(string? search, string? city, bool? activeOnly)
    {
        var query = _context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.CustomerName.ToLower().Contains(s)
                || (c.ContactPerson != null && c.ContactPerson.ToLower().Contains(s))
                || (c.Phone != null && c.Phone.ToLower().Contains(s))
                || (c.Email != null && c.Email.ToLower().Contains(s))
                || (c.GstNumber != null && c.GstNumber.ToLower().Contains(s))
                || (c.Pricelist != null && c.Pricelist.ToLower().Contains(s))
                || (c.City != null && c.City.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(c => c.City == city);
        }

        if (activeOnly == true)
        {
            query = query.Where(c => c.IsActive);
        }

        var customers = await query.OrderBy(c => c.CustomerName).ToListAsync();

        ViewBag.Search = search;
        ViewBag.SelectedCity = city;
        ViewBag.ActiveOnly = activeOnly;
        ViewBag.Cities = await _context.Customers.Where(c => !string.IsNullOrEmpty(c.City)).Select(c => c.City!).Distinct().OrderBy(c => c).ToListAsync();

        return View(customers);
    }

    private async Task PopulateDropdownsAsync()
    {
        ViewBag.Users = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FirstName).ToListAsync();
        ViewBag.Pricelists = await _context.Pricelists.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
        ViewBag.Categories = await _context.ProductCategories.Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync();
        ViewBag.Designs = await _context.Designs.OrderBy(d => d.DesignNumber).Select(d => new { d.Id, d.DesignNumber }).ToListAsync();
    }

    [PermissionAuthorize("CustomerMaster", "CanCreate")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View(new CustomerViewModel());
    }

    [PermissionAuthorize("CustomerMaster", "CanCreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerViewModel model, List<CustomerContact>? Contacts, List<CustomerSalesmanCommission>? Commissions)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(model);
        }

        var customer = MapToCustomer(model);
        customer.CreatedDate = DateTime.Now;
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        if (Contacts?.Any() == true)
        {
            foreach (var c in Contacts.Where(c => !string.IsNullOrWhiteSpace(c.ContactName)))
            {
                c.CustomerId = customer.Id;
                _context.CustomerContacts.Add(c);
            }
        }

        if (Commissions?.Any() == true)
        {
            foreach (var comm in Commissions.Where(c => !string.IsNullOrWhiteSpace(c.SalesmanName) && c.CommissionRate > 0))
            {
                comm.Id = 0;
                comm.CustomerId = customer.Id;
                _context.CustomerSalesmanCommissions.Add(comm);
            }
        }
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Customer '{customer.CustomerName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("CustomerMaster", "CanEdit")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _context.Customers
            .Include(c => c.Contacts)
            .Include(c => c.Commissions)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound();
        await PopulateDropdownsAsync();
        return View(MapToViewModel(customer));
    }

    [PermissionAuthorize("CustomerMaster", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerViewModel model, List<CustomerContact>? Contacts, List<CustomerSalesmanCommission>? Commissions)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(model);
        }

        var customer = await _context.Customers
            .Include(c => c.Contacts)
            .Include(c => c.Commissions)
            .FirstOrDefaultAsync(c => c.Id == model.Id);
        if (customer == null) return NotFound();

        MapToCustomer(model, customer);

        // Replace contacts
        _context.CustomerContacts.RemoveRange(customer.Contacts);
        if (Contacts?.Any() == true)
        {
            foreach (var c in Contacts.Where(c => !string.IsNullOrWhiteSpace(c.ContactName)))
            {
                c.Id = 0;
                c.CustomerId = customer.Id;
                _context.CustomerContacts.Add(c);
            }
        }

        // Replace commissions
        _context.CustomerSalesmanCommissions.RemoveRange(customer.Commissions);
        if (Commissions?.Any() == true)
        {
            foreach (var comm in Commissions.Where(c => !string.IsNullOrWhiteSpace(c.SalesmanName) && c.CommissionRate > 0))
            {
                comm.Id = 0;
                comm.CustomerId = customer.Id;
                _context.CustomerSalesmanCommissions.Add(comm);
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Customer '{customer.CustomerName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("CustomerMaster", "CanDelete")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _context.Customers.Include(c => c.Contacts).FirstOrDefaultAsync(c => c.Id == id);
        if (customer != null)
        {
            _context.CustomerContacts.RemoveRange(customer.Contacts);
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Customer '{customer.CustomerName}' deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("CustomerMaster", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            customer.IsActive = !customer.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Customer '{customer.CustomerName}' {(customer.IsActive ? "activated" : "deactivated")}.";
        }
        return RedirectToAction(nameof(Index));
    }

    private Customer MapToCustomer(CustomerViewModel model, Customer? existing = null)
    {
        var c = existing ?? new Customer();
        c.CustomerName = model.CustomerName;
        c.GstNumber = model.GstNumber;
        c.ContactPerson = model.ContactPerson;
        c.Phone = model.Phone;
        c.Email = model.Email;
        c.Address = model.Address;
        c.City = model.City;
        c.State = model.State;
        c.PinCode = model.PinCode;
        c.PanNumber = model.PanNumber;
        c.IsActive = model.IsActive;

        // Credit Limits & Exposure
        c.PartnerLimit = model.PartnerLimit;
        c.TotalReceivable = model.TotalReceivable;
        c.DaysSalesOutstanding = model.DaysSalesOutstanding;

        // Sales & Commercial
        c.Salesperson = model.Salesperson;
        c.AddDesignOnScan = model.AddDesignOnScan;
        c.SalesPaymentTerms = model.SalesPaymentTerms;
        c.SalesPaymentMethod = model.SalesPaymentMethod;
        c.PricelistId = model.PricelistId;
        if (model.PricelistId.HasValue && model.PricelistId.Value > 0)
        {
            var pl = _context.Pricelists.Find(model.PricelistId.Value);
            c.Pricelist = pl?.Name ?? model.Pricelist;
        }
        else
        {
            c.Pricelist = model.Pricelist;
        }
        c.DeliveryMethod = model.DeliveryMethod;
        c.Transporter = model.Transporter;
        c.Distance = model.Distance;

        // Bank Details
        c.BankName = model.BankName;
        c.AccountNumber = model.AccountNumber;
        c.IfscCode = model.IfscCode;
        c.BankBranch = model.BankBranch;

        return c;
    }

    private static CustomerViewModel MapToViewModel(Customer c) => new()
    {
        Id = c.Id,
        CustomerName = c.CustomerName,
        GstNumber = c.GstNumber,
        ContactPerson = c.ContactPerson,
        Phone = c.Phone,
        Email = c.Email,
        Address = c.Address,
        City = c.City,
        State = c.State,
        PinCode = c.PinCode,
        PanNumber = c.PanNumber,
        IsActive = c.IsActive,

        // Credit Limits & Exposure
        PartnerLimit = c.PartnerLimit,
        TotalReceivable = c.TotalReceivable,
        DaysSalesOutstanding = c.DaysSalesOutstanding,

        // Sales & Commercial
        Salesperson = c.Salesperson,
        AddDesignOnScan = c.AddDesignOnScan,
        SalesPaymentTerms = c.SalesPaymentTerms,
        SalesPaymentMethod = c.SalesPaymentMethod,
        Pricelist = c.Pricelist,
        PricelistId = c.PricelistId,
        DeliveryMethod = c.DeliveryMethod,
        Transporter = c.Transporter,
        Distance = c.Distance,

        // Bank Details
        BankName = c.BankName,
        AccountNumber = c.AccountNumber,
        IfscCode = c.IfscCode,
        BankBranch = c.BankBranch,

        // Child collections
        Contacts = c.Contacts?.ToList() ?? new(),
        Commissions = c.Commissions?.ToList() ?? new()
    };
}


