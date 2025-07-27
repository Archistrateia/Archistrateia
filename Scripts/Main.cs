using Godot;
using System.Collections.Generic;
using Archistrateia;

public partial class Main : Control
{
    [Export]
    public TurnManager TurnManager { get; set; }

    [Export]
    public Label TitleLabel { get; set; }

    [Export]
    public Button StartButton { get; set; }

    private GameManager _gameManager;
    private Button _nextPhaseButton;
    private Node2D _mapContainer;
    private Label _gameStatusLabel;
    private MapRenderer _mapRenderer;
    private Dictionary<TerrainType, Color> _terrainColors;
    private int _currentPlayerIndex = 0;
    
    // Zoom control UI elements
    private HSlider _zoomSlider;
    private Button _zoomInButton;
    private Button _zoomOutButton;
    private Button _resetZoomButton;
    private Label _zoomLabel;
    
    // Map dimensions come from centralized configuration
    private int MAP_WIDTH => MapConfiguration.MAP_WIDTH;
    private int MAP_HEIGHT => MapConfiguration.MAP_HEIGHT;

    public override void _Ready()
    {
        // Try to find UI elements if references are null
        if (StartButton == null)
        {
            StartButton = GetNodeOrNull<Button>("UI/StartButton");
        }

        if (TitleLabel == null)
        {
            TitleLabel = GetNodeOrNull<Label>("UI/TitleLabel");
        }

        InitializeTerrainColors();
        CreateZoomControls();
    }

    private void InitializeTerrainColors()
    {
        _terrainColors = new Dictionary<TerrainType, Color>
        {
            { TerrainType.Desert, new Color(0.9f, 0.8f, 0.6f) },
            { TerrainType.Hill, new Color(0.6f, 0.5f, 0.3f) },
            { TerrainType.River, new Color(0.3f, 0.6f, 0.9f) },
            { TerrainType.Shoreline, new Color(0.8f, 0.7f, 0.5f) },
            { TerrainType.Lagoon, new Color(0.2f, 0.5f, 0.7f) }
        };
    }

    private void CreateZoomControls()
    {
        // Create a container for zoom controls
        var zoomContainer = new VBoxContainer();
        zoomContainer.Position = new Vector2(GetViewport().GetVisibleRect().Size.X - 200, 10);
        zoomContainer.Size = new Vector2(180, 120);
        AddChild(zoomContainer);

        // Zoom label
        _zoomLabel = new Label();
        _zoomLabel.Text = "Zoom: 1.0x";
        _zoomLabel.HorizontalAlignment = HorizontalAlignment.Center;
        zoomContainer.AddChild(_zoomLabel);

        // Zoom slider
        _zoomSlider = new HSlider();
        _zoomSlider.MinValue = 0.1f;
        _zoomSlider.MaxValue = 3.0f;
        _zoomSlider.Value = 1.0f;
        _zoomSlider.Step = 0.1f;
        _zoomSlider.ValueChanged += OnZoomSliderChanged;
        zoomContainer.AddChild(_zoomSlider);

        // Zoom buttons container
        var buttonContainer = new HBoxContainer();
        zoomContainer.AddChild(buttonContainer);

        // Zoom out button
        _zoomOutButton = new Button();
        _zoomOutButton.Text = "-";
        _zoomOutButton.Size = new Vector2(40, 30);
        _zoomOutButton.Pressed += OnZoomOutPressed;
        buttonContainer.AddChild(_zoomOutButton);

        // Reset zoom button
        _resetZoomButton = new Button();
        _resetZoomButton.Text = "Reset";
        _resetZoomButton.Size = new Vector2(60, 30);
        _resetZoomButton.Pressed += OnResetZoomPressed;
        buttonContainer.AddChild(_resetZoomButton);

        // Zoom in button
        _zoomInButton = new Button();
        _zoomInButton.Text = "+";
        _zoomInButton.Size = new Vector2(40, 30);
        _zoomInButton.Pressed += OnZoomInPressed;
        buttonContainer.AddChild(_zoomInButton);
    }

    private void OnZoomSliderChanged(double value)
    {
        HexGridCalculator.SetZoom((float)value);
        RegenerateMapWithCurrentZoom();
        UpdateTitleLabel();
        UpdateZoomLabel();
    }

    private void OnZoomInPressed()
    {
        HexGridCalculator.ZoomIn();
        _zoomSlider.Value = HexGridCalculator.ZoomFactor;
        RegenerateMapWithCurrentZoom();
        UpdateTitleLabel();
        UpdateZoomLabel();
    }

    private void OnZoomOutPressed()
    {
        HexGridCalculator.ZoomOut();
        _zoomSlider.Value = HexGridCalculator.ZoomFactor;
        RegenerateMapWithCurrentZoom();
        UpdateTitleLabel();
        UpdateZoomLabel();
    }

    private void OnResetZoomPressed()
    {
        HexGridCalculator.SetZoom(1.0f);
        _zoomSlider.Value = HexGridCalculator.ZoomFactor;
        RegenerateMapWithCurrentZoom();
        UpdateTitleLabel();
        UpdateZoomLabel();
    }

    private void UpdateZoomLabel()
    {
        if (_zoomLabel != null)
        {
            _zoomLabel.Text = $"Zoom: {HexGridCalculator.ZoomFactor:F1}x";
        }
    }

    public void OnStartButtonPressed()
    {
        // Try to find StartButton if reference is null
        if (StartButton == null)
        {
            StartButton = GetNodeOrNull<Button>("UI/StartButton");
        }

        if (StartButton != null)
        {
            StartButton.Visible = false;
        }

        // Hide title label when game starts
        if (TitleLabel != null)
        {
            TitleLabel.Visible = false;
        }

        // Create dedicated game status label
        _gameStatusLabel = new Label();
        _gameStatusLabel.Position = new Vector2(10, 10);
        AddChild(_gameStatusLabel);

        GenerateMap();
        InitializeGameManager();

        // Create Next Phase button
        _nextPhaseButton = new Button();
        _nextPhaseButton.Text = "Next Phase";
        _nextPhaseButton.Position = new Vector2(10, GetViewport().GetVisibleRect().Size.Y - 50);
        _nextPhaseButton.Pressed += OnNextPhaseButtonPressed;
        AddChild(_nextPhaseButton);
    }

    private void GenerateMap()
    {
        if (_mapContainer != null)
        {
            _mapContainer.QueueFree();
        }

        _mapContainer = new Node2D();
        _mapContainer.Name = "MapContainer";
        AddChild(_mapContainer);

        int tilesCreated = 0;
        for (int x = 0; x < MAP_WIDTH; x++)
        {
            for (int y = 0; y < MAP_HEIGHT; y++)
            {
                var terrainType = GetRandomTerrainType();
                var worldPosition = HexGridCalculator.CalculateHexPositionCentered(x, y, GetViewport().GetVisibleRect().Size, MAP_WIDTH, MAP_HEIGHT);
                var gridPosition = new Vector2I(x, y);
                
                var visualTile = new VisualHexTile();
                visualTile.Initialize(gridPosition, terrainType, _terrainColors[terrainType], worldPosition);
                _mapContainer.AddChild(visualTile);
                
                tilesCreated++;
            }
        }

        GD.Print($"Generated hex map with {tilesCreated} tiles");
    }

    private void RegenerateMapWithCurrentZoom()
    {
        if (_mapContainer != null)
        {
            // Update all existing tiles' visual components
            foreach (Node child in _mapContainer.GetChildren())
            {
                if (child is VisualHexTile visualTile)
                {
                    // Update the tile's position with new zoom
                    var worldPosition = HexGridCalculator.CalculateHexPositionCentered(
                        visualTile.GridPosition.X, 
                        visualTile.GridPosition.Y, 
                        GetViewport().GetVisibleRect().Size, 
                        MAP_WIDTH, 
                        MAP_HEIGHT
                    );
                    visualTile.Position = worldPosition;
                    
                    // Update the visual components (hex shape, outline, collision)
                    visualTile.UpdateVisualComponents();
                }
            }
            
            // Update visual units' positions and visual components
            if (_mapRenderer != null)
            {
                foreach (var visualUnit in _mapRenderer.GetVisualUnits())
                {
                    // Find the logical tile this unit occupies
                    var logicalTile = FindLogicalTileWithUnit(visualUnit.LogicalUnit);
                    if (logicalTile != null)
                    {
                        // Calculate new position with current zoom
                        var newWorldPosition = HexGridCalculator.CalculateHexPositionCentered(
                            logicalTile.Position.X, 
                            logicalTile.Position.Y, 
                            GetViewport().GetVisibleRect().Size, 
                            MAP_WIDTH, 
                            MAP_HEIGHT
                        );
                        
                        // Update unit position and visual components
                        visualUnit.UpdatePosition(newWorldPosition);
                        visualUnit.UpdateVisualComponents();
                    }
                }
            }
        }
    }

    private TerrainType GetRandomTerrainType()
    {
        var terrainTypes = System.Enum.GetValues<TerrainType>();
        var randomType = terrainTypes[GD.RandRange(0, terrainTypes.Length - 1)];
        return randomType;
    }

    private void InitializeGameManager()
    {
        _gameManager = new GameManager();
        AddChild(_gameManager);
        
        // Use CallDeferred to connect TurnManager after GameManager's _Ready is called
        CallDeferred(MethodName.ConnectTurnManager);
    }

    private void ConnectTurnManager()
    {
        // Use the GameManager's TurnManager instead of the exported one
        TurnManager = _gameManager.TurnManager;
        
        // Now initialize MapRenderer with proper TurnManager
        InitializeMapRenderer();
    }

    private void InitializeMapRenderer()
    {
        _mapRenderer = new MapRenderer();
        _mapRenderer.Name = "MapRenderer";
        AddChild(_mapRenderer);
        _mapRenderer.Initialize(_gameManager);
        
        // Set initial player and phase
        if (_gameManager.Players.Count > 0)
        {
            _mapRenderer.SetCurrentPlayer(_gameManager.Players[_currentPlayerIndex]);
        }
        
        // TurnManager is now guaranteed to be available
        _mapRenderer.SetCurrentPhase(TurnManager.CurrentPhase);
        
        // Update the title label with the correct initial phase
        UpdateTitleLabel();
        
        // Register all visual tiles with the MapRenderer
        RegisterVisualTilesWithMapRenderer();
        
        CreateVisualUnitsForPlayers();
    }

    private void RegisterVisualTilesWithMapRenderer()
    {
        if (_mapContainer == null || _mapRenderer == null) return;
        
        foreach (Node child in _mapContainer.GetChildren())
        {
            if (child is VisualHexTile visualTile)
            {
                _mapRenderer.AddVisualTile(visualTile);
            }
        }
    }

    private void CreateVisualUnitsForPlayers()
    {
        if (_gameManager == null || _mapRenderer == null) return;

        foreach (var player in _gameManager.Players)
        {
            var playerColor = player.Name == "Pharaoh" ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.2f, 0.2f, 0.8f);
            
            foreach (var unit in player.Units)
            {
                var logicalTile = FindLogicalTileWithUnit(unit);
                if (logicalTile != null)
                {
                    var worldPosition = HexGridCalculator.CalculateHexPositionCentered(
                        logicalTile.Position.X, 
                        logicalTile.Position.Y, 
                        GetViewport().GetVisibleRect().Size, 
                        MAP_WIDTH, 
                        MAP_HEIGHT
                    );
                    
                    _mapRenderer.CreateVisualUnit(unit, worldPosition, playerColor);
                }
            }
        }
    }

    private HexTile FindLogicalTileWithUnit(Unit unit)
    {
        foreach (var kvp in _gameManager.GameMap)
        {
            if (kvp.Value.OccupyingUnit == unit)
            {
                return kvp.Value;
            }
        }
        return null;
    }

    private void OnNextPhaseButtonPressed()
    {
        if (TurnManager != null)
        {
            TurnManager.AdvancePhase();
            
            // Handle phase-specific actions with GameManager
            if (_gameManager != null)
            {
                HandlePhaseChange(TurnManager.CurrentPhase);
            }
        }

        UpdateTitleLabel();
    }

    private void HandlePhaseChange(GamePhase phase)
    {
        // Update MapRenderer with current phase
        if (_mapRenderer != null)
        {
            _mapRenderer.SetCurrentPhase(phase);
        }

        switch (phase)
        {
            case GamePhase.Earn:
                GD.Print("=== EARN PHASE: Processing city income ===");
                _gameManager.ProcessEarnPhase();
                // Switch to next player at start of new turn
                SwitchToNextPlayer();
                break;
            case GamePhase.Purchase:
                GD.Print("=== PURCHASE PHASE: Players can buy units ===");
                break;
            case GamePhase.Move:
                GD.Print("=== MOVE PHASE: Units can move ===");
                foreach (var player in _gameManager.Players)
                {
                    player.ResetUnitMovement();
                }
                // Deselect any units when entering move phase
                if (_mapRenderer != null)
                {
                    _mapRenderer.DeselectAll();
                }
                break;
            case GamePhase.Combat:
                GD.Print("=== COMBAT PHASE: Combat resolution ===");
                break;
        }
    }

    private void SwitchToNextPlayer()
    {
        if (_gameManager?.Players.Count > 0)
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _gameManager.Players.Count;
            var currentPlayer = _gameManager.Players[_currentPlayerIndex];
            
            GD.Print($"Switched to player: {currentPlayer.Name}");
            
            // Update MapRenderer with new current player
            if (_mapRenderer != null)
            {
                _mapRenderer.SetCurrentPlayer(currentPlayer);
            }
        }
    }

    private void UpdateTitleLabel()
    {
        // During gameplay, update the game status label instead of title label
        if (_gameStatusLabel != null && TurnManager != null)
        {
            var currentPlayerName = "Unknown";
            if (_gameManager?.Players.Count > 0 && _currentPlayerIndex < _gameManager.Players.Count)
            {
                currentPlayerName = _gameManager.Players[_currentPlayerIndex].Name;
            }
            
            var newText = $"Turn {TurnManager.CurrentTurn} - {TurnManager.CurrentPhase} - {currentPlayerName} - Zoom: {HexGridCalculator.ZoomFactor:F1}x";
            _gameStatusLabel.Text = newText;
        }
        // On title screen, update the title label
        else if (TitleLabel != null && TurnManager != null)
        {
            var newText = $"Turn {TurnManager.CurrentTurn} - {TurnManager.CurrentPhase}";
            TitleLabel.Text = newText;
        }
    }

    public override void _Input(InputEvent @event)
    {
        // Handle zoom controls
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
            {
                HexGridCalculator.ZoomIn();
                _zoomSlider.Value = HexGridCalculator.ZoomFactor;
                RegenerateMapWithCurrentZoom();
                UpdateTitleLabel();
                UpdateZoomLabel();
                GetViewport().SetInputAsHandled();
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
            {
                HexGridCalculator.ZoomOut();
                _zoomSlider.Value = HexGridCalculator.ZoomFactor;
                RegenerateMapWithCurrentZoom();
                UpdateTitleLabel();
                UpdateZoomLabel();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Space)
            {
                if (TurnManager != null)
                {
                    TurnManager.AdvancePhase();
                    UpdateTitleLabel();
                }
            }
            else if (keyEvent.Keycode == Key.Equal || keyEvent.Keycode == Key.KpAdd)
            {
                // Plus key for zoom in
                HexGridCalculator.ZoomIn();
                _zoomSlider.Value = HexGridCalculator.ZoomFactor;
                RegenerateMapWithCurrentZoom();
                UpdateTitleLabel();
                UpdateZoomLabel();
                GetViewport().SetInputAsHandled();
            }
            else if (keyEvent.Keycode == Key.Minus || keyEvent.Keycode == Key.KpSubtract)
            {
                // Minus key for zoom out
                HexGridCalculator.ZoomOut();
                _zoomSlider.Value = HexGridCalculator.ZoomFactor;
                RegenerateMapWithCurrentZoom();
                UpdateTitleLabel();
                UpdateZoomLabel();
                GetViewport().SetInputAsHandled();
            }
            else if (keyEvent.Keycode == Key.Key0)
            {
                // Reset zoom to 1.0
                HexGridCalculator.SetZoom(1.0f);
                _zoomSlider.Value = HexGridCalculator.ZoomFactor;
                RegenerateMapWithCurrentZoom();
                UpdateTitleLabel();
                UpdateZoomLabel();
                GetViewport().SetInputAsHandled();
            }
        }
    }
}
