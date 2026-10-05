using AashanaFashion.Data;
using AashanaFashion.Models;
using AashanaFashion.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class WhatsAppController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ICompanyContext _companyContext;

    public WhatsAppController(AppDbContext context, IWhatsAppService whatsAppService, ICompanyContext companyContext)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _companyContext = companyContext;
    }

    [HttpGet]
    public async Task<IActionResult> JobSlip(int id)
    {
        var slip = await _context.JobSlips
            .Include(j => j.Vendor)
            .Include(j => j.ProductionOrder)
                .ThenInclude(p => p!.Design)
            .Include(j => j.Items)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (slip == null)
        {
            TempData["Error"] = "Job slip not found.";
            return RedirectToAction("Index", "JobSlip");
        }

        var company = await _companyContext.GetActiveCompanyAsync();
        var phone = slip.Vendor?.Phone;
        var message = _whatsAppService.BuildJobSlipMessage(slip, company);

        if (string.IsNullOrWhiteSpace(phone))
        {
            TempData["Warning"] = $"Vendor '{slip.Vendor?.VendorName}' has no phone number set. Pre-filled WhatsApp message ready.";
        }

        var url = _whatsAppService.GenerateWhatsAppUrl(phone, message);
        return Redirect(url);
    }

    [HttpGet]
    public async Task<IActionResult> DispatchChallan(int id)
    {
        var challan = await _context.DeliveryChallans
            .Include(c => c.Customer)
            .Include(c => c.SalesOrder)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (challan == null)
        {
            TempData["Error"] = "Delivery challan not found.";
            return RedirectToAction("Index", "SalesOrder");
        }

        var company = await _companyContext.GetActiveCompanyAsync();
        var phone = challan.Customer?.Phone;
        var message = _whatsAppService.BuildDispatchChallanMessage(challan, company);

        if (string.IsNullOrWhiteSpace(phone))
        {
            TempData["Warning"] = $"Customer '{challan.Customer?.CustomerName}' has no phone number set. Pre-filled WhatsApp message ready.";
        }

        var url = _whatsAppService.GenerateWhatsAppUrl(phone, message);
        return Redirect(url);
    }

    [HttpGet]
    public async Task<IActionResult> PaymentReceipt(int id)
    {
        var receipt = await _context.PaymentReceipts
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (receipt == null)
        {
            TempData["Error"] = "Payment receipt not found.";
            return RedirectToAction("Index", "PaymentReceipt");
        }

        var invoice = receipt.TaxInvoiceId > 0
            ? await _context.TaxInvoices.FirstOrDefaultAsync(i => i.Id == receipt.TaxInvoiceId)
            : null;

        var company = await _companyContext.GetActiveCompanyAsync();
        var phone = receipt.Customer?.Phone ?? invoice?.Customer?.Phone;
        var message = _whatsAppService.BuildPaymentReceiptMessage(receipt, invoice, company);

        if (string.IsNullOrWhiteSpace(phone))
        {
            TempData["Warning"] = "Customer has no phone number set. Pre-filled WhatsApp message ready.";
        }

        var url = _whatsAppService.GenerateWhatsAppUrl(phone, message);
        return Redirect(url);
    }

    [HttpGet]
    public async Task<IActionResult> Invoice(int id)
    {
        var invoice = await _context.TaxInvoices
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
        {
            TempData["Error"] = "Tax invoice not found.";
            return RedirectToAction("Index", "Invoice");
        }

        var company = await _companyContext.GetActiveCompanyAsync();
        var phone = invoice.Customer?.Phone;
        var message = _whatsAppService.BuildInvoiceSummaryMessage(invoice, company);

        if (string.IsNullOrWhiteSpace(phone))
        {
            TempData["Warning"] = "Customer has no phone number set. Pre-filled WhatsApp message ready.";
        }

        var url = _whatsAppService.GenerateWhatsAppUrl(phone, message);
        return Redirect(url);
    }

    [HttpGet]
    public async Task<IActionResult> Lead(int id)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null)
        {
            TempData["Error"] = "Lead not found.";
            return RedirectToAction("Index", "Lead");
        }

        var company = await _companyContext.GetActiveCompanyAsync();
        var message = _whatsAppService.BuildLeadFollowUpMessage(lead, company);
        var url = _whatsAppService.GenerateWhatsAppUrl(lead.Phone, message);

        // Also record an activity on the lead
        var activity = new LeadActivity
        {
            LeadId = lead.Id,
            TenantId = lead.TenantId,
            ActivityType = "WhatsApp",
            Description = "Initiated WhatsApp communication with prospect.",
            ActivityDate = DateTime.Now,
            CreatedBy = User.Identity?.Name ?? "User"
        };
        _context.LeadActivities.Add(activity);
        await _context.SaveChangesAsync();

        return Redirect(url);
    }
}
