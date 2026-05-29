namespace AashanaFashion.Models;

public class ProductionEntity
{
    public int Id { get; set; }
    public int ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }
    public string EntityType { get; set; } = string.Empty; // Chaniya, Choli, Blouse, Duppata
    public string Colour { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public int SlNo { get; set; } // Serial number within the order (1, 2, 3...)
    public string Status { get; set; } = "Created"; // Created, AtDying, AtRoll, AtHandwork, AtStitching, Completed, Dispatched
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public List<ProcessTracking> ProcessTrackings { get; set; } = new();
}

public class ProcessTracking
{
    public int Id { get; set; }
    public int ProductionEntityId { get; set; }
    public ProductionEntity? ProductionEntity { get; set; }
    public string ProcessName { get; set; } = string.Empty; // Dying, Roll, Handwork, Stitching
    public DateTime? GivenDate { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public DateTime? ActualReturnDate { get; set; }
    public int? DaysLate => ExpectedReturnDate.HasValue && ActualReturnDate.HasValue 
        ? (int)(ActualReturnDate.Value - ExpectedReturnDate.Value).TotalDays 
        : (ExpectedReturnDate.HasValue && ActualReturnDate.HasValue == false && DateTime.Today > ExpectedReturnDate.Value 
            ? (int)(DateTime.Today - ExpectedReturnDate.Value).TotalDays 
            : null);
    public bool IsComplete => ActualReturnDate.HasValue;
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
