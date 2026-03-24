using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models
{
    public class UserFormViewModel
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string? Password { get; set; }

        [Required]
        public string Role { get; set; } = "Viewer";

        public bool IsActive { get; set; } = true;
    }
}
