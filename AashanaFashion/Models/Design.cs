namespace AashanaFashion.Models;

public class Design
{
    public int Id { get; set; }
    public string DesignNumber { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
    public string Colours { get; set; } = string.Empty;
    public string Sizes { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string CreationFlow { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public List<string> GetCreationSteps() =>
        CreationFlow.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();
}
