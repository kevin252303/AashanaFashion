using AashanaFashion.Data;
using AashanaFashion.Models;
using Microsoft.EntityFrameworkCore;

namespace AashanaFashion.Services;

public class ReadyInventoryService : IReadyInventoryService
{
    private readonly AppDbContext _context;

    public ReadyInventoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> InwardLotToReadyStockAsync(int productionOrderId)
    {
        var order = await _context.ProductionOrders
            .Include(o => o.Design)
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.Id == productionOrderId);

        if (order == null) return 0;

        // Must be in ReadyToDispatch or Dispatched status
        if (order.Status != OrderStatus.ReadyToDispatch && order.Status != OrderStatus.Dispatched)
            return 0;

        // Prevent duplicate stock inward
        if (order.IsStockInwarded)
            return 0;

        if (order.Design == null)
        {
            order.Design = await _context.Designs
                .Include(d => d.ColourImages)
                .FirstOrDefaultAsync(d => d.Id == order.DesignId);
        }
        else
        {
            await _context.Entry(order.Design).Collection(d => d.ColourImages).LoadAsync();
        }

        if (order.Design == null) return 0;

        var itemsToInward = order.Details != null && order.Details.Any(d => d.Quantity > 0)
            ? order.Details.Where(d => d.Quantity > 0).ToList()
            : new List<ProductionOrderDetail>
            {
                new ProductionOrderDetail
                {
                    Colour = "Standard",
                    Size = "Free Size",
                    Quantity = order.TotalQuantity
                }
            };

        int totalInwarded = 0;

        foreach (var item in itemsToInward)
        {
            var colour = string.IsNullOrWhiteSpace(item.Colour) ? "Standard" : item.Colour.Trim();
            var size = string.IsNullOrWhiteSpace(item.Size) ? "Free Size" : item.Size.Trim();
            int qty = item.Quantity;
            if (qty <= 0) continue;

            var matchingColourPhoto = order.Design.ColourImages?
                .FirstOrDefault(ci => string.Equals(ci.Colour, colour, StringComparison.OrdinalIgnoreCase))?.PhotoPath;
            var effectivePhoto = matchingColourPhoto ?? order.Design.PhotoPath;

            var rp = await _context.ReadyProducts
                .FirstOrDefaultAsync(r => r.CompanyId == order.CompanyId && r.DesignId == order.DesignId && r.Colour == colour && r.Size == size);

            if (rp == null)
            {
                var sku = $"RP-{order.Design.DesignNumber}-{colour}-{size}".Replace(" ", "");
                rp = new ReadyProduct
                {
                    CompanyId = order.CompanyId,
                    DesignId = order.DesignId,
                    DesignNumber = order.Design.DesignNumber,
                    Colour = colour,
                    Size = size,
                    Sku = sku,
                    Barcode = sku,
                    ChaniyaQuantityPerSet = 1,
                    CholiQuantityPerSet = 1,
                    DuppataQuantityPerSet = 1,
                    QuantityOnHand = qty,
                    AllocatedQuantity = 0,
                    MinimumStockAlert = 5,
                    UnitPrice = order.Design.SalesPrice > 0 ? order.Design.SalesPrice : order.Design.Price,
                    CostPrice = order.Design.TotalProductionCost,
                    PhotoPath = effectivePhoto,
                    Remarks = $"Auto-inwarded from Lot #{order.LotNo} (ReadyToDispatch)",
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };
                _context.ReadyProducts.Add(rp);
                await _context.SaveChangesAsync();
            }
            else
            {
                rp.QuantityOnHand += qty;
                if (string.IsNullOrEmpty(rp.PhotoPath) && !string.IsNullOrEmpty(effectivePhoto))
                {
                    rp.PhotoPath = effectivePhoto;
                }
                rp.UpdatedDate = DateTime.Now;
            }

            _context.ReadyProductTransactions.Add(new ReadyProductTransaction
            {
                CompanyId = order.CompanyId,
                ReadyProductId = rp.Id,
                CreatedDate = DateTime.Now,
                TransactionType = ReadyProductTransactionType.AssemblyFromProduction,
                Quantity = qty,
                BalanceAfter = rp.QuantityOnHand,
                ReferenceType = "Lot Production Complete",
                ReferenceNumber = order.LotNo,
                Notes = $"Auto ready stock: Lot #{order.LotNo} reached ReadyToDispatch ({qty} whole 3-piece sets: Chaniya + Choli + Dupatta)"
            });

            totalInwarded += qty;
        }

        order.IsStockInwarded = true;
        await _context.SaveChangesAsync();

        return totalInwarded;
    }

    public async Task<int> RevertLotFromReadyStockAsync(int productionOrderId)
    {
        var order = await _context.ProductionOrders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.Id == productionOrderId);

        if (order == null || !order.IsStockInwarded) return 0;

        var itemsToRevert = order.Details != null && order.Details.Any(d => d.Quantity > 0)
            ? order.Details.Where(d => d.Quantity > 0).ToList()
            : new List<ProductionOrderDetail>
            {
                new ProductionOrderDetail
                {
                    Colour = "Standard",
                    Size = "Free Size",
                    Quantity = order.TotalQuantity
                }
            };

        int totalReverted = 0;

        foreach (var item in itemsToRevert)
        {
            var colour = string.IsNullOrWhiteSpace(item.Colour) ? "Standard" : item.Colour.Trim();
            var size = string.IsNullOrWhiteSpace(item.Size) ? "Free Size" : item.Size.Trim();
            int qty = item.Quantity;
            if (qty <= 0) continue;

            var rp = await _context.ReadyProducts
                .FirstOrDefaultAsync(r => r.CompanyId == order.CompanyId && r.DesignId == order.DesignId && r.Colour == colour && r.Size == size);

            if (rp != null)
            {
                rp.QuantityOnHand = Math.Max(0, rp.QuantityOnHand - qty);
                rp.UpdatedDate = DateTime.Now;

                _context.ReadyProductTransactions.Add(new ReadyProductTransaction
                {
                    CompanyId = order.CompanyId,
                    ReadyProductId = rp.Id,
                    CreatedDate = DateTime.Now,
                    TransactionType = ReadyProductTransactionType.StockAdjustment,
                    Quantity = -qty,
                    BalanceAfter = rp.QuantityOnHand,
                    ReferenceType = "Lot Status Reverted",
                    ReferenceNumber = order.LotNo,
                    Notes = $"Stock deduction: Lot #{order.LotNo} reverted from ReadyToDispatch status (-{qty} sets)"
                });

                totalReverted += qty;
            }
        }

        order.IsStockInwarded = false;
        await _context.SaveChangesAsync();

        return totalReverted;
    }

    public async Task<int> SyncAllCompletedLotsAsync()
    {
        var eligibleOrders = await _context.ProductionOrders
            .Where(o => (o.Status == OrderStatus.ReadyToDispatch || o.Status == OrderStatus.Dispatched) && !o.IsStockInwarded)
            .Select(o => o.Id)
            .ToListAsync();

        int count = 0;
        foreach (var orderId in eligibleOrders)
        {
            count += await InwardLotToReadyStockAsync(orderId);
        }

        return count;
    }
}
