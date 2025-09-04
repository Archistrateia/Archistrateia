using Godot;
using System;
using NUnit.Framework;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class HexGridTest
    {
        private const float HEX_SIZE = 35.0f;
        private const float HEX_WIDTH = HEX_SIZE * 2.0f;
        private const float HEX_HEIGHT = HEX_SIZE * 1.732f;

        [Test]
        public void Should_Have_Correct_Constants()
        {
            HexGridCalculator.SetZoom(1.0f);
            
            Assert.AreEqual(35.0f, HexGridCalculator.HEX_SIZE, 0.001f, "HEX_SIZE should be 35.0f");
            Assert.AreEqual(70.0f, HexGridCalculator.HEX_WIDTH, 0.001f, "HEX_WIDTH should be 70.0f");
            Assert.AreEqual(60.62f, HexGridCalculator.HEX_HEIGHT, 0.01f, "HEX_HEIGHT should be 60.62f");
            
            Assert.AreEqual(70.0f, HEX_WIDTH, 0.001f, "HEX_WIDTH should be 2 * HEX_SIZE");
            Assert.AreEqual(60.62f, HEX_HEIGHT, 0.01f, "HEX_HEIGHT should be HEX_SIZE * sqrt(3)");
        }

        [Test]
        public void Should_Calculate_Flat_Top_Spacing_Constants()
        {
            HexGridCalculator.SetZoom(1.0f);
            
            float horizontalSpacing = HEX_WIDTH * 0.75f;
            float verticalSpacing = HEX_HEIGHT;
            float oddColumnOffset = HEX_HEIGHT * 0.5f;

            Assert.AreEqual(52.5f, horizontalSpacing, 0.001f, "Horizontal spacing should be HEX_WIDTH * 0.75");
            Assert.AreEqual(60.62f, verticalSpacing, 0.01f, "Vertical spacing should be HEX_HEIGHT");
            Assert.AreEqual(30.31f, oddColumnOffset, 0.01f, "Odd column offset should be HEX_HEIGHT * 0.5");
        }

        [Test]
        public void Should_Position_Even_Columns_Correctly()
        {
            HexGridCalculator.SetZoom(1.0f);
            
            var position = HexGridCalculator.CalculateHexPosition(0, 0);
            Assert.AreEqual(0, position.X, 0.001f, "X position for (0,0) should be 0");
            Assert.AreEqual(0, position.Y, 0.001f, "Y position for (0,0) should be 0");

            position = HexGridCalculator.CalculateHexPosition(0, 1);
            Assert.AreEqual(0, position.X, 0.001f, "X position for (0,1) should be 0");
            Assert.AreEqual(HEX_HEIGHT, position.Y, 0.001f, "Y position for (0,1) should be HEX_HEIGHT");

            position = HexGridCalculator.CalculateHexPosition(0, 2);
            Assert.AreEqual(0, position.X, 0.001f, "X position for (0,2) should be 0");
            Assert.AreEqual(HEX_HEIGHT * 2, position.Y, 0.001f, "Y position for (0,2) should be 2 * HEX_HEIGHT");
            
            position = HexGridCalculator.CalculateHexPosition(2, 0);
            Assert.AreEqual(HEX_WIDTH * 1.5f, position.X, 0.001f, "X position for (2,0) should be HEX_WIDTH * 1.5 (even column)");
            Assert.AreEqual(0, position.Y, 0.001f, "Y position for (2,0) should be 0");
        }

        [Test]
        public void Should_Position_Odd_Columns_Correctly()
        {
            HexGridCalculator.SetZoom(1.0f);
            
            var position = HexGridCalculator.CalculateHexPosition(1, 0);
            Assert.AreEqual(HEX_WIDTH * 0.75f, position.X, 0.001f, "X position for (1,0) should be HEX_WIDTH * 0.75");
            Assert.AreEqual(HEX_HEIGHT * 0.5f, position.Y, 0.001f, "Y position for (1,0) should be HEX_HEIGHT * 0.5");

            position = HexGridCalculator.CalculateHexPosition(1, 1);
            Assert.AreEqual(HEX_WIDTH * 0.75f, position.X, 0.001f, "X position for (1,1) should be HEX_WIDTH * 0.75");
            Assert.AreEqual(HEX_HEIGHT * 1.5f, position.Y, 0.001f, "Y position for (1,1) should be HEX_HEIGHT * 1.5");

            position = HexGridCalculator.CalculateHexPosition(1, 2);
            Assert.AreEqual(HEX_WIDTH * 0.75f, position.X, 0.001f, "X position for (1,2) should be HEX_WIDTH * 0.75");
            Assert.AreEqual(HEX_HEIGHT * 2.5f, position.Y, 0.001f, "Y position for (1,2) should be HEX_HEIGHT * 2.5");
            
            position = HexGridCalculator.CalculateHexPosition(3, 1);
            Assert.AreEqual(HEX_WIDTH * 2.25f, position.X, 0.001f, "X position for (3,1) should be HEX_WIDTH * 2.25 (odd column)");
            Assert.AreEqual(HEX_HEIGHT * 1.5f, position.Y, 0.001f, "Y position for (3,1) should be HEX_HEIGHT * 1.5");
        }

        [Test]
        public void Should_Calculate_Centered_Positions_Correctly()
        {
            HexGridCalculator.SetZoom(1.0f);
            
            var viewportSize = new Vector2(800, 600);
            int mapWidth = 10;
            int mapHeight = 10;

            var centeredPosition = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, mapWidth, mapHeight);

            float expectedCenterX = (viewportSize.X - (mapWidth * HexGridCalculator.HEX_WIDTH * 0.75f + HexGridCalculator.HEX_WIDTH * 0.25f)) / 2;
            float expectedCenterY = (viewportSize.Y - (mapHeight * HexGridCalculator.HEX_HEIGHT + HexGridCalculator.HEX_HEIGHT * 0.5f)) / 2;

            Assert.AreEqual(expectedCenterX, centeredPosition.X, 0.001f, "Centered X should match expected value");
            Assert.AreEqual(expectedCenterY, centeredPosition.Y, 0.001f, "Centered Y should match expected value");
        }

        [Test]
        public void Should_Create_Proper_Hexagon_Vertices()
        {
            HexGridCalculator.SetZoom(1.0f);
            
            var vertices = HexGridCalculator.CreateHexagonVertices();

            Assert.AreEqual(6, vertices.Length, "Should have 6 vertices");

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

        [Test]
        public void Should_Tessellate_Flat_Top_Hexagons_Correctly()
        {
            HexGridCalculator.SetZoom(1.0f);
            
            var pos00 = HexGridCalculator.CalculateHexPosition(0, 0);
            var pos10 = HexGridCalculator.CalculateHexPosition(1, 0);
            var pos01 = HexGridCalculator.CalculateHexPosition(0, 1);
            var pos11 = HexGridCalculator.CalculateHexPosition(1, 1);
            
            float horizontalSpacing = pos10.X - pos00.X;
            float verticalSpacing = pos01.Y - pos00.Y;
            float offsetShift = pos10.Y - pos00.Y;
            
            Assert.AreEqual(52.5f, horizontalSpacing, 0.01f, "Horizontal spacing should be 3/4 of hex width");
            Assert.AreEqual(60.62f, verticalSpacing, 0.01f, "Vertical spacing should be hex height");
            Assert.AreEqual(30.31f, offsetShift, 0.01f, "Offset shift should be half hex height");
            
            Assert.AreEqual(pos00.X + 52.5f, pos10.X, 0.01f, "Adjacent horizontal hex should be offset by 3/4 width");
            Assert.AreEqual(pos00.Y + 30.31f, pos10.Y, 0.01f, "Odd column should be offset down by half height");
            Assert.AreEqual(pos00.Y + 60.62f, pos01.Y, 0.01f, "Vertical neighbor should be one full height away");
            Assert.AreEqual(pos10.Y + 60.62f, pos11.Y, 0.01f, "Vertical spacing should be consistent across columns");
        }

        [Test]
        public void Should_Handle_Zoom_Correctly()
        {
            HexGridCalculator.SetZoom(2.0f);
            
            var position = HexGridCalculator.CalculateHexPosition(1, 1);
            
            var expectedX = HEX_WIDTH * 0.75f * 2.0f;
            var expectedY = HEX_HEIGHT * 1.5f * 2.0f;
            
            Assert.AreEqual(expectedX, position.X, 0.001f, "X position should be scaled by zoom factor");
            Assert.AreEqual(expectedY, position.Y, 0.001f, "Y position should be scaled by zoom factor");
            
            HexGridCalculator.SetZoom(1.0f);
        }

        [Test]
        public void Should_Maintain_Zoom_Factor_Constraints()
        {
            HexGridCalculator.SetZoom(0.05f);
            Assert.AreEqual(0.1f, HexGridCalculator.ZoomFactor, 0.001f, "Zoom should be clamped to minimum 0.1");
            
            HexGridCalculator.SetZoom(5.0f);
            Assert.AreEqual(3.0f, HexGridCalculator.ZoomFactor, 0.001f, "Zoom should be clamped to maximum 3.0");
            
            HexGridCalculator.SetZoom(1.5f);
            Assert.AreEqual(1.5f, HexGridCalculator.ZoomFactor, 0.001f, "Valid zoom should be preserved");
            
            HexGridCalculator.SetZoom(1.0f);
        }



        [Test]
        public void Should_Scale_Vertices_With_Zoom()
        {
            HexGridCalculator.SetZoom(2.0f);
            
            var vertices = HexGridCalculator.CreateHexagonVertices();
            
            for (int i = 0; i < 6; i++)
            {
                var vertex = vertices[i];
                float expectedAngle = i * Mathf.Pi / 3.0f;
                float expectedX = HexGridCalculator.HEX_SIZE * 2.0f * Mathf.Cos(expectedAngle);
                float expectedY = HexGridCalculator.HEX_SIZE * 2.0f * Mathf.Sin(expectedAngle);

                Assert.AreEqual(expectedX, vertex.X, 0.001f, $"Zoomed vertex {i} X should be scaled");
                Assert.AreEqual(expectedY, vertex.Y, 0.001f, $"Zoomed vertex {i} Y should be scaled");
            }
            
            HexGridCalculator.SetZoom(1.0f);
        }

        [Test]
        public void Should_Maintain_Zoom_Consistency()
        {
            HexGridCalculator.SetZoom(1.5f);
            
            var pos1 = HexGridCalculator.CalculateHexPosition(2, 3);
            var centeredPos1 = HexGridCalculator.CalculateHexPositionCentered(2, 3, new Vector2(800, 600), 10, 10);
            
            HexGridCalculator.SetZoom(1.0f);
            var pos2 = HexGridCalculator.CalculateHexPosition(2, 3);
            var centeredPos2 = HexGridCalculator.CalculateHexPositionCentered(2, 3, new Vector2(800, 600), 10, 10);
            
            Assert.AreEqual(pos1.X / 1.5f, pos2.X, 0.001f, "Position scaling should be consistent");
            Assert.AreEqual(pos1.Y / 1.5f, pos2.Y, 0.001f, "Position scaling should be consistent");
            
            HexGridCalculator.SetZoom(1.0f);
        }
    }
} 