using Godot;
using System;
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
        private OptionButton _purchaseUnitSelector;
        private Label _purchaseUnitDetailsLabel;
        private Label _purchaseGoldLabel;
        private Label _purchaseStatusLabel;
        private Button _purchaseBuyButton;
        private Button _purchaseCancelButton;
        private readonly PurchaseCoordinator _purchaseCoordinator = new();
        private PhaseTransitionCoordinator _phaseTransitionCoordinator;
        private readonly SemicircleDeploymentService _deploymentService = new();
        
        private Button _debugAdjacentButton;
        
        // Game state
        private bool _gameStarted = false;
        
        // Map dimensions come from centralized configuration
        private static int MAP_WIDTH => MapConfiguration.MAP_WIDTH;
        private static int MAP_HEIGHT => MapConfiguration.MAP_HEIGHT;
        
        // Centralized services
        private VisualPositionManager _positionManager;
        private ViewportController _viewportController;
        private TileUnitCoordinator _tileUnitCoordinator;
        private readonly HexGridViewState _hexGridViewState = new();
        private MapPreviewController _mapPreviewController;
        private GameRuntimeController _gameRuntimeController;
        private MainInputController _mainInputController;
        private DebugToolsController _debugToolsController;
        private Archistrateia.Debug.DebugScrollOverlay _debugScrollOverlay;
        private int _viewChangedDebugCounter = 0;
        private int _sliderDebugCounter = 0;
        
        // Centralized viewport size calculation to ensure consistency between tiles and units
        // Returns the actual game grid area (excluding top bar, bottom bar, and sidebar)
        private Vector2 GetGameAreaSize()
        {
            var viewportSize = GetViewport().GetVisibleRect().Size;
            
            return new Vector2(
                viewportSize.X - UILayoutMetrics.SidebarWidth, // width: remaining width after sidebar
                viewportSize.Y - UILayoutMetrics.TopBarHeight - UILayoutMetrics.BottomBarHeight // height: remaining height after top and bottom bars
            );
        }

        // Get the game grid area position and size as a Rect2
        private Rect2 GetGameGridRect()
        {
            var viewportSize = GetViewport().GetVisibleRect().Size;
            
            return new Rect2(
                0, // x: start at left edge (sidebar is on the right)
                UILayoutMetrics.TopBarHeight, // y: start after top bar
                viewportSize.X - UILayoutMetrics.SidebarWidth, // width: remaining width after sidebar
                viewportSize.Y - UILayoutMetrics.TopBarHeight - UILayoutMetrics.BottomBarHeight // height: remaining height after top and bottom bars
            );
        }

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
            
            // Initialize UI immediately - it's safe to do so in _Ready
            // The viewport is guaranteed to be ready when _Ready is called
            InitializeUI();
        }

        private void InitializeUI()
        {
            GD.Print("🚀 Starting modern UI initialization...");
            
            // Create the modern UI manager
            _uiManager = new ModernUIManager();
            _uiManager.Name = "ModernUIManager";
            AddChild(_uiManager);
            
            // Create debug scroll overlay
            _debugScrollOverlay = new Archistrateia.Debug.DebugScrollOverlay();
            _debugScrollOverlay.Name = "DebugScrollOverlay";
            AddChild(_debugScrollOverlay);
            
            // Get references to UI elements from the modern UI
            _nextPhaseButton = _uiManager.GetNextPhaseButton();
            _mapTypeSelector = _uiManager.GetMapTypeSelector();
            _regenerateMapButton = _uiManager.GetRegenerateMapButton();
            StartButton = _uiManager.GetStartGameButton();
            _zoomSlider = _uiManager.GetZoomSlider();
            _zoomLabel = _uiManager.GetZoomLabel();
            _purchaseUnitSelector = _uiManager.GetPurchaseUnitSelector();
            _purchaseUnitDetailsLabel = _uiManager.GetPurchaseUnitDetailsLabel();
            _purchaseGoldLabel = _uiManager.GetPurchaseGoldLabel();
            _purchaseStatusLabel = _uiManager.GetPurchaseStatusLabel();
            _purchaseBuyButton = _uiManager.GetPurchaseBuyButton();
            _purchaseCancelButton = _uiManager.GetPurchaseCancelButton();
            
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

            if (_purchaseUnitSelector != null)
            {
                PopulatePurchaseUnitSelector();
                _purchaseUnitSelector.ItemSelected += OnPurchaseUnitSelected;
            }

            if (_purchaseBuyButton != null)
            {
                _purchaseBuyButton.Pressed += OnPurchaseBuyPressed;
            }

            if (_purchaseCancelButton != null)
            {
                _purchaseCancelButton.Pressed += OnPurchaseCancelPressed;
            }
            
            // Generate initial map before game starts
            GenerateMap();
            SetPurchaseUIVisible(false);
            
            GD.Print("✨ Modern UI initialization complete");
        }

        private static void CreateStartButton()
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
            _positionManager = new VisualPositionManager(gameAreaSize, MAP_WIDTH, MAP_HEIGHT, _hexGridViewState);
            
            // Initialize viewport controller with callback to update positions when view changes
            _viewportController = new ViewportController(MAP_WIDTH, MAP_HEIGHT, OnViewChanged, _hexGridViewState);
            
            // Initialize tile-unit coordinator
            _tileUnitCoordinator = new TileUnitCoordinator();
            _gameRuntimeController = new GameRuntimeController(this, _tileUnitCoordinator, _positionManager);
            _mapPreviewController = new MapPreviewController(this, _uiManager, _positionManager, _viewportController, _terrainColors, _hexGridViewState);
            _debugToolsController = new DebugToolsController(
                () => _mapContainer?.GetChildren().OfType<IDebugHexTile>() ?? Enumerable.Empty<IDebugHexTile>());
            _mainInputController = new MainInputController(
                _viewportController,
                _debugScrollOverlay,
                () => GetViewport().GetMousePosition(),
                GetGameAreaSize,
                GetGameGridRect,
                IsMouseOverUIControls,
                IsMouseOverGameArea,
                mousePosition => _debugToolsController?.DebugMousePosition(mousePosition, _nextPhaseButton, _zoomSlider),
                () => _debugToolsController?.IsDebugAdjacentModeEnabled() ?? false,
                mousePosition => _debugToolsController?.HandleDebugAdjacentHover(mousePosition),
                () => GetViewport().SetInputAsHandled());
            
            GD.Print($"✅ Centralized services initialized | GameArea: {gameAreaSize.X}x{gameAreaSize.Y} | Map: {MAP_WIDTH}x{MAP_HEIGHT}");
        }

        private void OnViewChanged()
        {
            // Sample debug output to avoid spam
            _viewChangedDebugCounter++;
            if (_viewChangedDebugCounter % 60 == 0) // Show every 60 calls (about once per second at 60fps)
            {
                GD.Print($"🔍 VIEW CHANGED (Sample {_viewChangedDebugCounter}): Current zoom = {_hexGridViewState.ZoomFactor:F2}x, Slider = {_zoomSlider?.Value:F2}x");
            }
            
            // Update game area size in position manager
            _positionManager.UpdateGameAreaSize(GetGameAreaSize());
            
            // Update all visual positions
            if (_mapContainer != null)
            {
                if (_mapRenderer != null)
                {
                    // Game phase: use MapRenderer for full functionality
                    _positionManager.UpdateAllPositions(_mapContainer, _mapRenderer.GetVisualUnits(), _gameManager?.GameMap, _tileUnitCoordinator);
                }
                else
                {
                    // Preview phase: update map tiles directly without units
                    _positionManager.UpdateAllPositions(_mapContainer, new List<VisualUnit>(), _gameManager?.GameMap, _tileUnitCoordinator);
                }
            }
            
            // Update zoom UI - but only if the slider value doesn't match the current zoom
            // This prevents circular dependency when user changes zoom
            if (_zoomSlider != null)
            {
                var currentSliderValue = (float)_zoomSlider.Value;
                var currentZoom = _hexGridViewState.ZoomFactor;
                if (Mathf.Abs(currentSliderValue - currentZoom) > 0.001f)
                {
                    // Sample slider changes
                    _sliderDebugCounter++;
                    if (_sliderDebugCounter % 60 == 0) // Show every 60 calls
                    {
                        GD.Print($"🔍 VIEW CHANGED: Setting slider from {currentSliderValue:F2}x to {currentZoom:F2}x (Sample {_sliderDebugCounter})");
                    }
                    _zoomSlider.Value = currentZoom;
                }
                else
                {
                    // Sample slider matches
                    _sliderDebugCounter++;
                    if (_sliderDebugCounter % 60 == 0) // Show every 60 calls
                    {
                        GD.Print($"🔍 VIEW CHANGED: Slider already matches zoom ({currentZoom:F2}x) (Sample {_sliderDebugCounter})");
                    }
                }
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
            GD.Print($"🔍 SLIDER CHANGED: {value:F2}x");
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
                _zoomLabel.Text = $"Zoom: {_hexGridViewState.ZoomFactor:F1}x";
            }
        }
        
        private void UpdateUIPositions()
        {
            GD.Print($"🔍 UPDATE UI POSITIONS: Called with zoom {_hexGridViewState.ZoomFactor:F2}x, slider {_zoomSlider?.Value:F2}x");
            var viewportSize = GetViewport().GetVisibleRect().Size;
            
            // Note: Next Phase button is now handled by modern UI (doesn't need positioning)
            
            // Update Debug Adjacent button position
            if (_debugAdjacentButton != null)
            {
                _debugToolsController?.UpdateDebugButtonPosition(_debugAdjacentButton, viewportSize);
            }
            
            // Note: Zoom controls are now handled by modern UI (don't need manual positioning)
            
            // Status panel is now handled by ModernUIManager
        }

        private void PopulatePurchaseUnitSelector()
        {
            _purchaseUnitSelector?.Clear();
            if (_purchaseUnitSelector == null)
            {
                return;
            }

            foreach (var blueprint in UnitCatalog.GetAll())
            {
                _purchaseUnitSelector.AddItem(blueprint.DisplayName);
            }

            _purchaseUnitSelector.Selected = 0;
            UpdateSelectedPurchaseUnitDetails();
        }

        private Player GetCurrentPlayer()
        {
            if (_gameManager == null || _gameManager.Players.Count == 0)
            {
                return null;
            }

            if (_currentPlayerIndex < 0 || _currentPlayerIndex >= _gameManager.Players.Count)
            {
                return null;
            }

            return _gameManager.Players[_currentPlayerIndex];
        }

        private UnitBlueprint GetSelectedBlueprint()
        {
            if (_purchaseUnitSelector == null || _purchaseUnitSelector.Selected < 0)
            {
                return null;
            }

            var selectedIndex = (int)_purchaseUnitSelector.Selected;
            var all = UnitCatalog.GetAll();
            if (selectedIndex >= all.Count)
            {
                return null;
            }

            return all[selectedIndex];
        }

        private void OnPurchaseUnitSelected(long index)
        {
            UpdateSelectedPurchaseUnitDetails();
            RefreshPurchaseUI();
        }

        private void UpdateSelectedPurchaseUnitDetails()
        {
            if (_purchaseUnitDetailsLabel == null)
            {
                return;
            }

            var blueprint = GetSelectedBlueprint();
            if (blueprint == null)
            {
                _purchaseUnitDetailsLabel.Text = "Select a unit";
                return;
            }

            int recommendedCost = UnitValuationPolicy.GetRecommendedCost(blueprint.Attack, blueprint.Defense, blueprint.MovementPoints);
            _purchaseUnitDetailsLabel.Text =
                $"ATK {blueprint.Attack} | DEF {blueprint.Defense} | MP {blueprint.MovementPoints}\n" +
                $"Cost {blueprint.Cost} | Value {blueprint.ValueScore:F1} (rec {recommendedCost})";
        }

        private void SetPurchaseStatus(string message)
        {
            if (_purchaseStatusLabel != null)
            {
                _purchaseStatusLabel.Text = message;
            }
        }

        private void RefreshPurchaseUI()
        {
            var currentPlayer = GetCurrentPlayer();
            if (_purchaseGoldLabel != null)
            {
                _purchaseGoldLabel.Text = $"Gold: {currentPlayer?.Gold ?? 0}";
            }

            var selectedBlueprint = GetSelectedBlueprint();
            bool canAfford = currentPlayer != null && selectedBlueprint != null && currentPlayer.Gold >= selectedBlueprint.Cost;
            bool isPurchasePhase = TurnManager != null && TurnManager.CurrentPhase == GamePhase.Purchase;
            bool hasValidTiles = false;
            if (_gameManager != null && isPurchasePhase && currentPlayer != null)
            {
                hasValidTiles = _deploymentService.GetDeployableTilesForPlayer(
                    _gameManager.GameMap,
                    _currentPlayerIndex,
                    _gameManager.Players.Count
                ).Count > 0;
            }

            if (_purchaseBuyButton != null)
            {
                _purchaseBuyButton.Disabled = !isPurchasePhase || !canAfford || !hasValidTiles;
            }

            if (_purchaseCancelButton != null)
            {
                _purchaseCancelButton.Disabled = !_purchaseCoordinator.HasPendingPurchase;
            }

            if (isPurchasePhase && !hasValidTiles && !_purchaseCoordinator.HasPendingPurchase)
            {
                SetPurchaseStatus("No valid deployment tiles available.");
            }
        }

        private void SetPurchaseUIVisible(bool visible)
        {
            _uiManager?.SetPurchasePanelVisible(visible);
            if (!visible)
            {
                _mapRenderer?.ClearPurchasePlacementTiles();
            }
        }

        private void OnPurchaseBuyPressed()
        {
            if (TurnManager == null || _gameManager == null || TurnManager.CurrentPhase != GamePhase.Purchase)
            {
                SetPurchaseStatus("Purchase is only available during the Purchase phase.");
                return;
            }

            var currentPlayer = GetCurrentPlayer();
            var selectedBlueprint = GetSelectedBlueprint();
            if (currentPlayer == null || selectedBlueprint == null)
            {
                SetPurchaseStatus("Unable to start purchase.");
                return;
            }

            var selectionResult = _purchaseCoordinator.BeginSelection(
                currentPlayer,
                _currentPlayerIndex,
                selectedBlueprint.UnitType,
                _gameManager.GameMap,
                _gameManager.Players.Count
            );

            if (!selectionResult.Success)
            {
                _mapRenderer?.ClearPurchasePlacementTiles();
                SetPurchaseStatus(selectionResult.ErrorMessage);
                RefreshPurchaseUI();
                return;
            }

            _mapRenderer?.ShowPurchasePlacementTiles(selectionResult.ValidPlacementTiles);
            SetPurchaseStatus("Select a highlighted deployment tile to place your unit.");
            RefreshPurchaseUI();
        }

        private void OnPurchaseCancelPressed()
        {
            _purchaseCoordinator.CancelPendingPurchase();
            _mapRenderer?.ClearPurchasePlacementTiles();
            SetPurchaseStatus("Purchase cancelled.");
            RefreshPurchaseUI();
        }

        private void OnPurchaseTileClicked(Vector2I tilePosition)
        {
            if (TurnManager == null || _gameManager == null || TurnManager.CurrentPhase != GamePhase.Purchase)
            {
                return;
            }

            if (!_purchaseCoordinator.HasPendingPurchase)
            {
                SetPurchaseStatus("Choose a unit and press Buy + Place first.");
                return;
            }

            var currentPlayer = GetCurrentPlayer();
            if (currentPlayer == null)
            {
                SetPurchaseStatus("No active player.");
                return;
            }

            var placeResult = _purchaseCoordinator.TryPlacePendingUnit(
                currentPlayer,
                _currentPlayerIndex,
                tilePosition,
                _gameManager.GameMap,
                _gameManager.Players.Count
            );

            if (!placeResult.Success)
            {
                SetPurchaseStatus(placeResult.ErrorMessage);
                RefreshPurchaseUI();
                return;
            }

            var playerColor = _tileUnitCoordinator.GetPlayerColor(currentPlayer.Name);
            var worldPosition = _positionManager.CalculateWorldPosition(tilePosition);
            _mapRenderer?.CreateVisualUnit(placeResult.PurchasedUnit, worldPosition, playerColor);
            _mapRenderer?.UpdateTileOccupationStatus();
            _mapRenderer?.ClearPurchasePlacementTiles();

            SetPurchaseStatus($"Placed {placeResult.PurchasedUnit.Name}.");
            RefreshPurchaseUI();
        }

        public void OnStartButtonPressed()
        {
            GD.Print("🎮🎮🎮 START BUTTON PRESSED! 🎮🎮🎮");
            GD.Print($"🔍 BUTTON PRESS: Initial zoom state - Zoom: {_hexGridViewState.ZoomFactor:F2}x, Slider: {_zoomSlider?.Value:F2}x");
            
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
            _mapPreviewController?.HideMapGenerationControls();

            // Game status is now handled by ModernUIManager in the top bar

            // Keep the current zoom level (1.0 from preview) - don't change it
            // The map is already at the right size from the preview
            
            // Update zoom slider to reflect the current zoom
            if (_zoomSlider != null)
            {
                _zoomSlider.Value = _hexGridViewState.ZoomFactor;
            }
            
            // Update zoom label to reflect the optimal zoom
            UpdateZoomLabel();

            // Use the existing map as the game map (no regeneration needed)
            GD.Print("🎮 Starting game with current map - no regeneration needed");
            
            // Debug: Log zoom state before game start
            GD.Print($"🔍 GAME START DEBUG: Before InitializeGameManager - Zoom: {_hexGridViewState.ZoomFactor:F2}x, Slider: {_zoomSlider?.Value:F2}x");
            
            InitializeGameManager();
            
            // Debug: Log zoom state after game start
            GD.Print($"🔍 GAME START DEBUG: After InitializeGameManager - Zoom: {_hexGridViewState.ZoomFactor:F2}x, Slider: {_zoomSlider?.Value:F2}x");
            
            // Preserve the current zoom level instead of forcing it to 1.0
            // The zoom level should remain as the user set it during preview
            var currentZoom = _hexGridViewState.ZoomFactor;
            GD.Print($"🔍 Preserving zoom level: {currentZoom:F2}x");
            
            // Update zoom slider to reflect the current zoom (should already be correct)
            if (_zoomSlider != null)
            {
                _zoomSlider.Value = currentZoom;
            }
            UpdateZoomLabel();
            
            // Debug: Log zoom state before regeneration
            GD.Print($"🔍 GAME START DEBUG: Before RegenerateMapWithCurrentZoom - Zoom: {_hexGridViewState.ZoomFactor:F2}x, Slider: {_zoomSlider?.Value:F2}x");
            
            // Regenerate map visuals with correct zoom
            RegenerateMapWithCurrentZoom();
            
            // Debug: Log zoom state after regeneration
            GD.Print($"🔍 GAME START DEBUG: After RegenerateMapWithCurrentZoom - Zoom: {_hexGridViewState.ZoomFactor:F2}x, Slider: {_zoomSlider?.Value:F2}x");
            
            // Final debug: Log final zoom state
            GD.Print($"🔍 GAME START FINAL: Final zoom state - Zoom: {_hexGridViewState.ZoomFactor:F2}x, Slider: {_zoomSlider?.Value:F2}x");
            
            // Update UI positions after everything is initialized
            UpdateUIPositions();

            // Note: Next Phase button is now handled by the modern UI (no need to create here)
            
            // Create Debug Adjacent Tiles button
            _debugAdjacentButton = _debugToolsController?.CreateDebugAdjacentButton(this, () => GetViewport().GetVisibleRect().Size);
            
            // Debug UI elements to help troubleshoot
            _debugToolsController?.DebugUIElements(_nextPhaseButton, _zoomSlider);
        }

        private void GenerateMap()
        {
            _mapContainer = _mapPreviewController.GeneratePreviewMap(_mapContainer, _currentMapType);

            if (_zoomSlider != null)
            {
                _zoomSlider.Value = _hexGridViewState.ZoomFactor;
            }
            UpdateZoomLabel();
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
            GD.Print($"🔍 REGENERATE DEBUG: Before UpdateAllPositions - Zoom: {_hexGridViewState.ZoomFactor:F2}x, Slider: {_zoomSlider?.Value:F2}x");
            
            // Delegate to centralized position manager
            _positionManager?.UpdateAllPositions(_mapContainer, _mapRenderer?.GetVisualUnits() ?? new List<VisualUnit>(), _gameManager?.GameMap, _tileUnitCoordinator);
            
            GD.Print($"🔍 REGENERATE DEBUG: After UpdateAllPositions - Zoom: {_hexGridViewState.ZoomFactor:F2}x, Slider: {_zoomSlider?.Value:F2}x");
        }



        private void InitializeGameManager()
        {
            var logicalGameMap = _mapPreviewController.ConvertVisualMapToGameMap(_mapContainer);
            _gameManager = _gameRuntimeController.InitializeGameManager(logicalGameMap);
            
            // Use CallDeferred to connect TurnManager after GameManager's _Ready is called
            CallDeferred(MethodName.ConnectTurnManager);
        }

        private void ConnectTurnManager()
        {
            // Use the GameManager's TurnManager instead of the exported one
            TurnManager = _gameManager.TurnManager;
            TurnManager.PhaseChanged += OnTurnManagerPhaseChanged;
            
            // Now initialize MapRenderer with proper TurnManager
            InitializeMapRenderer();
            InitializePhaseTransitionCoordinator();
        }

        private void InitializeMapRenderer()
        {
            _mapRenderer = _gameRuntimeController.InitializeMapRenderer(
                _gameManager,
                TurnManager,
                _mapContainer,
                _currentPlayerIndex,
                _hexGridViewState,
                OnPurchaseTileClicked,
                UpdateTitleLabel,
                RefreshPurchaseUI);
        }

        private void InitializePhaseTransitionCoordinator()
        {
            _phaseTransitionCoordinator = new PhaseTransitionCoordinator(
                _gameManager,
                _purchaseCoordinator,
                phase => _mapRenderer?.OnPhaseChanged(phase),
                SetPurchaseUIVisible,
                UpdateSelectedPurchaseUnitDetails,
                SetPurchaseStatus,
                RefreshPurchaseUI,
                SwitchToNextPlayer,
                () => _mapRenderer?.DeselectAll());
        }

        private void OnNextPhaseButtonPressed()
        {
            GD.Print("🔘 Next Phase button pressed");
            AdvancePhaseWithSideEffects();
        }

        private void AdvancePhaseWithSideEffects()
        {
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
                GD.Print("✅ Phase advance completed successfully");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"❌ Error advancing phase: {ex.Message}");
                GD.PrintErr($"Stack trace: {ex.StackTrace}");
            }
        }

        private void OnTurnManagerPhaseChanged(int oldPhaseValue, int newPhaseValue)
        {
            if (_phaseTransitionCoordinator == null)
            {
                return;
            }

            var oldPhase = (GamePhase)oldPhaseValue;
            var newPhase = (GamePhase)newPhaseValue;
            GD.Print($"🔄 Main.OnTurnManagerPhaseChanged: {oldPhase} → {newPhase}");

            _phaseTransitionCoordinator.ApplyTransition(oldPhase, newPhase);
            UpdateTitleLabel();
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

                RefreshPurchaseUI();
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
            _mainInputController?.HandleInput(@event);
        }

        public override void _Process(double delta)
        {
            _mainInputController?.HandleProcess(delta);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            _mainInputController?.HandleUnhandledInput(
                @event,
                HandleDebugInput,
                HandlePhaseInput,
                HandleHoverInfoModeInput,
                HandleZoomInput,
                HandleScrollInput);
        }

        private bool HandleDebugInput(InputEventKey keyEvent)
        {
            return _debugToolsController?.HandleDebugInput(keyEvent, _debugScrollOverlay) ?? false;
        }

        private bool HandlePhaseInput(InputEventKey keyEvent)
        {
            if (keyEvent.Keycode == Key.Space)
            {
                AdvancePhaseWithSideEffects();
                return true;
            }
            return false;
        }

        private bool HandleHoverInfoModeInput(InputEventKey keyEvent)
        {
            if (keyEvent.Keycode == Key.I)
            {
                _mapRenderer?.ToggleHoverInfoMode();
                bool enabled = _mapRenderer?.IsHoverInfoModeEnabled() ?? false;
                GD.Print($"Hover info mode: {(enabled ? "ON" : "OFF")}");
                GetViewport().SetInputAsHandled();
                return true;
            }

            return false;
        }

        private bool HandleViewportInput(InputEventKey keyEvent)
        {
            var handled = _viewportController?.HandleKeyboardInput(keyEvent, GetGameAreaSize()) ?? false;
            
            if (handled)
            {
                GetViewport().SetInputAsHandled();
            }
            
            return handled;
        }

        private bool HandleZoomInput(InputEventKey keyEvent)
        {
            return HandleViewportInput(keyEvent);
        }

        private bool HandleScrollInput(InputEventKey keyEvent)
        {
            return HandleViewportInput(keyEvent);
        }

        private void UpdateZoomUI()
        {
            _zoomSlider.Value = _hexGridViewState.ZoomFactor;
            RegenerateMapWithCurrentZoom();
            UpdateTitleLabel();
            UpdateZoomLabel();
        }

        private void ApplyScrollDelta(Vector2 scrollDelta)
        {
            _viewportController?.ApplyScrollDelta(scrollDelta, GetViewport().GetVisibleRect().Size);
        }
        
        
        public bool IsMouseOverGameArea(Vector2 mousePosition)
        {
            // Instead of checking if mouse is over game area (which ignores mouse events),
            // check if mouse is NOT over UI controls - this allows panning everywhere except over UI
            return !IsMouseOverUIControls(mousePosition);
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

            if (_purchaseUnitSelector != null)
            {
                var selectorRect = new Rect2(_purchaseUnitSelector.GlobalPosition, _purchaseUnitSelector.Size);
                if (selectorRect.HasPoint(mousePosition))
                {
                    return true;
                }
            }

            if (_purchaseBuyButton != null)
            {
                var buyRect = new Rect2(_purchaseBuyButton.GlobalPosition, _purchaseBuyButton.Size);
                if (buyRect.HasPoint(mousePosition))
                {
                    return true;
                }
            }

            if (_purchaseCancelButton != null)
            {
                var cancelRect = new Rect2(_purchaseCancelButton.GlobalPosition, _purchaseCancelButton.Size);
                if (cancelRect.HasPoint(mousePosition))
                {
                    return true;
                }
            }
            
            // Game status panel is now handled by ModernUIManager
            
            return false;
        }

        public bool IsMouseWithinGameArea(Vector2 mousePosition)
        {
            if (_uiManager?.GetGameArea() == null)
            {
                return true; // If no game area, allow all clicks (fallback)
            }
            
            var gameArea = _uiManager.GetGameArea();
            var gameAreaRect = new Rect2(gameArea.GlobalPosition, gameArea.Size);
            bool withinBounds = gameAreaRect.HasPoint(mousePosition);
            
            return withinBounds;
        }
        
    }
}
