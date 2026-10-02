namespace AashanaFashion.Models;

public class DesignDiscontinuedVariant
{
    public int Id { get; set; }

    public int DesignId { get; set; }
    public Design? Design { get; set; }

    public string? Colour { get; set; }
    public string? Size { get; set; }

    public DateTime DiscontinuedDate { get; set; } = DateTime.Now;

    public string? Reason { get; set; }
}
