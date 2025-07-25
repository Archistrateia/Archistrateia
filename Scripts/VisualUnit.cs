using Godot;

public partial class VisualUnit : Node2D
{
    public Unit LogicalUnit { get; private set; }
    public bool IsSelected { get; private set; }

    public VisualUnit()
    {
    }

    public VisualUnit(Unit logicalUnit, Vector2 position, Color color)
    {
        Initialize(logicalUnit, position, color);
    }

    public void Initialize(Unit logicalUnit, Vector2 position, Color color)
    {
        LogicalUnit = logicalUnit;
        Position = position;
        CreateVisualComponents(color);
    }

    private void CreateVisualComponents(Color color)
    {
        var unitSprite = new Polygon2D();
        unitSprite.Name = "UnitSprite";
        var vertices = new Vector2[8];
        for (int i = 0; i < 8; i++)
        {
            var angle = i * Mathf.Pi / 4.0f;
            vertices[i] = new Vector2(
                15.0f * Mathf.Cos(angle),
                15.0f * Mathf.Sin(angle)
            );
        }
        unitSprite.Polygon = vertices;
        unitSprite.Color = color;
        AddChild(unitSprite);

        var unitOutline = new Line2D();
        unitOutline.Name = "UnitOutline";
        var outlinePoints = new Vector2[9];
        for (int i = 0; i < 8; i++)
        {
            var angle = i * Mathf.Pi / 4.0f;
            outlinePoints[i] = new Vector2(
                15.0f * Mathf.Cos(angle),
                15.0f * Mathf.Sin(angle)
            );
        }
        outlinePoints[8] = outlinePoints[0];
        unitOutline.Points = outlinePoints;
        unitOutline.DefaultColor = new Color(0.0f, 0.0f, 0.0f);
        unitOutline.Width = 2.0f;
        AddChild(unitOutline);

        var unitLabel = new Label();
        unitLabel.Name = "UnitLabel";
        unitLabel.Text = LogicalUnit.Name.Substring(0, 1);
        unitLabel.Position = new Vector2(-5, -10);
        unitLabel.AddThemeColorOverride("font_color", new Color(1.0f, 1.0f, 1.0f));
        AddChild(unitLabel);
    }

    public void UpdatePosition(Vector2 newPosition)
    {
        Position = newPosition;
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        
        if (selected)
        {
            CreateSelectionIndicator();
        }
        else
        {
            RemoveSelectionIndicator();
        }
    }

    private void CreateSelectionIndicator()
    {
        var existing = GetNodeOrNull<Node2D>("SelectionIndicator");
        if (existing != null) return;

        var selectionIndicator = new Node2D();
        selectionIndicator.Name = "SelectionIndicator";
        
        var selectionRing = new Line2D();
        var ringPoints = new Vector2[9];
        for (int i = 0; i < 8; i++)
        {
            var angle = i * Mathf.Pi / 4.0f;
            ringPoints[i] = new Vector2(
                25.0f * Mathf.Cos(angle),
                25.0f * Mathf.Sin(angle)
            );
        }
        ringPoints[8] = ringPoints[0];
        selectionRing.Points = ringPoints;
        selectionRing.DefaultColor = new Color(1.0f, 1.0f, 0.0f);
        selectionRing.Width = 3.0f;
        
        selectionIndicator.AddChild(selectionRing);
        AddChild(selectionIndicator);
    }

    private void RemoveSelectionIndicator()
    {
        var existing = GetNodeOrNull<Node2D>("SelectionIndicator");
        existing?.QueueFree();
    }
} 