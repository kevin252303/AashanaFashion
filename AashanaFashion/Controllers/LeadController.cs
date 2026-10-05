using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class LeadController : Controller
{
    private readonly AppDbContext _context;
    private readonly ICompanyContext _companyContext;

    public LeadController(AppDbContext context, ICompanyContext companyContext)
    {
        _context = context;
        _companyContext = companyContext;
    }

    public async Task<IActionResult> Index(string? search, LeadStage? stage, string? view = "board")
    {
        var company = await _companyContext.GetActiveCompanyAsync();
        var query = _context.Leads
            .Include(l => l.AssignedToUser)
            .Include(l => l.Customer)
            .Where(l => l.CompanyId == company.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(l => l.LeadNumber.Contains(search)
                || l.Title.Contains(search)
                || l.ContactPerson.Contains(search)
                || (l.CompanyName != null && l.CompanyName.Contains(search))
                || (l.Phone != null && l.Phone.Contains(search))
                || (l.City != null && l.City.Contains(search)));
        }

        if (stage.HasValue)
        {
            query = query.Where(l => l.Stage == stage.Value);
        }

        var leads = await query.OrderByDescending(l => l.CreatedDate).ToListAsync();

        // Calculate KPIs
        var allLeads = await _context.Leads.Where(l => l.CompanyId == company.Id).ToListAsync();
        ViewBag.TotalLeads = allLeads.Count;
        ViewBag.PipelineValue = allLeads.Where(l => l.Stage != LeadStage.Lost).Sum(l => l.EstimatedValue);
        ViewBag.WonValue = allLeads.Where(l => l.Stage == LeadStage.Won).Sum(l => l.EstimatedValue);
        var closedCount = allLeads.Count(l => l.Stage == LeadStage.Won || l.Stage == LeadStage.Lost);
        ViewBag.ConversionRate = closedCount > 0
            ? Math.Round((decimal)allLeads.Count(l => l.Stage == LeadStage.Won) / closedCount * 100, 1)
            : (allLeads.Count > 0 ? Math.Round((decimal)allLeads.Count(l => l.Stage == LeadStage.Won) / allLeads.Count * 100, 1) : 0);

        ViewBag.Search = search;
        ViewBag.Stage = stage;
        ViewBag.ViewMode = view ?? "board";

        return View(leads);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var company = await _companyContext.GetActiveCompanyAsync();
        var nextNum = await GenerateLeadNumberAsync(company.Id);

        var lead = new Lead
        {
            CompanyId = company.Id,
            LeadNumber = nextNum,
            Stage = LeadStage.New,
            ExpectedCloseDate = DateTime.Today.AddDays(15),
            NextFollowUpDate = DateTime.Today.AddDays(2)
        };

        await PopulateDropDownsAsync(company.Id);
        return View(lead);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lead model)
    {
        var company = await _companyContext.GetActiveCompanyAsync();
        model.CompanyId = company.Id;

        if (string.IsNullOrWhiteSpace(model.LeadNumber))
        {
            model.LeadNumber = await GenerateLeadNumberAsync(company.Id);
        }

        if (ModelState.IsValid)
        {
            model.CreatedDate = DateTime.Now;
            _context.Leads.Add(model);

            // Add initial activity
            var activity = new LeadActivity
            {
                Lead = model,
                ActivityType = "Note",
                Description = "Lead created.",
                ActivityDate = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? "User"
            };
            _context.LeadActivities.Add(activity);

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Lead {model.LeadNumber} created successfully.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        await PopulateDropDownsAsync(company.Id);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        var company = await _companyContext.GetActiveCompanyAsync();
        await PopulateDropDownsAsync(company.Id);
        return View(lead);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Lead model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var lead = await _context.Leads.FindAsync(id);
            if (lead == null) return NotFound();

            var oldStage = lead.Stage;

            lead.Title = model.Title;
            lead.CompanyName = model.CompanyName;
            lead.ContactPerson = model.ContactPerson;
            lead.Phone = model.Phone;
            lead.Email = model.Email;
            lead.City = model.City;
            lead.State = model.State;
            lead.EstimatedValue = model.EstimatedValue;
            lead.EstimatedQuantity = model.EstimatedQuantity;
            lead.Stage = model.Stage;
            lead.LostReason = model.LostReason;
            lead.Source = model.Source;
            lead.AssignedToUserId = model.AssignedToUserId;
            lead.ExpectedCloseDate = model.ExpectedCloseDate;
            lead.NextFollowUpDate = model.NextFollowUpDate;
            lead.Notes = model.Notes;
            lead.UpdatedDate = DateTime.Now;

            if (oldStage != model.Stage)
            {
                var stageActivity = new LeadActivity
                {
                    LeadId = lead.Id,
                    ActivityType = "Stage Change",
                    Description = $"Stage changed from {oldStage} to {model.Stage}." + (!string.IsNullOrWhiteSpace(model.LostReason) ? $" Reason: {model.LostReason}" : ""),
                    ActivityDate = DateTime.Now,
                    CreatedBy = User.Identity?.Name ?? "User"
                };
                _context.LeadActivities.Add(stageActivity);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Lead updated successfully.";
            return RedirectToAction(nameof(Details), new { id = lead.Id });
        }

        var comp = await _companyContext.GetActiveCompanyAsync();
        await PopulateDropDownsAsync(comp.Id);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var lead = await _context.Leads
            .Include(l => l.AssignedToUser)
            .Include(l => l.Customer)
            .Include(l => l.SalesOrder)
            .Include(l => l.Activities.OrderByDescending(a => a.ActivityDate))
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lead == null) return NotFound();

        return View(lead);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStage(int id, LeadStage stage, string? lostReason)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        var oldStage = lead.Stage;
        lead.Stage = stage;
        lead.LostReason = stage == LeadStage.Lost ? lostReason : null;
        lead.UpdatedDate = DateTime.Now;

        var activity = new LeadActivity
        {
            LeadId = lead.Id,
            ActivityType = "Stage Change",
            Description = $"Stage moved to {stage}." + (!string.IsNullOrWhiteSpace(lostReason) ? $" Reason: {lostReason}" : ""),
            ActivityDate = DateTime.Now,
            CreatedBy = User.Identity?.Name ?? "User"
        };
        _context.LeadActivities.Add(activity);

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Lead status updated to {stage}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertToCustomer(int id)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        if (lead.CustomerId.HasValue)
        {
            TempData["Info"] = "Lead is already linked to a customer.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var customerName = !string.IsNullOrWhiteSpace(lead.CompanyName) ? lead.CompanyName : lead.ContactPerson;

        // Check if customer already exists
        var existingCustomer = await _context.Customers
            .FirstOrDefaultAsync(c => c.CustomerName.ToLower() == customerName.ToLower()
                || (!string.IsNullOrEmpty(lead.Phone) && c.Phone == lead.Phone));

        Customer targetCustomer;
        if (existingCustomer != null)
        {
            targetCustomer = existingCustomer;
        }
        else
        {
            targetCustomer = new Customer
            {
                CustomerName = customerName,
                ContactPerson = lead.ContactPerson,
                Phone = lead.Phone,
                Email = lead.Email,
                City = lead.City,
                State = lead.State,
                CreatedDate = DateTime.Now,
                IsActive = true
            };
            _context.Customers.Add(targetCustomer);
            await _context.SaveChangesAsync();
        }

        lead.CustomerId = targetCustomer.Id;
        lead.Stage = LeadStage.Won;
        lead.UpdatedDate = DateTime.Now;

        var activity = new LeadActivity
        {
            LeadId = lead.Id,
            ActivityType = "Won / Converted",
            Description = $"Converted lead to Customer '{targetCustomer.CustomerName}'.",
            ActivityDate = DateTime.Now,
            CreatedBy = User.Identity?.Name ?? "User"
        };
        _context.LeadActivities.Add(activity);

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Lead converted to customer '{targetCustomer.CustomerName}' successfully.";
        return RedirectToAction("Edit", "Customer", new { id = targetCustomer.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertToSalesOrder(int id)
    {
        var lead = await _context.Leads
            .Include(l => l.Customer)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lead == null) return NotFound();

        if (lead.SalesOrderId.HasValue)
        {
            TempData["Info"] = "Lead is already converted to a Sales Order.";
            return RedirectToAction("Details", "SalesOrder", new { id = lead.SalesOrderId.Value });
        }

        // Ensure customer exists
        Customer customer;
        if (lead.CustomerId.HasValue)
        {
            customer = lead.Customer!;
        }
        else
        {
            var customerName = !string.IsNullOrWhiteSpace(lead.CompanyName) ? lead.CompanyName : lead.ContactPerson;
            customer = new Customer
            {
                CustomerName = customerName,
                ContactPerson = lead.ContactPerson,
                Phone = lead.Phone,
                Email = lead.Email,
                City = lead.City,
                State = lead.State,
                CreatedDate = DateTime.Now,
                IsActive = true
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            lead.CustomerId = customer.Id;
        }

        var company = await _companyContext.GetActiveCompanyAsync();

        // Generate SO Number
        var year = DateTime.Now.Year;
        var prefix = $"SO-{year}-";
        var count = await _context.SalesOrders.CountAsync(s => s.SoNumber.StartsWith(prefix)) + 1;
        var soNumber = $"{prefix}{count:D4}";

        var salesOrder = new SalesOrder
        {
            CompanyId = company.Id,
            CustomerId = customer.Id,
            SoNumber = soNumber,
            OrderDate = DateTime.Today,
            ExpectedDeliveryDate = lead.ExpectedCloseDate ?? DateTime.Today.AddDays(15),
            Status = SalesOrderStatus.Draft,
            Notes = $"Generated from Lead {lead.LeadNumber}: {lead.Title}" + (!string.IsNullOrWhiteSpace(lead.Notes) ? $"\nNotes: {lead.Notes}" : "")
        };

        _context.SalesOrders.Add(salesOrder);
        await _context.SaveChangesAsync();

        lead.SalesOrderId = salesOrder.Id;
        lead.Stage = LeadStage.Won;
        lead.UpdatedDate = DateTime.Now;

        var activity = new LeadActivity
        {
            LeadId = lead.Id,
            ActivityType = "Won / Converted",
            Description = $"Created Sales Order {salesOrder.SoNumber}.",
            ActivityDate = DateTime.Now,
            CreatedBy = User.Identity?.Name ?? "User"
        };
        _context.LeadActivities.Add(activity);

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Draft Sales Order {salesOrder.SoNumber} created from lead.";
        return RedirectToAction("Edit", "SalesOrder", new { id = salesOrder.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddActivity(int leadId, string activityType, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            TempData["Error"] = "Activity description cannot be empty.";
            return RedirectToAction(nameof(Details), new { id = leadId });
        }

        var lead = await _context.Leads.FindAsync(leadId);
        if (lead == null) return NotFound();

        var activity = new LeadActivity
        {
            LeadId = lead.Id,
            ActivityType = string.IsNullOrWhiteSpace(activityType) ? "Note" : activityType,
            Description = description.Trim(),
            ActivityDate = DateTime.Now,
            CreatedBy = User.Identity?.Name ?? "User"
        };

        _context.LeadActivities.Add(activity);
        lead.UpdatedDate = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Activity logged successfully.";
        return RedirectToAction(nameof(Details), new { id = leadId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound();

        _context.Leads.Remove(lead);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Lead deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<string> GenerateLeadNumberAsync(int companyId)
    {
        var year = DateTime.Now.Year;
        var prefix = $"LD-{year}-";
        var count = await _context.Leads.CountAsync(l => l.CompanyId == companyId && l.LeadNumber.StartsWith(prefix)) + 1;
        return $"{prefix}{count:D4}";
    }

    private async Task PopulateDropDownsAsync(int companyId)
    {
        var users = await _context.Users
            .Where(u => u.IsActive && u.Role != "Developer" && u.Role != "Customer")
            .Select(u => new { u.Id, Name = u.FullName })
            .ToListAsync();
        ViewBag.Users = new SelectList(users, "Id", "Name");

        ViewBag.Sources = new SelectList(new[]
        {
            "Direct Call", "WhatsApp Inquiry", "Website / Portal", "Referral", "Exhibition / Trade Fair", "Instagram / Social Media", "Agent / Broker"
        });
    }
}
