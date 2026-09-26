using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models;

public class ProductExtraCharge
{
    public int Id { get; set; }

    public int DesignId { get; set; }
    public Design? Design { get; set; }

    /// <summary>
    /// Type of attribute: "Colour" or "Size"
    /// </summary>
    [Required]
    [StringLength(50)]
    public string AttributeType { get; set; } = "Colour";

    /// <summary>
    /// The specific colour or size name, e.g. "Maroon", "XXL", "3XL"
    /// </summary>
    [Required]
    [StringLength(100)]
    public string AttributeValue { get; set; } = string.Empty;

    /// <summary>
    /// Extra charge amount in currency (₹) to be added to the base price
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal ExtraCharge { get; set; } = 0m;

    /// <summary>
    /// Optional reason or note, e.g. "Plus size fabric allowance", "Special wash/dyeing"
    /// </summary>
    [StringLength(200)]
    public string? Remarks { get; set; }
}
