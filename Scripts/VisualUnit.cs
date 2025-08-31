using Godot;
using Archistrateia;

public partial class VisualUnit : Area2D
{
    public Unit LogicalUnit { get; private set; }
    private Tween _animationTween;
    private AnimationPlayer _animationPlayer;
    private bool _isSelected = false;
    private MapRenderer _mapRenderer;

    [Signal]
    public delegate void UnitClickedEventHandler(VisualUnit visualUnit);

    public VisualUnit() { }

    public override void _Ready()
    {
        _animationTween = CreateTween();
        _animationPlayer = new AnimationPlayer();
        AddChild(_animationPlayer);
    }

    public void Initialize(Unit logicalUnit, Vector2 position, Color color)
    {
        LogicalUnit = logicalUnit;
        Position = position;
        ZIndex = 10;
        CreateVisualComponents(color);
        SetupClickDetection();
    }

    public void SetMapRenderer(MapRenderer mapRenderer)
    {
        _mapRenderer = mapRenderer;
    }

    private void SetupClickDetection()
    {
        var collisionShape = new CollisionShape2D();
        var shape = new CircleShape2D();
        shape.Radius = 20.0f * HexGridCalculator.ZoomFactor;
        collisionShape.Shape = shape;
        collisionShape.Name = "ClickCollision";
        AddChild(collisionShape);
        
        InputPickable = true;
        Monitoring = false;
        Monitorable = false;
        
        Connect("input_event", new Callable(this, MethodName.OnInputEvent));
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && 
            mouseEvent.Pressed && 
            mouseEvent.ButtonIndex == MouseButton.Left)
        {
            var localPos = ToLocal(mouseEvent.Position);
            if (localPos.Length() <= 20.0f * HexGridCalculator.ZoomFactor)
            {
                EmitSignal(SignalName.UnitClicked, this);
                GetViewport().SetInputAsHandled();
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
        unitOutline.Width = 2.0f * HexGridCalculator.ZoomFactor;
        AddChild(unitOutline);

        var unitLabel = new Label();
        unitLabel.Name = "UnitLabel";
        unitLabel.Text = LogicalUnit.Name.Substring(0, 1);
        // Use Godot's built-in positioning with zoom scaling
        unitLabel.Position = new Vector2(-5 * HexGridCalculator.ZoomFactor, -10 * HexGridCalculator.ZoomFactor);
        unitLabel.AddThemeColorOverride("font_color", new Color(1.0f, 1.0f, 1.0f));
        unitLabel.AddThemeFontSizeOverride("font_size", (int)(16 * HexGridCalculator.ZoomFactor));
        AddChild(unitLabel);
        
        // Movement status will be shown via border color changes
        // No separate indicator needed
    }

    private Vector2[] CreateUnitVertices()
    {
        // Use Godot's built-in circle generation for unit shapes
        var vertices = new Vector2[8];
        float scaledRadius = 15.0f * HexGridCalculator.ZoomFactor;
        
        // Generate octagon vertices using Godot's built-in math
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
        // Use Godot's built-in circle generation for outline
        var outlinePoints = new Vector2[9];
        float scaledRadius = 15.0f * HexGridCalculator.ZoomFactor;
        
        // Generate octagon outline using Godot's built-in math
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
        var unitSprite = GetNode<Polygon2D>("UnitSprite");
        if (unitSprite != null)
        {
            unitSprite.Polygon = CreateUnitVertices();
        }

        var unitOutline = GetNode<Line2D>("UnitOutline");
        if (unitOutline != null)
        {
            unitOutline.Points = CreateUnitOutlinePoints();
            unitOutline.Width = 2.0f * HexGridCalculator.ZoomFactor;
        }

        var collisionShape = GetNode<CollisionShape2D>("ClickCollision");
        if (collisionShape != null && collisionShape.Shape is CircleShape2D circleShape)
        {
            circleShape.Radius = 20.0f * HexGridCalculator.ZoomFactor;
        }

        var unitLabel = GetNode<Label>("UnitLabel");
        if (unitLabel != null)
        {
            unitLabel.Position = new Vector2(-5 * HexGridCalculator.ZoomFactor, -10 * HexGridCalculator.ZoomFactor);
            unitLabel.AddThemeFontSizeOverride("font_size", (int)(16 * HexGridCalculator.ZoomFactor));
        }

        var selectionRing = GetNodeOrNull<Line2D>("SelectionRing");
        if (selectionRing != null)
        {
            selectionRing.Points = CreateRingVertices(25.0f * HexGridCalculator.ZoomFactor);
            selectionRing.Width = 3.0f * HexGridCalculator.ZoomFactor;
        }

        var movementDisplay = GetNodeOrNull<Label>("MovementDisplay");
        if (movementDisplay != null)
        {
            movementDisplay.Position = new Vector2(-15 * HexGridCalculator.ZoomFactor, -35 * HexGridCalculator.ZoomFactor);
        }
        
        // Update movement indicator (border color)
        UpdateMovementIndicator();
    }

    private Player GetUnitOwner()
    {
        // Find the unit's owner by checking which player has this unit
        if (_mapRenderer?.GameManager?.Players != null)
        {
            foreach (var player in _mapRenderer.GameManager.Players)
            {
                if (player.Units.Contains(LogicalUnit))
                {
                    return player;
                }
            }
        }
        return null;
    }



    private void UpdateMovementIndicator()
    {
        // Change unit outline color based on movement status
        var unitOutline = GetNodeOrNull<Line2D>("UnitOutline");
        if (unitOutline != null)
        {
            // Use the stored MapRenderer reference
            if (_mapRenderer != null)
            {
                var currentPhase = _mapRenderer.GetCurrentPhase();
                var currentPlayer = _mapRenderer.GetCurrentPlayer();
                
                GD.Print($"   🔍 Unit {LogicalUnit.Name}: Phase={currentPhase}, CurrentPlayer={currentPlayer?.Name ?? "NULL"}");
                
                // Only show movement indicators for current player's units during movement phase
                if (currentPhase == GamePhase.Move && currentPlayer != null)
                {
                    // Check if this unit belongs to the current player
                    var unitOwner = GetUnitOwner();
                    GD.Print($"   🔍 Unit {LogicalUnit.Name}: UnitOwner={unitOwner?.Name ?? "NULL"}, MP={LogicalUnit.CurrentMovementPoints}");
                    
                    if (unitOwner == currentPlayer)
                    {
                        if (LogicalUnit.CurrentMovementPoints > 0)
                        {
                            // Green = can move (bright, visible)
                            unitOutline.DefaultColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                            GD.Print($"   🟢 Unit {LogicalUnit.Name}: Set GREEN border (can move)");
                        }
                        else
                        {
                            // Red = cannot move (bright, visible)
                            unitOutline.DefaultColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                            GD.Print($"   🔴 Unit {LogicalUnit.Name}: Set RED border (cannot move)");
                        }
                    }
                    else
                    {
                        // Not current player's unit - use default black outline
                        unitOutline.DefaultColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                        GD.Print($"   ⚫ Unit {LogicalUnit.Name}: Set BLACK border (not current player)");
                    }
                }
                else
                {
                    // Not movement phase - use default black outline
                    unitOutline.DefaultColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                    GD.Print($"   ⚫ Unit {LogicalUnit.Name}: Set BLACK border (not movement phase)");
                }
            }
            else
            {
                // Fallback to default black outline
                unitOutline.DefaultColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
                GD.Print($"   ⚫ Unit {LogicalUnit.Name}: Set BLACK border (MapRenderer not set)");
            }
        }
        else
        {
            GD.Print($"   ❌ Unit {LogicalUnit.Name}: No unitOutline found!");
        }
    }

    public void UpdatePosition(Vector2 newPosition)
    {
        Position = newPosition;
        
        if (GetNodeOrNull("SelectionRing") != null)
        {
            UpdateMovementPointsDisplay();
        }
    }

    public void SetSelected(bool selected)
    {
        if (selected)
        {
            if (GetNodeOrNull("SelectionRing") == null)
            {
                var ring = new Line2D();
                ring.Points = CreateRingVertices(25.0f * HexGridCalculator.ZoomFactor);
                ring.DefaultColor = new Color(1.0f, 1.0f, 0.0f, 0.9f);
                ring.Width = 3.0f * HexGridCalculator.ZoomFactor;
                ring.Name = "SelectionRing";
                ring.ZIndex = -1;
                AddChild(ring);
                
                AnimateSelectionRing(ring);
            }
            
            UpdateMovementPointsDisplay();
        }
        else
        {
            var ring = GetNodeOrNull<Line2D>("SelectionRing");
            if (ring != null)
            {
                AnimateDeselection(ring);
            }
            
            var movementDisplay = GetNodeOrNull<Label>("MovementDisplay");
            movementDisplay?.QueueFree();
        }
    }

    private void AnimateSelectionRing(Line2D ring)
    {
        _animationTween.Kill();
        _animationTween = CreateTween();
        
        ring.Modulate = new Color(1, 1, 0, 0);
        _animationTween.TweenProperty(ring, "modulate:a", 0.9f, 0.3f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
    }

    private void AnimateDeselection(Line2D ring)
    {
        _animationTween.Kill();
        _animationTween = CreateTween();
        
        _animationTween.TweenProperty(ring, "modulate:a", 0.0f, 0.2f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
        
        _animationTween.TweenCallback(Callable.From(() => ring.QueueFree()));
    }

    private Vector2[] CreateRingVertices(float radius)
    {
        // Use Godot's built-in circle generation for selection ring
        var vertices = new Vector2[9];
        for (int i = 0; i < 8; i++)
        {
            var angle = i * Mathf.Pi / 4.0f;
            vertices[i] = new Vector2(
                radius * Mathf.Cos(angle),
                radius * Mathf.Sin(angle)
            );
        }
        vertices[8] = vertices[0]; // Close the ring
        return vertices;
    }

    private void UpdateMovementPointsDisplay()
    {
        var existingDisplay = GetNodeOrNull("MovementDisplay");
        existingDisplay?.QueueFree();
        
        if (!MovementDisplayLogic.ShouldShowMovementDisplay(LogicalUnit, _isSelected))
        {
            return;
        }
        
        var movementDisplay = new Label();
        movementDisplay.Name = "MovementDisplay";
        movementDisplay.Text = MovementDisplayLogic.GetMovementDisplayText(LogicalUnit);
        // Use Godot's built-in positioning with zoom scaling
        movementDisplay.Position = new Vector2(-15 * HexGridCalculator.ZoomFactor, -35 * HexGridCalculator.ZoomFactor);
        movementDisplay.AddThemeColorOverride("font_color", new Color(1.0f, 1.0f, 0.0f));
        movementDisplay.AddThemeColorOverride("font_shadow_color", new Color(0.0f, 0.0f, 0.0f));
        movementDisplay.AddThemeFontSizeOverride("font_size", (int)(14 * HexGridCalculator.ZoomFactor));
        movementDisplay.ZIndex = 15;
        AddChild(movementDisplay);
        
        AnimateMovementDisplay(movementDisplay);
    }

    public void RefreshMovementDisplay()
    {
        // Force refresh of MP display when MP values change
        UpdateMovementPointsDisplay();
        
        // Also update the movement indicator
        UpdateMovementIndicator();
    }

    private void AnimateMovementDisplay(Label movementDisplay)
    {
        _animationTween.Kill();
        _animationTween = CreateTween();
        
        movementDisplay.Modulate = new Color(1, 1, 1, 0);
        _animationTween.TweenProperty(movementDisplay, "modulate:a", 1.0f, 0.3f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
    }
} 