using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Controllers;

[Authorize]
public class PMSController : Controller
{
    private readonly AppDbContext _context;

    public PMSController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var orders = await _context.ProductionOrders
            .Include(p => p.Design)
            .Include(p => p.Details)
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();

        var vm = new PMSDashboardViewModel
        {
            TotalOrders = orders.Count,
            InProgress = orders.Count(p => p.Status != OrderStatus.Dispatched && p.Status != OrderStatus.ReadyToDispatch),
            ReadyToDispatch = orders.Count(p => p.Status == OrderStatus.ReadyToDispatch),
            Dispatched = orders.Count(p => p.Status == OrderStatus.Dispatched),
            Orders = orders
        };

        return View(vm);
    }

    public async Task<IActionResult> ProductionEntities(int? orderId, string? entityType, string? status, string? colour)
    {
        var query = _context.ProductionEntities
            .Include(e => e.ProductionOrder)
            .ThenInclude(p => p!.Design)
            .Include(e => e.ProcessTrackings)
            .AsQueryable();

        if (orderId.HasValue)
            query = query.Where(e => e.ProductionOrderId == orderId.Value);
        if (!string.IsNullOrEmpty(entityType) && entityType != "All")
            query = query.Where(e => e.EntityType == entityType);
        if (!string.IsNullOrEmpty(status) && status != "All")
            query = query.Where(e => e.Status == status);
        if (!string.IsNullOrEmpty(colour) && colour != "All")
            query = query.Where(e => e.Colour == colour);

        var entities = await query.OrderByDescending(e => e.CreatedDate).ToListAsync();
        var orders = await _context.ProductionOrders.Include(p => p.Design).ToListAsync();
        var colours = entities.Select(e => e.Colour).Distinct().ToList();

        ViewBag.Orders = orders;
        ViewBag.SelectedOrderId = orderId;
        ViewBag.SelectedEntityType = entityType ?? "All";
        ViewBag.SelectedStatus = status ?? "All";
        ViewBag.SelectedColour = colour ?? "All";
        ViewBag.Colours = colours;
        ViewBag.EntityTypes = new[] { "Chaniya", "Choli", "Blouse", "Duppata" };
        ViewBag.Statuses = new[] { "Created", "AtDying", "AtRoll", "AtHandwork", "AtStitching", "Completed", "Dispatched" };

        return View(entities);
    }

    public async Task<IActionResult> ProcessTracking(int? entityId, int? orderId, string? processName)
    {
        var query = _context.ProcessTrackings
            .Include(p => p.ProductionEntity)
            .ThenInclude(e => e!.ProductionOrder)
            .ThenInclude(o => o!.Design)
            .AsQueryable();

        if (entityId.HasValue)
            query = query.Where(p => p.ProductionEntityId == entityId.Value);
        if (!string.IsNullOrEmpty(processName) && processName != "All")
            query = query.Where(p => p.ProcessName == processName);
        if (orderId.HasValue)
            query = query.Where(p => p.ProductionEntity!.ProductionOrderId == orderId.Value);

        var trackings = await query.OrderByDescending(p => p.GivenDate).ToListAsync();
        var entities = await _context.ProductionEntities.Include(e => e.ProductionOrder).ToListAsync();

        ViewBag.Entities = entities;
        ViewBag.SelectedEntityId = entityId;
        ViewBag.SelectedOrderId = orderId;
        ViewBag.SelectedProcessName = processName ?? "All";
        ViewBag.ProcessNames = new[] { "Dying", "Roll", "Handwork", "Stitching" };

        var vm = new ProcessTrackingViewModel
        {
            Trackings = trackings,
            DelayedTrackings = trackings.Where(t => t.DaysLate > 0).ToList()
        };

        return View(vm);
    }

    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GenerateEntities(int orderId)
    {
        var order = await _context.ProductionOrders
            .Include(p => p.Details)
            .Include(p => p.Design)
            .FirstOrDefaultAsync(p => p.Id == orderId);

        if (order == null) return NotFound();

        var creationSteps = order.Design?.GetCreationSteps() ?? new List<string>();
        int slNo = 1;

        foreach (var detail in order.Details)
        {
            var colours = detail.Colour.Split(',').Select(c => c.Trim()).ToList();
            var sizes = detail.Size.Split(',').Select(s => s.Trim()).ToList();

            for (int i = 0; i < detail.Quantity; i++)
            {
                foreach (var colour in colours)
                {
                    foreach (var size in sizes)
                    {
                        var entity = new ProductionEntity
                        {
                            ProductionOrderId = order.Id,
                            EntityType = "Chaniya", // Default, can be changed
                            Colour = colour,
                            Size = size,
                            SlNo = slNo++,
                            Status = "Created"
                        };
                        _context.ProductionEntities.Add(entity);
                    }
                }
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Generated production entities for order {order.LotNo}";
        return RedirectToAction(nameof(ProductionEntities), new { orderId });
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    public async Task<IActionResult> SendForProcess(int entityId, string processName, DateTime expectedReturn)
    {
        var entity = await _context.ProductionEntities.FindAsync(entityId);
        if (entity == null) return NotFound();

        var tracking = new ProcessTracking
        {
            ProductionEntityId = entityId,
            ProcessName = processName,
            GivenDate = DateTime.Today,
            ExpectedReturnDate = expectedReturn
        };

        _context.ProcessTrackings.Add(tracking);

        // Update entity status based on process
        entity.Status = processName switch
        {
            "Dying" => "AtDying",
            "Roll" => "AtRoll",
            "Handwork" => "AtHandwork",
            "Stitching" => "AtStitching",
            _ => entity.Status
        };

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Sent for {processName}";
        return RedirectToAction(nameof(ProcessTracking));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost]
    public async Task<IActionResult> MarkReturned(int trackingId)
    {
        var tracking = await _context.ProcessTrackings
            .Include(t => t.ProductionEntity)
            .FirstOrDefaultAsync(t => t.Id == trackingId);

        if (tracking == null) return NotFound();

        tracking.ActualReturnDate = DateTime.Today;

        // Check if all processes are complete
        var entity = tracking.ProductionEntity;
        if (entity != null)
        {
            var allComplete = entity.ProcessTrackings.All(t => t.ActualReturnDate.HasValue);
            if (allComplete)
                entity.Status = "Completed";
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Marked as returned";
        return RedirectToAction(nameof(ProcessTracking));
    }

    public async Task<IActionResult> ExportEntities(int? orderId, string? entityType, string? status, string? colour)
    {
        var query = _context.ProductionEntities
            .Include(e => e.ProductionOrder)
            .ThenInclude(p => p!.Design)
            .Include(e => e.ProcessTrackings)
            .AsQueryable();

        if (orderId.HasValue)
            query = query.Where(e => e.ProductionOrderId == orderId.Value);
        if (!string.IsNullOrEmpty(entityType) && entityType != "All")
            query = query.Where(e => e.EntityType == entityType);
        if (!string.IsNullOrEmpty(status) && status != "All")
            query = query.Where(e => e.Status == status);
        if (!string.IsNullOrEmpty(colour) && colour != "All")
            query = query.Where(e => e.Colour == colour);

        var entities = await query.OrderByDescending(e => e.CreatedDate).ToListAsync();

        var csv = "Sl No,Order,Lot No,Design,Entity Type,Colour,Size,Status,Created Date\n";
        foreach (var e in entities)
        {
            csv += $"{e.SlNo},{e.ProductionOrder?.LotNo},{e.ProductionOrder?.Design?.DesignNumber},{e.EntityType},{e.Colour},{e.Size},{e.Status},{e.CreatedDate:dd/MM/yyyy}\n";
        }

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "ProductionEntities.csv");
    }

    public async Task<IActionResult> ExportProcessTracking(int? orderId, string? processName)
    {
        var query = _context.ProcessTrackings
            .Include(p => p.ProductionEntity)
            .ThenInclude(e => e!.ProductionOrder)
            .ThenInclude(o => o!.Design)
            .AsQueryable();

        if (orderId.HasValue)
            query = query.Where(p => p.ProductionEntity!.ProductionOrderId == orderId.Value);
        if (!string.IsNullOrEmpty(processName) && processName != "All")
            query = query.Where(p => p.ProcessName == processName);

        var trackings = await query.OrderByDescending(p => p.GivenDate).ToListAsync();

        var csv = "Sl No,Lot No,Design,Entity,Colour,Size,Process,Given Date,Expected Return,Actual Return,Days Late,Status\n";
        foreach (var t in trackings)
        {
            csv += $"{t.ProductionEntity?.SlNo},{t.ProductionEntity?.ProductionOrder?.LotNo},{t.ProductionEntity?.ProductionOrder?.Design?.DesignNumber},{t.ProductionEntity?.EntityType},{t.ProductionEntity?.Colour},{t.ProductionEntity?.Size},{t.ProcessName},{t.GivenDate?.ToString("dd/MM/yyyy")},{t.ExpectedReturnDate?.ToString("dd/MM/yyyy")},{t.ActualReturnDate?.ToString("dd/MM/yyyy")},{t.DaysLate ?? 0},{(t.IsComplete ? "Complete" : "Pending")}\n";
        }

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "ProcessTracking.csv");
    }
}

public class PMSDashboardViewModel
{
    public int TotalOrders { get; set; }
    public int InProgress { get; set; }
    public int ReadyToDispatch { get; set; }
    public int Dispatched { get; set; }
    public List<ProductionOrder> Orders { get; set; } = new();
}

public class ProcessTrackingViewModel
{
    public List<ProcessTracking> Trackings { get; set; } = new();
    public List<ProcessTracking> DelayedTrackings { get; set; } = new();
}
