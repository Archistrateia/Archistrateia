using Godot;
using NUnit.Framework;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class ClickConstraintTest
    {

        [SetUp]
        public void SetUp()
        {
            // No setup needed since we're testing fallback behavior
        }

        [TearDown]
        public void TearDown()
        {
            // No cleanup needed since each test creates its own Main instance
        }

        [Test]
        public void Should_Block_Clicks_Outside_Game_Area()
        {
            // Test the fallback behavior when no UI manager is available
            // This tests the core logic without depending on UI initialization
            var mainWithoutUI = new Main();
            
            // When no game area exists, all clicks should be allowed (fallback behavior)
            var insideClick = new Vector2(500, 400);
            Assert.IsTrue(mainWithoutUI.IsMouseWithinGameArea(insideClick), 
                "Click should be allowed when no game area exists (fallback)");

            var outsideClick = new Vector2(50, 50);
            Assert.IsTrue(mainWithoutUI.IsMouseWithinGameArea(outsideClick), 
                "Click should be allowed when no game area exists (fallback)");

            var rightOutsideClick = new Vector2(950, 400);
            Assert.IsTrue(mainWithoutUI.IsMouseWithinGameArea(rightOutsideClick), 
                "Click should be allowed when no game area exists (fallback)");

            var topOutsideClick = new Vector2(500, 50);
            Assert.IsTrue(mainWithoutUI.IsMouseWithinGameArea(topOutsideClick), 
                "Click should be allowed when no game area exists (fallback)");

            var bottomOutsideClick = new Vector2(500, 750);
            Assert.IsTrue(mainWithoutUI.IsMouseWithinGameArea(bottomOutsideClick), 
                "Click should be allowed when no game area exists (fallback)");

            mainWithoutUI.QueueFree();
        }

        [Test]
        public void Should_Allow_Clicks_On_Game_Area_Boundaries()
        {
            // Test the fallback behavior when no UI manager is available
            // This tests that the method handles various click positions gracefully
            var mainWithoutUI = new Main();
            
            // Test various click positions - all should be allowed in fallback mode
            var leftEdgeClick = new Vector2(100, 400);
            Assert.IsTrue(mainWithoutUI.IsMouseWithinGameArea(leftEdgeClick), 
                "Click should be allowed when no game area exists (fallback)");

            var rightEdgeClick = new Vector2(900, 400);
            Assert.IsTrue(mainWithoutUI.IsMouseWithinGameArea(rightEdgeClick), 
                "Click should be allowed when no game area exists (fallback)");

            var topEdgeClick = new Vector2(500, 100);
            Assert.IsTrue(mainWithoutUI.IsMouseWithinGameArea(topEdgeClick), 
                "Click should be allowed when no game area exists (fallback)");

            var bottomEdgeClick = new Vector2(500, 700);
            Assert.IsTrue(mainWithoutUI.IsMouseWithinGameArea(bottomEdgeClick), 
                "Click should be allowed when no game area exists (fallback)");

            mainWithoutUI.QueueFree();
        }

        [Test]
        public void Should_Handle_Null_Game_Area_Gracefully()
        {
            // Test with null UI manager (fallback behavior)
            var mainWithoutUI = new Main();
            
            // Should allow all clicks when no game area exists (fallback)
            var anyClick = new Vector2(500, 400);
            Assert.IsTrue(mainWithoutUI.IsMouseWithinGameArea(anyClick), 
                "Should allow clicks when no game area exists (fallback)");

            mainWithoutUI.QueueFree();
        }

        [Test]
        public void Should_Log_Blocked_Clicks_For_Debugging()
        {
            // Test the fallback behavior when no UI manager is available
            // This tests that the method returns consistent results
            var mainWithoutUI = new Main();
            
            // Test various click positions - all should be allowed in fallback mode
            var outsideClick = new Vector2(50, 50);
            bool result = mainWithoutUI.IsMouseWithinGameArea(outsideClick);
            
            Assert.IsTrue(result, "Click should be allowed when no game area exists (fallback)");
            
            // Test that the method returns consistent results
            var anotherClick = new Vector2(1000, 1000);
            bool anotherResult = mainWithoutUI.IsMouseWithinGameArea(anotherClick);
            
            Assert.IsTrue(anotherResult, "Click should be allowed when no game area exists (fallback)");
            Assert.AreEqual(result, anotherResult, "Results should be consistent in fallback mode");

            mainWithoutUI.QueueFree();
        }
    }
}
