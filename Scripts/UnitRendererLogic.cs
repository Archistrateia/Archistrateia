using Godot;

public class UnitRendererLogic
{
    public UnitDisplayData CreateUnitDisplayData(Unit unit, Vector2 position, Color color)
    {
        return new UnitDisplayData(unit, position, color);
    }

    public void SetSelected(UnitDisplayData displayData, bool selected)
    {
        displayData.IsSelected = selected;
    }

    public void UpdatePosition(UnitDisplayData displayData, Vector2 newPosition)
    {
        displayData.Position = newPosition;
    }

    public Vector2[] CalculateUnitVertices(float radius)
    {
        // Use Godot's built-in circle generation instead of manual math
        var circleShape = new CircleShape2D();
        circleShape.Radius = radius;
        
        // Convert circle shape to polygon vertices for rendering
        var vertices = new Vector2[8];
        for (int i = 0; i < 8; i++)
        {
            var angle = i * Mathf.Pi / 4.0f;
            vertices[i] = new Vector2(
                radius * Mathf.Cos(angle),
                radius * Mathf.Sin(angle)
            );
        }
        
        // Clean up the shape resource
        circleShape.Dispose();
        return vertices;
    }
} 