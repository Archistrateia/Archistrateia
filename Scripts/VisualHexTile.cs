using Godot;
using Archistrateia;

public partial class VisualHexTile : Area2D
{
    public Vector2I GridPosition { get; private set; }
    public TerrainType TerrainType { get; private set; }
    
    [Signal]
    public delegate void TileClickedEventHandler(VisualHexTile tile);
    
    [Signal]
    public delegate void TileHoveredEventHandler(VisualHexTile tile);
    
    [Signal]
    public delegate void TileUnhoveredEventHandler(VisualHexTile tile);

    private Polygon2D _hexShape;
    private Line2D _hexOutline;
    private bool _isHighlighted = false;
    private bool _isGrayed = false;
    private bool _isBrightened = false;
    private bool _isOccupied = false;
    private bool _isUnavailable = false;
    private bool _isInMovementPhase = false;
    private Color _highlightColor = default; // Added for custom highlight color

    private Color _baseTerrainColor;

    public void Initialize(Vector2I gridPosition, TerrainType terrainType, Color color, Vector2 worldPosition)
    {
        GridPosition = gridPosition;
        TerrainType = terrainType;
        _baseTerrainColor = color; // Store the passed color
        Position = worldPosition;
        Name = $"VisualHexTile_{gridPosition.X}_{gridPosition.Y}";
        ZIndex = 1; // Ensure tiles are above the map container but below units
        
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
        
        // Connect input event and mouse events
        Connect("input_event", new Callable(this, MethodName.OnInputEvent));
        Connect("mouse_entered", new Callable(this, MethodName.OnMouseEntered));
        Connect("mouse_exited", new Callable(this, MethodName.OnMouseExited));
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
        
        // Get base terrain color - use stored color if available, otherwise fallback
        var terrainColor = _baseTerrainColor != default ? _baseTerrainColor : GetTerrainColor(TerrainType);
        
        // Apply state-based modifications in priority order
        if (_isInMovementPhase && _isOccupied)
        {
            // Only show occupied tiles as red during movement phase
            terrainColor = terrainColor.Darkened(0.6f);
            terrainColor = terrainColor.Blend(new Color(0.8f, 0.2f, 0.2f, 0.7f));
        }
        else if (_isUnavailable)
        {
            // Unavailable tiles get a dark gray overlay
            terrainColor = terrainColor.Darkened(0.7f);
            terrainColor = terrainColor.Blend(new Color(0.3f, 0.3f, 0.3f, 0.8f));
        }
        else if (_isHighlighted)
        {
            // Highlighted tiles get a bright glow using custom color or default green
            if (_highlightColor != default)
            {
                terrainColor = terrainColor.Lightened(0.4f);
                terrainColor = terrainColor.Blend(_highlightColor);
            }
            else
            {
                // Default green highlight
                terrainColor = terrainColor.Lightened(0.4f);
                terrainColor = terrainColor.Blend(new Color(0.2f, 0.8f, 0.2f, 0.6f));
            }
        }
        else if (_isBrightened)
        {
            // Brightened tiles (available destinations) get enhanced visibility
            terrainColor = terrainColor.Lightened(0.3f);
            terrainColor = terrainColor.Blend(new Color(0.1f, 0.6f, 0.1f, 0.4f));
        }
        else if (_isGrayed)
        {
            // Grayed tiles (background) get a subtle darkening
            terrainColor = terrainColor.Darkened(0.3f);
        }
        
        // Update hex shape color
        _hexShape.Color = terrainColor;
        
        // Update outline color based on state
        if (_isHighlighted)
        {
            if (_highlightColor != default)
            {
                // Use a brighter version of the custom highlight color for the outline
                var outlineColor = _highlightColor;
                outlineColor.A = 1.0f; // Full opacity for outline
                _hexOutline.DefaultColor = outlineColor;
            }
            else
            {
                _hexOutline.DefaultColor = new Color(0.0f, 1.0f, 0.0f);
            }
            _hexOutline.Width = 3.0f;
        }
        else if (_isBrightened)
        {
            _hexOutline.DefaultColor = new Color(0.0f, 0.8f, 0.0f);
            _hexOutline.Width = 2.5f;
        }
        else if (_isInMovementPhase && _isOccupied)
        {
            // Only show red outline during movement phase
            _hexOutline.DefaultColor = new Color(0.8f, 0.0f, 0.0f);
            _hexOutline.Width = 2.5f;
        }
        else
        {
            _hexOutline.DefaultColor = new Color(0.2f, 0.2f, 0.2f);
            _hexOutline.Width = 2.0f;
        }
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
            TerrainType.Grassland => new Color(0.4f, 0.8f, 0.3f),
            TerrainType.Mountain => new Color(0.5f, 0.4f, 0.4f),
            TerrainType.Water => new Color(0.1f, 0.4f, 0.8f),
            _ => Colors.Gray
        };
    }

    private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent && 
            mouseEvent.Pressed && 
            mouseEvent.ButtonIndex == MouseButton.Left)
        {
            // Check if click is within game area bounds before processing
            if (!IsClickWithinGameArea(mouseEvent.GlobalPosition))
            {
                return;
            }
            
            GD.Print($"🎯 TILE CLICK DEBUG: Tile Grid({GridPosition.X},{GridPosition.Y}) at World({Position.X:F1},{Position.Y:F1}) clicked");
            EmitSignal(SignalName.TileClicked, this);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && 
            mouseEvent.Pressed && 
            mouseEvent.ButtonIndex == MouseButton.Left)
        {
            // Check if click is within game area bounds before processing
            if (!IsClickWithinGameArea(mouseEvent.GlobalPosition))
            {
                return;
            }
            
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

    private static bool IsPointInHexagon(Vector2 point)
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
        _highlightColor = highlightColor; // Store the custom highlight color
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

    public void SetOccupied(bool occupied)
    {
        _isOccupied = occupied;
        UpdateVisualAppearance();
    }

    public void SetUnavailable(bool unavailable)
    {
        _isUnavailable = unavailable;
        UpdateVisualAppearance();
    }

    public void SetMovementPhase(bool inMovementPhase)
    {
        _isInMovementPhase = inMovementPhase;
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

    public bool IsOccupied()
    {
        return _isOccupied;
    }

    public bool IsUnavailable()
    {
        return _isUnavailable;
    }

    public bool IsInMovementPhase()
    {
        return _isInMovementPhase;
    }

    private void OnMouseEntered()
    {
        EmitSignal(SignalName.TileHovered, this);
    }

    private void OnMouseExited()
    {
        EmitSignal(SignalName.TileUnhovered, this);
    }

    private bool IsClickWithinGameArea(Vector2 globalMousePosition)
    {
        // Find the Main scene to access the centralized constraint checking
        var mainScene = GetTree().CurrentScene as Main;
        if (mainScene != null)
        {
            return mainScene.IsMouseWithinGameArea(globalMousePosition);
        }
        
        // Fallback: allow click if Main scene not found
        return true;
    }
} 