using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AashanaFashion.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AashanaFashion.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppService> _logger;
    private readonly HttpClient _httpClient;

    public WhatsAppService(IConfiguration configuration, ILogger<WhatsAppService> logger, HttpClient? httpClient = null)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();
    }

    public string NormalizePhoneNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        // Remove all non-numeric characters
        string digits = Regex.Replace(phone, @"[^\d]", "");

        // If 10 digits (standard Indian mobile without country code), prepend 91
        if (digits.Length == 10)
        {
            digits = "91" + digits;
        }
        else if (digits.Length == 12 && digits.StartsWith("00"))
        {
            digits = digits.Substring(2);
        }

        return digits;
    }

    public string GenerateWhatsAppUrl(string? phone, string message)
    {
        string normalized = NormalizePhoneNumber(phone);
        string encoded = Uri.EscapeDataString(message);
        if (string.IsNullOrEmpty(normalized))
        {
            return $"https://wa.me/?text={encoded}";
        }
        return $"https://wa.me/{normalized}?text={encoded}";
    }

    public string BuildJobSlipMessage(JobSlip slip, Company? company)
    {
        var companyName = company?.CompanyName ?? "Kriyex Garment Manufacturing";
        var vendorName = slip.Vendor?.VendorName ?? "Job Worker / Karigar";
        var designNo = slip.ProductionOrder?.Design?.DesignNumber ?? slip.ProductionOrder?.LotNo ?? "N/A";

        var sb = new StringBuilder();
        sb.AppendLine($"*JOB SLIP / PROCESS CHALLAN*");
        sb.AppendLine($"*From:* {companyName}");
        sb.AppendLine($"*Slip No:* {slip.SlipNumber}");
        sb.AppendLine($"*Date:* {slip.IssueDate:dd MMM yyyy}");
        sb.AppendLine($"--------------------------------");
        sb.AppendLine($"*Karigar / Vendor:* {vendorName}");
        sb.AppendLine($"*Process:* {slip.ProcessName}");
        sb.AppendLine($"*Design / Lot:* {designNo}");
        sb.AppendLine($"*Assigned Quantity:* {slip.TotalQuantity} Pcs");
        if (slip.Rate.HasValue && slip.Rate.Value > 0)
        {
            sb.AppendLine($"*Rate / Pc:* Rs. {slip.Rate.Value:N2}");
            if (slip.TotalAmount.HasValue && slip.TotalAmount.Value > 0)
            {
                sb.AppendLine($"*Total Amount:* Rs. {slip.TotalAmount.Value:N2}");
            }
        }
        if (slip.ExpectedReturnDate.HasValue)
        {
            sb.AppendLine($"*Expected Completion:* {slip.ExpectedReturnDate.Value:dd MMM yyyy}");
        }
        if (!string.IsNullOrWhiteSpace(slip.Remarks))
        {
            sb.AppendLine($"*Remarks:* {slip.Remarks}");
        }
        sb.AppendLine($"--------------------------------");
        sb.AppendLine($"Please inspect the materials upon delivery and verify piece counts.");

        return sb.ToString();
    }

    public string BuildDispatchChallanMessage(DeliveryChallan challan, Company? company)
    {
        var companyName = company?.CompanyName ?? "Kriyex Fashion";
        var customerName = challan.Customer?.CustomerName ?? "Valued Buyer";

        var sb = new StringBuilder();
        sb.AppendLine($"*DISPATCH ADVICE & DELIVERY CHALLAN*");
        sb.AppendLine($"*From:* {companyName}");
        sb.AppendLine($"*Challan No:* {challan.ChallanNumber}");
        sb.AppendLine($"*Date:* {challan.ChallanDate:dd MMM yyyy}");
        sb.AppendLine($"--------------------------------");
        sb.AppendLine($"*Customer:* {customerName}");
        if (challan.SalesOrder != null)
        {
            sb.AppendLine($"*Order Ref:* {challan.SalesOrder.SoNumber}");
        }
        sb.AppendLine($"*Total Pieces Dispatched:* {challan.TotalQuantity} Pcs");
        sb.AppendLine($"*Total Parcels / Boxes:* {challan.NumberOfBoxes}");

        if (!string.IsNullOrWhiteSpace(challan.TransporterName))
        {
            sb.AppendLine($"*Transporter:* {challan.TransporterName}");
        }
        if (!string.IsNullOrWhiteSpace(challan.VehicleNumber))
        {
            sb.AppendLine($"*Vehicle No:* {challan.VehicleNumber}");
        }
        if (!string.IsNullOrWhiteSpace(challan.LrNumber))
        {
            sb.AppendLine($"*LR / Bilty No:* {challan.LrNumber}");
        }
        if (!string.IsNullOrWhiteSpace(challan.EwayBillNumber))
        {
            sb.AppendLine($"*E-Way Bill:* {challan.EwayBillNumber}");
        }
        if (!string.IsNullOrWhiteSpace(challan.ShippingAddress))
        {
            sb.AppendLine($"*Destination:* {challan.ShippingAddress}");
        }
        sb.AppendLine($"--------------------------------");
        sb.AppendLine($"Your consignment is dispatched. Please confirm receipt upon arrival.");

        return sb.ToString();
    }

    public string BuildPaymentReceiptMessage(PaymentReceipt receipt, TaxInvoice? invoice, Company? company)
    {
        var companyName = company?.CompanyName ?? "Kriyex Garments";
        var customerName = receipt.Customer?.CustomerName ?? invoice?.Customer?.CustomerName ?? "Valued Customer";

        var sb = new StringBuilder();
        sb.AppendLine($"*PAYMENT RECEIPT CONFIRMATION*");
        sb.AppendLine($"*From:* {companyName}");
        sb.AppendLine($"*Receipt No:* {receipt.ReceiptNumber}");
        sb.AppendLine($"*Date:* {receipt.PaymentDate:dd MMM yyyy}");
        sb.AppendLine($"--------------------------------");
        sb.AppendLine($"*Customer:* {customerName}");
        sb.AppendLine($"*Amount Received:* Rs. {receipt.Amount:N2}");
        sb.AppendLine($"*Payment Mode:* {receipt.PaymentMode}");
        if (!string.IsNullOrWhiteSpace(receipt.ReferenceNumber))
        {
            sb.AppendLine($"*Ref / UTR No:* {receipt.ReferenceNumber}");
        }
        if (invoice != null)
        {
            sb.AppendLine($"*Applied to Invoice:* {invoice.InvoiceNumber}");
            sb.AppendLine($"*Remaining Invoice Balance:* Rs. {invoice.BalanceDue:N2}");
        }
        sb.AppendLine($"--------------------------------");
        sb.AppendLine($"Thank you for your prompt business settlement!");

        return sb.ToString();
    }

    public string BuildInvoiceSummaryMessage(TaxInvoice invoice, Company? company)
    {
        var companyName = company?.CompanyName ?? "Kriyex Garments";
        var customerName = invoice.Customer?.CustomerName ?? "Valued Customer";

        var sb = new StringBuilder();
        sb.AppendLine($"*TAX INVOICE ADVICE*");
        sb.AppendLine($"*From:* {companyName}");
        sb.AppendLine($"*Invoice No:* {invoice.InvoiceNumber}");
        sb.AppendLine($"*Invoice Date:* {invoice.InvoiceDate:dd MMM yyyy}");
        sb.AppendLine($"*Due Date:* {invoice.DueDate:dd MMM yyyy}");
        sb.AppendLine($"--------------------------------");
        sb.AppendLine($"*Customer:* {customerName}");
        sb.AppendLine($"*Total Amount:* Rs. {invoice.GrandTotal:N2}");
        sb.AppendLine($"*Paid Amount:* Rs. {invoice.PaidAmount:N2}");
        sb.AppendLine($"*Balance Due:* Rs. {invoice.BalanceDue:N2}");
        sb.AppendLine($"--------------------------------");
        sb.AppendLine($"You can view and download your full invoice online on the buyer portal.");

        return sb.ToString();
    }

    public string BuildLeadFollowUpMessage(Lead lead, Company? company)
    {
        var companyName = company?.CompanyName ?? "Kriyex Fashion";
        var sb = new StringBuilder();
        sb.AppendLine($"*Greetings from {companyName}!*");
        sb.AppendLine($"Dear {lead.ContactPerson},");
        sb.AppendLine($"Thank you for connecting with us regarding: *{lead.Title}*.");
        if (lead.EstimatedQuantity > 0)
        {
            sb.AppendLine($"We have noted your interest in ~{lead.EstimatedQuantity} pieces.");
        }
        sb.AppendLine($"Our sales team is happy to assist you with catalogs, swatches, and best pricing.");
        sb.AppendLine($"Please let us know a convenient time for a quick call.");

        return sb.ToString();
    }

    public async Task<(bool Success, string Message)> SendDirectMessageAsync(string phone, string message)
    {
        string normalizedPhone = NormalizePhoneNumber(phone);
        if (string.IsNullOrEmpty(normalizedPhone))
        {
            return (false, "Invalid phone number provided.");
        }

        string? apiToken = _configuration["WhatsApp:ApiToken"];
        string? phoneNumberId = _configuration["WhatsApp:PhoneNumberId"];

        if (string.IsNullOrWhiteSpace(apiToken) || string.IsNullOrWhiteSpace(phoneNumberId))
        {
            // Meta Cloud API is not configured; fallback to wa.me web dispatch
            _logger.LogInformation("WhatsApp Cloud API credentials not configured. Using web wa.me URL generator.");
            return (true, GenerateWhatsAppUrl(phone, message));
        }

        try
        {
            var url = $"https://graph.facebook.com/v18.0/{phoneNumberId}/messages";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);

            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = normalizedPhone,
                type = "text",
                text = new { preview_url = false, body = message }
            };

            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return (true, "Message sent successfully via WhatsApp Cloud API.");
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("WhatsApp Cloud API error: {Error}", errorBody);
            return (false, $"Cloud API error: {response.StatusCode}. Falling back to web link.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message");
            return (false, ex.Message);
        }
    }
}
