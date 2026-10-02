using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AashanaFashion.Data;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public class JobSlipService : IJobSlipService
{
    private readonly AppDbContext _context;

    public JobSlipService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<JobSlip>> GenerateJobSlipsForOrderAsync(int productionOrderId, string? createdBy = null)
    {
        var order = await _context.ProductionOrders
            .Include(p => p.Design)
            .Include(p => p.Details)
            .Include(p => p.ComponentAssignments)
                .ThenInclude(a => a.Vendor)
            .Include(p => p.JobSlips)
                .ThenInclude(js => js.Items)
            .FirstOrDefaultAsync(p => p.Id == productionOrderId);

        if (order == null) return new List<JobSlip>();

        // Find assignments that are delegated to external vendors (excluding in-house)
        var externalAssignments = order.ComponentAssignments
            .Where(a => a.VendorId.HasValue && a.VendorId.Value > 0 && !string.IsNullOrWhiteSpace(a.ComponentName) && !string.IsNullOrWhiteSpace(a.ProcessName))
            .ToList();

        // Group external assignments by (ProcessName, VendorId)
        var groups = externalAssignments
            .GroupBy(a => new { 
                ProcessName = a.ProcessName.Trim(), 
                VendorId = a.VendorId!.Value 
            })
            .ToList();

        var activeKeys = new HashSet<string>();

        foreach (var group in groups)
        {
            var procName = group.Key.ProcessName;
            var vendorId = group.Key.VendorId;
            var key = $"{procName}___{vendorId}";
            activeKeys.Add(key);

            var components = group.Select(a => a.ComponentName.Trim())
                                  .Distinct(StringComparer.OrdinalIgnoreCase)
                                  .ToList();
            var componentsStr = string.Join(", ", components);
            var rate = group.Where(a => a.Rate > 0).Select(a => (decimal?)a.Rate).FirstOrDefault();

            var existingSlip = order.JobSlips.FirstOrDefault(js => 
                string.Equals(js.ProcessName, procName, StringComparison.OrdinalIgnoreCase) && 
                js.VendorId == vendorId);

            if (existingSlip != null)
            {
                // Update components, rate and items if slip is in Issued status
                existingSlip.Components = componentsStr;
                if (rate.HasValue && rate > 0) existingSlip.Rate = rate;

                if (existingSlip.Status == "Issued")
                {
                    // Re-sync items for components
                    _context.JobSlipItems.RemoveRange(existingSlip.Items);
                    existingSlip.Items.Clear();

                    BuildSlipItems(existingSlip, order, components, existingSlip.Rate);
                    existingSlip.TotalQuantity = existingSlip.Items.Sum(i => i.Quantity);
                    existingSlip.TotalAmount = existingSlip.TotalQuantity * (existingSlip.Rate ?? 0);
                }
            }
            else
            {
                // Generate a new Job Slip
                var slipNumber = await GenerateNextSlipNumberAsync(order.LotNo, procName, vendorId);

                var newSlip = new JobSlip
                {
                    SlipNumber = slipNumber,
                    ProductionOrderId = order.Id,
                    ProcessName = procName,
                    VendorId = vendorId,
                    Components = componentsStr,
                    Rate = rate,
                    Status = "Issued",
                    IssueDate = DateTime.Now,
                    ExpectedReturnDate = DateTime.Now.AddDays(7),
                    CreatedDate = DateTime.Now,
                    CreatedBy = createdBy ?? "System"
                };

                BuildSlipItems(newSlip, order, components, rate);
                newSlip.TotalQuantity = newSlip.Items.Sum(i => i.Quantity);
                newSlip.TotalAmount = newSlip.TotalQuantity * (rate ?? 0);

                _context.JobSlips.Add(newSlip);
            }
        }

        // Clean up obsolete slips if they were issued but no longer assigned to that vendor/process
        var obsoleteSlips = order.JobSlips
            .Where(js => js.Status == "Issued" && !activeKeys.Contains($"{js.ProcessName}___{js.VendorId}"))
            .ToList();
        if (obsoleteSlips.Any())
        {
            _context.JobSlips.RemoveRange(obsoleteSlips);
        }

        await _context.SaveChangesAsync();

        return await GetJobSlipsForOrderAsync(productionOrderId);
    }

    private void BuildSlipItems(JobSlip slip, ProductionOrder order, List<string> components, decimal? rate)
    {
        foreach (var comp in components)
        {
            if (order.Details != null && order.Details.Any(d => d.Quantity > 0))
            {
                foreach (var detail in order.Details.Where(d => d.Quantity > 0))
                {
                    slip.Items.Add(new JobSlipItem
                    {
                        ComponentName = comp,
                        Colour = detail.Colour ?? "Standard",
                        Size = detail.Size ?? "Free Size",
                        Quantity = detail.Quantity,
                        Rate = rate
                    });
                }
            }
            else if (order.TotalQuantity > 0)
            {
                slip.Items.Add(new JobSlipItem
                {
                    ComponentName = comp,
                    Colour = "Standard",
                    Size = "Free Size",
                    Quantity = order.TotalQuantity,
                    Rate = rate
                });
            }
        }
    }

    public async Task<List<JobSlip>> GetJobSlipsForOrderAsync(int productionOrderId)
    {
        return await _context.JobSlips
            .Include(js => js.Vendor)
            .Include(js => js.ProductionOrder)
                .ThenInclude(p => p!.Design)
            .Include(js => js.Items)
            .Where(js => js.ProductionOrderId == productionOrderId)
            .OrderBy(js => js.ProcessName)
                .ThenBy(js => js.Id)
            .ToListAsync();
    }

    public async Task<JobSlip?> GetJobSlipByIdAsync(int id)
    {
        return await _context.JobSlips
            .Include(js => js.Vendor)
            .Include(js => js.ProductionOrder)
                .ThenInclude(p => p!.Design)
            .Include(js => js.ProductionOrder)
                .ThenInclude(p => p!.Details)
            .Include(js => js.Items)
            .FirstOrDefaultAsync(js => js.Id == id);
    }

    public async Task<string> GenerateNextSlipNumberAsync(string lotNo, string processName, int vendorId)
    {
        var procCode = GetProcessCode(processName);
        var cleanLot = Regex.Replace(lotNo ?? "LOT", @"[^a-zA-Z0-9\-]", "");
        var baseNumber = $"JS-{cleanLot}-{procCode}-V{vendorId}";

        // Check if unique, else add suffix
        var exists = await _context.JobSlips.AnyAsync(js => js.SlipNumber == baseNumber);
        if (!exists) return baseNumber;

        int seq = 2;
        while (await _context.JobSlips.AnyAsync(js => js.SlipNumber == $"{baseNumber}-{seq}"))
        {
            seq++;
        }
        return $"{baseNumber}-{seq}";
    }

    private static string GetProcessCode(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return "GEN";
        var lower = processName.Trim().ToLower();
        if (lower.Contains("handwork")) return "HW";
        if (lower.Contains("stitching")) return "ST";
        if (lower.Contains("dying")) return "DY";
        if (lower.Contains("cutting")) return "CT";
        if (lower.Contains("roll")) return "RP";
        if (lower.Contains("pack")) return "PK";
        if (lower.Contains("finish")) return "FN";
        if (lower.Contains("embroid")) return "EMB";
        
        var words = processName.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 1)
        {
            return string.Concat(words.Select(w => char.ToUpper(w[0])));
        }
        return processName.Length >= 3 ? processName.Substring(0, 3).ToUpper() : processName.ToUpper();
    }

    public async Task<bool> UpdateJobSlipStatusAsync(int id, string status, DateTime? receivedDate, string? remarks)
    {
        var slip = await _context.JobSlips.FindAsync(id);
        if (slip == null) return false;

        slip.Status = status;
        if (receivedDate.HasValue) slip.ReceivedDate = receivedDate.Value;
        if (!string.IsNullOrWhiteSpace(remarks)) slip.Remarks = remarks;

        await _context.SaveChangesAsync();
        return true;
    }
}
