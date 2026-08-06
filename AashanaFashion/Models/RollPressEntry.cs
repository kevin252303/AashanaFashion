namespace AashanaFashion.Models;

public class RollPressEntry
{
    public int Id { get; set; }
    public string LotNo { get; set; } = string.Empty;
    public string DesignNo { get; set; } = string.Empty;
    public DateTime GivenDate { get; set; }
    public DateTime ArrivalDate { get; set; }
    public int NumberOfPieces { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
