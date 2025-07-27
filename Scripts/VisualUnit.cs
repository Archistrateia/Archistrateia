using Godot;
using Archistrateia;

public partial class VisualUnit : Area2D
{
    public Unit LogicalUnit { get; private set; }
    private MovementDisplayLogic _movementDisplayLogic = new MovementDisplayLogic();

    [Signal]
    public delegate void UnitClickedEventHandler(VisualUnit visualUnit);

    public VisualUnit() { } // Parameterless constructor for Godot

    public void Initialize(Unit logicalUnit, Vector2 position, Color color)
    {
        LogicalUnit = logicalUnit;
        Position = position;
        ZIndex = 10; // Ensure units render on top of hex tiles
        CreateVisualComponents(color);
        SetupClickDetection();
    }

    private void SetupClickDetection()
    {
        // Create collision shape for click detection
        var collisionShape = new CollisionShape2D();
        var shape = new CircleShape2D();
        shape.Radius = 20.0f * HexGridCalculator.ZoomFactor; // Scale collision with zoom
        collisionShape.Shape = shape;
        collisionShape.Name = "ClickCollision";
        AddChild(collisionShape);
        
        // Configure Area2D for input
        InputPickable = true;
        Monitoring = false;  // We don't need area monitoring
        Monitorable = false; // We don't need to be monitored
        
        // Try signal connection
        Connect("input_event", new Callable(this, MethodName.OnInputEvent));
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && 
            mouseEvent.Pressed && 
            mouseEvent.ButtonIndex == MouseButton.Left)
        {
            // Check if the click is within our bounds
            var localPos = ToLocal(mouseEvent.Position);
            if (localPos.Length() <= 20.0f * HexGridCalculator.ZoomFactor) // Scale collision radius with zoom
            {
                EmitSignal(SignalName.UnitClicked, this);
                GetViewport().SetInputAsHandled(); // Mark event as handled
            }
        }
    }

    private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent && 
            mouseEvent.Pressed && 
            mouseEvent.ButtonIndex == MouseButton.Left)
        {
            EmitSignal(SignalName.UnitClicked, this);
        }
    }

    private void CreateVisualComponents(Color color)
    {
        var unitSprite = new Polygon2D();
        unitSprite.Name = "UnitSprite";
        var vertices = CreateUnitVertices();
        unitSprite.Polygon = vertices;
        unitSprite.Color = color;
        AddChild(unitSprite);

        var unitOutline = new Line2D();
        unitOutline.Name = "UnitOutline";
        var outlinePoints = CreateUnitOutlinePoints();
        unitOutline.Points = outlinePoints;
        unitOutline.DefaultColor = new Color(0.0f, 0.0f, 0.0f);
        unitOutline.Width = 2.0f * HexGridCalculator.ZoomFactor; // Scale outline width with zoom
        AddChild(unitOutline);

        var unitLabel = new Label();
        unitLabel.Name = "UnitLabel";
        unitLabel.Text = LogicalUnit.Name.Substring(0, 1);
        unitLabel.Position = new Vector2(-5 * HexGridCalculator.ZoomFactor, -10 * HexGridCalculator.ZoomFactor);
        unitLabel.AddThemeColorOverride("font_color", new Color(1.0f, 1.0f, 1.0f));
        unitLabel.AddThemeFontSizeOverride("font_size", (int)(16 * HexGridCalculator.ZoomFactor));
        AddChild(unitLabel);
    }

    private Vector2[] CreateUnitVertices()
    {
        var vertices = new Vector2[8];
        float scaledRadius = 15.0f * HexGridCalculator.ZoomFactor;
        
        for (int i = 0; i < 8; i++)
        {
            var angle = i * Mathf.Pi / 4.0f;
            vertices[i] = new Vector2(
                scaledRadius * Mathf.Cos(angle),
                scaledRadius * Mathf.Sin(angle)
            );
        }
        return vertices;
    }

    private Vector2[] CreateUnitOutlinePoints()
    {
        var outlinePoints = new Vector2[9];
        float scaledRadius = 15.0f * HexGridCalculator.ZoomFactor;
        
        for (int i = 0; i < 8; i++)
        {
            var angle = i * Mathf.Pi / 4.0f;
            outlinePoints[i] = new Vector2(
                scaledRadius * Mathf.Cos(angle),
                scaledRadius * Mathf.Sin(angle)
            );
        }
        outlinePoints[8] = outlinePoints[0]; // Close the outline
        return outlinePoints;
    }

    public void UpdateVisualComponents()
    {
        // Update unit sprite vertices
        var unitSprite = GetNode<Polygon2D>("UnitSprite");
        if (unitSprite != null)
        {
            unitSprite.Polygon = CreateUnitVertices();
        }

        // Update unit outline
        var unitOutline = GetNode<Line2D>("UnitOutline");
        if (unitOutline != null)
        {
            unitOutline.Points = CreateUnitOutlinePoints();
            unitOutline.Width = 2.0f * HexGridCalculator.ZoomFactor;
        }

        // Update collision shape
        var collisionShape = GetNode<CollisionShape2D>("ClickCollision");
        if (collisionShape != null && collisionShape.Shape is CircleShape2D circleShape)
        {
            circleShape.Radius = 20.0f * HexGridCalculator.ZoomFactor;
        }

        // Update unit label position
        var unitLabel = GetNode<Label>("UnitLabel");
        if (unitLabel != null)
        {
            unitLabel.Position = new Vector2(-5 * HexGridCalculator.ZoomFactor, -10 * HexGridCalculator.ZoomFactor);
            unitLabel.AddThemeFontSizeOverride("font_size", (int)(16 * HexGridCalculator.ZoomFactor));
        }

        // Update selection ring if it exists
        var selectionRing = GetNodeOrNull<Line2D>("SelectionRing");
        if (selectionRing != null)
        {
            selectionRing.Points = CreateRingVertices(25.0f * HexGridCalculator.ZoomFactor);
            selectionRing.Width = 3.0f * HexGridCalculator.ZoomFactor;
        }

        // Update movement display position
        var movementDisplay = GetNodeOrNull<Label>("MovementDisplay");
        if (movementDisplay != null)
        {
            movementDisplay.Position = new Vector2(-15 * HexGridCalculator.ZoomFactor, -35 * HexGridCalculator.ZoomFactor);
        }
    }

    public void UpdatePosition(Vector2 newPosition)
    {
        Position = newPosition;
        
        // Update movement points display if unit is selected
        if (GetNodeOrNull("SelectionRing") != null)
        {
            UpdateMovementPointsDisplay();
        }
    }

    public void SetSelected(bool selected)
    {
        if (selected)
        {
            // Add thin yellow selection ring outline
            if (GetNodeOrNull("SelectionRing") == null)
            {
                var ring = new Line2D();
                ring.Points = CreateRingVertices(25.0f * HexGridCalculator.ZoomFactor); // Scale with zoom
                ring.DefaultColor = new Color(1.0f, 1.0f, 0.0f, 0.9f); // Yellow outline
                ring.Width = 3.0f * HexGridCalculator.ZoomFactor; // Scale line width with zoom
                ring.Name = "SelectionRing";
                ring.ZIndex = -1; // Behind unit
                AddChild(ring);
            }
            
            // Add movement points indicator
            UpdateMovementPointsDisplay();
        }
        else
        {
            // Remove selection ring
            var ring = GetNodeOrNull("SelectionRing");
            ring?.QueueFree();
            
            // Remove movement points display
            var movementDisplay = GetNodeOrNull("MovementDisplay");
            movementDisplay?.QueueFree();
        }
    }

    private Vector2[] CreateRingVertices(float radius)
    {
        var vertices = new Vector2[9];
        for (int i = 0; i < 8; i++)
        {
            var angle = i * Mathf.Pi / 4.0f;
            vertices[i] = new Vector2(
                radius * Mathf.Cos(angle),
                radius * Mathf.Sin(angle)
            );
        }
        vertices[8] = vertices[0];
        return vertices;
    }

    private void UpdateMovementPointsDisplay()
    {
        // Remove existing display
        var existingDisplay = GetNodeOrNull("MovementDisplay");
        existingDisplay?.QueueFree();
        
        // Check if we should show the display using tested logic
        if (!_movementDisplayLogic.ShouldShowMovementDisplay(LogicalUnit, isSelected: true))
        {
            return;
        }
        
        // Create new movement points display using tested logic
        var movementDisplay = new Label();
        movementDisplay.Name = "MovementDisplay";
        movementDisplay.Text = _movementDisplayLogic.GetMovementDisplayText(LogicalUnit);
        movementDisplay.Position = new Vector2(-15 * HexGridCalculator.ZoomFactor, -35 * HexGridCalculator.ZoomFactor); // Scale position with zoom
        movementDisplay.AddThemeColorOverride("font_color", new Color(1.0f, 1.0f, 0.0f)); // Yellow text
        movementDisplay.AddThemeColorOverride("font_shadow_color", new Color(0.0f, 0.0f, 0.0f)); // Black shadow
        movementDisplay.AddThemeFontSizeOverride("font_size", (int)(14 * HexGridCalculator.ZoomFactor));
        movementDisplay.ZIndex = 15; // On top
        AddChild(movementDisplay);
    }
} 