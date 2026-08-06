namespace AashanaFashion.Models;

public class DyingEntry
{
    public int Id { get; set; }
    public string LotNo { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public decimal Meters { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
