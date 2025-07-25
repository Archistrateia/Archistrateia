using Godot;
using System;
using NUnit.Framework;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class HexGridPositionTest
    {
        private const float HEX_SIZE = 35.0f;
        private const float HEX_WIDTH = HEX_SIZE * 2.0f;
        private const float HEX_HEIGHT = HEX_SIZE * 1.732f;

        [Test]
        public void TestHexPositionCalculation_EvenColumn()
        {
            // Test even column (column 0) - should align normally
            var position = CalculateHexPosition(0, 0);
            Assert.AreEqual(0, position.X, 0.001f, "X position for (0,0) should be 0");
            Assert.AreEqual(0, position.Y, 0.001f, "Y position for (0,0) should be 0");

            position = CalculateHexPosition(0, 1);
            Assert.AreEqual(0, position.X, 0.001f, "X position for (0,1) should be 0");
            Assert.AreEqual(HEX_HEIGHT, position.Y, 0.001f, "Y position for (0,1) should be HEX_HEIGHT");

            position = CalculateHexPosition(0, 2);
            Assert.AreEqual(0, position.X, 0.001f, "X position for (0,2) should be 0");
            Assert.AreEqual(HEX_HEIGHT * 2, position.Y, 0.001f, "Y position for (0,2) should be 2 * HEX_HEIGHT");
        }

        [Test]
        public void TestHexPositionCalculation_OddColumn()
        {
            // Test odd column (column 1) - should be offset down by HEX_HEIGHT * 0.5
            var position = CalculateHexPosition(1, 0);
            Assert.AreEqual(HEX_WIDTH * 0.75f, position.X, 0.001f, "X position for (1,0) should be HEX_WIDTH * 0.75");
            Assert.AreEqual(HEX_HEIGHT * 0.5f, position.Y, 0.001f, "Y position for (1,0) should be HEX_HEIGHT * 0.5");

            position = CalculateHexPosition(1, 1);
            Assert.AreEqual(HEX_WIDTH * 0.75f, position.X, 0.001f, "X position for (1,1) should be HEX_WIDTH * 0.75");
            Assert.AreEqual(HEX_HEIGHT * 1.5f, position.Y, 0.001f, "Y position for (1,1) should be HEX_HEIGHT * 1.5");

            position = CalculateHexPosition(1, 2);
            Assert.AreEqual(HEX_WIDTH * 0.75f, position.X, 0.001f, "X position for (1,2) should be HEX_WIDTH * 0.75");
            Assert.AreEqual(HEX_HEIGHT * 2.5f, position.Y, 0.001f, "Y position for (1,2) should be HEX_HEIGHT * 2.5");
        }

        [Test]
        public void TestHexPositionCalculation_MixedColumns()
        {
            // Test mixed columns to verify the offset pattern
            var position = CalculateHexPosition(2, 0);
            Assert.AreEqual(HEX_WIDTH * 1.5f, position.X, 0.001f, "X position for (2,0) should be HEX_WIDTH * 1.5 (even column)");
            Assert.AreEqual(0, position.Y, 0.001f, "Y position for (2,0) should be 0");

            position = CalculateHexPosition(3, 1);
            Assert.AreEqual(HEX_WIDTH * 2.25f, position.X, 0.001f, "X position for (3,1) should be HEX_WIDTH * 2.25 (odd column)");
            Assert.AreEqual(HEX_HEIGHT * 1.5f, position.Y, 0.001f, "Y position for (3,1) should be HEX_HEIGHT * 1.5");
        }

        [Test]
        public void TestHexPositionCalculation_Constants()
        {
            // Verify our constants are correct
            Assert.AreEqual(70.0f, HEX_WIDTH, 0.001f, "HEX_WIDTH should be 2 * HEX_SIZE");
            Assert.AreEqual(60.62f, HEX_HEIGHT, 0.01f, "HEX_HEIGHT should be HEX_SIZE * sqrt(3)");
        }

        private Vector2 CalculateHexPosition(int x, int y)
        {
            // Use the actual HexGridCalculator implementation (now with honeycomb formula)
            return HexGridCalculator.CalculateHexPosition(x, y);
        }
    }
}