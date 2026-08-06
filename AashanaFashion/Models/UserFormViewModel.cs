using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models
{
    public class UserFormViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        [Required]
        public string LastName { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? ContactNumber { get; set; }

        public string? Address { get; set; }

        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }

        [Required]
        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public List<UserRole> AvailableRoles { get; set; } = new();
    }
}
