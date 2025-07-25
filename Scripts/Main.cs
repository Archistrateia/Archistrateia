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
    private const int MAP_WIDTH = 14;
    private const int MAP_HEIGHT = 10;

    public override void _Ready()
    {
        GD.Print("=== MAIN: _Ready() called ===");
        GD.Print("🚀 NEW VERSION OF MAIN.CS IS LOADED! 🚀");
        GD.Print($"Main loaded. Node path: {GetPath()}");

        // Try to find UI elements if references are null
        if (StartButton == null)
        {
            GD.Print("StartButton reference is null, trying to find it...");
            StartButton = GetNodeOrNull<Button>("UI/StartButton");
            if (StartButton != null)
            {
                GD.Print("Found StartButton in scene!");
            }
            else
            {
                GD.PrintErr("ERROR: Could not find StartButton in scene!");
            }
        }

        if (TitleLabel == null)
        {
            GD.Print("TitleLabel reference is null, trying to find it...");
            TitleLabel = GetNodeOrNull<Label>("UI/TitleLabel");
            if (TitleLabel != null)
            {
                GD.Print("Found TitleLabel in scene!");
            }
            else
            {
                GD.PrintErr("ERROR: Could not find TitleLabel in scene!");
            }
        }

        GD.Print($"StartButton reference: {(StartButton != null ? "VALID" : "NULL")}");
        GD.Print($"TitleLabel reference: {(TitleLabel != null ? "VALID" : "NULL")}");
        GD.Print($"TurnManager reference: {(TurnManager != null ? "VALID" : "NULL")}");

        InitializeTerrainColors();

        GD.Print("=== MAIN: _Ready() completed ===");
    }

    private void InitializeTerrainColors()
    {
        GD.Print("=== MAIN: InitializeTerrainColors() called ===");
        _terrainColors = new Dictionary<TerrainType, Color>
        {
            { TerrainType.Desert, new Color(0.9f, 0.8f, 0.6f) },
            { TerrainType.Hill, new Color(0.6f, 0.5f, 0.3f) },
            { TerrainType.River, new Color(0.3f, 0.6f, 0.9f) },
            { TerrainType.Shoreline, new Color(0.8f, 0.7f, 0.5f) },
            { TerrainType.Lagoon, new Color(0.2f, 0.5f, 0.7f) }
        };
        GD.Print($"Initialized {_terrainColors.Count} terrain colors");
        GD.Print("=== MAIN: InitializeTerrainColors() completed ===");
    }

    public void OnStartButtonPressed()
    {
        GD.Print("=== MAIN: OnStartButtonPressed() called ===");
        GD.Print("Start Button Pressed. Game Starting...");

        // Try to find StartButton if reference is null
        if (StartButton == null)
        {
            GD.Print("StartButton reference is null, trying to find it...");
            StartButton = GetNodeOrNull<Button>("UI/StartButton");
            if (StartButton != null)
            {
                GD.Print("Found StartButton in scene!");
            }
            else
            {
                GD.PrintErr("ERROR: Could not find StartButton in scene!");
            }
        }

        if (StartButton != null)
        {
            GD.Print("Hiding StartButton");
            StartButton.Visible = false;
        }
        else
        {
            GD.PrintErr("ERROR: StartButton is still null!");
        }

        // Hide title label when game starts (standard approach)
        if (TitleLabel != null)
        {
            GD.Print("Hiding TitleLabel");
            TitleLabel.Visible = false;
        }

        // Create dedicated game status label
        _gameStatusLabel = new Label();
        _gameStatusLabel.Text = "Turn 1 - Movement";
        _gameStatusLabel.Position = new Vector2(10, 10);
        AddChild(_gameStatusLabel);
        GD.Print("Game status label created");

        GD.Print("Calling GenerateMap()");
        GenerateMap();

        // Initialize game logic after visual map is created
        GD.Print("Initializing GameManager");
        InitializeGameManager();

        GD.Print("Creating Next Phase button");
        _nextPhaseButton = new Button();
        _nextPhaseButton.Text = "Next Phase";
        _nextPhaseButton.Position = new Vector2(10, GetViewport().GetVisibleRect().Size.Y - 50);
        _nextPhaseButton.Pressed += OnNextPhaseButtonPressed;
        AddChild(_nextPhaseButton);
        GD.Print("Next Phase button added to scene");

        GD.Print("=== MAIN: OnStartButtonPressed() completed ===");
    }

    private void GenerateMap()
    {
        GD.Print("=== MAIN: GenerateMap() called ===");

        if (_mapContainer != null)
        {
            GD.Print("Clearing existing map container");
            _mapContainer.QueueFree();
        }

        GD.Print("Creating new map container");
        _mapContainer = new Node2D();
        _mapContainer.Name = "MapContainer";
        AddChild(_mapContainer);
        GD.Print($"MapContainer added to scene at path: {_mapContainer.GetPath()}");

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

                if (tilesCreated % 25 == 0)
                {
                    GD.Print($"Created {tilesCreated} visual tiles...");
                }
            }
        }

        GD.Print($"Generated hex map with {tilesCreated} visual tiles");
        GD.Print($"MapContainer now has {_mapContainer.GetChildCount()} children");
        GD.Print("=== MAIN: GenerateMap() completed ===");
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
        
        GD.Print("GameManager added to scene, connecting TurnManager deferred");
    }

    private void ConnectTurnManager()
    {
        // Use the GameManager's TurnManager instead of the exported one
        TurnManager = _gameManager.TurnManager;
        
        GD.Print("GameManager initialized with players and units");
        GD.Print($"TurnManager connected: {TurnManager != null}");
        
        if (TurnManager != null)
        {
            GD.Print($"Current phase: {TurnManager.CurrentPhase}");
        }
        
        // Now initialize MapRenderer with proper TurnManager
        GD.Print("Initializing MapRenderer with connected TurnManager");
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
        GD.Print($"MapRenderer set to phase: {TurnManager.CurrentPhase}");
        
        // Register all visual tiles with the MapRenderer
        RegisterVisualTilesWithMapRenderer();
        
        CreateVisualUnitsForPlayers();
        GD.Print("MapRenderer initialized with visual units and tiles");
    }

    private void RegisterVisualTilesWithMapRenderer()
    {
        if (_mapContainer == null || _mapRenderer == null) return;
        
        int tilesRegistered = 0;
        foreach (Node child in _mapContainer.GetChildren())
        {
            if (child is VisualHexTile visualTile)
            {
                _mapRenderer.AddVisualTile(visualTile);
                tilesRegistered++;
            }
        }
        
        GD.Print($"🗺️ Registered {tilesRegistered} visual tiles with MapRenderer");
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
                    GD.Print($"Created visual unit for {unit.Name} at ({logicalTile.Position.X}, {logicalTile.Position.Y})");
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
        GD.Print("=== MAIN: OnNextPhaseButtonPressed() called ===");
        GD.Print("Next Phase button pressed.");

        if (TurnManager != null)
        {
            GD.Print("Advancing phase via TurnManager");
            TurnManager.AdvancePhase();
            
            // Handle phase-specific actions with GameManager
            if (_gameManager != null)
            {
                HandlePhaseChange(TurnManager.CurrentPhase);
            }
        }
        else
        {
            GD.PrintErr("ERROR: TurnManager is null!");
        }

        UpdateTitleLabel();
        GD.Print("=== MAIN: OnNextPhaseButtonPressed() completed ===");
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
        GD.Print("=== MAIN: UpdateTitleLabel() called ===");

        // During gameplay, update the game status label instead of title label
        if (_gameStatusLabel != null && TurnManager != null)
        {
            var currentPlayerName = "Unknown";
            if (_gameManager?.Players.Count > 0 && _currentPlayerIndex < _gameManager.Players.Count)
            {
                currentPlayerName = _gameManager.Players[_currentPlayerIndex].Name;
            }
            
            var newText = $"Turn {TurnManager.CurrentTurn} - {TurnManager.CurrentPhase} - {currentPlayerName}";
            GD.Print($"Updating game status to: {newText}");
            _gameStatusLabel.Text = newText;
        }
        // On title screen, update the title label
        else if (TitleLabel != null && TurnManager != null)
        {
            var newText = $"Turn {TurnManager.CurrentTurn} - {TurnManager.CurrentPhase}";
            GD.Print($"Updating title to: {newText}");
            TitleLabel.Text = newText;
        }
        // Initial state - game not started yet
        else if (TitleLabel != null && TurnManager == null)
        {
            GD.Print("Initial state - keeping title as is");
        }
        else
        {
            GD.Print($"UpdateTitleLabel called but components not ready: GameStatusLabel={(_gameStatusLabel == null ? "NULL" : "VALID")}, TitleLabel={(TitleLabel == null ? "NULL" : "VALID")}, TurnManager={(TurnManager == null ? "NULL" : "VALID")}");
        }

        GD.Print("=== MAIN: UpdateTitleLabel() completed ===");
    }

    public override void _Input(InputEvent @event)
    {
        // This should capture ALL input events before they're processed
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            GD.Print($"🔴 _Input: Mouse click detected at {mouseEvent.Position}");
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Space)
        {
            GD.Print("=== MAIN: Space key pressed ===");
            if (TurnManager != null)
            {
                TurnManager.AdvancePhase();
                UpdateTitleLabel();
            }
            else
            {
                GD.PrintErr("ERROR: TurnManager is null on space key press!");
            }
        }
        
        // Debug mouse clicks
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            GD.Print($"🖱️ Global mouse click detected: Button={mouseEvent.ButtonIndex}, Position={mouseEvent.Position}");
        }
    }
}
