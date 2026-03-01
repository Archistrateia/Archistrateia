using Godot;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class MainDecompositionTest
    {
        [Test]
        public void Main_Should_Hold_Runtime_And_Preview_Controller_Fields()
        {
            var mainType = typeof(Main);

            Assert.IsNotNull(
                mainType.GetField("_mapPreviewController", BindingFlags.NonPublic | BindingFlags.Instance),
                "Main should compose preview behavior through MapPreviewController.");
            Assert.IsNotNull(
                mainType.GetField("_gameRuntimeController", BindingFlags.NonPublic | BindingFlags.Instance),
                "Main should compose runtime bootstrap behavior through GameRuntimeController.");
            Assert.IsNotNull(
                mainType.GetField("_mainInputController", BindingFlags.NonPublic | BindingFlags.Instance),
                "Main should compose raw input routing through MainInputController.");
            Assert.IsNotNull(
                mainType.GetField("_mainViewController", BindingFlags.NonPublic | BindingFlags.Instance),
                "Main should compose zoom/view UI updates through MainViewController.");
            Assert.IsNotNull(
                mainType.GetField("_debugToolsController", BindingFlags.NonPublic | BindingFlags.Instance),
                "Main should compose debug tooling through DebugToolsController.");
        }

        [Test]
        public void MapPreviewController_Should_Generate_MapContainer_With_Expected_Tile_Count()
        {
            var host = new Node();
            var viewState = new HexGridViewState();
            var viewportController = new ViewportController(MapConfiguration.MAP_WIDTH, MapConfiguration.MAP_HEIGHT, null, viewState);
            var positionManager = new VisualPositionManager(new Vector2(1200, 800), MapConfiguration.MAP_WIDTH, MapConfiguration.MAP_HEIGHT, viewState);
            var terrainColors = new Dictionary<TerrainType, Color>
            {
                { TerrainType.Desert, Colors.Beige },
                { TerrainType.Hill, Colors.Brown },
                { TerrainType.River, Colors.Blue },
                { TerrainType.Shoreline, Colors.SandyBrown },
                { TerrainType.Lagoon, Colors.CadetBlue },
                { TerrainType.Grassland, Colors.Green },
                { TerrainType.Mountain, Colors.Gray },
                { TerrainType.Water, Colors.DarkBlue }
            };

            var controller = new MapPreviewController(host, null, positionManager, viewportController, terrainColors, viewState);

            var mapContainer = controller.GeneratePreviewMap(null, MapType.Continental);

            Assert.AreEqual("MapContainer", mapContainer.Name.ToString());
            Assert.AreEqual(MapConfiguration.MAP_WIDTH * MapConfiguration.MAP_HEIGHT, mapContainer.GetChildCount(),
                "Preview controller should generate one visual tile per logical tile.");

            mapContainer.QueueFree();
            host.QueueFree();
        }
    }
}
