using Godot;
using Archistrateia;

public partial class VisualHexTile : Area2D
{
    public Vector2I GridPosition { get; private set; }
    public TerrainType TerrainType { get; private set; }
    
    [Signal]
    public delegate void TileClickedEventHandler(VisualHexTile tile);

    private Polygon2D _hexShape;
    private Line2D _hexOutline;
    private bool _isHighlighted = false;
    private bool _isGrayed = false;
    private bool _isBrightened = false;

    public void Initialize(Vector2I gridPosition, TerrainType terrainType, Color color, Vector2 worldPosition)
    {
        GridPosition = gridPosition;
        TerrainType = terrainType;
        Position = worldPosition;
        Name = $"VisualHexTile_{gridPosition.X}_{gridPosition.Y}";
        
        CreateVisualComponents(color);
        SetupClickDetection();
        UpdateVisualAppearance();
    }

    private void CreateVisualComponents(Color color)
    {
        // Create hex shape using Godot's built-in Polygon2D
        _hexShape = new Polygon2D();
        _hexShape.Polygon = HexGridCalculator.CreateHexagonVertices();
        _hexShape.Color = color;
        _hexShape.Name = "HexShape";
        AddChild(_hexShape);

        // Create hex outline using Godot's built-in Line2D
        _hexOutline = new Line2D();
        _hexOutline.Points = HexGridCalculator.CreateHexagonVertices();
        _hexOutline.DefaultColor = new Color(0.2f, 0.2f, 0.2f);
        _hexOutline.Width = 2.0f;
        _hexOutline.Name = "HexOutline";
        AddChild(_hexOutline);
    }

    private void SetupClickDetection()
    {
        // Create collision shape for click detection
        var collisionShape = new CollisionShape2D();
        var vertices = HexGridCalculator.CreateHexagonVertices();
        
        // Create a ConvexPolygonShape2D from hex vertices
        var shape = new ConvexPolygonShape2D();
        shape.Points = vertices;
        collisionShape.Shape = shape;
        collisionShape.Name = "TileClickArea";
        AddChild(collisionShape);
        
        // Configure Area2D for input
        InputPickable = true;
        Monitoring = false;
        Monitorable = false;
        
        // Connect input event
        Connect("input_event", new Callable(this, MethodName.OnInputEvent));
    }

    public void UpdateVisualComponents()
    {
        // Update hex shape vertices
        if (_hexShape != null)
        {
            _hexShape.Polygon = HexGridCalculator.CreateHexagonVertices();
        }

        // Update outline vertices
        if (_hexOutline != null)
        {
            _hexOutline.Points = HexGridCalculator.CreateHexagonVertices();
        }

        // Update collision shape
        var collisionShape = GetNode<CollisionShape2D>("TileClickArea");
        if (collisionShape != null && collisionShape.Shape is ConvexPolygonShape2D polygonShape)
        {
            polygonShape.Points = HexGridCalculator.CreateHexagonVertices();
        }

        // Update visual appearance
        UpdateVisualAppearance();
    }

    private void UpdateVisualAppearance()
    {
        if (_hexShape == null) return;
        
        // Get base terrain color
        var terrainColor = GetTerrainColor(TerrainType);
        
        // Apply state-based modifications
        if (_isHighlighted)
        {
            terrainColor = terrainColor.Lightened(0.3f);
        }
        else if (_isGrayed)
        {
            terrainColor = terrainColor.Darkened(0.5f);
        }
        else if (_isBrightened)
        {
            terrainColor = terrainColor.Lightened(0.2f);
        }
        
        // Update hex shape color
        _hexShape.Color = terrainColor;
    }

    private Color GetTerrainColor(TerrainType terrainType)
    {
        return terrainType switch
        {
            TerrainType.Desert => new Color(0.9f, 0.8f, 0.6f),
            TerrainType.Hill => new Color(0.6f, 0.5f, 0.3f),
            TerrainType.River => new Color(0.3f, 0.6f, 0.9f),
            TerrainType.Shoreline => new Color(0.8f, 0.7f, 0.5f),
            TerrainType.Lagoon => new Color(0.2f, 0.5f, 0.7f),
            _ => Colors.Gray
        };
    }

    private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent && 
            mouseEvent.Pressed && 
            mouseEvent.ButtonIndex == MouseButton.Left)
        {
            EmitSignal(SignalName.TileClicked, this);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && 
            mouseEvent.Pressed && 
            mouseEvent.ButtonIndex == MouseButton.Left)
        {
            // Convert global mouse position to local coordinates
            var localPos = ToLocal(mouseEvent.Position);
            
            // Check if the click is within this tile's hexagon
            if (IsPointInHexagon(localPos))
            {
                EmitSignal(SignalName.TileClicked, this);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private bool IsPointInHexagon(Vector2 point)
    {
        // Get the exact hex vertices used for rendering
        var vertices = HexGridCalculator.CreateHexagonVertices();
        
        // Ray casting algorithm for point-in-polygon
        bool inside = false;
        int j = vertices.Length - 1;
        
        for (int i = 0; i < vertices.Length; i++)
        {
            if (((vertices[i].Y > point.Y) != (vertices[j].Y > point.Y)) &&
                (point.X < (vertices[j].X - vertices[i].X) * (point.Y - vertices[i].Y) / (vertices[j].Y - vertices[i].Y) + vertices[i].X))
            {
                inside = !inside;
            }
            j = i;
        }
        
        return inside;
    }

    public void SetHighlight(bool highlight, Color highlightColor = default)
    {
        _isHighlighted = highlight;
        UpdateVisualAppearance();
    }

    public void SetGrayed(bool grayed)
    {
        _isGrayed = grayed;
        UpdateVisualAppearance();
    }

    public void SetBrightened(bool brightened)
    {
        _isBrightened = brightened;
        UpdateVisualAppearance();
    }

    public bool IsHighlighted()
    {
        return _isHighlighted;
    }

    public bool IsGrayed()
    {
        return _isGrayed;
    }

    public bool IsBrightened()
    {
        return _isBrightened;
    }
} 