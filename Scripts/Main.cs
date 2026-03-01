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
        private MainViewController _mainViewController;
        private DebugToolsController _debugToolsController;
        private MainLifecycleController _mainLifecycleController;
        private MainMapSetupController _mainMapSetupController;
        private MainRuntimeBootstrapController _mainRuntimeBootstrapController;
        private MainUIHitTestController _mainUIHitTestController;
        private MainZoomController _mainZoomController;
        private PurchaseUIController _purchaseUIController;
        private GameStartController _gameStartController;
        private Archistrateia.Debug.DebugScrollOverlay _debugScrollOverlay;
        
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
            _mainViewController = new MainViewController(
                _positionManager,
                _tileUnitCoordinator,
                _hexGridViewState,
                _zoomSlider,
                _zoomLabel,
                GetGameAreaSize,
                () => GetViewport().GetVisibleRect().Size,
                viewportSize => _debugToolsController?.UpdateDebugButtonPosition(_debugAdjacentButton, viewportSize));
            _mainUIHitTestController = new MainUIHitTestController(
                () => _zoomSlider,
                () => _nextPhaseButton,
                () => _debugAdjacentButton,
                () => _purchaseUnitSelector,
                () => _purchaseBuyButton,
                () => _purchaseCancelButton,
                () => _uiManager?.GetGameArea());
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
            _mainZoomController = new MainZoomController(
                () => _zoomSlider,
                zoom => _viewportController?.SetZoom(zoom),
                UpdateTitleLabel,
                () => _mainViewController?.UpdateUIPositions(),
                GD.Print);
            _mainLifecycleController = new MainLifecycleController(
                () => TurnManager,
                () => _gameManager,
                () => _currentPlayerIndex,
                index => _currentPlayerIndex = index,
                player => _mapRenderer?.SetCurrentPlayer(player),
                (oldPhase, newPhase) => _phaseTransitionCoordinator?.ApplyTransition(oldPhase, newPhase),
                RefreshPurchaseUI,
                (playerName, phaseName, turn) => _uiManager?.UpdatePlayerInfo(playerName, phaseName, turn),
                text =>
                {
                    if (TitleLabel != null)
                    {
                        TitleLabel.Text = text;
                    }
                });
            _mainRuntimeBootstrapController = new MainRuntimeBootstrapController(
                () => _mapContainer,
                mapContainer => _mapPreviewController.ConvertVisualMapToGameMap(mapContainer),
                logicalGameMap => _gameRuntimeController.InitializeGameManager(logicalGameMap),
                gameManager => _gameManager = gameManager,
                () => CallDeferred(MethodName.ConnectTurnManager),
                () => _gameManager,
                turnManager => TurnManager = turnManager,
                OnTurnManagerPhaseChanged,
                (gameManager, turnManager, mapContainer, currentPlayerIndex, viewState, onPurchaseTileClicked, updateTitleLabel, refreshPurchaseUI) =>
                    _gameRuntimeController.InitializeMapRenderer(
                        gameManager,
                        turnManager,
                        mapContainer,
                        currentPlayerIndex,
                        viewState,
                        onPurchaseTileClicked,
                        updateTitleLabel,
                        refreshPurchaseUI),
                () => TurnManager,
                () => _currentPlayerIndex,
                () => _hexGridViewState,
                OnPurchaseTileClicked,
                UpdateTitleLabel,
                RefreshPurchaseUI,
                mapRenderer => _mapRenderer = mapRenderer,
                () => new PhaseTransitionCoordinator(
                    _gameManager,
                    _purchaseCoordinator,
                    phase => _mapRenderer?.OnPhaseChanged(phase),
                    SetPurchaseUIVisible,
                    UpdateSelectedPurchaseUnitDetails,
                    SetPurchaseStatus,
                    RefreshPurchaseUI,
                    SwitchToNextPlayer,
                    () => _mapRenderer?.DeselectAll()),
                coordinator => _phaseTransitionCoordinator = coordinator);
            _mainMapSetupController = new MainMapSetupController(
                () => _gameStartController?.IsGameStarted ?? false,
                () => _currentMapType,
                mapType => _currentMapType = mapType,
                () => _mapContainer,
                mapContainer => _mapContainer = mapContainer,
                (mapContainer, mapType) => _mapPreviewController.GeneratePreviewMap(mapContainer, mapType),
                () => _hexGridViewState.ZoomFactor,
                zoom =>
                {
                    if (_zoomSlider != null)
                    {
                        _zoomSlider.Value = zoom;
                    }
                },
                () => _mainViewController?.UpdateZoomLabel(),
                description =>
                {
                    if (_mapTypeDescriptionLabel != null)
                    {
                        _mapTypeDescriptionLabel.Text = description;
                    }
                },
                GD.Print);

            _purchaseUIController = new PurchaseUIController(
                _purchaseUnitSelector,
                _purchaseUnitDetailsLabel,
                _purchaseGoldLabel,
                _purchaseStatusLabel,
                _purchaseBuyButton,
                _purchaseCancelButton,
                _purchaseCoordinator,
                _deploymentService,
                () => _gameManager,
                () => _currentPlayerIndex,
                () => TurnManager,
                visible => _uiManager?.SetPurchasePanelVisible(visible),
                () => _mapRenderer?.ClearPurchasePlacementTiles(),
                tiles => _mapRenderer?.ShowPurchasePlacementTiles(tiles),
                player => _tileUnitCoordinator.GetPlayerColor(player.Name),
                tilePosition => _positionManager.CalculateWorldPosition(tilePosition),
                (unit, worldPosition, playerColor) => _mapRenderer?.CreateVisualUnit(unit, worldPosition, playerColor),
                () => _mapRenderer?.UpdateTileOccupationStatus());

            _gameStartController = new GameStartController(
                hideStartButton: () =>
                {
                    if (_uiManager != null)
                    {
                        _uiManager.HideStartButton();
                    }
                    else if (StartButton != null)
                    {
                        StartButton.Visible = false;
                    }
                },
                hideTitleLabel: () =>
                {
                    if (TitleLabel != null)
                    {
                        TitleLabel.Visible = false;
                    }
                },
                hideMapGenerationControls: () => _mapPreviewController?.HideMapGenerationControls(),
                getCurrentZoom: () => _hexGridViewState.ZoomFactor,
                setZoomSliderValue: zoom =>
                {
                    if (_zoomSlider != null)
                    {
                        _zoomSlider.Value = zoom;
                    }
                },
                updateZoomLabel: () => _mainViewController?.UpdateZoomLabel(),
                initializeGameManager: InitializeGameManager,
                regenerateMapWithCurrentZoom: RegenerateMapWithCurrentZoom,
                updateUIPositions: () => _mainViewController?.UpdateUIPositions(),
                initializeDebugTools: () =>
                {
                    _debugAdjacentButton = _debugToolsController?.CreateDebugAdjacentButton(this, () => GetViewport().GetVisibleRect().Size);
                    _debugToolsController?.DebugUIElements(_nextPhaseButton, _zoomSlider);
                });
            
            GD.Print($"✅ Centralized services initialized | GameArea: {gameAreaSize.X}x{gameAreaSize.Y} | Map: {MAP_WIDTH}x{MAP_HEIGHT}");
        }

        private void OnViewChanged()
        {
            _mainViewController?.HandleViewChanged(_mapContainer, _mapRenderer, _gameManager?.GameMap);
        }

        private void OnZoomSliderChanged(double value)
        {
            _mainZoomController?.OnZoomSliderChanged(value);
        }
        
        private void OnZoomSliderInput(InputEvent @event)
        {
            _mainZoomController?.OnZoomSliderInput(@event, OnZoomSliderChanged);
        }
        private void PopulatePurchaseUnitSelector()
        {
            _purchaseUIController?.PopulatePurchaseUnitSelector();
        }

        private void OnPurchaseUnitSelected(long index)
        {
            _purchaseUIController?.OnPurchaseUnitSelected(index);
        }

        private void UpdateSelectedPurchaseUnitDetails()
        {
            _purchaseUIController?.UpdateSelectedPurchaseUnitDetails();
        }

        private void SetPurchaseStatus(string message)
        {
            _purchaseUIController?.SetPurchaseStatus(message);
        }

        private void RefreshPurchaseUI()
        {
            _purchaseUIController?.RefreshPurchaseUI();
        }

        private void SetPurchaseUIVisible(bool visible)
        {
            _purchaseUIController?.SetPurchaseUIVisible(visible);
        }

        private void OnPurchaseBuyPressed()
        {
            _purchaseUIController?.OnPurchaseBuyPressed();
        }

        private void OnPurchaseCancelPressed()
        {
            _purchaseUIController?.OnPurchaseCancelPressed();
        }

        private void OnPurchaseTileClicked(Vector2I tilePosition)
        {
            _purchaseUIController?.OnPurchaseTileClicked(tilePosition);
        }

        public void OnStartButtonPressed()
        {
            _gameStartController?.StartGame();
        }

        private void GenerateMap()
        {
            _mainMapSetupController?.GenerateMap();
        }


        private void OnMapTypeSelected(long index)
        {
            _mainMapSetupController?.OnMapTypeSelected(index);
        }
        
        private void OnRegenerateMapPressed()
        {
            _mainMapSetupController?.OnRegenerateMapPressed();
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
            _mainRuntimeBootstrapController?.InitializeGameManager();
        }

        private void ConnectTurnManager()
        {
            _mainRuntimeBootstrapController?.ConnectTurnManager();
        }

        private void InitializeMapRenderer()
        {
            _mainRuntimeBootstrapController?.InitializeMapRenderer();
        }

        private void InitializePhaseTransitionCoordinator()
        {
            _mainRuntimeBootstrapController?.InitializePhaseTransitionCoordinator();
        }

        private void OnNextPhaseButtonPressed()
        {
            GD.Print("🔘 Next Phase button pressed");
            AdvancePhaseWithSideEffects();
        }

        private void AdvancePhaseWithSideEffects()
        {
            _mainLifecycleController?.AdvancePhaseWithSideEffects();
        }

        private void OnTurnManagerPhaseChanged(int oldPhaseValue, int newPhaseValue)
        {
            if (_phaseTransitionCoordinator == null)
            {
                return;
            }

            _mainLifecycleController?.HandleTurnManagerPhaseChanged(oldPhaseValue, newPhaseValue);
        }

        private void SwitchToNextPlayer()
        {
            _mainLifecycleController?.SwitchToNextPlayer();
        }

        private void UpdateTitleLabel()
        {
            _mainLifecycleController?.UpdateTitleLabel();
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

        public bool IsMouseOverGameArea(Vector2 mousePosition)
        {
            return _mainUIHitTestController?.IsMouseOverGameArea(mousePosition) ?? true;
        }

        public bool IsMouseOverUIControls(Vector2 mousePosition)
        {
            return _mainUIHitTestController?.IsMouseOverUIControls(mousePosition) ?? false;
        }

        public bool IsMouseWithinGameArea(Vector2 mousePosition)
        {
            return _mainUIHitTestController?.IsMouseWithinGameArea(mousePosition) ?? true;
        }
        
    }
}
