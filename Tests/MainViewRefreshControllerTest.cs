using Godot;
using NUnit.Framework;
using System.Collections.Generic;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class MainViewRefreshControllerTest
    {
        [Test]
        public void RegenerateMapWithCurrentZoom_Should_Call_PositionManager_Update()
        {
            var viewState = new HexGridViewState { ZoomFactor = 1.3f };
            var positionManager = new VisualPositionManager(new Vector2(1200, 800), 10, 10, viewState);
            var container = new Node2D();
            var gameMap = BuildRectMap(3, 3);
            var tileUnitCoordinator = new TileUnitCoordinator();
            var gameManager = new GameManager();
            gameManager.SetGameMap(gameMap);
            var controller = new MainViewRefreshController(
                () => viewState.ZoomFactor,
                () => 1.3,
                () => positionManager,
                () => container,
                () => null,
                () => gameManager,
                () => tileUnitCoordinator);

            Assert.DoesNotThrow(() => controller.RegenerateMapWithCurrentZoom());
        }

        private static Dictionary<Vector2I, HexTile> BuildRectMap(int width, int height)
        {
            var map = new Dictionary<Vector2I, HexTile>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pos = new Vector2I(x, y);
                    map[pos] = new HexTile(pos, TerrainType.Grassland);
                }
            }

            return map;
        }
    }
}
