namespace ColorLeaves.Core.Models;

public class SheetInfo
{
    public string Name { get; set; } = string.Empty;
    public string Number => $"L-{Name}";
    public string ColorTag { get; set; } = "Default";
}
