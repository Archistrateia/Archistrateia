using Godot;
using NUnit.Framework;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class HexGridZoomTest
    {
        [Test]
        public void TestZoomFactorClamping()
        {
            // Test that zoom factor is properly clamped
            HexGridCalculator.SetZoom(0.05f); // Below minimum
            Assert.AreEqual(0.1f, HexGridCalculator.ZoomFactor, 0.001f, "Zoom should be clamped to minimum 0.1");
            
            HexGridCalculator.SetZoom(5.0f); // Above maximum
            Assert.AreEqual(3.0f, HexGridCalculator.ZoomFactor, 0.001f, "Zoom should be clamped to maximum 3.0");
            
            HexGridCalculator.SetZoom(1.5f); // Within range
            Assert.AreEqual(1.5f, HexGridCalculator.ZoomFactor, 0.001f, "Zoom should be set to 1.5");
        }

        [Test]
        public void TestZoomInAndOut()
        {
            HexGridCalculator.SetZoom(1.0f);
            
            // Test zoom in
            HexGridCalculator.ZoomIn();
            Assert.AreEqual(1.2f, HexGridCalculator.ZoomFactor, 0.001f, "Zoom in should multiply by 1.2");
            
            // Test zoom out
            HexGridCalculator.ZoomOut();
            Assert.AreEqual(1.0f, HexGridCalculator.ZoomFactor, 0.001f, "Zoom out should divide by 1.2");
        }

        [Test]
        public void TestHexPositionWithZoom()
        {
            HexGridCalculator.SetZoom(1.0f);
            var position1 = HexGridCalculator.CalculateHexPosition(1, 1);
            
            HexGridCalculator.SetZoom(2.0f);
            var position2 = HexGridCalculator.CalculateHexPosition(1, 1);
            
            // Position should scale with zoom
            Assert.AreEqual(position1.X * 2.0f, position2.X, 0.001f, "X position should scale with zoom");
            Assert.AreEqual(position1.Y * 2.0f, position2.Y, 0.001f, "Y position should scale with zoom");
        }

        [Test]
        public void TestHexVerticesWithZoom()
        {
            HexGridCalculator.SetZoom(1.0f);
            var vertices1 = HexGridCalculator.CreateHexagonVertices();
            
            HexGridCalculator.SetZoom(1.5f);
            var vertices2 = HexGridCalculator.CreateHexagonVertices();
            
            // All vertices should scale with zoom
            for (int i = 0; i < vertices1.Length; i++)
            {
                Assert.AreEqual(vertices1[i].X * 1.5f, vertices2[i].X, 0.001f, $"Vertex {i} X should scale with zoom");
                Assert.AreEqual(vertices1[i].Y * 1.5f, vertices2[i].Y, 0.001f, $"Vertex {i} Y should scale with zoom");
            }
        }

        [Test]
        public void TestCenteredPositionWithZoom()
        {
            var viewportSize = new Vector2(800, 600);
            int mapWidth = 10;
            int mapHeight = 10;
            
            HexGridCalculator.SetZoom(1.0f);
            var centered1 = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, mapWidth, mapHeight);
            
            HexGridCalculator.SetZoom(0.5f);
            var centered2 = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, mapWidth, mapHeight);
            
            // Centered position should account for zoom in centering calculation
            // The exact values depend on the centering formula, but they should be different
            Assert.AreNotEqual(centered1.X, centered2.X, "Centered X should change with zoom");
            Assert.AreNotEqual(centered1.Y, centered2.Y, "Centered Y should change with zoom");
        }

        [Test]
        public void TestZoomConsistency()
        {
            // Test that zoom factor is consistent across different operations
            HexGridCalculator.SetZoom(1.5f);
            var zoomFactor = HexGridCalculator.ZoomFactor;
            
            // Perform various operations
            var position = HexGridCalculator.CalculateHexPosition(2, 3);
            var vertices = HexGridCalculator.CreateHexagonVertices();
            var centered = HexGridCalculator.CalculateHexPositionCentered(1, 1, new Vector2(800, 600), 10, 10);
            
            // Zoom factor should remain consistent
            Assert.AreEqual(zoomFactor, HexGridCalculator.ZoomFactor, 0.001f, "Zoom factor should remain consistent");
        }
    }
} 