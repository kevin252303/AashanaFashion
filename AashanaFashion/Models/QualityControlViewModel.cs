using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models;

public class QcDashboardViewModel
{
    public int TotalInspections { get; set; }
    public int TotalUnitsInspected { get; set; }
    public int TotalUnitsPassed { get; set; }
    public int TotalUnitsRework { get; set; }
    public int TotalUnitsScrapped { get; set; }
    public decimal OverallPassRate => TotalUnitsInspected > 0
        ? Math.Round((decimal)TotalUnitsPassed / TotalUnitsInspected * 100m, 1)
        : 100m;
    public int ActiveReworkCount { get; set; }

    public List<QualityInspection> RecentInspections { get; set; } = new();
    public List<VendorQualityScorecardItem> VendorScorecards { get; set; } = new();
    public List<DefectCategoryBreakdownItem> CategoryBreakdowns { get; set; } = new();

    public QcStage? StageFilter { get; set; }
    public int? SelectedOrderId { get; set; }
    public int? SelectedVendorId { get; set; }
}

public class VendorQualityScorecardItem
{
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public int InspectedPieces { get; set; }
    public int PassedPieces { get; set; }
    public int DefectivePieces { get; set; }
    public decimal DefectRate => InspectedPieces > 0
        ? Math.Round((decimal)DefectivePieces / InspectedPieces * 100m, 1)
        : 0m;
}

public class DefectCategoryBreakdownItem
{
    public DefectCategory Category { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int DefectCount { get; set; }
    public decimal Percentage { get; set; }
}

public class CreateInspectionViewModel
{
    [Required]
    [Display(Name = "QC Inspection #")]
    public string InspectionNumber { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Inspection Date")]
    public DateTime InspectionDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "Inspector Name")]
    public string InspectorName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Production Order / Lot")]
    public int ProductionOrderId { get; set; }

    [Display(Name = "Component / Entity (Optional)")]
    public int? ProductionEntityId { get; set; }

    [Required]
    [Display(Name = "Inspection Stage")]
    public QcStage Stage { get; set; } = QcStage.FinalGarmentInspection;

    [Display(Name = "Assigned Vendor / Job Worker (Optional)")]
    public int? VendorId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Inspected quantity must be at least 1.")]
    [Display(Name = "Total Quantity Inspected")]
    public int TotalInspected { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Passed Quantity")]
    public int TotalPassed { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Rework Quantity")]
    public int TotalRework { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Scrap Quantity")]
    public int TotalScrap { get; set; }

    [Display(Name = "Overall Result")]
    public QcResult OverallResult { get; set; } = QcResult.Passed;

    [Display(Name = "Inspector Remarks")]
    public string? Remarks { get; set; }

    public List<DefectItemInputModel> Defects { get; set; } = new();
}

public class DefectItemInputModel
{
    public DefectCategory DefectCategory { get; set; } = DefectCategory.Other;
    public string DefectReason { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public DefectSeverity Severity { get; set; } = DefectSeverity.Major;
    public DefectAction Action { get; set; } = DefectAction.ReworkInHouse;
    public string? ReworkAssignedTo { get; set; }
    public string? Remarks { get; set; }
}

public class ReworkQueueViewModel
{
    public int TotalPendingRework { get; set; }
    public int TotalSentToRework { get; set; }
    public int TotalCompletedRework { get; set; }
    public List<QualityDefect> DefectItems { get; set; } = new();
}
