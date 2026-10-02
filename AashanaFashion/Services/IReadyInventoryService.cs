using AashanaFashion.Models;

namespace AashanaFashion.Services;

public interface IReadyInventoryService
{
    /// <summary>
    /// Automatically inwards whole 3-piece sets (Chaniya, Choli, Dupatta) for a lot into ReadyProducts inventory when lot reaches ReadyToDispatch
    /// </summary>
    Task<int> InwardLotToReadyStockAsync(int productionOrderId);

    /// <summary>
    /// Reverses the inwarded ready stock if a lot is moved back from ReadyToDispatch
    /// </summary>
    Task<int> RevertLotFromReadyStockAsync(int productionOrderId);

    /// <summary>
    /// One-click sync to ensure all completed lots (ReadyToDispatch or Dispatched) are inwarded into ready inventory
    /// </summary>
    Task<int> SyncAllCompletedLotsAsync();
}
