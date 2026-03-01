using Godot;
using NUnit.Framework;
using System.Collections.Generic;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class MainServiceCompositionControllerTest
    {
        [Test]
        public void Compose_Should_Return_All_Composed_Controllers()
        {
            var host = new Node();
            var uiManager = new ModernUIManager();
            var overlay = new Archistrateia.Debug.DebugScrollOverlay();
            var zoomSlider = new HSlider();
            var zoomLabel = new Label();
            var composer = new MainServiceCompositionController();

            var result = composer.Compose(new MainServiceCompositionController.ComposeRequest
            {
                Host = host,
                UIManager = uiManager,
                DebugScrollOverlay = overlay,
                TerrainColors = TerrainColorPalette.Default,
                ViewState = new HexGridViewState(),
                MapWidth = MapConfiguration.MAP_WIDTH,
                MapHeight = MapConfiguration.MAP_HEIGHT,
                ZoomSlider = zoomSlider,
                ZoomLabel = zoomLabel,
                GetGameAreaSize = () => new Vector2(1200, 800),
                GetViewportSize = () => new Vector2(1200, 800),
                GetGameGridRect = () => new Rect2(0, 60, 1200, 740),
                IsMouseOverUIControls = _ => false,
                IsMouseOverGameArea = _ => true,
                GetMousePosition = () => Vector2.Zero,
                GetDebugTiles = () => new List<IDebugHexTile>(),
                IsDebugAdjacentModeEnabled = () => false,
                HandleDebugAdjacentHover = _ => { },
                DebugMousePosition = _ => { },
                MarkInputHandled = () => { },
                OnViewChanged = () => { },
                GetMapContainer = () => null,
                GetMapRenderer = () => null,
                GetGameMap = () => null,
                GetNextPhaseButton = () => null,
                GetDebugAdjacentButton = () => null,
                GetPurchaseUnitSelector = () => null,
                GetPurchaseBuyButton = () => null,
                GetPurchaseCancelButton = () => null,
                GetGameArea = () => null,
                SetViewportZoom = _ => { },
                UpdateTitleLabel = () => { },
                UpdateUIPositions = () => { }
            });

            Assert.IsNotNull(result.PositionManager);
            Assert.IsNotNull(result.ViewportController);
            Assert.IsNotNull(result.TileUnitCoordinator);
            Assert.IsNotNull(result.GameRuntimeController);
            Assert.IsNotNull(result.MapPreviewController);
            Assert.IsNotNull(result.DebugToolsController);
            Assert.IsNotNull(result.MainViewController);
            Assert.IsNotNull(result.MainUIHitTestController);
            Assert.IsNotNull(result.MainInputController);
            Assert.IsNotNull(result.MainZoomController);
        }
    }
}
