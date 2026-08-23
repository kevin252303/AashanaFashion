using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models;

public class Colour
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string ColourName { get; set; } = string.Empty;

    [StringLength(7)]
    public string? ColourCode { get; set; } // Hex code like #FF0000

    public bool IsActive { get; set; } = true;
}
