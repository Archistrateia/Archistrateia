using Godot;
using NUnit.Framework;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class FlatTopHexGridTest
    {
        private const float HEX_SIZE = 35.0f;
        private const float HEX_WIDTH = HEX_SIZE * 2.0f;
        private const float HEX_HEIGHT = HEX_SIZE * 1.732f;

        [Test]
        public void TestFlatTopConstants()
        {
            // Flat-top spacing constants
            float horizontalSpacing = HEX_WIDTH * 0.75f;
            float verticalSpacing = HEX_HEIGHT;
            float oddColumnOffset = HEX_HEIGHT * 0.5f;

            Assert.AreEqual(52.5f, horizontalSpacing, 0.001f, "Horizontal spacing should be HEX_WIDTH * 0.75");
            Assert.AreEqual(60.62f, verticalSpacing, 0.01f, "Vertical spacing should be HEX_HEIGHT");
            Assert.AreEqual(30.31f, oddColumnOffset, 0.01f, "Odd column offset should be HEX_HEIGHT * 0.5");
        }

        [Test]
        public void TestFlatTopPositioning_EvenColumns()
        {
            // Even columns should align normally
            var position = CalculateFlatTopPosition(0, 0);
            Assert.AreEqual(0, position.X, 0.001f, "X position for (0,0) should be 0");
            Assert.AreEqual(0, position.Y, 0.001f, "Y position for (0,0) should be 0");

            position = CalculateFlatTopPosition(0, 1);
            Assert.AreEqual(0, position.X, 0.001f, "X position for (0,1) should be 0");
            Assert.AreEqual(HEX_HEIGHT, position.Y, 0.001f, "Y position for (0,1) should be HEX_HEIGHT");

            position = CalculateFlatTopPosition(0, 2);
            Assert.AreEqual(0, position.X, 0.001f, "X position for (0,2) should be 0");
            Assert.AreEqual(HEX_HEIGHT * 2, position.Y, 0.001f, "Y position for (0,2) should be 2 * HEX_HEIGHT");
        }

        [Test]
        public void TestFlatTopPositioning_OddColumns()
        {
            // Odd columns should be offset down by HEX_HEIGHT * 0.5
            var position = CalculateFlatTopPosition(1, 0);
            Assert.AreEqual(HEX_WIDTH * 0.75f, position.X, 0.001f, "X position for (1,0) should be HEX_WIDTH * 0.75");
            Assert.AreEqual(HEX_HEIGHT * 0.5f, position.Y, 0.001f, "Y position for (1,0) should be HEX_HEIGHT * 0.5");

            position = CalculateFlatTopPosition(1, 1);
            Assert.AreEqual(HEX_WIDTH * 0.75f, position.X, 0.001f, "X position for (1,1) should be HEX_WIDTH * 0.75");
            Assert.AreEqual(HEX_HEIGHT * 1.5f, position.Y, 0.001f, "Y position for (1,1) should be HEX_HEIGHT * 1.5");

            position = CalculateFlatTopPosition(1, 2);
            Assert.AreEqual(HEX_WIDTH * 0.75f, position.X, 0.001f, "X position for (1,2) should be HEX_WIDTH * 0.75");
            Assert.AreEqual(HEX_HEIGHT * 2.5f, position.Y, 0.001f, "Y position for (1,2) should be HEX_HEIGHT * 2.5");
        }

        [Test]
        public void TestFlatTopPositioning_MixedColumns()
        {
            // Test the flat-top pattern across multiple columns
            var position = CalculateFlatTopPosition(2, 0);
            Assert.AreEqual(HEX_WIDTH * 1.5f, position.X, 0.001f, "X position for (2,0) should be HEX_WIDTH * 1.5 (even column)");
            Assert.AreEqual(0, position.Y, 0.001f, "Y position for (2,0) should be 0");

            position = CalculateFlatTopPosition(2, 1);
            Assert.AreEqual(HEX_WIDTH * 1.5f, position.X, 0.001f, "X position for (2,1) should be HEX_WIDTH * 1.5 (even column)");
            Assert.AreEqual(HEX_HEIGHT, position.Y, 0.001f, "Y position for (2,1) should be HEX_HEIGHT");

            position = CalculateFlatTopPosition(3, 0);
            Assert.AreEqual(HEX_WIDTH * 2.25f, position.X, 0.001f, "X position for (3,0) should be HEX_WIDTH * 2.25 (odd column)");
            Assert.AreEqual(HEX_HEIGHT * 0.5f, position.Y, 0.001f, "Y position for (3,0) should be HEX_HEIGHT * 0.5");
        }

        [Test]
        public void TestFlatTopTessellation()
        {
            // Test that adjacent hexagons are properly spaced in flat-top layout
            var center = CalculateFlatTopPosition(0, 0);
            var right = CalculateFlatTopPosition(1, 0);
            var down = CalculateFlatTopPosition(0, 1);
            
            // Distance between horizontally adjacent hexes (accounting for vertical offset)
            // Right hex is at (52.5, 30.31), center is at (0, 0)
            // Distance = sqrt(52.5^2 + 30.31^2)
            float expectedHorizontalDistance = Mathf.Sqrt(52.5f * 52.5f + 30.31f * 30.31f);
            float actualHorizontalDistance = center.DistanceTo(right);
            Assert.AreEqual(expectedHorizontalDistance, actualHorizontalDistance, 0.1f, "Distance to right neighbor should account for vertical offset");
            
            // Vertical spacing between rows in same column
            float expectedVerticalDistance = HEX_HEIGHT;
            float actualVerticalDistance = center.DistanceTo(down);
            Assert.AreEqual(expectedVerticalDistance, actualVerticalDistance, 0.1f, "Distance to down neighbor should be HEX_HEIGHT");
        }

        private Vector2 CalculateFlatTopPosition(int x, int y)
        {
            // Use the actual HexGridCalculator implementation (now with flat-top formula)
            return HexGridCalculator.CalculateHexPosition(x, y);
        }
    }
}