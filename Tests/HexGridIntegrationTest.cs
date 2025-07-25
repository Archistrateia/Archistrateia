using Godot;
using System;
using Archistrateia;
using NUnit.Framework;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class HexGridIntegrationTest
    {
        [Test]
        public void TestHexGridCalculatorConstants()
        {
            Assert.AreEqual(35.0f, HexGridCalculator.HEX_SIZE, 0.001f, "HEX_SIZE should be 35.0f");
            Assert.AreEqual(70.0f, HexGridCalculator.HEX_WIDTH, 0.001f, "HEX_WIDTH should be 70.0f");
            Assert.AreEqual(60.62f, HexGridCalculator.HEX_HEIGHT, 0.01f, "HEX_HEIGHT should be 60.62f");
        }

        [Test]
        public void TestHexGridCalculatorPositioning()
        {
            // Test even column positioning
            var position = HexGridCalculator.CalculateHexPosition(0, 0);
            Assert.AreEqual(0, position.X, 0.001f, "X position for (0,0) should be 0");
            Assert.AreEqual(0, position.Y, 0.001f, "Y position for (0,0) should be 0");

            position = HexGridCalculator.CalculateHexPosition(0, 1);
            Assert.AreEqual(0, position.X, 0.001f, "X position for (0,1) should be 0");
            Assert.AreEqual(60.62f, position.Y, 0.01f, "Y position for (0,1) should be 60.62f");

            // Test odd column positioning
            position = HexGridCalculator.CalculateHexPosition(1, 0);
            Assert.AreEqual(52.5f, position.X, 0.001f, "X position for (1,0) should be 52.5f");
            Assert.AreEqual(30.31f, position.Y, 0.01f, "Y position for (1,0) should be 30.31f");

            position = HexGridCalculator.CalculateHexPosition(1, 1);
            Assert.AreEqual(52.5f, position.X, 0.001f, "X position for (1,1) should be 52.5f");
            Assert.AreEqual(90.93f, position.Y, 0.01f, "Y position for (1,1) should be 90.93f");
        }

        [Test]
        public void TestHexGridCalculatorCentering()
        {
            var viewportSize = new Vector2(800, 600);
            int mapWidth = 10;
            int mapHeight = 10;

            var centeredPosition = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, mapWidth, mapHeight);

            // The base position should be (0,0), so the centered position should be the centering offset
            float expectedCenterX = (viewportSize.X - (mapWidth * HexGridCalculator.HEX_WIDTH * 0.75f + HexGridCalculator.HEX_WIDTH * 0.25f)) / 2;
            float expectedCenterY = (viewportSize.Y - (mapHeight * HexGridCalculator.HEX_HEIGHT + HexGridCalculator.HEX_HEIGHT * 0.5f)) / 2 + HexGridCalculator.HEX_HEIGHT * 0.5f;

            Assert.AreEqual(expectedCenterX, centeredPosition.X, 0.001f, "Centered X should match expected value");
            Assert.AreEqual(expectedCenterY, centeredPosition.Y, 0.001f, "Centered Y should match expected value");
        }

        [Test]
        public void TestHexGridCalculatorVertices()
        {
            var vertices = HexGridCalculator.CreateHexagonVertices();

            Assert.AreEqual(6, vertices.Length, "Should have 6 vertices");

            // Test that vertices form a proper hexagon
            for (int i = 0; i < 6; i++)
            {
                var vertex = vertices[i];
                float expectedAngle = i * Mathf.Pi / 3.0f;
                float expectedX = HexGridCalculator.HEX_SIZE * Mathf.Cos(expectedAngle);
                float expectedY = HexGridCalculator.HEX_SIZE * Mathf.Sin(expectedAngle);

                Assert.AreEqual(expectedX, vertex.X, 0.001f, $"Vertex {i} X should match expected value");
                Assert.AreEqual(expectedY, vertex.Y, 0.001f, $"Vertex {i} Y should match expected value");
            }
        }
    }
}