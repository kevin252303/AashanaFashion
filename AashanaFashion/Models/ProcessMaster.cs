using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models;

public class ProcessMaster
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Process Name")]
    public string ProcessName { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Process Code")]
    public string? ProcessCode { get; set; }

    [Required]
    [Display(Name = "Sequence Order")]
    public int DisplayOrder { get; set; } = 1;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
