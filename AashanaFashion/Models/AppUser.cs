using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models
{
    [Table("UserList")]
    public class AppUser : IMustHaveTenant
    {
        public int Id { get; set; }
        public int TenantId { get; set; } = 1;
        public Tenant? Tenant { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Viewer";
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string? ContactNumber { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;

        public int? DefaultCompanyId { get; set; }
        public Company? DefaultCompany { get; set; }

        [NotMapped]
        public string FullName => string.Join(" ", new[] { FirstName, MiddleName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
