using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models;

public class ProductCategory
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Category Name is required.")]
    [StringLength(100)]
    [Display(Name = "Category Name")]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Category Code")]
    public string? CategoryCode { get; set; }

    [Display(Name = "Parent Category")]
    public int? ParentCategoryId { get; set; }

    [ForeignKey(nameof(ParentCategoryId))]
    public ProductCategory? ParentCategory { get; set; }

    public ICollection<ProductCategory> SubCategories { get; set; } = new List<ProductCategory>();

    [StringLength(20)]
    [Display(Name = "Default HSN/SAC Code")]
    public string? DefaultHsnCode { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    [Display(Name = "Default GST Rate (%)")]
    public decimal? DefaultGstRate { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Navigation property to linked Products (Designs)
    public ICollection<Design> Products { get; set; } = new List<Design>();
}

public class QuickCreateCategoryModel
{
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryCode { get; set; }
    public int? ParentCategoryId { get; set; }
    public string? DefaultHsnCode { get; set; }
    public decimal? DefaultGstRate { get; set; }
    public string? Description { get; set; }
}
