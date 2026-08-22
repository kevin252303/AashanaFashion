using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class CustomerController : Controller
{
    private readonly AppDbContext _context;

    public CustomerController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var customers = await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync();
        return View(customers);
    }

    [PermissionAuthorize("CustomerMaster", "CanCreate")]
    [HttpGet]
    public IActionResult Create() => View(new CustomerViewModel());

    [PermissionAuthorize("CustomerMaster", "CanCreate")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerViewModel model, List<CustomerContact>? Contacts)
    {
        if (!ModelState.IsValid) return View(model);

        var customer = MapToCustomer(model);
        customer.CreatedDate = DateTime.Now;
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        if (Contacts?.Any() == true)
            foreach (var c in Contacts.Where(c => !string.IsNullOrWhiteSpace(c.ContactName)))
            {
                c.CustomerId = customer.Id;
                _context.CustomerContacts.Add(c);
            }
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Customer '{customer.CustomerName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [PermissionAuthorize("CustomerMaster", "CanEdit")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _context.Customers.Include(c => c.Contacts).FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound();
        return View(MapToViewModel(customer));
    }

    [PermissionAuthorize("CustomerMaster", "CanEdit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerViewModel model, List<CustomerContact>? Contacts)
    {
        if (!ModelState.IsValid) return View(model);

        var customer = await _context.Customers.Include(c => c.Contacts).FirstOrDefaultAsync(c => c.Id == model.Id);
        if (customer == null) return NotFound();

        MapToCustomer(model, customer);

        // Replace contacts
        _context.CustomerContacts.RemoveRange(customer.Contacts);
        if (Contacts?.Any() == true)
            foreach (var c in Contacts.Where(c => !string.IsNullOrWhiteSpace(c.ContactName)))
            {
                c.Id = 0;
                c.CustomerId = customer.Id;
                _context.CustomerContacts.Add(c);
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

    private static Customer MapToCustomer(CustomerViewModel model, Customer? existing = null)
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

        c.CustomerCompany = model.CustomerCompany;
        c.Website = model.Website;
        c.Industry = model.Industry;
        c.Reference = model.Reference;
        c.PartnerId = model.PartnerId;

        c.Salesperson = model.Salesperson;
        c.AddDesignOnScan = model.AddDesignOnScan;
        c.SalesPaymentTerms = model.SalesPaymentTerms;
        c.SalesPaymentMethod = model.SalesPaymentMethod;
        c.Pricelist = model.Pricelist;
        c.DeliveryMethod = model.DeliveryMethod;
        c.Transporter = model.Transporter;
        c.Distance = model.Distance;

        c.AccountReceivable = model.AccountReceivable;
        c.AutoPostBills = model.AutoPostBills;
        c.CustomerInvoices = model.CustomerInvoices;
        c.InvoiceReport = model.InvoiceReport;
        c.PeppolId = model.PeppolId;
        c.FollowUpLevel = model.FollowUpLevel;
        c.FollowUpStatus = model.FollowUpStatus;
        c.Reminders = model.Reminders;
        c.NextReminder = model.NextReminder;
        c.AccountingResponsible = model.AccountingResponsible;
        c.JournalItems = model.JournalItems;
        c.Send = model.Send;
        c.TotalReceivable = model.TotalReceivable;
        c.DaysSalesOutstanding = model.DaysSalesOutstanding;
        c.PartnerLimit = model.PartnerLimit;
        c.AnalyticDistribution = model.AnalyticDistribution;

        c.BankName = model.BankName;
        c.AccountNumber = model.AccountNumber;
        c.IfscCode = model.IfscCode;

        c.SM1Name = model.SM1Name;
        c.SM1CommissionPct = model.SM1CommissionPct;
        c.SM2Name = model.SM2Name;
        c.SM2CommissionPct = model.SM2CommissionPct;
        c.SM3Name = model.SM3Name;
        c.SM3CommissionPct = model.SM3CommissionPct;
        c.CommissionStartDate = model.CommissionStartDate;
        c.CommissionEndDate = model.CommissionEndDate;

        c.Activation = model.Activation;
        c.LevelWeight = model.LevelWeight;
        c.LatestReview = model.LatestReview;
        c.NextReview = model.NextReview;
        c.PartnershipDate = model.PartnershipDate;
        c.GeoLatitude = model.GeoLatitude;
        c.GeoLongitude = model.GeoLongitude;
        c.ComputeBasedOnAddress = model.ComputeBasedOnAddress;

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

        CustomerCompany = c.CustomerCompany,
        Website = c.Website,
        Industry = c.Industry,
        Reference = c.Reference,
        PartnerId = c.PartnerId,

        Salesperson = c.Salesperson,
        AddDesignOnScan = c.AddDesignOnScan,
        SalesPaymentTerms = c.SalesPaymentTerms,
        SalesPaymentMethod = c.SalesPaymentMethod,
        Pricelist = c.Pricelist,
        DeliveryMethod = c.DeliveryMethod,
        Transporter = c.Transporter,
        Distance = c.Distance,

        AccountReceivable = c.AccountReceivable,
        AutoPostBills = c.AutoPostBills,
        CustomerInvoices = c.CustomerInvoices,
        InvoiceReport = c.InvoiceReport,
        PeppolId = c.PeppolId,
        FollowUpLevel = c.FollowUpLevel,
        FollowUpStatus = c.FollowUpStatus,
        Reminders = c.Reminders,
        NextReminder = c.NextReminder,
        AccountingResponsible = c.AccountingResponsible,
        JournalItems = c.JournalItems,
        Send = c.Send,
        TotalReceivable = c.TotalReceivable,
        DaysSalesOutstanding = c.DaysSalesOutstanding,
        PartnerLimit = c.PartnerLimit,
        AnalyticDistribution = c.AnalyticDistribution,

        BankName = c.BankName,
        AccountNumber = c.AccountNumber,
        IfscCode = c.IfscCode,

        SM1Name = c.SM1Name,
        SM1CommissionPct = c.SM1CommissionPct,
        SM2Name = c.SM2Name,
        SM2CommissionPct = c.SM2CommissionPct,
        SM3Name = c.SM3Name,
        SM3CommissionPct = c.SM3CommissionPct,
        CommissionStartDate = c.CommissionStartDate,
        CommissionEndDate = c.CommissionEndDate,

        Activation = c.Activation,
        LevelWeight = c.LevelWeight,
        LatestReview = c.LatestReview,
        NextReview = c.NextReview,
        PartnershipDate = c.PartnershipDate,
        GeoLatitude = c.GeoLatitude,
        GeoLongitude = c.GeoLongitude,
        ComputeBasedOnAddress = c.ComputeBasedOnAddress,
        Contacts = c.Contacts?.ToList() ?? new()
    };
}
