using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models;

[Table("DesignColourImages")]
public class DesignColourImage
{
    public int Id { get; set; }

    [Required]
    public int DesignId { get; set; }
    public Design? Design { get; set; }

    [Required]
    [StringLength(50)]
    public string Colour { get; set; } = string.Empty;

    [StringLength(20)]
    public string? ColourCode { get; set; }

    [Required]
    [StringLength(500)]
    public string PhotoPath { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
