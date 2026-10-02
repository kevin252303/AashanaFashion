using AashanaFashion.Models;

namespace AashanaFashion.Services;

public interface ICompanyContext
{
    Task<Company> GetActiveCompanyAsync();
    Task<int> GetActiveCompanyIdAsync();
    Task<List<Company>> GetAllowedCompaniesAsync();
    void SetActiveCompanyId(int companyId);
}
