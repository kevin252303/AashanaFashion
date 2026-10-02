using System.Threading.Tasks;
using AashanaFashion.Models;

namespace AashanaFashion.Services;

public interface ITenantContext
{
    int CurrentTenantId { get; }
    Tenant? CurrentTenant { get; }
    Task<Tenant> GetCurrentTenantAsync();
    void SetCurrentTenant(Tenant tenant);
}
