namespace AashanaFashion.Services;

public interface IPmsSyncService
{
    Task SyncOrderTrackingAsync(int orderId);
    Task SyncAllActiveOrdersTrackingAsync();
}
