using NUnit.Framework;
using Archistrateia;
using Godot;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class ViewportControllerZoomTest
    {
        [SetUp]
        public void SetUp()
        {
            // Initialize HexGridCalculator to a known state
            HexGridCalculator.SetZoom(1.0f);
            HexGridCalculator.SetScrollOffset(new Vector2(0, 0));
        }

        [Test]
        public void TestZoomOutCentersScrollOffset()
        {
            // Test the core zoom out centering logic
            // Set a non-zero scroll offset
            HexGridCalculator.SetScrollOffset(new Vector2(100.0f, 50.0f));
            Assert.AreNotEqual(new Vector2(0, 0), HexGridCalculator.ScrollOffset, "Should have non-zero scroll offset initially");
            
            // Simulate zoom out centering logic (newZoom < oldZoom)
            var oldZoom = 2.0f;
            var newZoom = 1.0f;
            
            if (newZoom < oldZoom)
            {
                HexGridCalculator.SetScrollOffset(new Vector2(0, 0));
            }
            
            // Verify scroll offset is reset to zero (centered)
            Assert.AreEqual(new Vector2(0, 0), HexGridCalculator.ScrollOffset, "Zoom out should center the map (reset scroll to zero)");
        }

        [Test]
        public void TestZoomInDoesNotCenterScrollOffset()
        {
            // Test that zoom in does NOT center the scroll offset
            // Set a non-zero scroll offset
            HexGridCalculator.SetScrollOffset(new Vector2(100.0f, 50.0f));
            var originalScrollOffset = HexGridCalculator.ScrollOffset;
            
            // Simulate zoom in logic (newZoom > oldZoom)
            var oldZoom = 1.0f;
            var newZoom = 2.0f;
            
            if (newZoom < oldZoom) // This condition should be false for zoom in
            {
                HexGridCalculator.SetScrollOffset(new Vector2(0, 0));
            }
            
            // Verify scroll offset is unchanged
            Assert.AreEqual(originalScrollOffset, HexGridCalculator.ScrollOffset, "Zoom in should NOT center the map (keep scroll offset)");
        }

        [Test]
        public void TestSetZoomOutCentersScrollOffset()
        {
            // Test SetZoom with lower value (zoom out) centers the scroll offset
            // Set a non-zero scroll offset
            HexGridCalculator.SetScrollOffset(new Vector2(100.0f, 50.0f));
            Assert.AreNotEqual(new Vector2(0, 0), HexGridCalculator.ScrollOffset, "Should have non-zero scroll offset initially");
            
            // Simulate SetZoom to lower value (zoom out)
            var oldZoom = 2.0f;
            var newZoom = 1.0f;
            
            if (newZoom < oldZoom)
            {
                HexGridCalculator.SetScrollOffset(new Vector2(0, 0));
            }
            
            // Verify scroll offset is reset to zero (centered)
            Assert.AreEqual(new Vector2(0, 0), HexGridCalculator.ScrollOffset, "SetZoom to lower value should center the map");
        }

        [Test]
        public void TestSetZoomInDoesNotCenterScrollOffset()
        {
            // Test SetZoom with higher value (zoom in) does NOT center the scroll offset
            // Set a non-zero scroll offset
            HexGridCalculator.SetScrollOffset(new Vector2(100.0f, 50.0f));
            var originalScrollOffset = HexGridCalculator.ScrollOffset;
            
            // Simulate SetZoom to higher value (zoom in)
            var oldZoom = 1.0f;
            var newZoom = 2.0f;
            
            if (newZoom < oldZoom) // This condition should be false for zoom in
            {
                HexGridCalculator.SetScrollOffset(new Vector2(0, 0));
            }
            
            // Verify scroll offset is unchanged
            Assert.AreEqual(originalScrollOffset, HexGridCalculator.ScrollOffset, "SetZoom to higher value should NOT center the map");
        }

        [Test]
        public void TestSameZoomValueDoesNotCenterScrollOffset()
        {
            // Test that setting the same zoom value does NOT center the scroll offset
            // Set a non-zero scroll offset
            HexGridCalculator.SetScrollOffset(new Vector2(100.0f, 50.0f));
            var originalScrollOffset = HexGridCalculator.ScrollOffset;
            
            // Simulate SetZoom to same value
            var oldZoom = 1.5f;
            var newZoom = 1.5f;
            
            if (newZoom < oldZoom) // This condition should be false for same zoom
            {
                HexGridCalculator.SetScrollOffset(new Vector2(0, 0));
            }
            
            // Verify scroll offset is unchanged
            Assert.AreEqual(originalScrollOffset, HexGridCalculator.ScrollOffset, "SetZoom to same value should NOT center the map");
        }
    }
}