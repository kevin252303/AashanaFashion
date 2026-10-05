using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AashanaFashion.Services;

public class CommunicationService : ICommunicationService
{
    private readonly AppDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IEmailService _emailService;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<CommunicationService> _logger;

    public CommunicationService(
        AppDbContext context,
        IWhatsAppService whatsAppService,
        IEmailService emailService,
        ICompanyContext companyContext,
        ILogger<CommunicationService> logger)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _emailService = emailService;
        _companyContext = companyContext;
        _logger = logger;
    }

    public async Task<DocumentChatterViewModel> GetChatterViewModelAsync(string documentType, int documentId)
    {
        var vm = new DocumentChatterViewModel
        {
            DocumentType = documentType,
            DocumentId = documentId
        };

        var company = await _companyContext.GetActiveCompanyAsync();

        // 1. Fetch document specific details for recipient info & pre-filled templates
        switch (documentType)
        {
            case "SalesOrder":
                var order = await _context.SalesOrders
                    .Include(s => s.Customer)
                    .Include(s => s.Details)
                        .ThenInclude(d => d.Design)
                    .FirstOrDefaultAsync(s => s.Id == documentId);

                if (order != null)
                {
                    vm.DocumentReference = order.SoNumber;
                    vm.DefaultRecipientName = order.Customer?.CustomerName;
                    vm.DefaultRecipientPhone = order.Customer?.Phone;
                    vm.DefaultRecipientEmail = order.Customer?.Email;
                    vm.PreFilledWhatsAppText = _whatsAppService.BuildSalesOrderMessage(order, company);
                    vm.PreFilledEmailSubject = $"Sales Order Confirmation #{order.SoNumber} — {company?.CompanyName ?? "Aashana Fashion"}";
                    vm.PreFilledEmailHtml = _emailService.BuildSalesOrderEmailHtml(order, company);
                }
                break;

            case "TaxInvoice":
                var invoice = await _context.TaxInvoices
                    .Include(i => i.Customer)
                    .Include(i => i.Items)
                    .FirstOrDefaultAsync(i => i.Id == documentId);

                if (invoice != null)
                {
                    vm.DocumentReference = invoice.InvoiceNumber;
                    vm.DefaultRecipientName = invoice.CustomerName;
                    vm.DefaultRecipientPhone = invoice.Customer?.Phone;
                    vm.DefaultRecipientEmail = invoice.Customer?.Email;
                    vm.PreFilledWhatsAppText = _whatsAppService.BuildInvoiceSummaryMessage(invoice, company);
                    vm.PreFilledEmailSubject = $"Tax Invoice #{invoice.InvoiceNumber} — {company?.CompanyName ?? "Aashana Fashion"}";
                    vm.PreFilledEmailHtml = _emailService.BuildInvoiceEmailHtml(invoice, company);
                }
                break;

            case "DeliveryChallan":
                var challan = await _context.DeliveryChallans
                    .Include(c => c.Customer)
                    .Include(c => c.SalesOrder)
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.Id == documentId);

                if (challan != null)
                {
                    vm.DocumentReference = challan.ChallanNumber;
                    vm.DefaultRecipientName = challan.Customer?.CustomerName;
                    vm.DefaultRecipientPhone = challan.Customer?.Phone;
                    vm.DefaultRecipientEmail = challan.Customer?.Email;
                    vm.PreFilledWhatsAppText = _whatsAppService.BuildDispatchChallanMessage(challan, company);
                    vm.PreFilledEmailSubject = $"Dispatch Advice — Delivery Challan #{challan.ChallanNumber}";
                    vm.PreFilledEmailHtml = _emailService.BuildDispatchChallanEmailHtml(challan, company);
                }
                break;

            case "JobSlip":
                var slip = await _context.JobSlips
                    .Include(j => j.Vendor)
                    .Include(j => j.ProductionOrder)
                        .ThenInclude(p => p!.Design)
                    .Include(j => j.Items)
                    .FirstOrDefaultAsync(j => j.Id == documentId);

                if (slip != null)
                {
                    vm.DocumentReference = slip.SlipNumber;
                    vm.DefaultRecipientName = slip.Vendor?.VendorName;
                    vm.DefaultRecipientPhone = slip.Vendor?.Phone;
                    vm.DefaultRecipientEmail = slip.Vendor?.Email;
                    vm.PreFilledWhatsAppText = _whatsAppService.BuildJobSlipMessage(slip, company);
                    vm.PreFilledEmailSubject = $"Job Slip #{slip.SlipNumber} — {slip.ProcessName}";
                    vm.PreFilledEmailHtml = $"<p>Dear {slip.Vendor?.VendorName},</p><p>Please find attached job slip #{slip.SlipNumber} for process: {slip.ProcessName}. Total Quantity: {slip.TotalQuantity} pcs.</p>";
                }
                break;

            case "PaymentReceipt":
                var receipt = await _context.PaymentReceipts
                    .Include(r => r.Customer)
                    .FirstOrDefaultAsync(r => r.Id == documentId);

                if (receipt != null)
                {
                    var taxInv = receipt.TaxInvoiceId > 0
                        ? await _context.TaxInvoices.FirstOrDefaultAsync(i => i.Id == receipt.TaxInvoiceId)
                        : null;

                    vm.DocumentReference = receipt.ReceiptNumber;
                    vm.DefaultRecipientName = receipt.Customer?.CustomerName ?? taxInv?.Customer?.CustomerName;
                    vm.DefaultRecipientPhone = receipt.Customer?.Phone ?? taxInv?.Customer?.Phone;
                    vm.DefaultRecipientEmail = receipt.Customer?.Email ?? taxInv?.Customer?.Email;
                    vm.PreFilledWhatsAppText = _whatsAppService.BuildPaymentReceiptMessage(receipt, taxInv, company);
                    vm.PreFilledEmailSubject = $"Payment Receipt Voucher #{receipt.ReceiptNumber}";
                    vm.PreFilledEmailHtml = _emailService.BuildPaymentReceiptEmailHtml(receipt, taxInv, company);
                }
                break;

            case "Lead":
                var lead = await _context.Leads.FirstOrDefaultAsync(l => l.Id == documentId);
                if (lead != null)
                {
                    vm.DocumentReference = lead.LeadNumber;
                    vm.DefaultRecipientName = lead.ContactPerson;
                    vm.DefaultRecipientPhone = lead.Phone;
                    vm.DefaultRecipientEmail = lead.Email;
                    vm.PreFilledWhatsAppText = _whatsAppService.BuildLeadFollowUpMessage(lead, company);
                    vm.PreFilledEmailSubject = $"Greetings from {company?.CompanyName ?? "Aashana Fashion"} regarding {lead.Title}";
                    vm.PreFilledEmailHtml = $"<p>Dear {lead.ContactPerson},</p><p>Thank you for reaching out regarding {lead.Title}. Our team is pleased to assist you with samples and custom pricing.</p>";
                }
                break;
        }

        // 2. Fetch all communication history / chatter logs
        vm.Logs = await _context.CommunicationLogs
            .Where(c => c.DocumentType == documentType && c.DocumentId == documentId)
            .OrderByDescending(c => c.SentAt)
            .ToListAsync();

        return vm;
    }

    public async Task<(bool Success, string Message, string? ExternalId)> SendWhatsAppAsync(SendCommunicationInputModel model, string? sentBy)
    {
        if (string.IsNullOrWhiteSpace(model.Recipient))
        {
            return (false, "Recipient phone number is required.", null);
        }

        bool isDirect = model.SendViaDirectApi;
        string? externalId = null;

        if (isDirect)
        {
            var result = await _whatsAppService.SendDirectMessageAsync(model.Recipient, model.Message);
            if (!result.Success)
            {
                await _whatsAppService.LogWhatsAppSentAsync(
                    model.DocumentType, model.DocumentId, model.DocumentReference,
                    model.Recipient, model.RecipientName, model.Message,
                    true, false, result.Message, sentBy);

                return (false, result.Message, null);
            }
            externalId = $"WA-CLOUD-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
        else
        {
            externalId = _whatsAppService.GenerateWhatsAppUrl(model.Recipient, model.Message);
        }

        await _whatsAppService.LogWhatsAppSentAsync(
            model.DocumentType, model.DocumentId, model.DocumentReference,
            model.Recipient, model.RecipientName, model.Message,
            isDirect, true, null, sentBy);

        return (true, isDirect ? "WhatsApp delivered via Cloud API." : "WhatsApp link generated and logged.", externalId);
    }

    public async Task<(bool Success, string Message, string? ExternalId)> SendEmailAsync(SendCommunicationInputModel model, string? sentBy)
    {
        if (string.IsNullOrWhiteSpace(model.Recipient))
        {
            return (false, "Recipient email address is required.", null);
        }

        var subject = string.IsNullOrWhiteSpace(model.Subject)
            ? $"{model.DocumentType} {model.DocumentReference}"
            : model.Subject;

        var result = await _emailService.SendEmailAsync(
            model.Recipient,
            model.RecipientName ?? "Valued Customer",
            subject,
            model.Message,
            model.DocumentType,
            model.DocumentId,
            model.DocumentReference,
            sentBy);

        return (result.Success, result.Message, result.MessageId);
    }

    public async Task<(bool Success, string Message)> AddInternalNoteAsync(SendCommunicationInputModel model, string? sentBy)
    {
        if (string.IsNullOrWhiteSpace(model.Message))
        {
            return (false, "Note content cannot be empty.");
        }

        var log = new CommunicationLog
        {
            DocumentType = model.DocumentType,
            DocumentId = model.DocumentId,
            DocumentReference = model.DocumentReference,
            Channel = CommunicationChannel.InternalNote,
            Recipient = "Internal Team",
            RecipientName = "Team Member",
            Subject = "Internal Team Note",
            Body = model.Message,
            Status = CommunicationStatus.Sent,
            SentAt = DateTime.Now,
            SentBy = sentBy ?? "User"
        };

        _context.CommunicationLogs.Add(log);
        await _context.SaveChangesAsync();

        return (true, "Internal note logged successfully.");
    }

    public async Task LogSystemActivityAsync(string documentType, int documentId, string documentReference, string title, string description, string? user = null)
    {
        try
        {
            var log = new CommunicationLog
            {
                DocumentType = documentType,
                DocumentId = documentId,
                DocumentReference = documentReference,
                Channel = CommunicationChannel.InternalNote,
                Recipient = "System Audit",
                RecipientName = "System",
                Subject = title,
                Body = description,
                Status = CommunicationStatus.Sent,
                SentAt = DateTime.Now,
                SentBy = user ?? "System"
            };

            _context.CommunicationLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log system activity");
        }
    }
}
