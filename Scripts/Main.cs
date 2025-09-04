using Godot;
using System.Collections.Generic;
using System.Linq;
using Archistrateia;

namespace Archistrateia
{
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
        private MapRenderer _mapRenderer;
        private Dictionary<TerrainType, Color> _terrainColors;
        private int _currentPlayerIndex = 0;
        
        // Modern UI Manager
        private ModernUIManager _uiManager;
        
        // Zoom control UI elements
        private HSlider _zoomSlider;
        private Label _zoomLabel;
        
        // Map generation UI elements
        private OptionButton _mapTypeSelector;
        private Button _regenerateMapButton;
        private Label _mapTypeDescriptionLabel;
        private MapType _currentMapType = MapType.Continental;
        
        // Debug functionality
        private Button _debugAdjacentButton;
        private bool _debugAdjacentMode = false;
        private VisualHexTile _lastHoveredTile = null;
        
        // Game state
        private bool _gameStarted = false;
        
        // Map dimensions come from centralized configuration
        private static int MAP_WIDTH => MapConfiguration.MAP_WIDTH;
        private static int MAP_HEIGHT => MapConfiguration.MAP_HEIGHT;
        
        // Centralized services
        private VisualPositionManager _positionManager;
        private ViewportController _viewportController;
        private TileUnitCoordinator _tileUnitCoordinator;
        
        // Centralized viewport size calculation to ensure consistency between tiles and units
        private Vector2 GetGameAreaSize()
        {
            var gameAreaSize = GetViewport().GetVisibleRect().Size;
            gameAreaSize.X -= 200; // Subtract sidebar width
            GD.Print($"🔧 VIEWPORT: Full={GetViewport().GetVisibleRect().Size.X}x{GetViewport().GetVisibleRect().Size.Y}, Game={gameAreaSize.X}x{gameAreaSize.Y}");
            return gameAreaSize;
        }

        private const float EDGE_SCROLL_THRESHOLD = 50.0f; // pixels from edge to trigger scrolling
        private const float SCROLL_SPEED = 300.0f; // pixels per second

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
            
            // Use CallDeferred to ensure viewport is ready before creating UI
            CallDeferred(MethodName.InitializeUI);
        }

        private void InitializeUI()
        {
            GD.Print("🚀 Starting modern UI initialization...");
            
            // Create the modern UI manager
            _uiManager = new ModernUIManager();
            _uiManager.Name = "ModernUIManager";
            AddChild(_uiManager);
            
            // Get references to UI elements from the modern UI
            _nextPhaseButton = _uiManager.GetNextPhaseButton();
            _mapTypeSelector = _uiManager.GetMapTypeSelector();
            _regenerateMapButton = _uiManager.GetRegenerateMapButton();
            StartButton = _uiManager.GetStartGameButton();
            _zoomSlider = _uiManager.GetZoomSlider();
            _zoomLabel = _uiManager.GetZoomLabel();
            
            // Initialize centralized services
            InitializeCentralizedServices();
            
            // Connect signals for the new UI
            if (_nextPhaseButton != null)
            {
                _nextPhaseButton.Pressed += OnNextPhaseButtonPressed;
            }
            
            if (_mapTypeSelector != null)
            {
                _mapTypeSelector.ItemSelected += OnMapTypeSelected;
            }
            
            if (_regenerateMapButton != null)
            {
                _regenerateMapButton.Pressed += OnRegenerateMapPressed;
            }
            
            if (_zoomSlider != null)
            {
                _zoomSlider.ValueChanged += OnZoomSliderChanged;
            }
            
            if (StartButton != null)
            {
                StartButton.Pressed += OnStartButtonPressed;
            }
            
            // Generate initial map before game starts
            GenerateMap();
            
            GD.Print("✨ Modern UI initialization complete");
        }

        private void CreateStartButton()
        {
            // Don't create the start button here - it will be added to the map controls panel
            GD.Print("✨ Start button will be created in map controls");
        }

        private void CreateTitleLabel()
        {
            var viewportSize = GetViewport().GetVisibleRect().Size;
            GD.Print($"🖥️ Viewport size for title: {viewportSize}");
            
            TitleLabel = new Label();
            TitleLabel.Text = "Archistrateia";
            TitleLabel.Position = new Vector2(viewportSize.X / 2 - 150, 20);
            TitleLabel.Size = new Vector2(300, 60);
            TitleLabel.AddThemeFontSizeOverride("font_size", 48);
            TitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            TitleLabel.ZIndex = 1000;
            AddChild(TitleLabel);
            GD.Print($"✨ Created title label at position: {TitleLabel.Position}");
        }

        private void InitializeTerrainColors()
        {
            _terrainColors = new Dictionary<TerrainType, Color>
            {
                { TerrainType.Desert, new Color(0.9f, 0.8f, 0.6f) },
                { TerrainType.Hill, new Color(0.6f, 0.5f, 0.3f) },
                { TerrainType.River, new Color(0.3f, 0.6f, 0.9f) },
                { TerrainType.Shoreline, new Color(0.8f, 0.7f, 0.5f) },
                { TerrainType.Lagoon, new Color(0.2f, 0.5f, 0.7f) },
                { TerrainType.Grassland, new Color(0.4f, 0.8f, 0.3f) },
                { TerrainType.Mountain, new Color(0.5f, 0.4f, 0.4f) },
                { TerrainType.Water, new Color(0.1f, 0.4f, 0.8f) }
            };
        }

        private void InitializeCentralizedServices()
        {
            GD.Print("🔧 Initializing centralized services...");
            
            // Initialize position manager with game area size
            var gameAreaSize = GetGameAreaSize();
            _positionManager = new VisualPositionManager(gameAreaSize, MAP_WIDTH, MAP_HEIGHT);
            
            // Initialize viewport controller with callback to update positions when view changes
            _viewportController = new ViewportController(MAP_WIDTH, MAP_HEIGHT, OnViewChanged);
            
            // Initialize tile-unit coordinator
            _tileUnitCoordinator = new TileUnitCoordinator();
            
            GD.Print($"✅ Centralized services initialized | GameArea: {gameAreaSize.X}x{gameAreaSize.Y} | Map: {MAP_WIDTH}x{MAP_HEIGHT}");
        }

        private void OnViewChanged()
        {
            // Update game area size in position manager
            _positionManager.UpdateGameAreaSize(GetGameAreaSize());
            
            // Update all visual positions
            if (_mapContainer != null && _mapRenderer != null)
            {
                _positionManager.UpdateAllPositions(_mapContainer, _mapRenderer.GetVisualUnits(), _gameManager?.GameMap, _tileUnitCoordinator);
            }
            
            // Update zoom UI
            if (_zoomSlider != null)
            {
                _zoomSlider.Value = HexGridCalculator.ZoomFactor;
                UpdateZoomLabel();
            }
        }

        private void CreateMapGenerationControls()
        {
            var viewportSize = GetViewport().GetVisibleRect().Size;
            GD.Print($"🖥️ Viewport size for controls: {viewportSize}");
            
            // Create a background panel for map generation controls
            var mapControlPanel = new Panel();
            mapControlPanel.Position = new Vector2(viewportSize.X - 180, 80);
            mapControlPanel.Size = new Vector2(170, 180); // Increased height for start button
            mapControlPanel.ZIndex = 1000;
            mapControlPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
            AddChild(mapControlPanel);
            GD.Print($"📋 Created map control panel at position: {mapControlPanel.Position}");

            var mapContainer = new VBoxContainer();
            mapContainer.Position = new Vector2(5, 5);
            mapContainer.Size = new Vector2(160, 170); // Increased height
            mapContainer.AddThemeConstantOverride("separation", 3);
            mapControlPanel.AddChild(mapContainer);

            // Map type label
            var mapTypeLabel = new Label();
            mapTypeLabel.Text = "Map Type:";
            mapTypeLabel.AddThemeFontSizeOverride("font_size", 12);
            mapContainer.AddChild(mapTypeLabel);

            // Map type selector
            _mapTypeSelector = new OptionButton();
            _mapTypeSelector.CustomMinimumSize = new Vector2(150, 25);
            
            foreach (MapType mapType in System.Enum.GetValues<MapType>())
            {
                var config = MapTypeConfiguration.GetConfig(mapType);
                _mapTypeSelector.AddItem(config.Name);
            }
            
            _mapTypeSelector.Selected = 0; // Continental
            _mapTypeSelector.Connect("item_selected", new Callable(this, MethodName.OnMapTypeSelected));
            mapContainer.AddChild(_mapTypeSelector);

            // Map description
            _mapTypeDescriptionLabel = new Label();
            _mapTypeDescriptionLabel.Text = MapTypeConfiguration.GetConfig(_currentMapType).Description;
            _mapTypeDescriptionLabel.AddThemeFontSizeOverride("font_size", 9);
            _mapTypeDescriptionLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _mapTypeDescriptionLabel.CustomMinimumSize = new Vector2(150, 50);
            _mapTypeDescriptionLabel.VerticalAlignment = VerticalAlignment.Top;
            mapContainer.AddChild(_mapTypeDescriptionLabel);

            // Regenerate button
            _regenerateMapButton = new Button();
            _regenerateMapButton.Text = "Regenerate";
            _regenerateMapButton.CustomMinimumSize = new Vector2(150, 25);
            _regenerateMapButton.Connect("pressed", new Callable(this, MethodName.OnRegenerateMapPressed));
            mapContainer.AddChild(_regenerateMapButton);

            // Start Game button (only visible before game starts)
            if (StartButton == null)
            {
                StartButton = new Button();
                StartButton.Text = "Start Game";
                StartButton.CustomMinimumSize = new Vector2(150, 35);
                StartButton.AddThemeFontSizeOverride("font_size", 16);
                StartButton.Connect("pressed", new Callable(this, MethodName.OnStartButtonPressed));
                mapContainer.AddChild(StartButton);
                GD.Print("✨ Created and added new Start Game button to controls panel");
            }
            else
            {
                // Start button exists from scene - just add it to the controls panel
                if (StartButton.GetParent() != null)
                {
                    StartButton.GetParent().RemoveChild(StartButton);
                }
                StartButton.Text = "Start Game";
                StartButton.CustomMinimumSize = new Vector2(150, 35);
                StartButton.AddThemeFontSizeOverride("font_size", 16);
                mapContainer.AddChild(StartButton);
                GD.Print("✨ Moved existing Start Game button to controls panel");
            }
        }

        private void CreateZoomControls()
        {
            // Create a background panel for zoom controls
            var backgroundPanel = new Panel();
            backgroundPanel.Position = new Vector2(GetViewport().GetVisibleRect().Size.X - 150, 10);
            backgroundPanel.Size = new Vector2(130, 60);
            backgroundPanel.ZIndex = 1000; // Ensure zoom controls are always on top
            backgroundPanel.MouseFilter = Control.MouseFilterEnum.Ignore; // Don't block mouse events
            AddChild(backgroundPanel);

            // Create a container for zoom controls
            var zoomContainer = new VBoxContainer();
            zoomContainer.Position = new Vector2(5, 5); // Small margin from panel edges
            zoomContainer.Size = new Vector2(120, 50);
            zoomContainer.CustomMinimumSize = new Vector2(120, 50); // Ensure minimum size
            backgroundPanel.AddChild(zoomContainer);

            // Zoom label
            _zoomLabel = new Label();
            _zoomLabel.Text = "Zoom: 1.0x";
            _zoomLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _zoomLabel.AddThemeFontSizeOverride("font_size", 16);
            _zoomLabel.CustomMinimumSize = new Vector2(120, 20); // Set minimum size
            zoomContainer.AddChild(_zoomLabel);

            // Zoom slider
            _zoomSlider = new HSlider();
            _zoomSlider.MinValue = 0.1f;
            _zoomSlider.MaxValue = 3.0f;
            _zoomSlider.Value = 1.0f;
            _zoomSlider.Step = 0.1f;
            _zoomSlider.CustomMinimumSize = new Vector2(120, 20); // Set minimum size
            _zoomSlider.MouseFilter = Control.MouseFilterEnum.Stop; // Ensure slider receives mouse events
            _zoomSlider.ValueChanged += OnZoomSliderChanged;
            _zoomSlider.GuiInput += OnZoomSliderInput; // Add input event handler
            _zoomSlider.AllowGreater = false; // Ensure value stays within bounds
            _zoomSlider.AllowLesser = false; // Ensure value stays within bounds
            zoomContainer.AddChild(_zoomSlider);
        }

        private void OnZoomSliderChanged(double value)
        {
            _viewportController?.SetZoom((float)value);
            UpdateTitleLabel();
            UpdateUIPositions(); // Update UI positions when zoom changes
        }
        
        private void OnZoomSliderInput(InputEvent @event)
        {
            GD.Print($"Zoom slider received input event: {@event.GetType().Name}");
            
            // Handle mouse button events manually if the slider isn't responding
            if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
            {
                GD.Print($"Zoom slider mouse button pressed: {mouseEvent.ButtonIndex}");
                
                // Use Godot's built-in slider value calculation
                var localPos = _zoomSlider.GetLocalMousePosition();
                var sliderWidth = _zoomSlider.Size.X;
                var normalizedPos = localPos.X / sliderWidth;
                
                // Calculate new value using Godot's built-in math functions
                var newValue = _zoomSlider.MinValue + (normalizedPos * (_zoomSlider.MaxValue - _zoomSlider.MinValue));
                newValue = Mathf.Clamp(newValue, _zoomSlider.MinValue, _zoomSlider.MaxValue);
                
                GD.Print($"Calculated new zoom value: {newValue} from mouse position {localPos}");
                
                // Update the slider value and trigger change event
                _zoomSlider.Value = newValue;
                OnZoomSliderChanged(newValue);
            }
        }



        private void UpdateZoomLabel()
        {
            if (_zoomLabel != null)
            {
                _zoomLabel.Text = $"Zoom: {HexGridCalculator.ZoomFactor:F1}x";
            }
        }
        
        private void UpdateUIPositions()
        {
            var viewportSize = GetViewport().GetVisibleRect().Size;
            
            // Note: Next Phase button is now handled by modern UI (doesn't need positioning)
            
            // Update Debug Adjacent button position
            if (_debugAdjacentButton != null)
            {
                _debugAdjacentButton.Position = new Vector2(10, viewportSize.Y - 50); // Move to left since Next Phase button is gone
            }
            
            // Update zoom controls position
            if (_zoomSlider != null)
            {
                var zoomPanel = _zoomSlider.GetParent().GetParent() as Panel;
                if (zoomPanel != null)
                {
                    zoomPanel.Position = new Vector2(viewportSize.X - 150, 10);
                }
            }
            
            // Status panel is now handled by ModernUIManager
        }

        public void OnStartButtonPressed()
        {
            _gameStarted = true;
            
            // Hide start button using modern UI manager
            if (_uiManager != null)
            {
                _uiManager.HideStartButton();
            }
            else if (StartButton != null)
            {
                StartButton.Visible = false;
            }

            // Hide title label when game starts
            if (TitleLabel != null)
            {
                TitleLabel.Visible = false;
            }

            // Hide map generation controls when game starts
            HideMapGenerationControls();

            // Game status is now handled by ModernUIManager in the top bar

            // Keep the current zoom level (1.0 from preview) - don't change it
            // The map is already at the right size from the preview
            
            // Update zoom slider to reflect the current zoom
            if (_zoomSlider != null)
            {
                _zoomSlider.Value = HexGridCalculator.ZoomFactor;
            }
            
            // Update zoom label to reflect the optimal zoom
            UpdateZoomLabel();

            // Use the existing map as the game map (no regeneration needed)
            GD.Print("🎮 Starting game with current map - no regeneration needed");
            InitializeGameManager();
            
            // Ensure zoom stays at 1.0 after game initialization
            HexGridCalculator.SetZoom(1.0f);
            if (_zoomSlider != null)
            {
                _zoomSlider.Value = 1.0f;
            }
            UpdateZoomLabel();
            
            // Regenerate map visuals with correct zoom
            RegenerateMapWithCurrentZoom();
            
            // Update UI positions after everything is initialized
            UpdateUIPositions();

            // Note: Next Phase button is now handled by the modern UI (no need to create here)
            
            // Create Debug Adjacent Tiles button
            _debugAdjacentButton = new Button();
            _debugAdjacentButton.Text = "Debug Adjacent";
            _debugAdjacentButton.Position = new Vector2(10, GetViewport().GetVisibleRect().Size.Y - 50);
            _debugAdjacentButton.Size = new Vector2(120, 40); // Set explicit size
            _debugAdjacentButton.AddThemeFontSizeOverride("font_size", 16);
            _debugAdjacentButton.ZIndex = 1000; // Ensure UI is always on top
            _debugAdjacentButton.MouseFilter = Control.MouseFilterEnum.Stop; // Ensure button receives mouse events
            _debugAdjacentButton.Pressed += OnDebugAdjacentButtonPressed;
            AddChild(_debugAdjacentButton);
            
            // Debug UI elements to help troubleshoot
            DebugUIElements();
        }

        // Centralized map generation method - handles both preview and game map needs
        private void GenerateMap()
        {
            if (_mapContainer != null)
            {
                _mapContainer.QueueFree();
            }

            // Set map to full size (zoom 1.0) for consistent generation
            HexGridCalculator.SetZoom(1.0f);
            
            // Update zoom slider to match
            if (_zoomSlider != null)
            {
                _zoomSlider.Value = 1.0f;
            }
            UpdateZoomLabel();
            
            // Reset scroll offset when generating new map
            _viewportController?.ResetScroll();

            _mapContainer = new Node2D();
            _mapContainer.Name = "MapContainer";
            _mapContainer.ZIndex = 0; // Ensure map is below UI elements
            
            // Add to the game area if modern UI is available, otherwise to main
            if (_uiManager != null && _uiManager.GetGameArea() != null)
            {
                _uiManager.GetGameArea().AddChild(_mapContainer);
            }
            else
            {
                AddChild(_mapContainer);
            }

            var gameMap = MapGenerator.GenerateMap(MAP_WIDTH, MAP_HEIGHT, _currentMapType);
            int tilesCreated = 0;
            
            foreach (var kvp in gameMap)
            {
                var gridPosition = kvp.Key;
                var tile = kvp.Value;
                var terrainType = tile.TerrainType;
                
                // Use centralized position manager for consistent positioning
                var worldPosition = _positionManager.CalculateWorldPosition(gridPosition);
                GD.Print($"🗺️ MAP GEN: Grid({gridPosition.X},{gridPosition.Y}) -> World({worldPosition.X:F1},{worldPosition.Y:F1}) | Type({terrainType})");
                
                var visualTile = new VisualHexTile();
                visualTile.Initialize(gridPosition, terrainType, _terrainColors[terrainType], worldPosition);
                _mapContainer.AddChild(visualTile);
                
                tilesCreated++;
            }

            GD.Print($"🗺️ Generated map with {tilesCreated} tiles of type {_currentMapType}");
        }


        private void OnMapTypeSelected(long index)
        {
            if (_gameStarted)
            {
                // Game has started - map type selection is disabled
                return;
            }
            
            var mapTypes = System.Enum.GetValues<MapType>();
            if (index >= 0 && index < mapTypes.Length)
            {
                _currentMapType = mapTypes[index];
                var config = MapTypeConfiguration.GetConfig(_currentMapType);
                _mapTypeDescriptionLabel.Text = config.Description;
                GD.Print($"🗺️ Selected map type: {config.Name}");
                
                // Regenerate map with new type
                GenerateMap();
            }
        }
        
        private void HideMapGenerationControls()
        {
            // Find and hide the map generation control panel
            foreach (Node child in GetChildren())
            {
                if (child is Panel panel && panel.GetChildCount() > 0)
                {
                    // Check if this panel contains map generation controls
                    var container = panel.GetChild(0);
                    if (container is VBoxContainer vbox)
                    {
                        // Look for the map type selector
                        foreach (Node vboxChild in vbox.GetChildren())
                        {
                            if (vboxChild is OptionButton)
                            {
                                // This is the map generation panel
                                panel.Visible = false;
                                GD.Print("🙈 Hidden map generation controls");
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void OnRegenerateMapPressed()
        {
            if (_gameStarted)
            {
                // Game has started - regeneration is disabled
                GD.Print("⚠️ Map regeneration disabled during gameplay");
                return;
            }
            
            GD.Print($"🔄 Regenerating map as {_currentMapType}");
            GenerateMap();
        }




        private void RegenerateMapWithCurrentZoom()
        {
            // Delegate to centralized position manager
            _positionManager?.UpdateAllPositions(_mapContainer, _mapRenderer?.GetVisualUnits() ?? new List<VisualUnit>(), _gameManager?.GameMap, _tileUnitCoordinator);
        }



        private void InitializeGameManager()
        {
            _gameManager = new GameManager();
            AddChild(_gameManager);
            
            // Convert the visual map to a logical game map
            var logicalGameMap = ConvertVisualMapToGameMap();
            
            // Set the game map before initialization
            _gameManager.SetGameMap(logicalGameMap);
            
            // Now initialize the game with the logical map
            _gameManager.InitializeGame();
            
            // Use CallDeferred to connect TurnManager after GameManager's _Ready is called
            CallDeferred(MethodName.ConnectTurnManager);
        }

        private Dictionary<Vector2I, HexTile> ConvertVisualMapToGameMap()
        {
            var gameMap = new Dictionary<Vector2I, HexTile>();
            
            if (_mapContainer != null)
            {
                foreach (Node child in _mapContainer.GetChildren())
                {
                    if (child is VisualHexTile visualTile)
                    {
                        var logicalTile = new HexTile(visualTile.GridPosition, visualTile.TerrainType);
                        gameMap[visualTile.GridPosition] = logicalTile;
                    }
                }
            }
            
            GD.Print($"🔄 Converted visual map to logical game map with {gameMap.Count} tiles");
            return gameMap;
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
            _mapRenderer.Initialize(_gameManager, _tileUnitCoordinator, _mapContainer);
            
            // Connect MapRenderer to GameManager for phase change notifications
            _gameManager.SetMapRenderer(_mapRenderer);
            
            // Set initial player and phase
            if (_gameManager.Players.Count > 0)
            {
                _mapRenderer.SetCurrentPlayer(_gameManager.Players[_currentPlayerIndex]);
            }
            
            // TurnManager is now guaranteed to be available
            _mapRenderer.SetCurrentPhase(TurnManager.CurrentPhase);
            
            // Ensure all tiles know the current phase state
            _mapRenderer.UpdateTileOccupationStatus();
            
            // Update the title label with the correct initial phase
            UpdateTitleLabel();
            
            // Register all visual tiles with the MapRenderer
            RegisterVisualTilesWithMapRenderer();
            
            CreateVisualUnitsForPlayers();
            
            // Update initial tile occupation status
            _mapRenderer.UpdateTileOccupationStatus();
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
            // Delegate to centralized tile-unit coordinator
            _tileUnitCoordinator?.CreateVisualUnitsForPlayers(
                _gameManager?.Players,
                _gameManager?.GameMap,
                _mapRenderer,
                _positionManager,
                _mapContainer
            );
        }


        private void OnNextPhaseButtonPressed()
        {
            GD.Print("🔘 Next Phase button pressed");
            
            if (TurnManager == null)
            {
                GD.PrintErr("❌ TurnManager is null! Cannot advance phase.");
                return;
            }
            
            GD.Print($"📋 Current phase before advance: {TurnManager.CurrentPhase}");
            
            try
            {
                TurnManager.AdvancePhase();
                GD.Print($"📋 New phase after advance: {TurnManager.CurrentPhase}");
                
                // Handle phase-specific actions with GameManager
                if (_gameManager != null)
                {
                    HandlePhaseChange(TurnManager.CurrentPhase);
                }
                else
                {
                    GD.PrintErr("❌ GameManager is null! Cannot handle phase change.");
                }
                
                // Update MapRenderer with new phase
                if (_mapRenderer != null)
                {
                    _mapRenderer.OnPhaseChanged(TurnManager.CurrentPhase);
                }
                else
                {
                    GD.PrintErr("❌ MapRenderer is null! Cannot update phase.");
                }
                
                UpdateTitleLabel();
                GD.Print("✅ Phase advance completed successfully");
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"❌ Error advancing phase: {ex.Message}");
                GD.PrintErr($"Stack trace: {ex.StackTrace}");
            }
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
            if (TurnManager == null) return;
            
            var currentPlayerName = "Unknown";
            if (_gameManager?.Players.Count > 0 && _currentPlayerIndex < _gameManager.Players.Count)
            {
                currentPlayerName = _gameManager.Players[_currentPlayerIndex].Name;
            }
            
            // Update modern UI if available
            if (_uiManager != null)
            {
                _uiManager.UpdatePlayerInfo(currentPlayerName, TurnManager.CurrentPhase.ToString(), TurnManager.CurrentTurn);
            }
            
            // On title screen, update the title label
            if (TitleLabel != null)
            {
                var newText = $"Turn {TurnManager.CurrentTurn} - {TurnManager.CurrentPhase}";
                TitleLabel.Text = newText;
            }
        }

        public override void _Input(InputEvent @event)
        {
            // Handle debug adjacent hover functionality
            if (_debugAdjacentMode && @event is InputEventMouseMotion)
            {
                var mousePosition = GetViewport().GetMousePosition();
                HandleDebugAdjacentHover(mousePosition);
            }
            
            // Handle zoom controls through ViewportController
            if (@event is InputEventMouseButton mouseEvent)
            {
                var handled = _viewportController?.HandleMouseInput(mouseEvent, GetViewport().GetVisibleRect().Size) ?? false;
                
                if (handled)
                {
                    GetViewport().SetInputAsHandled();
                }
                else if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    // Debug mouse clicks to see if UI controls are being detected
                    var mousePosition = GetViewport().GetMousePosition();
                    DebugMousePosition(mousePosition);
                    
                    // Don't mark left clicks as handled to allow UI controls to receive them
                }
            }
            
            // Handle two-finger scroll for Mac trackpad through ViewportController
            if (@event is InputEventPanGesture panGesture)
            {
                _viewportController?.HandlePanGesture(panGesture, GetViewport().GetVisibleRect().Size, IsMouseOverUIControls);
                GetViewport().SetInputAsHandled();
            }
            
            // Don't handle other input events to allow UI controls to receive them
        }

        public override void _Process(double delta)
        {
            HandleEdgeScrolling(delta);
        }

        private void HandleEdgeScrolling(double delta)
        {
            var mousePosition = GetViewport().GetMousePosition();
            var viewportSize = GetViewport().GetVisibleRect().Size;
            
            // Only activate scrolling if the grid extends beyond the viewport
            if (_viewportController == null || !_viewportController.IsScrollingNeeded(viewportSize))
            {
                return;
            }
            
            // Check if mouse is hovering over UI controls - if so, don't scroll
            if (IsMouseOverUIControls(mousePosition))
            {
                return;
            }
            
            var scrollDelta = Vector2.Zero;
            
            // Check if mouse is near edges
            if (mousePosition.X < EDGE_SCROLL_THRESHOLD)
            {
                scrollDelta.X = -SCROLL_SPEED * (float)delta;
            }
            else if (mousePosition.X > viewportSize.X - EDGE_SCROLL_THRESHOLD)
            {
                scrollDelta.X = SCROLL_SPEED * (float)delta;
            }
            
            if (mousePosition.Y < EDGE_SCROLL_THRESHOLD)
            {
                scrollDelta.Y = -SCROLL_SPEED * (float)delta;
            }
            else if (mousePosition.Y > viewportSize.Y - EDGE_SCROLL_THRESHOLD)
            {
                scrollDelta.Y = SCROLL_SPEED * (float)delta;
            }
            
            // Apply scroll delta if any
            if (scrollDelta != Vector2.Zero)
            {
                _viewportController.ApplyScrollDelta(scrollDelta, viewportSize);
            }
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                if (HandlePhaseInput(keyEvent)) return;
                if (HandleZoomInput(keyEvent)) return;
                HandleScrollInput(keyEvent);
            }
        }

        private bool HandlePhaseInput(InputEventKey keyEvent)
        {
            if (keyEvent.Keycode == Key.Space)
            {
                if (TurnManager != null)
                {
                    TurnManager.AdvancePhase();
                    UpdateTitleLabel();
                }
                return true;
            }
            return false;
        }

        private bool HandleZoomInput(InputEventKey keyEvent)
        {
            var handled = _viewportController?.HandleKeyboardInput(keyEvent, GetViewport().GetVisibleRect().Size) ?? false;
            
            if (handled)
            {
                GetViewport().SetInputAsHandled();
            }
            
            return handled;
        }

        private bool HandleScrollInput(InputEventKey keyEvent)
        {
            var handled = _viewportController?.HandleKeyboardInput(keyEvent, GetViewport().GetVisibleRect().Size) ?? false;
            
            if (handled)
            {
                GetViewport().SetInputAsHandled();
            }
            
            return handled;
        }

        private void UpdateZoomUI()
        {
            _zoomSlider.Value = HexGridCalculator.ZoomFactor;
            RegenerateMapWithCurrentZoom();
            UpdateTitleLabel();
            UpdateZoomLabel();
        }

        private void ApplyScrollDelta(Vector2 scrollDelta)
        {
            _viewportController?.ApplyScrollDelta(scrollDelta, GetViewport().GetVisibleRect().Size);
        }
        
        
        public bool IsMouseOverUIControls(Vector2 mousePosition)
        {
            // Check if mouse is over zoom controls (top-right panel)
            if (_zoomSlider != null)
            {
                var zoomPanel = _zoomSlider.GetParent().GetParent() as Panel;
                if (zoomPanel != null)
                {
                    var panelRect = new Rect2(zoomPanel.GlobalPosition, zoomPanel.Size);
                    if (panelRect.HasPoint(mousePosition))
                    {
                        return true;
                    }
                }
            }
            
            // Check if mouse is over Next Phase button
            if (_nextPhaseButton != null)
            {
                var buttonRect = new Rect2(_nextPhaseButton.GlobalPosition, _nextPhaseButton.Size);
                if (buttonRect.HasPoint(mousePosition))
                {
                    return true;
                }
            }
            
            // Check if mouse is over Debug Adjacent button
            if (_debugAdjacentButton != null)
            {
                var buttonRect = new Rect2(_debugAdjacentButton.GlobalPosition, _debugAdjacentButton.Size);
                if (buttonRect.HasPoint(mousePosition))
                {
                    return true;
                }
            }
            
            // Game status panel is now handled by ModernUIManager
            
            return false;
        }
        
        // Enhanced debug method to help troubleshoot UI issues with mouse coordinates
        private void DebugMousePosition(Vector2 mousePosition)
        {
            GD.Print($"=== DEBUGGING MOUSE POSITION: {mousePosition} ===");
            
            if (_nextPhaseButton != null)
            {
                var buttonRect = new Rect2(_nextPhaseButton.GlobalPosition, _nextPhaseButton.Size);
                bool overButton = buttonRect.HasPoint(mousePosition);
                GD.Print($"Next Phase Button: GlobalPos={_nextPhaseButton.GlobalPosition}, Size={_nextPhaseButton.Size}, Over={overButton}");
            }
            
            if (_zoomSlider != null)
            {
                var zoomPanel = _zoomSlider.GetParent().GetParent() as Panel;
                if (zoomPanel != null)
                {
                    var panelRect = new Rect2(zoomPanel.GlobalPosition, zoomPanel.Size);
                    bool overZoom = panelRect.HasPoint(mousePosition);
                    GD.Print($"Zoom Panel: GlobalPos={zoomPanel.GlobalPosition}, Size={zoomPanel.Size}, Over={overZoom}");
                }
            }
            
            // Game status panel is now handled by ModernUIManager
        }
        
        // Debug method to help troubleshoot UI issues
        private void DebugUIElements()
        {
            GD.Print("=== DEBUGGING UI ELEMENTS ===");
            
            if (_nextPhaseButton != null)
            {
                GD.Print($"Next Phase Button: Position={_nextPhaseButton.GlobalPosition}, Size={_nextPhaseButton.Size}, Visible={_nextPhaseButton.Visible}");
            }
            else
            {
                GD.Print("Next Phase Button: NULL");
            }
            
            if (_zoomSlider != null)
            {
                var zoomPanel = _zoomSlider.GetParent().GetParent() as Panel;
                if (zoomPanel != null)
                {
                    GD.Print($"Zoom Panel: Position={zoomPanel.GlobalPosition}, Size={zoomPanel.Size}, Visible={zoomPanel.Visible}");
                }
                GD.Print($"Zoom Slider: Position={_zoomSlider.GlobalPosition}, Size={_zoomSlider.Size}, Visible={_zoomSlider.Visible}");
            }
            else
            {
                GD.Print("Zoom Slider: NULL");
            }
            
            // Game status panel is now handled by ModernUIManager
        }
        
        private void OnDebugAdjacentButtonPressed()
        {
            _debugAdjacentMode = !_debugAdjacentMode;
            _debugAdjacentButton.Text = _debugAdjacentMode ? "Debug Adjacent: ON" : "Debug Adjacent";
            
            if (!_debugAdjacentMode)
            {
                // Clear any existing debug highlights
                ClearDebugHighlights();
            }
            
            GD.Print($"Debug adjacent mode: {(_debugAdjacentMode ? "ENABLED" : "DISABLED")}");
        }
        
        private void ClearDebugHighlights()
        {
            if (_mapContainer == null) return;
            
            foreach (Node child in _mapContainer.GetChildren())
            {
                if (child is VisualHexTile visualTile)
                {
                    visualTile.SetHighlight(false);
                }
            }
            
            _lastHoveredTile = null;
        }
        
        private void HandleDebugAdjacentHover(Vector2 mousePosition)
        {
            if (!_debugAdjacentMode || _mapContainer == null) return;
            
            // Find the tile under the mouse
            VisualHexTile hoveredTile = null;
            foreach (Node child in _mapContainer.GetChildren())
            {
                if (child is VisualHexTile visualTile)
                {
                    var localPos = visualTile.ToLocal(mousePosition);
                    if (IsPointInHexagon(localPos, visualTile))
                    {
                        hoveredTile = visualTile;
                        break;
                    }
                }
            }
            
            // If we're hovering over a different tile, update highlights
            if (hoveredTile != _lastHoveredTile)
            {
                // Clear previous highlights
                ClearDebugHighlights();
                
                if (hoveredTile != null)
                {
                    // Highlight the hovered tile
                    hoveredTile.SetHighlight(true, new Color(1.0f, 1.0f, 0.0f, 0.5f));
                    
                    // Get adjacent positions using the actual method
                    var adjacentPositions = MovementValidationLogic.GetAdjacentPositions(hoveredTile.GridPosition);
                    
                    GD.Print($"Hovering over tile at {hoveredTile.GridPosition}, adjacent tiles: {string.Join(", ", adjacentPositions)}");
                    
                    // Highlight adjacent tiles
                    foreach (var adjacentPos in adjacentPositions)
                    {
                        var adjacentTile = FindVisualTileAtPosition(adjacentPos);
                        if (adjacentTile != null)
                        {
                            adjacentTile.SetHighlight(true, new Color(0.0f, 1.0f, 0.0f, 0.5f));
                        }
                    }
                    
                    _lastHoveredTile = hoveredTile;
                }
            }
        }
        
        private bool IsPointInHexagon(Vector2 point, VisualHexTile tile)
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
        
        private VisualHexTile FindVisualTileAtPosition(Vector2I position)
        {
            if (_mapContainer == null) return null;
            
            foreach (Node child in _mapContainer.GetChildren())
            {
                if (child is VisualHexTile visualTile && visualTile.GridPosition == position)
                {
                    return visualTile;
                }
            }
            
            return null;
        }
    }
}
