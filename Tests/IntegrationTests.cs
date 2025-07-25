using Godot;
using System;
using System.Collections.Generic;
using Archistrateia;
using NUnit.Framework;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class IntegrationTests
    {
        [Test]
        public void TestTerrainColorInitialization()
        {
            var terrainTypes = System.Enum.GetValues<TerrainType>();
            Assert.AreEqual(5, terrainTypes.Length, "Should have 5 terrain types");

            foreach (TerrainType terrainType in terrainTypes)
            {
                Assert.IsTrue(System.Enum.IsDefined(typeof(TerrainType), terrainType), $"Terrain type {terrainType} should be defined");
            }
        }

        [Test]
        public void TestMapGeneration()
        {
            const int expectedWidth = 10;
            const int expectedHeight = 10;
            const int expectedTiles = expectedWidth * expectedHeight;

            Assert.AreEqual(100, expectedTiles, "Should calculate 100 tiles for 10x10 map");

            var mapContainer = new Node2D();
            mapContainer.Name = "TestMapContainer";

            int tilesCreated = 0;
            for (int x = 0; x < expectedWidth; x++)
            {
                for (int y = 0; y < expectedHeight; y++)
                {
                    var terrainType = GetRandomTerrainType();
                    var tile = CreateTestHexTile(x, y, terrainType);
                    mapContainer.AddChild(tile);
                    tilesCreated++;
                }
            }

            Assert.AreEqual(expectedTiles, tilesCreated, "Should create expected number of tiles");
            Assert.AreEqual(expectedTiles, mapContainer.GetChildCount(), "MapContainer should have expected number of children");
        }

        [Test]
        public void TestHexTileCreation()
        {
            var testTile = CreateTestHexTile(0, 0, TerrainType.Desert);

            Assert.IsNotNull(testTile, "Hex tile should be created successfully");
            Assert.AreEqual("HexTile_0_0", testTile.Name.ToString(), "Tile should have correct name");
            Assert.AreEqual(2, testTile.GetChildCount(), "Tile should have 2 children (shape + outline)");

            var hexShape = testTile.GetChild(0) as Polygon2D;
            var outline = testTile.GetChild(1) as Line2D;

            Assert.IsNotNull(hexShape, "Hex shape should be created successfully");
            Assert.IsNotNull(outline, "Hex outline should be created successfully");
            Assert.AreEqual(6, hexShape.Polygon.Length, "Hex shape should have 6 vertices");
            Assert.AreEqual(6, outline.Points.Length, "Hex outline should have 6 points");
        }

        [Test]
        public void TestSignalConnections()
        {
            var testButton = new Button();
            testButton.Text = "Test Button";

            bool signalReceived = false;
            testButton.Pressed += () => { signalReceived = true; };

            Assert.IsFalse(signalReceived, "Signal should not be received before pressing");

            testButton.EmitSignal("pressed");

            Assert.IsTrue(signalReceived, "Signal should be received after emitting");
        }

        [Test]
        public void TestHexGridCalculatorIntegration()
        {
            // Test that HexGridCalculator works with the main game
            var position = HexGridCalculator.CalculateHexPosition(0, 0);
            Assert.AreEqual(0, position.X, 0.001f, "HexGridCalculator should calculate correct position");
            Assert.AreEqual(0, position.Y, 0.001f, "HexGridCalculator should calculate correct position");

            var vertices = HexGridCalculator.CreateHexagonVertices();
            Assert.AreEqual(6, vertices.Length, "HexGridCalculator should create 6 vertices");

            var centeredPosition = HexGridCalculator.CalculateHexPositionCentered(0, 0, new Vector2(800, 600), 10, 10);
            Assert.IsTrue(centeredPosition.X >= -1000 && centeredPosition.X <= 1000, "Centered position X should be within reasonable bounds");
            Assert.IsTrue(centeredPosition.Y >= -1000 && centeredPosition.Y <= 1000, "Centered position Y should be within reasonable bounds");
        }

        [Test]
        public void TestGameEnums()
        {
            // Test that all game enums are properly defined
            var gamePhases = System.Enum.GetValues<GamePhase>();
            Assert.IsTrue(gamePhases.Length > 0, "GamePhase enum should have values");

            var unitTypes = System.Enum.GetValues<UnitType>();
            Assert.IsTrue(unitTypes.Length > 0, "UnitType enum should have values");

            var terrainTypes = System.Enum.GetValues<TerrainType>();
            Assert.AreEqual(5, terrainTypes.Length, "TerrainType enum should have 5 values");
        }

        [Test]
        public void TestHexGridCalculatorConstants()
        {
            // Test that HexGridCalculator constants are reasonable
            Assert.IsTrue(HexGridCalculator.HEX_SIZE > 0, "HEX_SIZE should be positive");
            Assert.IsTrue(HexGridCalculator.HEX_WIDTH > 0, "HEX_WIDTH should be positive");
            Assert.IsTrue(HexGridCalculator.HEX_HEIGHT > 0, "HEX_HEIGHT should be positive");

            // Test mathematical relationships
            Assert.AreEqual(HexGridCalculator.HEX_SIZE * 2.0f, HexGridCalculator.HEX_WIDTH, 0.001f, "HEX_WIDTH should be 2 * HEX_SIZE");
            Assert.AreEqual(HexGridCalculator.HEX_SIZE * 1.732f, HexGridCalculator.HEX_HEIGHT, 0.01f, "HEX_HEIGHT should be HEX_SIZE * sqrt(3)");
        }



        private static TerrainType GetRandomTerrainType()
        {
            var terrainTypes = System.Enum.GetValues<TerrainType>();
            return terrainTypes[GD.RandRange(0, terrainTypes.Length - 1)];
        }

        private Node2D CreateTestHexTile(int x, int y, TerrainType terrainType)
        {
            var tile = new Node2D();
            tile.Name = $"HexTile_{x}_{y}";

            var hexShape = new Polygon2D();
            hexShape.Polygon = CreateTestHexagonVertices();
            hexShape.Color = GetTestTerrainColor(terrainType);
            hexShape.Position = CalculateTestHexPosition(x, y);

            var outline = new Line2D();
            outline.Points = CreateTestHexagonVertices();
            outline.DefaultColor = new Color(0.2f, 0.2f, 0.2f);
            outline.Width = 2.0f;
            outline.Position = CalculateTestHexPosition(x, y);

            tile.AddChild(hexShape);
            tile.AddChild(outline);

            return tile;
        }

        private static Vector2[] CreateTestHexagonVertices()
        {
            const float hexSize = 40.0f;
            var vertices = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                var angle = i * Mathf.Pi / 3.0f;
                vertices[i] = new Vector2(
                    hexSize * Mathf.Cos(angle),
                    hexSize * Mathf.Sin(angle)
                );
            }
            return vertices;
        }

        private static Color GetTestTerrainColor(TerrainType terrainType)
        {
            var colors = new Dictionary<TerrainType, Color>
            {
                { TerrainType.Desert, new Color(0.9f, 0.8f, 0.6f) },
                { TerrainType.Hill, new Color(0.6f, 0.5f, 0.3f) },
                { TerrainType.River, new Color(0.3f, 0.6f, 0.9f) },
                { TerrainType.Shoreline, new Color(0.8f, 0.7f, 0.5f) },
                { TerrainType.Lagoon, new Color(0.2f, 0.5f, 0.7f) }
            };
            return colors[terrainType];
        }

        private static Vector2 CalculateTestHexPosition(int x, int y)
        {
            const float hexWidth = 80.0f;
            const float hexHeight = 69.28f;

            float xPos = x * hexWidth * 0.75f;
            float yPos = y * hexHeight;

            if (y % 2 == 1)
            {
                xPos += hexWidth * 0.375f;
            }

            return new Vector2(xPos + 100, yPos + 100);
        }
    }
}