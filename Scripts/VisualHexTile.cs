using Godot;
using Archistrateia;

public partial class VisualHexTile : Area2D
{
    public Vector2I GridPosition { get; private set; }
    public TerrainType TerrainType { get; private set; }
    
    [Signal]
    public delegate void TileClickedEventHandler(VisualHexTile tile);

    public void Initialize(Vector2I gridPosition, TerrainType terrainType, Color color, Vector2 worldPosition)
    {
        GridPosition = gridPosition;
        TerrainType = terrainType;
        Position = worldPosition;
        Name = $"VisualHexTile_{gridPosition.X}_{gridPosition.Y}";
        
        CreateVisualComponents(color);
        SetupClickDetection();
    }

    private void CreateVisualComponents(Color color)
    {
        var hexShape = new Polygon2D();
        hexShape.Polygon = HexGridCalculator.CreateHexagonVertices();
        hexShape.Color = color;
        hexShape.Name = "HexShape";
        AddChild(hexShape);

        var outline = new Line2D();
        outline.Points = HexGridCalculator.CreateHexagonVertices();
        outline.DefaultColor = new Color(0.2f, 0.2f, 0.2f);
        outline.Width = 2.0f;
        outline.Name = "HexOutline";
        AddChild(outline);
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
        var hexShape = GetNode<Polygon2D>("HexShape");
        if (hexShape != null)
        {
            hexShape.Polygon = HexGridCalculator.CreateHexagonVertices();
        }

        // Update outline vertices
        var outline = GetNode<Line2D>("HexOutline");
        if (outline != null)
        {
            outline.Points = HexGridCalculator.CreateHexagonVertices();
        }

        // Update collision shape
        var collisionShape = GetNode<CollisionShape2D>("TileClickArea");
        if (collisionShape != null && collisionShape.Shape is ConvexPolygonShape2D polygonShape)
        {
            polygonShape.Points = HexGridCalculator.CreateHexagonVertices();
        }

        // Update any overlay vertices
        var highlightOverlay = GetNodeOrNull<Polygon2D>("HighlightOverlay");
        if (highlightOverlay != null)
        {
            highlightOverlay.Polygon = HexGridCalculator.CreateHexagonVertices();
        }

        var grayOverlay = GetNodeOrNull<Polygon2D>("GrayOverlay");
        if (grayOverlay != null)
        {
            grayOverlay.Polygon = HexGridCalculator.CreateHexagonVertices();
        }

        var brightOverlay = GetNodeOrNull<Polygon2D>("BrightOverlay");
        if (brightOverlay != null)
        {
            brightOverlay.Polygon = HexGridCalculator.CreateHexagonVertices();
        }
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
        var hexShape = GetNode<Polygon2D>("HexShape");
        if (highlight)
        {
            if (highlightColor == default)
                highlightColor = new Color(1.0f, 1.0f, 0.0f, 0.7f); // Yellow highlight default
            
            // Add highlight overlay
            if (GetNodeOrNull("HighlightOverlay") == null)
            {
                var overlay = new Polygon2D();
                overlay.Polygon = HexGridCalculator.CreateHexagonVertices();
                overlay.Color = highlightColor;
                overlay.Name = "HighlightOverlay";
                overlay.ZIndex = 1;
                AddChild(overlay);
            }
        }
        else
        {
            // Remove highlight overlay IMMEDIATELY
            var overlay = GetNodeOrNull("HighlightOverlay");
            if (overlay != null)
            {
                RemoveChild(overlay);
                overlay.QueueFree();
            }
        }
    }

    public void SetGrayed(bool grayed)
    {
        var hexShape = GetNode<Polygon2D>("HexShape");
        if (grayed)
        {
            // Darken the hex significantly with a much darker overlay
            if (GetNodeOrNull("GrayOverlay") == null)
            {
                var overlay = new Polygon2D();
                overlay.Polygon = HexGridCalculator.CreateHexagonVertices();
                overlay.Color = new Color(0.0f, 0.0f, 0.0f, 0.85f); // Almost black overlay for maximum contrast
                overlay.Name = "GrayOverlay";
                overlay.ZIndex = 2; // Above highlight but below units
                AddChild(overlay);
            }
        }
        else
        {
            // Remove gray overlay IMMEDIATELY to fix timing issues
            var overlay = GetNodeOrNull("GrayOverlay");
            if (overlay != null)
            {
                RemoveChild(overlay);
                overlay.QueueFree(); // Still queue for cleanup, but remove from tree immediately
            }
        }
    }

    public void SetBrightened(bool brightened)
    {
        if (brightened)
        {
            // Add a much more prominent bright overlay for maximum visibility
            if (GetNodeOrNull("BrightOverlay") == null)
            {
                var overlay = new Polygon2D();
                overlay.Polygon = HexGridCalculator.CreateHexagonVertices();
                overlay.Color = new Color(1.0f, 1.0f, 0.8f, 0.6f); // Bright yellow-white overlay with higher opacity
                overlay.Name = "BrightOverlay";
                overlay.ZIndex = 1; // Below gray overlay
                AddChild(overlay);
            }
        }
        else
        {
            // Remove bright overlay IMMEDIATELY to fix timing issues
            var overlay = GetNodeOrNull("BrightOverlay");
            if (overlay != null)
            {
                RemoveChild(overlay);
                overlay.QueueFree(); // Still queue for cleanup, but remove from tree immediately
            }
        }
    }
} 