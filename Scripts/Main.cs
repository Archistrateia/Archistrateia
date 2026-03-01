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
        private IReadOnlyDictionary<TerrainType, Color> _terrainColors;
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
        private MainUIBootstrapController _mainUIBootstrapController;
        private MainServiceCompositionController _mainServiceCompositionController;
        private MainHoverInfoController _mainHoverInfoController;
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

            _terrainColors = TerrainColorPalette.Default;
            _mainUIBootstrapController = new MainUIBootstrapController();
            _mainServiceCompositionController = new MainServiceCompositionController();
            
            // Initialize UI immediately - it's safe to do so in _Ready
            // The viewport is guaranteed to be ready when _Ready is called
            InitializeUI();
        }

        private void InitializeUI()
        {
            GD.Print("🚀 Starting modern UI initialization...");
            
            var uiBootstrapResult = _mainUIBootstrapController.CreateAndAttachUI(this);
            _uiManager = uiBootstrapResult.UIManager;
            _debugScrollOverlay = uiBootstrapResult.DebugScrollOverlay;
            _nextPhaseButton = uiBootstrapResult.NextPhaseButton;
            _mapTypeSelector = uiBootstrapResult.MapTypeSelector;
            _regenerateMapButton = uiBootstrapResult.RegenerateMapButton;
            StartButton = uiBootstrapResult.StartButton;
            _zoomSlider = uiBootstrapResult.ZoomSlider;
            _zoomLabel = uiBootstrapResult.ZoomLabel;
            _purchaseUnitSelector = uiBootstrapResult.PurchaseUnitSelector;
            _purchaseUnitDetailsLabel = uiBootstrapResult.PurchaseUnitDetailsLabel;
            _purchaseGoldLabel = uiBootstrapResult.PurchaseGoldLabel;
            _purchaseStatusLabel = uiBootstrapResult.PurchaseStatusLabel;
            _purchaseBuyButton = uiBootstrapResult.PurchaseBuyButton;
            _purchaseCancelButton = uiBootstrapResult.PurchaseCancelButton;
            
            // Initialize centralized services
            InitializeCentralizedServices();
            
            _mainUIBootstrapController.ConnectUISignals(
                uiBootstrapResult,
                OnNextPhaseButtonPressed,
                OnMapTypeSelected,
                OnRegenerateMapPressed,
                OnZoomSliderChanged,
                OnStartButtonPressed,
                PopulatePurchaseUnitSelector,
                OnPurchaseUnitSelected,
                OnPurchaseBuyPressed,
                OnPurchaseCancelPressed);
            
            // Generate initial map before game starts
            GenerateMap();
            SetPurchaseUIVisible(false);
            
            GD.Print("✨ Modern UI initialization complete");
        }

        private void InitializeCentralizedServices()
        {
            GD.Print("🔧 Initializing centralized services...");
            var composed = _mainServiceCompositionController.Compose(new MainServiceCompositionController.ComposeRequest
            {
                Host = this,
                UIManager = _uiManager,
                DebugScrollOverlay = _debugScrollOverlay,
                TerrainColors = _terrainColors,
                ViewState = _hexGridViewState,
                MapWidth = MAP_WIDTH,
                MapHeight = MAP_HEIGHT,
                ZoomSlider = _zoomSlider,
                ZoomLabel = _zoomLabel,
                GetGameAreaSize = GetGameAreaSize,
                GetViewportSize = () => GetViewport().GetVisibleRect().Size,
                GetGameGridRect = GetGameGridRect,
                IsMouseOverUIControls = IsMouseOverUIControls,
                IsMouseOverGameArea = IsMouseOverGameArea,
                GetMousePosition = () => GetViewport().GetMousePosition(),
                GetDebugTiles = () => _mapContainer?.GetChildren().OfType<IDebugHexTile>() ?? Enumerable.Empty<IDebugHexTile>(),
                IsDebugAdjacentModeEnabled = () => _debugToolsController?.IsDebugAdjacentModeEnabled() ?? false,
                HandleDebugAdjacentHover = mousePosition => _debugToolsController?.HandleDebugAdjacentHover(mousePosition),
                DebugMousePosition = mousePosition => _debugToolsController?.DebugMousePosition(mousePosition, _nextPhaseButton, _zoomSlider),
                MarkInputHandled = () => GetViewport().SetInputAsHandled(),
                OnViewChanged = OnViewChanged,
                GetMapContainer = () => _mapContainer,
                GetMapRenderer = () => _mapRenderer,
                GetGameMap = () => _gameManager?.GameMap,
                GetNextPhaseButton = () => _nextPhaseButton,
                GetDebugAdjacentButton = () => _debugAdjacentButton,
                GetPurchaseUnitSelector = () => _purchaseUnitSelector,
                GetPurchaseBuyButton = () => _purchaseBuyButton,
                GetPurchaseCancelButton = () => _purchaseCancelButton,
                GetGameArea = () => _uiManager?.GetGameArea(),
                SetViewportZoom = zoom => _viewportController?.SetZoom(zoom),
                UpdateTitleLabel = UpdateTitleLabel,
                UpdateUIPositions = () => _mainViewController?.UpdateUIPositions()
            });
            _positionManager = composed.PositionManager;
            _viewportController = composed.ViewportController;
            _tileUnitCoordinator = composed.TileUnitCoordinator;
            _gameRuntimeController = composed.GameRuntimeController;
            _mapPreviewController = composed.MapPreviewController;
            _debugToolsController = composed.DebugToolsController;
            _mainViewController = composed.MainViewController;
            _mainUIHitTestController = composed.MainUIHitTestController;
            _mainInputController = composed.MainInputController;
            _mainZoomController = composed.MainZoomController;
            _mainHoverInfoController = new MainHoverInfoController(
                () => _mapRenderer,
                () => GetViewport().SetInputAsHandled(),
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
            
            var gameAreaSize = GetGameAreaSize();
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
            return _mainHoverInfoController?.HandleHoverInfoModeInput(keyEvent) ?? false;
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
