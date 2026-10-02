namespace AashanaFashion.Models;

public class UserCompany
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }
}
