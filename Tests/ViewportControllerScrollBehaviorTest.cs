using NUnit.Framework;
using Godot;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class ViewportControllerScrollBehaviorTest
    {
        private static readonly Vector2 GameAreaSize = new Vector2(600, 400);
        private static readonly Rect2 GameGridRect = new Rect2(0, 0, 600, 400);
        private static readonly Vector2 LeftEdgeMouse = new Vector2(10, 200);

        [SetUp]
        public void SetUp()
        {
            HexGridCalculator.SetZoom(1.0f);
            HexGridCalculator.SetScrollOffset(Vector2.Zero);
        }

        [Test]
        public void EdgeScroll_Should_Not_Move_Before_Delay()
        {
            var controller = new ViewportController(50, 30);

            // First edge sample only arms the pending direction.
            controller.HandleEdgeScrolling(LeftEdgeMouse, GameGridRect, GameAreaSize, false, 0.10);
            // Second sample accumulates time but remains below threshold (0.18s).
            controller.HandleEdgeScrolling(LeftEdgeMouse, GameGridRect, GameAreaSize, false, 0.05);

            Assert.AreEqual(Vector2.Zero, controller.ScrollOffset,
                "Edge scrolling should not move before the hover delay is reached.");
        }

        [Test]
        public void EdgeScroll_Should_Move_After_Delay()
        {
            var controller = new ViewportController(50, 30);

            controller.HandleEdgeScrolling(LeftEdgeMouse, GameGridRect, GameAreaSize, false, 0.10);
            controller.HandleEdgeScrolling(LeftEdgeMouse, GameGridRect, GameAreaSize, false, 0.10);
            controller.HandleEdgeScrolling(LeftEdgeMouse, GameGridRect, GameAreaSize, false, 0.10);

            Assert.AreNotEqual(Vector2.Zero, controller.ScrollOffset,
                "Edge scrolling should move once hover delay is reached.");
        }

        [Test]
        public void ArrowKeyScroll_Should_Temporarily_Override_MouseEdgeScroll()
        {
            var controller = new ViewportController(50, 30);

            // Establish active edge scrolling first.
            controller.HandleEdgeScrolling(LeftEdgeMouse, GameGridRect, GameAreaSize, false, 0.10);
            controller.HandleEdgeScrolling(LeftEdgeMouse, GameGridRect, GameAreaSize, false, 0.10);
            controller.HandleEdgeScrolling(LeftEdgeMouse, GameGridRect, GameAreaSize, false, 0.10);

            var rightArrow = new InputEventKey { Keycode = Key.Right, Pressed = true };
            controller.HandleKeyboardInput(rightArrow, GameAreaSize);
            var afterArrow = controller.ScrollOffset;

            // During override window, edge scroll should be suppressed.
            controller.HandleEdgeScrolling(LeftEdgeMouse, GameGridRect, GameAreaSize, false, 0.20);

            Assert.AreEqual(afterArrow, controller.ScrollOffset,
                "Arrow key input should suppress mouse edge scrolling during override window.");

            // Expire override and verify edge scrolling resumes.
            controller.Update(0.36);
            controller.HandleEdgeScrolling(LeftEdgeMouse, GameGridRect, GameAreaSize, false, 0.10);
            controller.HandleEdgeScrolling(LeftEdgeMouse, GameGridRect, GameAreaSize, false, 0.10);
            controller.HandleEdgeScrolling(LeftEdgeMouse, GameGridRect, GameAreaSize, false, 0.10);

            Assert.AreNotEqual(afterArrow, controller.ScrollOffset,
                "Edge scrolling should resume after the keyboard override window expires.");
        }
    }
}
