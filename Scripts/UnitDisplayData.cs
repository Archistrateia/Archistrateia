using Godot;

public class UnitDisplayData
{
    public Unit LogicalUnit { get; set; }
    public Vector2 Position { get; set; }
    public Color Color { get; set; }
    public string DisplayText { get; set; }
    public bool IsSelected { get; set; }

    public UnitDisplayData(Unit logicalUnit, Vector2 position, Color color)
    {
        LogicalUnit = logicalUnit;
        Position = position;
        Color = color;
        DisplayText = logicalUnit.Name.Substring(0, 1);
        IsSelected = false;
    }
} 