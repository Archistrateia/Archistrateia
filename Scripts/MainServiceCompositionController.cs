using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Archistrateia
{
    public sealed class MainServiceCompositionController
    {
        public sealed class ComposeRequest
        {
            public Node Host { get; init; }
            public ModernUIManager UIManager { get; init; }
            public Archistrateia.Debug.DebugScrollOverlay DebugScrollOverlay { get; init; }
            public IReadOnlyDictionary<TerrainType, Color> TerrainColors { get; init; }
            public HexGridViewState ViewState { get; init; }
            public int MapWidth { get; init; }
            public int MapHeight { get; init; }
            public HSlider ZoomSlider { get; init; }
            public Label ZoomLabel { get; init; }
            public Func<Vector2> GetGameAreaSize { get; init; }
            public Func<Vector2> GetViewportSize { get; init; }
            public Func<Rect2> GetGameGridRect { get; init; }
            public Func<Vector2, bool> IsMouseOverUIControls { get; init; }
            public Func<Vector2, bool> IsMouseOverGameArea { get; init; }
            public Func<Vector2> GetMousePosition { get; init; }
            public Func<IEnumerable<IDebugHexTile>> GetDebugTiles { get; init; }
            public Func<bool> IsDebugAdjacentModeEnabled { get; init; }
            public Action<Vector2> HandleDebugAdjacentHover { get; init; }
            public Action<Vector2> DebugMousePosition { get; init; }
            public Action MarkInputHandled { get; init; }
            public Action OnViewChanged { get; init; }
            public Func<Node2D> GetMapContainer { get; init; }
            public Func<MapRenderer> GetMapRenderer { get; init; }
            public Func<Dictionary<Vector2I, HexTile>> GetGameMap { get; init; }
            public Func<Button> GetNextPhaseButton { get; init; }
            public Func<Button> GetDebugAdjacentButton { get; init; }
            public Func<OptionButton> GetPurchaseUnitSelector { get; init; }
            public Func<Button> GetPurchaseBuyButton { get; init; }
            public Func<Button> GetPurchaseCancelButton { get; init; }
            public Func<Control> GetGameArea { get; init; }
            public Action<float> SetViewportZoom { get; init; }
            public Action UpdateTitleLabel { get; init; }
            public Action UpdateUIPositions { get; init; }
        }

        public sealed class ComposeResult
        {
            public VisualPositionManager PositionManager { get; init; }
            public ViewportController ViewportController { get; init; }
            public TileUnitCoordinator TileUnitCoordinator { get; init; }
            public GameRuntimeController GameRuntimeController { get; init; }
            public MapPreviewController MapPreviewController { get; init; }
            public DebugToolsController DebugToolsController { get; init; }
            public MainViewController MainViewController { get; init; }
            public MainUIHitTestController MainUIHitTestController { get; init; }
            public MainInputController MainInputController { get; init; }
            public MainZoomController MainZoomController { get; init; }
        }

        public ComposeResult Compose(ComposeRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var positionManager = new VisualPositionManager(request.GetGameAreaSize(), request.MapWidth, request.MapHeight, request.ViewState);
            var viewportController = new ViewportController(request.MapWidth, request.MapHeight, request.OnViewChanged, request.ViewState);
            var tileUnitCoordinator = new TileUnitCoordinator();
            var gameRuntimeController = new GameRuntimeController(request.Host, tileUnitCoordinator, positionManager);
            var mapPreviewController = new MapPreviewController(request.Host, request.UIManager, positionManager, viewportController, request.TerrainColors, request.ViewState);
            var debugToolsController = new DebugToolsController(() => request.GetDebugTiles?.Invoke() ?? Enumerable.Empty<IDebugHexTile>());
            var mainViewController = new MainViewController(
                positionManager,
                tileUnitCoordinator,
                request.ViewState,
                request.ZoomSlider,
                request.ZoomLabel,
                request.GetGameAreaSize,
                request.GetViewportSize,
                viewportSize => debugToolsController.UpdateDebugButtonPosition(request.GetDebugAdjacentButton?.Invoke(), viewportSize));
            var mainUiHitTestController = new MainUIHitTestController(
                () => request.ZoomSlider,
                request.GetNextPhaseButton,
                request.GetDebugAdjacentButton,
                request.GetPurchaseUnitSelector,
                request.GetPurchaseBuyButton,
                request.GetPurchaseCancelButton,
                request.GetGameArea);
            var mainInputController = new MainInputController(
                viewportController,
                request.DebugScrollOverlay,
                request.GetMousePosition,
                request.GetGameAreaSize,
                request.GetGameGridRect,
                request.IsMouseOverUIControls,
                request.IsMouseOverGameArea,
                request.DebugMousePosition,
                request.IsDebugAdjacentModeEnabled,
                request.HandleDebugAdjacentHover,
                request.MarkInputHandled);
            var mainZoomController = new MainZoomController(
                () => request.ZoomSlider,
                request.SetViewportZoom,
                request.UpdateTitleLabel,
                request.UpdateUIPositions,
                GD.Print);

            return new ComposeResult
            {
                PositionManager = positionManager,
                ViewportController = viewportController,
                TileUnitCoordinator = tileUnitCoordinator,
                GameRuntimeController = gameRuntimeController,
                MapPreviewController = mapPreviewController,
                DebugToolsController = debugToolsController,
                MainViewController = mainViewController,
                MainUIHitTestController = mainUiHitTestController,
                MainInputController = mainInputController,
                MainZoomController = mainZoomController
            };
        }
    }
}
