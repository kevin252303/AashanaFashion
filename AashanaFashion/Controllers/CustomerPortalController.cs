using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AashanaFashion.Controllers;

[Authorize(Roles = "Customer,Admin")]
public class CustomerPortalController : Controller
{
    private readonly AppDbContext _context;

    public CustomerPortalController(AppDbContext context)
    {
        _context = context;
    }

    private async Task<Customer?> GetCurrentCustomerAsync(int? overrideCustomerId = null)
    {
        if (User.IsInRole("Admin") && overrideCustomerId.HasValue)
        {
            return await _context.Customers.FindAsync(overrideCustomerId.Value);
        }

        var customerIdClaim = User.FindFirst("CustomerId")?.Value;
        if (!string.IsNullOrEmpty(customerIdClaim) && int.TryParse(customerIdClaim, out var customerId))
        {
            return await _context.Customers.FindAsync(customerId);
        }

        // Fallback: look up user by username
        var username = User.Identity?.Name;
        if (!string.IsNullOrEmpty(username))
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user?.CustomerId != null)
            {
                return await _context.Customers.FindAsync(user.CustomerId.Value);
            }
        }

        // If admin with no specific customer specified, get first customer
        if (User.IsInRole("Admin"))
        {
            return await _context.Customers.FirstOrDefaultAsync();
        }

        return null;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? customerId = null)
    {
        var customer = await GetCurrentCustomerAsync(customerId);
        if (customer == null)
        {
            return View("NoCustomerLinked");
        }

        // Load metrics & summary data
        var orders = await _context.SalesOrders
            .Where(o => o.CustomerId == customer.Id)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var invoices = await _context.TaxInvoices
            .Where(i => i.CustomerId == customer.Id)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        var challans = await _context.DeliveryChallans
            .Include(c => c.Items)
            .Where(c => c.CustomerId == customer.Id)
            .OrderByDescending(c => c.ChallanDate)
            .ToListAsync();

        ViewBag.Customer = customer;
        ViewBag.TotalOrders = orders.Count;
        ViewBag.ActiveOrders = orders.Count(o => o.Status != SalesOrderStatus.Cancelled && o.Status != SalesOrderStatus.Dispatched);
        ViewBag.TotalInvoiced = invoices.Sum(i => i.GrandTotal);
        ViewBag.OutstandingBalance = invoices.Sum(i => i.BalanceDue);
        ViewBag.TotalDispatchedPcs = challans.Sum(c => c.TotalQuantity);

        ViewBag.RecentOrders = orders.Take(5).ToList();
        ViewBag.RecentInvoices = invoices.Take(5).ToList();
        ViewBag.RecentChallans = challans.Take(5).ToList();

        return View(customer);
    }

    [HttpGet]
    public async Task<IActionResult> Orders(string? search, SalesOrderStatus? status, int? customerId = null)
    {
        var customer = await GetCurrentCustomerAsync(customerId);
        if (customer == null) return View("NoCustomerLinked");

        var query = _context.SalesOrders
            .Include(o => o.Details)
            .Where(o => o.CustomerId == customer.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(o => o.SoNumber.Contains(search)
                || (o.CustomerPoReference != null && o.CustomerPoReference.Contains(search)));
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        ViewBag.Customer = customer;
        ViewBag.Search = search;
        ViewBag.Status = status;

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> OrderDetails(int id, int? customerId = null)
    {
        var customer = await GetCurrentCustomerAsync(customerId);
        if (customer == null) return View("NoCustomerLinked");

        var order = await _context.SalesOrders
            .Include(o => o.Details)
            .Include(o => o.Challans)
                .ThenInclude(c => c.Items)
            .FirstOrDefaultAsync(o => o.Id == id && (o.CustomerId == customer.Id || User.IsInRole("Admin")));

        if (order == null) return NotFound();

        ViewBag.Customer = customer;
        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> Invoices(string? search, int? customerId = null)
    {
        var customer = await GetCurrentCustomerAsync(customerId);
        if (customer == null) return View("NoCustomerLinked");

        var query = _context.TaxInvoices
            .Include(i => i.Items)
            .Where(i => i.CustomerId == customer.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(i => i.InvoiceNumber.Contains(search));
        }

        var invoices = await query.OrderByDescending(i => i.InvoiceDate).ToListAsync();
        ViewBag.Customer = customer;
        ViewBag.Search = search;

        return View(invoices);
    }

    [HttpGet]
    public async Task<IActionResult> Challans(string? search, int? customerId = null)
    {
        var customer = await GetCurrentCustomerAsync(customerId);
        if (customer == null) return View("NoCustomerLinked");

        var query = _context.DeliveryChallans
            .Include(c => c.SalesOrder)
            .Include(c => c.Items)
            .Where(c => c.CustomerId == customer.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c => c.ChallanNumber.Contains(search)
                || (c.VehicleNumber != null && c.VehicleNumber.Contains(search))
                || (c.LrNumber != null && c.LrNumber.Contains(search)));
        }

        var challans = await query.OrderByDescending(c => c.ChallanDate).ToListAsync();
        ViewBag.Customer = customer;
        ViewBag.Search = search;

        return View(challans);
    }

    [HttpGet]
    public async Task<IActionResult> Profile(int? customerId = null)
    {
        var customer = await GetCurrentCustomerAsync(customerId);
        if (customer == null) return View("NoCustomerLinked");

        ViewBag.Customer = customer;
        return View(customer);
    }
}
