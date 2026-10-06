using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required."), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public class RegisterClientViewModel
    {
        [Required(ErrorMessage = "Client / Contact person name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Business / Company name is required.")]
        [StringLength(150, ErrorMessage = "Business name cannot exceed 150 characters.")]
        [Display(Name = "Business / Company Name")]
        public string BusinessName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(100)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100)]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required.")]
        [StringLength(100)]
        [Display(Name = "State")]
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AuthViewModel
    {
        public string ActiveTab { get; set; } = "login"; // "login" or "register"
        public LoginViewModel Login { get; set; } = new();
        public RegisterClientViewModel Register { get; set; } = new();
    }
}
