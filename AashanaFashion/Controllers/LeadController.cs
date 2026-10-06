using AashanaFashion.Data;
using AashanaFashion.Helpers;
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

    private IQueryable<Lead> GetLeadQuery()
    {
        if (User.IsInRole("Developer"))
        {
            return _context.Leads.IgnoreQueryFilters();
        }
        return _context.Leads;
    }

    private async Task<Lead?> FindLeadAsync(int id)
    {
        if (User.IsInRole("Developer"))
        {
            return await _context.Leads.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == id);
        }
        return await _context.Leads.FindAsync(id);
    }

    private void SetTempData(string key, string message)
    {
        try
        {
            if (TempData != null)
            {
                TempData[key] = message;
            }
        }
        catch
        {
            // Non-blocking in headless environments
        }
    }

    public async Task<IActionResult> Index(string? search, LeadStage? stage, string? view = "board")
    {
        var isDeveloper = User.IsInRole("Developer");
        var baseQuery = GetLeadQuery()
            .Include(l => l.AssignedToUser)
            .Include(l => l.Customer);

        IQueryable<Lead> query;
        IQueryable<Lead> allLeadsQuery;

        if (isDeveloper)
        {
            query = baseQuery;
            allLeadsQuery = GetLeadQuery();
        }
        else
        {
            var company = await _companyContext.GetActiveCompanyAsync();
            query = baseQuery.Where(l => l.CompanyId == company.Id);
            allLeadsQuery = GetLeadQuery().Where(l => l.CompanyId == company.Id);
        }

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

        var leadTenants = new Dictionary<int, Tenant>();
        if (isDeveloper)
        {
            var allTenants = await _context.Tenants.IgnoreQueryFilters().ToListAsync();
            foreach (var l in leads)
            {
                var matched = allTenants.FirstOrDefault(t =>
                    (!string.IsNullOrEmpty(l.Email) && string.Equals(t.AdminEmail, l.Email, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(l.CompanyName) && string.Equals(t.BusinessName, l.CompanyName, StringComparison.OrdinalIgnoreCase)));
                if (matched != null)
                {
                    leadTenants[l.Id] = matched;
                }
            }
        }
        ViewBag.LeadTenants = leadTenants;

        // Calculate KPIs
        var allLeads = await allLeadsQuery.ToListAsync();
        ViewBag.TotalLeads = allLeads.Count;
        ViewBag.PipelineValue = allLeads.Where(l => l.Stage != LeadStage.Lost).Sum(l => l.EstimatedValue);
        ViewBag.WonValue = allLeads.Where(l => l.Stage == LeadStage.Won).Sum(l => l.EstimatedValue);
        var closedCount = allLeads.Count(l => l.Stage == LeadStage.Won || l.Stage == LeadStage.Lost);
        ViewBag.ConversionRate = closedCount > 0
            ? Math.Round((decimal)allLeads.Count(l => l.Stage == LeadStage.Won) / closedCount * 100, 1)
            : (allLeads.Count > 0 ? Math.Round((decimal)allLeads.Count(l => l.Stage == LeadStage.Won) / allLeads.Count * 100, 1) : 0);

        // Product Owner SaaS specific KPIs
        ViewBag.PipelineMrr = ViewBag.PipelineValue;
        ViewBag.WonArr = ((decimal)ViewBag.WonValue) * 12;
        ViewBag.ActiveInbound = allLeads.Count(l => l.Stage == LeadStage.New);
        ViewBag.DemoCount = allLeads.Count(l => l.Stage == LeadStage.SampleSent);
        ViewBag.ProposalCount = allLeads.Count(l => l.Stage == LeadStage.QuotationSent);
        ViewBag.OnboardedCount = allLeads.Count(l => l.Stage == LeadStage.Won);

        ViewBag.Search = search;
        ViewBag.Stage = stage;
        ViewBag.ViewMode = view ?? "board";
        ViewBag.IsDeveloper = isDeveloper;

        return View(leads);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        int targetCompanyId;
        if (User.IsInRole("Developer"))
        {
            var rootCompany = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.TenantId == 1)
                              ?? await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync();
            targetCompanyId = rootCompany?.Id ?? 1;
        }
        else
        {
            var company = await _companyContext.GetActiveCompanyAsync();
            targetCompanyId = company.Id;
        }

        var nextNum = await GenerateLeadNumberAsync(targetCompanyId);

        var lead = new Lead
        {
            CompanyId = targetCompanyId,
            LeadNumber = nextNum,
            Stage = LeadStage.New,
            ExpectedCloseDate = DateTime.Today.AddDays(15),
            NextFollowUpDate = DateTime.Today.AddDays(2)
        };

        await PopulateDropDownsAsync(targetCompanyId);
        return View(lead);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lead model)
    {
        int targetCompanyId;
        if (User.IsInRole("Developer"))
        {
            var rootCompany = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.TenantId == 1)
                              ?? await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync();
            targetCompanyId = rootCompany?.Id ?? 1;
            model.TenantId = 1;
        }
        else
        {
            var company = await _companyContext.GetActiveCompanyAsync();
            targetCompanyId = company.Id;
        }
        model.CompanyId = targetCompanyId;

        if (string.IsNullOrWhiteSpace(model.LeadNumber))
        {
            model.LeadNumber = await GenerateLeadNumberAsync(targetCompanyId);
        }

        if (ModelState.IsValid)
        {
            model.CreatedDate = DateTime.Now;
            _context.Leads.Add(model);

            // Add initial activity
            var activity = new LeadActivity
            {
                TenantId = model.TenantId > 0 ? model.TenantId : 1,
                Lead = model,
                ActivityType = "Note",
                Description = "Lead created.",
                ActivityDate = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? "User"
            };
            _context.LeadActivities.Add(activity);

            await _context.SaveChangesAsync();
            SetTempData("Success", $"Lead {model.LeadNumber} created successfully.");
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        await PopulateDropDownsAsync(targetCompanyId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var lead = await FindLeadAsync(id);
        if (lead == null) return NotFound();

        await PopulateDropDownsAsync(lead.CompanyId);
        return View(lead);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Lead model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var lead = await FindLeadAsync(id);
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
                    TenantId = lead.TenantId,
                    LeadId = lead.Id,
                    ActivityType = "Stage Change",
                    Description = $"Stage changed from {oldStage} to {model.Stage}." + (!string.IsNullOrWhiteSpace(model.LostReason) ? $" Reason: {model.LostReason}" : ""),
                    ActivityDate = DateTime.Now,
                    CreatedBy = User.Identity?.Name ?? "User"
                };
                _context.LeadActivities.Add(stageActivity);
            }

            await _context.SaveChangesAsync();
            SetTempData("Success", "Lead updated successfully.");
            return RedirectToAction(nameof(Details), new { id = lead.Id });
        }

        await PopulateDropDownsAsync(model.CompanyId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var isDeveloper = User.IsInRole("Developer");
        var lead = await GetLeadQuery()
            .Include(l => l.AssignedToUser)
            .Include(l => l.Customer)
            .Include(l => l.SalesOrder)
            .Include(l => l.Activities.OrderByDescending(a => a.ActivityDate))
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lead == null) return NotFound();

        Tenant? matchingTenant = null;
        if (isDeveloper)
        {
            matchingTenant = await _context.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => (!string.IsNullOrEmpty(lead.Email) && t.AdminEmail == lead.Email)
                    || (!string.IsNullOrEmpty(lead.CompanyName) && t.BusinessName.ToLower() == lead.CompanyName.ToLower()));
        }

        ViewBag.MatchingTenant = matchingTenant;
        ViewBag.IsDeveloper = isDeveloper;
        await PopulateDropDownsAsync(lead.CompanyId);

        return View(lead);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickMoveStage(int id, LeadStage stage, string? returnView)
    {
        var lead = await FindLeadAsync(id);
        if (lead == null) return NotFound();

        var oldStage = lead.Stage;
        lead.Stage = stage;
        lead.UpdatedDate = DateTime.Now;

        var isDeveloper = User.IsInRole("Developer");
        var oldName = oldStage.ToStageDisplay(isDeveloper);
        var newName = stage.ToStageDisplay(isDeveloper);

        var activity = new LeadActivity
        {
            TenantId = lead.TenantId,
            LeadId = lead.Id,
            ActivityType = "Stage Change",
            Description = $"Stage moved from {oldName} to {newName}.",
            ActivityDate = DateTime.Now,
            CreatedBy = User.Identity?.Name ?? "User"
        };
        _context.LeadActivities.Add(activity);

        await _context.SaveChangesAsync();
        SetTempData("Success", $"Lead {lead.LeadNumber} moved to {newName}.");
        return RedirectToAction(nameof(Index), new { view = returnView ?? "board" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStage(int id, LeadStage stage, string? lostReason)
    {
        var lead = await FindLeadAsync(id);
        if (lead == null) return NotFound();

        var oldStage = lead.Stage;
        lead.Stage = stage;
        lead.LostReason = stage == LeadStage.Lost ? lostReason : null;
        lead.UpdatedDate = DateTime.Now;

        var activity = new LeadActivity
        {
            TenantId = lead.TenantId,
            LeadId = lead.Id,
            ActivityType = "Stage Change",
            Description = $"Stage moved to {stage}." + (!string.IsNullOrWhiteSpace(lostReason) ? $" Reason: {lostReason}" : ""),
            ActivityDate = DateTime.Now,
            CreatedBy = User.Identity?.Name ?? "User"
        };
        _context.LeadActivities.Add(activity);

        await _context.SaveChangesAsync();
        SetTempData("Success", $"Lead status updated to {stage}.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertToCustomer(int id)
    {
        var lead = await FindLeadAsync(id);
        if (lead == null) return NotFound();

        if (lead.CustomerId.HasValue)
        {
            SetTempData("Info", "Lead is already linked to a customer.");
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
            TenantId = lead.TenantId,
            LeadId = lead.Id,
            ActivityType = "Won / Converted",
            Description = $"Converted lead to Customer '{targetCustomer.CustomerName}'.",
            ActivityDate = DateTime.Now,
            CreatedBy = User.Identity?.Name ?? "User"
        };
        _context.LeadActivities.Add(activity);

        await _context.SaveChangesAsync();
        SetTempData("Success", $"Lead converted to customer '{targetCustomer.CustomerName}' successfully.");
        return RedirectToAction("Edit", "Customer", new { id = targetCustomer.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertToSalesOrder(int id)
    {
        var lead = await GetLeadQuery()
            .Include(l => l.Customer)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lead == null) return NotFound();

        if (lead.SalesOrderId.HasValue)
        {
            SetTempData("Info", "Lead is already converted to a Sales Order.");
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
            TenantId = lead.TenantId,
            LeadId = lead.Id,
            ActivityType = "Won / Converted",
            Description = $"Created Sales Order {salesOrder.SoNumber}.",
            ActivityDate = DateTime.Now,
            CreatedBy = User.Identity?.Name ?? "User"
        };
        _context.LeadActivities.Add(activity);

        await _context.SaveChangesAsync();
        SetTempData("Success", $"Draft Sales Order {salesOrder.SoNumber} created from lead.");
        return RedirectToAction("Edit", "SalesOrder", new { id = salesOrder.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddActivity(int leadId, string activityType, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            SetTempData("Error", "Activity description cannot be empty.");
            return RedirectToAction(nameof(Details), new { id = leadId });
        }

        var lead = await FindLeadAsync(leadId);
        if (lead == null) return NotFound();

        var activity = new LeadActivity
        {
            TenantId = lead.TenantId,
            LeadId = lead.Id,
            ActivityType = string.IsNullOrWhiteSpace(activityType) ? "Note" : activityType,
            Description = description.Trim(),
            ActivityDate = DateTime.Now,
            CreatedBy = User.Identity?.Name ?? "User"
        };

        _context.LeadActivities.Add(activity);
        lead.UpdatedDate = DateTime.Now;
        await _context.SaveChangesAsync();

        SetTempData("Success", "Activity logged successfully.");
        return RedirectToAction(nameof(Details), new { id = leadId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var lead = await FindLeadAsync(id);
        if (lead == null) return NotFound();

        _context.Leads.Remove(lead);
        await _context.SaveChangesAsync();

        SetTempData("Success", "Lead deleted successfully.");
        return RedirectToAction(nameof(Index));
    }

    private async Task<string> GenerateLeadNumberAsync(int companyId)
    {
        var year = DateTime.Now.Year;
        var prefix = $"LD-{year}-";
        var count = await _context.Leads.IgnoreQueryFilters().CountAsync(l => l.CompanyId == companyId && l.LeadNumber.StartsWith(prefix)) + 1;
        return $"{prefix}{count:D4}";
    }

    private async Task PopulateDropDownsAsync(int companyId)
    {
        var isDeveloper = User.IsInRole("Developer");
        var usersQuery = isDeveloper
            ? _context.Users.IgnoreQueryFilters()
            : _context.Users.AsQueryable();

        var users = await usersQuery
            .Where(u => u.IsActive && u.Role != "Customer")
            .Select(u => new { u.Id, Name = u.FullName })
            .ToListAsync();
        ViewBag.Users = new SelectList(users, "Id", "Name");

        if (isDeveloper)
        {
            ViewBag.Sources = new SelectList(new[]
            {
                "Website Registration", "Inbound Call / WhatsApp", "Product Demo Request", "LinkedIn / Social", "Founder Outreach", "Referral", "Partner / Consultant", "Direct"
            });
            ViewBag.ActivityTypes = new[]
            {
                "Product Demo", "Discovery Call", "WhatsApp Follow-up", "Commercial Proposal", "Onboarding Check-in", "Feature Request", "Internal Note"
            };
        }
        else
        {
            ViewBag.Sources = new SelectList(new[]
            {
                "Direct Call", "WhatsApp Inquiry", "Website / Portal", "Referral", "Exhibition / Trade Fair", "Instagram / Social Media", "Agent / Broker"
            });
            ViewBag.ActivityTypes = new[]
            {
                "Note", "Call", "WhatsApp", "Sample", "Quotation", "Meeting"
            };
        }
    }
}
