using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models;

public class Vendor
{
    public int Id { get; set; }

    [Required]
    public string VendorName { get; set; } = string.Empty;

    public string? GstNumber { get; set; }

    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PinCode { get; set; }

    public string? BankName { get; set; }

    public string? AccountNumber { get; set; }

    public string? IfscCode { get; set; }

    public string? PanNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
