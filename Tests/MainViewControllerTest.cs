using Godot;
using NUnit.Framework;
using System.Collections.Generic;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class MainViewControllerTest
    {
        [Test]
        public void HandleViewChanged_Should_Sync_ZoomSlider_And_Label()
        {
            var viewState = new HexGridViewState { ZoomFactor = 1.5f };
            var zoomSlider = new HSlider
            {
                MinValue = 0.1,
                MaxValue = 3.0,
                Step = 0.1,
                Value = 1.0
            };
            var zoomLabel = new Label();
            var positionManager = new VisualPositionManager(new Vector2(1200, 800), 10, 10, viewState);
            var tileUnitCoordinator = new TileUnitCoordinator();
            var controller = new MainViewController(
                positionManager,
                tileUnitCoordinator,
                viewState,
                zoomSlider,
                zoomLabel,
                () => new Vector2(1200, 800),
                () => new Vector2(1200, 800),
                _ => { });

            controller.HandleViewChanged(null, null, new Dictionary<Vector2I, HexTile>());

            Assert.AreEqual(1.5f, (float)zoomSlider.Value, 0.001f, "Slider should follow injected zoom state.");
            Assert.AreEqual("Zoom: 1.5x", zoomLabel.Text, "Label should reflect current zoom.");
        }

        [Test]
        public void UpdateUIPositions_Should_Invoke_DebugButtonPositionUpdater()
        {
            var viewState = new HexGridViewState { ZoomFactor = 1.0f };
            int calls = 0;
            Vector2 lastViewport = Vector2.Zero;
            var controller = new MainViewController(
                new VisualPositionManager(new Vector2(1000, 700), 10, 10, viewState),
                new TileUnitCoordinator(),
                viewState,
                null,
                null,
                () => new Vector2(1000, 700),
                () => new Vector2(1440, 900),
                viewport =>
                {
                    calls++;
                    lastViewport = viewport;
                });

            controller.UpdateUIPositions();

            Assert.AreEqual(1, calls, "UI update should reposition debug button once per call.");
            Assert.AreEqual(new Vector2(1440, 900), lastViewport);
        }
    }
}
