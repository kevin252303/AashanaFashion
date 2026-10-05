using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AashanaFashion.Data;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public class PmsSyncService : IPmsSyncService
{
    private readonly AppDbContext _context;

    public PmsSyncService(AppDbContext context)
    {
        _context = context;
    }

    public async Task SyncOrderTrackingAsync(int orderId)
    {
        var order = await _context.ProductionOrders
            .Include(p => p.Details)
            .Include(p => p.ComponentAssignments)
            .FirstOrDefaultAsync(p => p.Id == orderId);

        if (order == null) return;

        // 1. Ensure production entities exist for the order
        var entities = await _context.ProductionEntities
            .Include(e => e.ProcessTrackings)
            .Where(e => e.ProductionOrderId == order.Id)
            .ToListAsync();

        if (!entities.Any())
        {
            var initEntityStatus = order.Status switch
            {
                OrderStatus.AtDying => "AtDying",
                OrderStatus.AtHandwork => "AtHandwork",
                OrderStatus.AtStitching => "AtStitching",
                OrderStatus.ReadyToDispatch => "Completed",
                OrderStatus.Dispatched => "Dispatched",
                _ => "Created"
            };

            int sl = 1;
            if (order.Details != null && order.Details.Any())
            {
                foreach (var d in order.Details)
                {
                    for (int i = 0; i < d.Quantity; i++)
                    {
                        var entity = new ProductionEntity
                        {
                            ProductionOrderId = order.Id,
                            EntityType = "Garment",
                            Colour = d.Colour,
                            Size = d.Size,
                            SlNo = sl,
                            Barcode = BarcodeService.FormatEntityBarcode(order.Id, sl),
                            Status = initEntityStatus,
                            CreatedDate = DateTime.Now
                        };
                        _context.ProductionEntities.Add(entity);
                        entities.Add(entity);
                        sl++;
                    }
                }
            }
            else
            {
                int qty = order.TotalQuantity > 0 ? order.TotalQuantity : 1;
                for (int i = 0; i < qty; i++)
                {
                    var entity = new ProductionEntity
                    {
                        ProductionOrderId = order.Id,
                        EntityType = "Garment",
                        Colour = "Standard",
                        Size = "Free Size",
                        SlNo = sl,
                        Barcode = BarcodeService.FormatEntityBarcode(order.Id, sl),
                        Status = initEntityStatus,
                        CreatedDate = DateTime.Now
                    };
                    _context.ProductionEntities.Add(entity);
                    entities.Add(entity);
                    sl++;
                }
            }

            if (entities.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        // 2. Synchronize entity status with order status
        var currentEntityStatus = order.Status switch
        {
            OrderStatus.ReadyToDispatch => "Completed",
            OrderStatus.Dispatched => "Dispatched",
            OrderStatus.AtDying => "AtDying",
            OrderStatus.AtHandwork => "AtHandwork",
            OrderStatus.AtStitching => "AtStitching",
            _ => string.IsNullOrWhiteSpace(order.CurrentProcess) ? "Created" : order.CurrentProcess
        };

        foreach (var ent in entities)
        {
            ent.Status = currentEntityStatus;
        }

        // 3. Determine active process name if order is in a production stage
        string? activeProcess = null;
        if (order.Status == OrderStatus.AtHandwork || (!string.IsNullOrEmpty(order.CurrentProcess) && order.CurrentProcess.Contains("Handwork", StringComparison.OrdinalIgnoreCase)))
        {
            activeProcess = "Handwork";
        }
        else if (order.Status == OrderStatus.AtStitching || (!string.IsNullOrEmpty(order.CurrentProcess) && order.CurrentProcess.Contains("Stitching", StringComparison.OrdinalIgnoreCase)))
        {
            activeProcess = "Stitching";
        }
        else if (order.Status == OrderStatus.AtDying || (!string.IsNullOrEmpty(order.CurrentProcess) && order.CurrentProcess.Contains("Dying", StringComparison.OrdinalIgnoreCase)))
        {
            activeProcess = "Dying";
        }

        if (activeProcess != null)
        {
            // Identify subcontractor / vendor
            int? vendorId = null;
            if (activeProcess == "Handwork")
            {
                vendorId = order.HandworkWorkerId 
                    ?? order.ComponentAssignments?.FirstOrDefault(a => a.ProcessName.Contains("Hand", StringComparison.OrdinalIgnoreCase))?.VendorId;
            }
            else if (activeProcess == "Stitching")
            {
                vendorId = order.StitchingWorkerId 
                    ?? order.ComponentAssignments?.FirstOrDefault(a => a.ProcessName.Contains("Stitch", StringComparison.OrdinalIgnoreCase))?.VendorId;
            }
            else if (activeProcess == "Dying")
            {
                vendorId = order.ComponentAssignments?.FirstOrDefault(a => a.ProcessName.Contains("Dye", StringComparison.OrdinalIgnoreCase))?.VendorId;
            }

            if (!vendorId.HasValue && order.ComponentAssignments != null)
            {
                vendorId = order.ComponentAssignments.FirstOrDefault(a => string.Equals(a.ProcessName, activeProcess, StringComparison.OrdinalIgnoreCase))?.VendorId;
            }

            var expectedDate = DateTime.Today.AddDays(3);

            foreach (var ent in entities)
            {
                // Close prior different processes if still open
                if (ent.ProcessTrackings != null)
                {
                    foreach (var prior in ent.ProcessTrackings.Where(t => !string.Equals(t.ProcessName, activeProcess, StringComparison.OrdinalIgnoreCase) && !t.ActualReturnDate.HasValue))
                    {
                        prior.ActualReturnDate = DateTime.Today;
                    }
                }

                // Check if active tracking for this process already exists
                var existingTracking = ent.ProcessTrackings?
                    .FirstOrDefault(t => string.Equals(t.ProcessName, activeProcess, StringComparison.OrdinalIgnoreCase) && !t.ActualReturnDate.HasValue);

                if (existingTracking == null)
                {
                    var newTracking = new ProcessTracking
                    {
                        ProductionEntityId = ent.Id,
                        ProcessName = activeProcess,
                        VendorId = vendorId,
                        GivenDate = DateTime.Today,
                        ExpectedReturnDate = expectedDate,
                        CreatedDate = DateTime.Now
                    };
                    _context.ProcessTrackings.Add(newTracking);
                    ent.ProcessTrackings?.Add(newTracking);
                }
                else if (!existingTracking.VendorId.HasValue && vendorId.HasValue)
                {
                    existingTracking.VendorId = vendorId;
                }
            }
        }
        else if (order.Status == OrderStatus.ReadyToDispatch || order.Status == OrderStatus.Dispatched)
        {
            // Close any still open process trackings for this order
            foreach (var ent in entities)
            {
                if (ent.ProcessTrackings != null)
                {
                    foreach (var t in ent.ProcessTrackings.Where(x => !x.ActualReturnDate.HasValue))
                    {
                        t.ActualReturnDate = DateTime.Today;
                    }
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task SyncAllActiveOrdersTrackingAsync()
    {
        var activeOrderIds = await _context.ProductionOrders
            .Where(o => o.Status == OrderStatus.AtDying || 
                        o.Status == OrderStatus.AtHandwork || 
                        o.Status == OrderStatus.AtStitching ||
                        o.CurrentProcess == "Dying" ||
                        o.CurrentProcess == "Handwork" ||
                        o.CurrentProcess == "Stitching")
            .Select(o => o.Id)
            .ToListAsync();

        foreach (var id in activeOrderIds)
        {
            await SyncOrderTrackingAsync(id);
        }
    }
}
