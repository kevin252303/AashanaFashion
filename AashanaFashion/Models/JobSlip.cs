using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace AashanaFashion.Models;

public class JobSlip
{
    public int Id { get; set; }

    [Required]
    [MaxLength(60)]
    public string SlipNumber { get; set; } = string.Empty;

    public int ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    [Required]
    [MaxLength(100)]
    public string ProcessName { get; set; } = string.Empty;

    public int VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    [MaxLength(500)]
    public string Components { get; set; } = string.Empty;

    public int TotalQuantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Rate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TotalAmount { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Issued"; // "Issued", "In Process", "Received", "Cancelled"

    public DateTime IssueDate { get; set; } = DateTime.Now;
    public DateTime? ExpectedReturnDate { get; set; }
    public DateTime? ReceivedDate { get; set; }

    [MaxLength(1000)]
    public string? Remarks { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public string? CreatedBy { get; set; }

    public List<JobSlipItem> Items { get; set; } = new();

    public List<string> GetComponentsList()
    {
        if (string.IsNullOrWhiteSpace(Components)) return new List<string>();
        return Components.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(c => c.Trim())
                         .Where(c => !string.IsNullOrEmpty(c))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .ToList();
    }
}

public class JobSlipItem
{
    public int Id { get; set; }

    public int JobSlipId { get; set; }
    public JobSlip? JobSlip { get; set; }

    [Required]
    [MaxLength(100)]
    public string ComponentName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Colour { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Size { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public int ReceivedQuantity { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Rate { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }
}
