using Godot;

public partial class VisualUnit : Area2D
{
    public Unit LogicalUnit { get; private set; }
    public bool IsSelected { get; private set; }
    
    [Signal]
    public delegate void UnitClickedEventHandler(VisualUnit visualUnit);

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
        
        // Ensure units render on top of hex tiles
        ZIndex = 10;
        
        GD.Print($"🔧 Initializing VisualUnit for {LogicalUnit.Name} at {Position}");
        CreateVisualComponents(color);
        SetupClickDetection();
        GD.Print($"✅ VisualUnit {LogicalUnit.Name} fully initialized with ZIndex={ZIndex}");
    }

    private void SetupClickDetection()
    {
        GD.Print($"🎯 Setting up click detection for {LogicalUnit.Name}");
        
        // Create collision shape for click detection
        var collisionShape = new CollisionShape2D();
        var shape = new CircleShape2D();
        shape.Radius = 20.0f; // Slightly larger than visual for easier clicking
        collisionShape.Shape = shape;
        collisionShape.Name = "ClickCollision";
        AddChild(collisionShape);
        GD.Print($"   Added collision shape with radius {shape.Radius}");
        
        // Configure Area2D for input
        InputPickable = true;
        Monitoring = false;  // We don't need area monitoring
        Monitorable = false; // We don't need to be monitored
        GD.Print($"   InputPickable set to {InputPickable}");
        
        // Try signal connection
        Connect("input_event", new Callable(this, MethodName.OnInputEvent));
        GD.Print($"   input_event signal connected");
        
        GD.Print($"🎯 Click detection setup complete for {LogicalUnit.Name}");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && 
            mouseEvent.Pressed && 
            mouseEvent.ButtonIndex == MouseButton.Left)
        {
            // Check if the click is within our bounds
            var localPos = ToLocal(mouseEvent.Position);
            if (localPos.Length() <= 20.0f) // Within our collision radius
            {
                GD.Print($"🟢 _Input: Unit {LogicalUnit.Name} LEFT CLICKED at local {localPos}!");
                EmitSignal(SignalName.UnitClicked, this);
                GetViewport().SetInputAsHandled(); // Mark event as handled
            }
        }
    }

    private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
    {
        GD.Print($"🔍 Input event received on {LogicalUnit.Name}: {@event.GetType().Name}");
        
        if (@event is InputEventMouseButton mouseEvent && 
            mouseEvent.Pressed && 
            mouseEvent.ButtonIndex == MouseButton.Left)
        {
            GD.Print($"🖱️ Unit {LogicalUnit.Name} LEFT CLICKED! Position: {Position}");
            EmitSignal(SignalName.UnitClicked, this);
        }
        else if (@event is InputEventMouseButton mouseEvent2)
        {
            GD.Print($"🖱️ Mouse event on {LogicalUnit.Name}: Pressed={mouseEvent2.Pressed}, Button={mouseEvent2.ButtonIndex}");
        }
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