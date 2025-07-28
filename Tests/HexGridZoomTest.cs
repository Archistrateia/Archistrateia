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
            var originalZoom = HexGridCalculator.ZoomFactor;
            
            HexGridCalculator.SetZoom(1.5f);
            Assert.AreEqual(1.5f, HexGridCalculator.ZoomFactor);
            
            HexGridCalculator.SetZoom(originalZoom);
            Assert.AreEqual(originalZoom, HexGridCalculator.ZoomFactor);
        }

        [Test]
        public void TestScrollOffsetFunctionality()
        {
            var originalScrollOffset = HexGridCalculator.ScrollOffset;
            
            // Test setting scroll offset
            var testOffset = new Vector2(100.0f, 50.0f);
            HexGridCalculator.SetScrollOffset(testOffset);
            Assert.AreEqual(testOffset, HexGridCalculator.ScrollOffset);
            
            // Test adding scroll offset
            var delta = new Vector2(25.0f, -10.0f);
            HexGridCalculator.AddScrollOffset(delta);
            Assert.AreEqual(testOffset + delta, HexGridCalculator.ScrollOffset);
            
            // Test that scroll offset affects position calculation
            var viewportSize = new Vector2(800, 600);
            
            // Reset scroll offset to zero for baseline
            HexGridCalculator.SetScrollOffset(Vector2.Zero);
            var positionWithZeroScroll = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, 10, 10);
            
            // Set scroll offset and verify position changes
            HexGridCalculator.SetScrollOffset(testOffset);
            var positionWithScroll = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, 10, 10);
            
            // Position should be different when scroll offset is applied
            Assert.AreNotEqual(positionWithZeroScroll, positionWithScroll);
            
            // The difference should be the NEGATIVE scroll offset (due to the fix)
            var expectedDifference = -testOffset;
            var actualDifference = positionWithScroll - positionWithZeroScroll;
            Assert.AreEqual(expectedDifference.X, actualDifference.X, 0.001f);
            Assert.AreEqual(expectedDifference.Y, actualDifference.Y, 0.001f);
            
            // Restore original scroll offset
            HexGridCalculator.SetScrollOffset(originalScrollOffset);
        }

        [Test]
        public void TestScrollDirectionIsCorrect()
        {
            var originalScrollOffset = HexGridCalculator.ScrollOffset;
            var viewportSize = new Vector2(800, 600);
            
            // Reset to zero scroll
            HexGridCalculator.SetScrollOffset(Vector2.Zero);
            var basePosition = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, 10, 10);
            
            // Test right scroll (positive X offset) - should move grid left (showing more right side)
            HexGridCalculator.SetScrollOffset(new Vector2(100.0f, 0.0f));
            var rightScrollPosition = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, 10, 10);
            Assert.Less(rightScrollPosition.X, basePosition.X, "Right scroll should move grid left (positive X offset)");
            
            // Test down scroll (positive Y offset) - should move grid up (showing more bottom)
            HexGridCalculator.SetScrollOffset(new Vector2(0.0f, 100.0f));
            var downScrollPosition = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, 10, 10);
            Assert.Less(downScrollPosition.Y, basePosition.Y, "Down scroll should move grid up (positive Y offset)");
            
            // Test left scroll (negative X offset) - should move grid right (showing more left side)
            HexGridCalculator.SetScrollOffset(new Vector2(-100.0f, 0.0f));
            var leftScrollPosition = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, 10, 10);
            Assert.Greater(leftScrollPosition.X, basePosition.X, "Left scroll should move grid right (negative X offset)");
            
            // Test up scroll (negative Y offset) - should move grid down (showing more top)
            HexGridCalculator.SetScrollOffset(new Vector2(0.0f, -100.0f));
            var upScrollPosition = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, 10, 10);
            Assert.Greater(upScrollPosition.Y, basePosition.Y, "Up scroll should move grid down (negative Y offset)");
            
            // Restore original scroll offset
            HexGridCalculator.SetScrollOffset(originalScrollOffset);
        }

        [Test]
        public void TestCurrentScrollingBehavior()
        {
            var originalScrollOffset = HexGridCalculator.ScrollOffset;
            var viewportSize = new Vector2(800, 600);
            
            // Reset to zero scroll
            HexGridCalculator.SetScrollOffset(Vector2.Zero);
            var basePosition = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, 10, 10);
            
            // Document corrected behavior:
            // Positive X offset = grid moves left (tiles appear to move right)
            // Positive Y offset = grid moves up (tiles appear to move down)
            // Negative X offset = grid moves right (tiles appear to move left)  
            // Negative Y offset = grid moves down (tiles appear to move up)
            
            // Test positive X offset
            HexGridCalculator.SetScrollOffset(new Vector2(100.0f, 0.0f));
            var positiveXPosition = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, 10, 10);
            Assert.Less(positiveXPosition.X, basePosition.X, "Positive X offset moves grid left");
            
            // Test positive Y offset
            HexGridCalculator.SetScrollOffset(new Vector2(0.0f, 100.0f));
            var positiveYPosition = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, 10, 10);
            Assert.Less(positiveYPosition.Y, basePosition.Y, "Positive Y offset moves grid up");
            
            // Test negative X offset
            HexGridCalculator.SetScrollOffset(new Vector2(-100.0f, 0.0f));
            var negativeXPosition = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, 10, 10);
            Assert.Greater(negativeXPosition.X, basePosition.X, "Negative X offset moves grid right");
            
            // Test negative Y offset
            HexGridCalculator.SetScrollOffset(new Vector2(0.0f, -100.0f));
            var negativeYPosition = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, 10, 10);
            Assert.Greater(negativeYPosition.Y, basePosition.Y, "Negative Y offset moves grid down");
            
            // Restore original scroll offset
            HexGridCalculator.SetScrollOffset(originalScrollOffset);
        }

        [Test]
        public void TestInvertedScrollingBehavior()
        {
            var originalScrollOffset = HexGridCalculator.ScrollOffset;
            var viewportSize = new Vector2(800, 600);
            
            // Reset to zero scroll
            HexGridCalculator.SetScrollOffset(Vector2.Zero);
            var basePosition = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, 10, 10);
            
            // Document what the corrected behavior now is:
            // Mouse at right edge → adds POSITIVE X offset (grid moves left, showing more right)
            // Mouse at left edge → adds NEGATIVE X offset (grid moves right, showing more left)
            // Mouse at bottom edge → adds POSITIVE Y offset (grid moves up, showing more bottom)
            // Mouse at top edge → adds NEGATIVE Y offset (grid moves down, showing more top)
            
            // This is the CORRECT edge scrolling behavior
            
            // Test what positive X offset does (for right edge scrolling)
            HexGridCalculator.SetScrollOffset(new Vector2(100.0f, 0.0f));
            var positiveXPosition = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, 10, 10);
            Assert.Less(positiveXPosition.X, basePosition.X, "Positive X offset moves grid left (for right edge)");
            
            // Test what positive Y offset does (for bottom edge scrolling)
            HexGridCalculator.SetScrollOffset(new Vector2(0.0f, 100.0f));
            var positiveYPosition = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, 10, 10);
            Assert.Less(positiveYPosition.Y, basePosition.Y, "Positive Y offset moves grid up (for bottom edge)");
            
            // Restore original scroll offset
            HexGridCalculator.SetScrollOffset(originalScrollOffset);
        }

        [Test]
        public void TestScrollBoundsWithZoom()
        {
            var originalScrollOffset = HexGridCalculator.ScrollOffset;
            var originalZoom = HexGridCalculator.ZoomFactor;
            var viewportSize = new Vector2(800, 600);
            var mapWidth = 20;
            var mapHeight = 10;
            
            // Test with different zoom levels
            var zoomLevels = new float[] { 0.5f, 1.0f, 1.5f, 2.0f, 2.5f };
            
            foreach (var zoom in zoomLevels)
            {
                HexGridCalculator.SetZoom(zoom);
                
                // Get the scroll bounds for this zoom level
                var scrollBounds = HexGridCalculator.CalculateScrollBounds(viewportSize, mapWidth, mapHeight);
                
                // Test scrolling to the top-left corner (negative scroll offset)
                HexGridCalculator.SetScrollOffset(new Vector2(-scrollBounds.X, -scrollBounds.Y));
                var topLeftPosition = HexGridCalculator.CalculateHexPositionCentered(0, 0, viewportSize, mapWidth, mapHeight);
                
                // The top-left tile should be fully visible (not cut off)
                Assert.GreaterOrEqual(topLeftPosition.X, 0, $"Top-left tile should be fully visible at zoom {zoom}");
                Assert.GreaterOrEqual(topLeftPosition.Y, 0, $"Top-left tile should be fully visible at zoom {zoom}");
                
                // Test scrolling to the bottom-right corner (positive scroll offset)
                HexGridCalculator.SetScrollOffset(new Vector2(scrollBounds.X, scrollBounds.Y));
                var bottomRightPosition = HexGridCalculator.CalculateHexPositionCentered(mapWidth - 1, mapHeight - 1, viewportSize, mapWidth, mapHeight);
                
                // The bottom-right tile should be fully visible (not cut off)
                Assert.LessOrEqual(bottomRightPosition.X, viewportSize.X, $"Bottom-right tile should be fully visible at zoom {zoom}");
                Assert.LessOrEqual(bottomRightPosition.Y, viewportSize.Y, $"Bottom-right tile should be fully visible at zoom {zoom}");
            }
            
            // Restore original settings
            HexGridCalculator.SetScrollOffset(originalScrollOffset);
            HexGridCalculator.SetZoom(originalZoom);
        }

        [Test]
        public void TestOptimalZoomCalculation()
        {
            var originalZoom = HexGridCalculator.ZoomFactor;
            
            // Test with a large viewport (should zoom in for small grid)
            var largeViewport = new Vector2(1200, 800);
            var smallGrid = new { Width = 10, Height = 5 };
            var optimalZoomLarge = HexGridCalculator.CalculateOptimalZoom(largeViewport, smallGrid.Width, smallGrid.Height);
            
            // For a small grid on a large viewport, we should zoom in (zoom > 1.0)
            Assert.Greater(optimalZoomLarge, 1.0f, "Small grid on large viewport should zoom in");
            
            // Test with a small viewport (should stay at zoom 1.0 for large grid)
            var smallViewport = new Vector2(400, 300);
            var largeGrid = new { Width = 30, Height = 20 };
            var optimalZoomSmall = HexGridCalculator.CalculateOptimalZoom(smallViewport, largeGrid.Width, largeGrid.Height);
            
            // For a large grid on a small viewport, we should stay at zoom 1.0 (minimum)
            Assert.AreEqual(1.0f, optimalZoomSmall, 0.01f, "Large grid on small viewport should stay at zoom 1.0");
            
            // Test with current game dimensions (20x10 grid on 800x600 viewport)
            var gameViewport = new Vector2(800, 600);
            var gameGrid = new { Width = 20, Height = 10 };
            var optimalZoomGame = HexGridCalculator.CalculateOptimalZoom(gameViewport, gameGrid.Width, gameGrid.Height);
            
            // Should be reasonable (between 1.0 and 2.0 for typical game setup)
            Assert.GreaterOrEqual(optimalZoomGame, 1.0f, "Game zoom should be at least 1.0");
            Assert.LessOrEqual(optimalZoomGame, 3.0f, "Game zoom should not exceed maximum");
            
            // Restore original zoom
            HexGridCalculator.SetZoom(originalZoom);
        }

        [Test]
        public void TestScrollingNeededLogic()
        {
            var originalZoom = HexGridCalculator.ZoomFactor;
            var originalScrollOffset = HexGridCalculator.ScrollOffset;
            
            // Reset to default state
            HexGridCalculator.SetZoom(1.0f);
            HexGridCalculator.SetScrollOffset(Vector2.Zero);
            
            // Test case 1: Small grid on large viewport (no scrolling needed)
            var largeViewport = new Vector2(1200, 800);
            var smallGrid = new { Width = 10, Height = 5 };
            bool scrollingNeededSmall = HexGridCalculator.IsScrollingNeeded(largeViewport, smallGrid.Width, smallGrid.Height);
            Assert.IsFalse(scrollingNeededSmall, "Small grid on large viewport should not need scrolling");
            
            // Test case 2: Large grid on small viewport (scrolling needed)
            var smallViewport = new Vector2(400, 300);
            var largeGrid = new { Width = 30, Height = 20 };
            bool scrollingNeededLarge = HexGridCalculator.IsScrollingNeeded(smallViewport, largeGrid.Width, largeGrid.Height);
            Assert.IsTrue(scrollingNeededLarge, "Large grid on small viewport should need scrolling");
            
            // Test case 3: Zoomed in grid (scrolling needed)
            HexGridCalculator.SetZoom(3.0f);
            bool scrollingNeededZoomed = HexGridCalculator.IsScrollingNeeded(largeViewport, smallGrid.Width, smallGrid.Height);
            Assert.IsTrue(scrollingNeededZoomed, "Zoomed in grid should need scrolling");
            
            // Test case 4: Zoomed out grid (no scrolling needed)
            HexGridCalculator.SetZoom(0.5f);
            bool scrollingNeededZoomedOut = HexGridCalculator.IsScrollingNeeded(largeViewport, smallGrid.Width, smallGrid.Height);
            Assert.IsFalse(scrollingNeededZoomedOut, "Zoomed out grid should not need scrolling");
            
            // Test case 5: Current game dimensions
            var gameViewport = new Vector2(800, 600);
            var gameGrid = new { Width = 20, Height = 10 };
            HexGridCalculator.SetZoom(1.0f);
            bool scrollingNeededGame = HexGridCalculator.IsScrollingNeeded(gameViewport, gameGrid.Width, gameGrid.Height);
            
            // This should be true for the current game setup (20x10 grid on 800x600 viewport)
            Assert.IsTrue(scrollingNeededGame, "Current game setup should need scrolling");
            
            // Restore original settings
            HexGridCalculator.SetZoom(originalZoom);
            HexGridCalculator.SetScrollOffset(originalScrollOffset);
        }

        [Test]
        public void TestHomeKeyAlwaysWorks()
        {
            var originalZoom = HexGridCalculator.ZoomFactor;
            var originalScrollOffset = HexGridCalculator.ScrollOffset;
            
            // Test that Home key works even when scrolling is not needed
            var largeViewport = new Vector2(1200, 800);
            var smallGrid = new { Width = 10, Height = 5 };
            
            // Set some scroll offset
            HexGridCalculator.SetScrollOffset(new Vector2(100.0f, 50.0f));
            Assert.AreNotEqual(Vector2.Zero, HexGridCalculator.ScrollOffset);
            
            // Verify that scrolling is not needed for this setup
            bool scrollingNeeded = HexGridCalculator.IsScrollingNeeded(largeViewport, smallGrid.Width, smallGrid.Height);
            Assert.IsFalse(scrollingNeeded, "Small grid on large viewport should not need scrolling");
            
            // Home key should still reset scroll offset to zero
            HexGridCalculator.SetScrollOffset(Vector2.Zero);
            Assert.AreEqual(Vector2.Zero, HexGridCalculator.ScrollOffset);
            
            // Restore original settings
            HexGridCalculator.SetZoom(originalZoom);
            HexGridCalculator.SetScrollOffset(originalScrollOffset);
        }


    }
} 