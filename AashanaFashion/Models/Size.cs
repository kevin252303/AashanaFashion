using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models;

public class Size
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string SizeName { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0; // XS = 1, S = 2, M = 3, etc.

    public bool IsActive { get; set; } = true;
}
