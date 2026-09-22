using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models;

public enum QcStage
{
    [Display(Name = "Fabric Inspection (Raw/Grey)")]
    FabricInspection,

    [Display(Name = "Post-Dying")]
    PostDying,

    [Display(Name = "Post-Handwork / Embroidery")]
    PostHandwork,

    [Display(Name = "Post-Stitching / Tailoring")]
    PostStitching,

    [Display(Name = "Final Garment Inspection")]
    FinalGarmentInspection
}

public enum QcResult
{
    [Display(Name = "Passed")]
    Passed,

    [Display(Name = "Passed with Rework")]
    PassedWithRework,

    [Display(Name = "Failed / Rejected")]
    Failed
}

public enum DefectCategory
{
    [Display(Name = "Fabric / Weaving Defect")]
    FabricDefect,

    [Display(Name = "Color Shade / Dyeing Flaw")]
    ColorShadeDefect,

    [Display(Name = "Embroidery / Handwork Defect")]
    EmbroideryHandworkDefect,

    [Display(Name = "Stitching / Seam Defect")]
    StitchingSewingDefect,

    [Display(Name = "Finishing / Stain / Oil Spot")]
    FinishingStainDefect,

    [Display(Name = "Measurement / Size Variation")]
    MeasurementDefect,

    [Display(Name = "Trims / Zipper / Button Issue")]
    TrimsIssue,

    [Display(Name = "Other")]
    Other
}

public enum DefectSeverity
{
    Minor,
    Major,
    Critical
}

public enum DefectAction
{
    [Display(Name = "Rework by Vendor")]
    ReworkByVendor,

    [Display(Name = "Rework In-House")]
    ReworkInHouse,

    [Display(Name = "Accept as B-Grade / Discounted")]
    AcceptAsBGrade,

    [Display(Name = "Scrap / Irrecoverable Loss")]
    ScrapWriteOff
}

public enum ReworkStatus
{
    [Display(Name = "N/A (Scrapped / Accepted)")]
    NotApplicable,

    [Display(Name = "Pending Rework")]
    PendingRework,

    [Display(Name = "Sent to Rework")]
    SentToRework,

    [Display(Name = "Rework Completed / Re-Passed")]
    ReworkCompleted,

    [Display(Name = "Scrapped after Rework")]
    Scrapped
}

public class QualityInspection
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string InspectionNumber { get; set; } = string.Empty;

    public DateTime InspectionDate { get; set; } = DateTime.Today;

    [Required]
    [StringLength(100)]
    public string InspectorName { get; set; } = string.Empty;

    public int ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public int? ProductionEntityId { get; set; }
    public ProductionEntity? ProductionEntity { get; set; }

    public QcStage Stage { get; set; } = QcStage.FinalGarmentInspection;

    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public int TotalInspected { get; set; }
    public int TotalPassed { get; set; }
    public int TotalRework { get; set; }
    public int TotalScrap { get; set; }

    public QcResult OverallResult { get; set; } = QcResult.Passed;

    [StringLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public List<QualityDefect> Defects { get; set; } = new();

    public decimal PassRatePercent => TotalInspected > 0
        ? Math.Round((decimal)TotalPassed / TotalInspected * 100m, 1)
        : 100m;

    public decimal DefectRatePercent => TotalInspected > 0
        ? Math.Round((decimal)(TotalRework + TotalScrap) / TotalInspected * 100m, 1)
        : 0m;
}

public class QualityDefect
{
    public int Id { get; set; }

    public int QualityInspectionId { get; set; }
    public QualityInspection? QualityInspection { get; set; }

    public DefectCategory DefectCategory { get; set; } = DefectCategory.Other;

    [Required]
    [StringLength(200)]
    public string DefectReason { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    public DefectSeverity Severity { get; set; } = DefectSeverity.Major;

    public DefectAction Action { get; set; } = DefectAction.ReworkInHouse;

    public ReworkStatus ReworkStatus { get; set; } = ReworkStatus.PendingRework;

    [StringLength(100)]
    public string? ReworkAssignedTo { get; set; }

    public DateTime? ReworkCompletionDate { get; set; }

    [StringLength(500)]
    public string? ResolutionNotes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
