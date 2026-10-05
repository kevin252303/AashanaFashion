using System.Net;
using System.Net.Mail;
using System.Text;
using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AashanaFashion.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public EmailService(
        IConfiguration configuration,
        ILogger<EmailService> logger,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<(bool Success, string Message, string? MessageId)> SendEmailAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        string? docType = null,
        int? docId = null,
        string? docRef = null,
        string? sentBy = null,
        byte[]? attachmentBytes = null,
        string? attachmentFileName = null)
    {
        if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains('@'))
        {
            return (false, "Invalid recipient email address.", null);
        }

        string smtpHost = _configuration["Smtp:Host"] ?? string.Empty;
        int smtpPort = int.TryParse(_configuration["Smtp:Port"], out var port) ? port : 587;
        string smtpUser = _configuration["Smtp:Username"] ?? string.Empty;
        string smtpPass = _configuration["Smtp:Password"] ?? string.Empty;
        bool enableSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out var ssl) ? ssl : true;
        string fromEmail = _configuration["Smtp:FromEmail"] ?? "orders@aashanafashion.com";
        string fromName = _configuration["Smtp:FromName"] ?? "Aashana Fashion ERP";

        string messageId = $"MSG-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        bool isSent = false;
        string? error = null;

        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            // Development / Test mode: Record simulated dispatch without failing
            _logger.LogInformation("SMTP Host not configured. Simulating email dispatch to {Email} with Subject '{Subject}'", toEmail, subject);
            isSent = true;
        }
        else
        {
            try
            {
                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Timeout = 15000
                };

                if (!string.IsNullOrWhiteSpace(smtpUser))
                {
                    client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                }

                using var mail = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8
                };

                mail.To.Add(new MailAddress(toEmail, toName));
                mail.Headers.Add("X-Message-ID", messageId);

                if (attachmentBytes != null && !string.IsNullOrWhiteSpace(attachmentFileName))
                {
                    mail.Attachments.Add(new Attachment(new MemoryStream(attachmentBytes), attachmentFileName));
                }

                await client.SendMailAsync(mail);
                isSent = true;
                _logger.LogInformation("Email dispatched successfully to {Email} via SMTP {Host}:{Port}", toEmail, smtpHost, smtpPort);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                error = ex.Message;
                isSent = false;
            }
        }

        // Record in database CommunicationLog
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var log = new CommunicationLog
            {
                DocumentType = docType ?? "General",
                DocumentId = docId ?? 0,
                DocumentReference = docRef ?? string.Empty,
                Channel = CommunicationChannel.Email,
                Recipient = toEmail,
                RecipientName = toName,
                Subject = subject,
                Body = htmlBody,
                Status = isSent ? CommunicationStatus.Sent : CommunicationStatus.Failed,
                ExternalMessageId = messageId,
                ErrorMessage = error,
                SentAt = DateTime.Now,
                SentBy = sentBy ?? "System"
            };

            db.CommunicationLogs.Add(log);
            await db.SaveChangesAsync();
        }
        catch (Exception dbEx)
        {
            _logger.LogWarning(dbEx, "Failed to persist email communication log");
        }

        if (isSent)
        {
            return (true, string.IsNullOrWhiteSpace(smtpHost)
                ? $"Email simulated and logged to {toEmail} (Development Mode)"
                : $"Email successfully dispatched to {toEmail}", messageId);
        }

        return (false, $"Email dispatch failed: {error}", null);
    }

    public string BuildInvoiceEmailHtml(TaxInvoice invoice, Company? company)
    {
        var companyName = company?.CompanyName ?? "Aashana Fashion";
        var companyGst = company?.Gstin ?? "24AABCA1234F1Z9";
        var companyPhone = company?.Phone ?? "+91 98765 43210";
        var companyEmail = company?.Email ?? "billing@aashanafashion.com";
        var customerName = invoice.CustomerName;

        var sb = new StringBuilder();
        sb.AppendLine($@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
    <title>Tax Invoice #{invoice.InvoiceNumber}</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; color: #1e293b; margin: 0; padding: 24px; }}
        .email-container {{ max-width: 640px; margin: 0 auto; background: #ffffff; border-radius: 12px; border: 1px solid #e2e8f0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05); }}
        .header {{ background: #0f172a; color: #ffffff; padding: 28px 32px; }}
        .header h1 {{ margin: 0 0 6px 0; font-size: 22px; font-weight: 700; letter-spacing: -0.5px; }}
        .header p {{ margin: 0; color: #94a3b8; font-size: 14px; }}
        .body {{ padding: 32px; }}
        .greeting {{ font-size: 16px; font-weight: 600; margin-bottom: 12px; }}
        .badge-box {{ background: #f1f5f9; border-left: 4px solid #2563eb; padding: 14px 18px; border-radius: 4px; margin: 20px 0; }}
        .badge-row {{ display: flex; justify-content: space-between; margin-bottom: 6px; font-size: 14px; }}
        .badge-row:last-child {{ margin-bottom: 0; }}
        .label {{ color: #64748b; font-weight: 500; }}
        .value {{ font-weight: 600; color: #0f172a; }}
        table {{ width: 100%; border-collapse: collapse; margin: 24px 0; font-size: 14px; }}
        th {{ background: #f8fafc; border-bottom: 2px solid #e2e8f0; padding: 10px 12px; text-align: left; color: #475569; font-weight: 600; }}
        td {{ border-bottom: 1px solid #f1f5f9; padding: 10px 12px; color: #334155; }}
        .total-box {{ background: #fafafa; border: 1px solid #e5e7eb; border-radius: 8px; padding: 16px; margin: 20px 0; }}
        .total-line {{ display: flex; justify-content: space-between; padding: 4px 0; font-size: 14px; }}
        .grand-total {{ font-size: 18px; font-weight: 700; color: #0f172a; border-top: 2px solid #e2e8f0; margin-top: 8px; padding-top: 8px; }}
        .footer {{ background: #f8fafc; border-top: 1px solid #e2e8f0; padding: 20px 32px; font-size: 12px; color: #64748b; text-align: center; }}
    </style>
</head>
<body>
    <div class='email-container'>
        <div class='header'>
            <h1>{companyName}</h1>
            <p>Tax Invoice Advice & Payment Schedule · GSTIN: {companyGst}</p>
        </div>
        <div class='body'>
            <div class='greeting'>Dear {customerName},</div>
            <p>Thank you for choosing <strong>{companyName}</strong>. Please find the details of your official GST Tax Invoice below.</p>
            
            <div class='badge-box'>
                <div class='badge-row'><span class='label'>Invoice Number:</span><span class='value'>{invoice.InvoiceNumber}</span></div>
                <div class='badge-row'><span class='label'>Invoice Date:</span><span class='value'>{invoice.InvoiceDate:dd MMM yyyy}</span></div>
                <div class='badge-row'><span class='label'>Payment Due Date:</span><span class='value'>{invoice.DueDate:dd MMM yyyy}</span></div>
                {(string.IsNullOrWhiteSpace(invoice.EwayBillNumber) ? "" : $"<div class='badge-row'><span class='label'>E-Way Bill:</span><span class='value'>{invoice.EwayBillNumber}</span></div>")}
            </div>

            <table>
                <thead>
                    <tr>
                        <th>Item / Description</th>
                        <th style='text-align:center;'>Qty (Pcs)</th>
                        <th style='text-align:right;'>Rate (₹)</th>
                        <th style='text-align:right;'>Amount (₹)</th>
                    </tr>
                </thead>
                <tbody>");

        foreach (var item in invoice.Items)
        {
            var itemDesc = !string.IsNullOrWhiteSpace(item.Description) ? item.Description : item.Design?.DesignNumber ?? "Garment Item";
            sb.AppendLine($@"<tr>
                <td><strong>{itemDesc}</strong> {(item.Colour != null ? $"· {item.Colour}" : "")} {(item.Size != null ? $"· {item.Size}" : "")}</td>
                <td style='text-align:center;'>{item.Quantity}</td>
                <td style='text-align:right;'>₹{item.UnitPrice:N2}</td>
                <td style='text-align:right;'>₹{item.TaxableValue:N2}</td>
            </tr>");
        }

        sb.AppendLine($@"</tbody>
            </table>

            <div class='total-box'>
                <div class='total-line'><span class='label'>Taxable Subtotal:</span><span class='value'>₹{invoice.TaxableAmount:N2}</span></div>
                <div class='total-line'><span class='label'>Total GST (CGST+SGST/IGST):</span><span class='value'>₹{(invoice.CgstAmount + invoice.SgstAmount + invoice.IgstAmount):N2}</span></div>
                <div class='total-line grand-total'><span class='label'>Grand Total:</span><span class='value'>₹{invoice.GrandTotal:N2}</span></div>
                <div class='total-line' style='margin-top:6px; color:#16a34a;'><span class='label'>Paid / Settled:</span><span class='value'>₹{invoice.PaidAmount:N2}</span></div>
                <div class='total-line' style='font-size:16px; font-weight:700; color:#dc2626;'><span class='label'>Balance Due:</span><span class='value'>₹{invoice.BalanceDue:N2}</span></div>
            </div>

            <p style='font-size:13px; color:#64748b;'>You can access and verify your invoice, payment receipts, and delivery notes in your B2B Customer Portal account.</p>
        </div>
        <div class='footer'>
            <p>{companyName} · {companyPhone} · {companyEmail}<br/>This is an automated transaction advice generated by Aashana Fashion ERP.</p>
        </div>
    </div>
</body>
</html>");

        return sb.ToString();
    }

    public string BuildSalesOrderEmailHtml(SalesOrder order, Company? company)
    {
        var companyName = company?.CompanyName ?? "Aashana Fashion";
        var customerName = order.Customer?.CustomerName ?? "Valued Buyer";
        var totalQty = order.Details.Sum(d => d.Quantity);
        var totalAmount = order.Details.Sum(d => d.TotalPrice);

        var sb = new StringBuilder();
        sb.AppendLine($@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
    <title>Sales Order Confirmation #{order.SoNumber}</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; color: #1e293b; margin: 0; padding: 24px; }}
        .email-container {{ max-width: 640px; margin: 0 auto; background: #ffffff; border-radius: 12px; border: 1px solid #e2e8f0; overflow: hidden; }}
        .header {{ background: #0f172a; color: #ffffff; padding: 28px 32px; }}
        .body {{ padding: 32px; }}
        .badge-box {{ background: #f8fafc; border-left: 4px solid #16a34a; padding: 14px 18px; border-radius: 4px; margin: 20px 0; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; font-size: 14px; }}
        th {{ background: #f8fafc; border-bottom: 2px solid #e2e8f0; padding: 10px 12px; text-align: left; }}
        td {{ border-bottom: 1px solid #f1f5f9; padding: 10px 12px; }}
        .footer {{ background: #f8fafc; border-top: 1px solid #e2e8f0; padding: 20px 32px; font-size: 12px; color: #64748b; text-align: center; }}
    </style>
</head>
<body>
    <div class='email-container'>
        <div class='header'>
            <h1 style='margin:0 0 6px 0; font-size:22px;'>{companyName}</h1>
            <p style='margin:0; color:#94a3b8;'>Sales Order Confirmation #{order.SoNumber}</p>
        </div>
        <div class='body'>
            <p>Dear <strong>{customerName}</strong>,</p>
            <p>Your order has been confirmed and scheduled for production & fulfillment.</p>
            <div class='badge-box'>
                <div><strong>Order No:</strong> {order.SoNumber}</div>
                <div><strong>Order Date:</strong> {order.OrderDate:dd MMM yyyy}</div>
                <div><strong>Total Pieces:</strong> {totalQty} Pcs</div>
                <div><strong>Estimated Value:</strong> ₹{totalAmount:N2}</div>
            </div>
            <table>
                <thead>
                    <tr><th>Design / Item</th><th style='text-align:center;'>Qty</th><th style='text-align:right;'>Price</th></tr>
                </thead>
                <tbody>");

        foreach (var item in order.Details)
        {
            sb.AppendLine($"<tr><td><strong>{item.Design?.DesignNumber ?? item.DesignNumber}</strong></td><td style='text-align:center;'>{item.Quantity} pcs</td><td style='text-align:right;'>₹{item.TotalPrice:N2}</td></tr>");
        }

        sb.AppendLine($@"</tbody>
            </table>
            <p>You can track cutting, stitching, and dispatch progress online anytime.</p>
        </div>
        <div class='footer'>
            <p>{companyName} · Garment Manufacturing & B2B Solutions</p>
        </div>
    </div>
</body>
</html>");

        return sb.ToString();
    }

    public string BuildDispatchChallanEmailHtml(DeliveryChallan challan, Company? company)
    {
        var companyName = company?.CompanyName ?? "Aashana Fashion";
        var customerName = challan.Customer?.CustomerName ?? "Valued Buyer";

        var sb = new StringBuilder();
        sb.AppendLine($@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
    <title>Dispatch Advice — Challan #{challan.ChallanNumber}</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; color: #1e293b; margin: 0; padding: 24px; }}
        .email-container {{ max-width: 640px; margin: 0 auto; background: #ffffff; border-radius: 12px; border: 1px solid #e2e8f0; overflow: hidden; }}
        .header {{ background: #0f172a; color: #ffffff; padding: 28px 32px; }}
        .body {{ padding: 32px; }}
        .info-card {{ background: #eff6ff; border-left: 4px solid #3b82f6; padding: 16px; border-radius: 4px; margin: 20px 0; font-size: 14px; line-height: 1.6; }}
        .footer {{ background: #f8fafc; border-top: 1px solid #e2e8f0; padding: 20px 32px; font-size: 12px; color: #64748b; text-align: center; }}
    </style>
</head>
<body>
    <div class='email-container'>
        <div class='header'>
            <h1 style='margin:0 0 6px 0; font-size:22px;'>{companyName}</h1>
            <p style='margin:0; color:#94a3b8;'>Dispatch Advice & Delivery Challan #{challan.ChallanNumber}</p>
        </div>
        <div class='body'>
            <p>Dear <strong>{customerName}</strong>,</p>
            <p>Your order consignment has been dispatched from our warehouse.</p>
            <div class='info-card'>
                <div><strong>Delivery Challan:</strong> {challan.ChallanNumber}</div>
                <div><strong>Dispatch Date:</strong> {challan.ChallanDate:dd MMM yyyy, HH:mm}</div>
                <div><strong>Total Pieces Dispatched:</strong> {challan.TotalQuantity} Pcs ({challan.NumberOfBoxes} Boxes)</div>
                <div><strong>Transporter:</strong> {challan.TransporterName ?? "Direct Vehicle"}</div>
                <div><strong>Vehicle Number:</strong> {challan.VehicleNumber ?? "N/A"}</div>
                <div><strong>LR / Bilty Number:</strong> {challan.LrNumber ?? "N/A"}</div>
                {(string.IsNullOrWhiteSpace(challan.EwayBillNumber) ? "" : $"<div><strong>E-Way Bill:</strong> {challan.EwayBillNumber}</div>")}
                {(string.IsNullOrWhiteSpace(challan.ShippingAddress) ? "" : $"<div><strong>Destination:</strong> {challan.ShippingAddress}</div>")}
            </div>
            <p>Please inspect packages upon arrival and notify us immediately of any discrepancies.</p>
        </div>
        <div class='footer'>
            <p>{companyName} Logistics Department</p>
        </div>
    </div>
</body>
</html>");

        return sb.ToString();
    }

    public string BuildPaymentReceiptEmailHtml(PaymentReceipt receipt, TaxInvoice? invoice, Company? company)
    {
        var companyName = company?.CompanyName ?? "Aashana Fashion";
        var customerName = receipt.Customer?.CustomerName ?? invoice?.Customer?.CustomerName ?? "Valued Customer";

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
    <title>Payment Receipt #{receipt.ReceiptNumber}</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; color: #1e293b; margin: 0; padding: 24px; }}
        .email-container {{ max-width: 640px; margin: 0 auto; background: #ffffff; border-radius: 12px; border: 1px solid #e2e8f0; overflow: hidden; }}
        .header {{ background: #0f172a; color: #ffffff; padding: 28px 32px; }}
        .body {{ padding: 32px; }}
        .receipt-card {{ background: #f0fdf4; border-left: 4px solid #16a34a; padding: 18px; border-radius: 4px; margin: 20px 0; font-size: 14px; line-height: 1.7; }}
        .footer {{ background: #f8fafc; border-top: 1px solid #e2e8f0; padding: 20px 32px; font-size: 12px; color: #64748b; text-align: center; }}
    </style>
</head>
<body>
    <div class='email-container'>
        <div class='header'>
            <h1 style='margin:0 0 6px 0; font-size:22px;'>{companyName}</h1>
            <p style='margin:0; color:#94a3b8;'>Payment Receipt Voucher #{receipt.ReceiptNumber}</p>
        </div>
        <div class='body'>
            <p>Dear <strong>{customerName}</strong>,</p>
            <p>We gratefully acknowledge receipt of your payment.</p>
            <div class='receipt-card'>
                <div><strong>Receipt No:</strong> {receipt.ReceiptNumber}</div>
                <div><strong>Payment Date:</strong> {receipt.PaymentDate:dd MMM yyyy}</div>
                <div><strong>Amount Received:</strong> ₹{receipt.Amount:N2}</div>
                <div><strong>Payment Mode:</strong> {receipt.PaymentMode}</div>
                {(string.IsNullOrWhiteSpace(receipt.ReferenceNumber) ? "" : $"<div><strong>Reference / UTR:</strong> {receipt.ReferenceNumber}</div>")}
                {(invoice != null ? $"<div><strong>Invoice Reference:</strong> {invoice.InvoiceNumber} | <strong>Remaining Balance:</strong> ₹{invoice.BalanceDue:N2}</div>" : "")}
            </div>
            <p>Thank you for your valued partnership.</p>
        </div>
        <div class='footer'>
            <p>{companyName} Accounts & Billing</p>
        </div>
    </div>
</body>
</html>";
    }
}
