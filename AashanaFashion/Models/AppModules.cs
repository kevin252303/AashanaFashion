using System;
using System.Collections.Generic;
using System.Linq;

namespace AashanaFashion.Models;

public record ModuleDefinition(
    string Key,
    string Name,
    string Description,
    string Icon,
    string Group,
    string Badge = "Standard"
);

public static class AppModules
{
    public const string Sales = "Sales";
    public const string Purchase = "Purchase";
    public const string Crm = "CRM";
    public const string Masters = "Masters";
    public const string Inventory = "Inventory";
    public const string JobWork = "JobWork";
    public const string Production = "Production";
    public const string QualityControl = "QualityControl";
    public const string Barcode = "Barcode";
    public const string Hr = "HR";
    public const string Accounting = "Accounting";
    public const string CustomerPortal = "CustomerPortal";

    public static readonly List<ModuleDefinition> All = new()
    {
        new(Sales, "Sales & orders", "Sales orders, delivery challans, pricing and proforma invoices.", "bi-bag", "Commercial"),
        new(Purchase, "Purchase management", "Purchase orders, supplier tracking and inward verification.", "bi-cart3", "Commercial"),
        new(Crm, "CRM & leads", "Lead pipeline, stage Kanban, customer inquiries and conversion.", "bi-funnel", "Commercial"),
        new(Masters, "Admin masters", "Product designs, categories, customers, vendors, pricelists, colours, sizes and companies.", "bi-database", "Master data"),
        new(Inventory, "Inventory & stock", "Raw materials, fabric ledger, ready goods stock and outward dispatch.", "bi-boxes", "Inventory"),
        new(JobWork, "Job work entry", "Third-party job work vouchers for fabric dying and roll pressing.", "bi-tools", "Manufacturing"),
        new(Production, "Production & PMS", "Production lots, cutting, stitching, job slips, WIP dashboard and tracking.", "bi-diagram-3", "Manufacturing"),
        new(QualityControl, "Quality control (QC)", "Fabric and garment QC inspection, defect logging and audits.", "bi-check-all", "Manufacturing"),
        new(Barcode, "Barcode & scanner", "Barcode scanning, lot labels and tag configuration.", "bi-upc-scan", "Manufacturing"),
        new(Hr, "HR & payroll", "Staff directory, biometric and face attendance kiosk, monthly grid and salary slips.", "bi-person-badge", "Workforce"),
        new(Accounting, "Finance & accounts", "Tax invoices, double-entry general ledger, balance sheet, GST returns and CMS payments.", "bi-receipt", "Finance"),
        new(CustomerPortal, "B2B buyer portal", "Self-service customer portal for order tracking, challan downloads and invoices.", "bi-globe", "Portals")
    };

    public static string AllModulesCommaSeparated => string.Join(",", All.Select(m => m.Key));

    public static string? GetModuleForController(string controllerName) => controllerName switch
    {
        "SalesOrder" => Sales,
        "Purchase" => Purchase,
        "Lead" => Crm,
        "Design" or "Archive" or "Category" or "Customer" or "Vendor" or "Pricelist" or "Colour" or "Size" or "ProcessMaster" or "Company" => Masters,
        "RawMaterial" or "ReadyProduct" => Inventory,
        "Dying" or "RollPress" => JobWork,
        "Production" or "PMS" or "JobSlip" => Production,
        "QualityControl" => QualityControl,
        "Barcode" => Barcode,
        "Employee" or "Attendance" or "Salary" or "BiometricDevice" => Hr,
        "Invoice" or "Accounting" or "GstReturn" or "BankPayment" => Accounting,
        "CustomerPortal" => CustomerPortal,
        _ => null
    };
}
