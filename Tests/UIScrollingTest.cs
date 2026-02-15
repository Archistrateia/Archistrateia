using Godot;
using NUnit.Framework;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class UIScrollingTest
    {
        private static readonly Vector2 LargeGameArea = new Vector2(600, 400);
        private static readonly Rect2 GameGridRect = new Rect2(0, 0, 600, 400);

        [SetUp]
        public void SetUp()
        {
            HexGridCalculator.SetZoom(1.0f);
            HexGridCalculator.SetScrollOffset(Vector2.Zero);
        }

        [Test]
        public void EdgeScroll_Should_Not_Move_When_Mouse_Is_Over_UI_Controls()
        {
            var controller = new ViewportController(50, 30);
            var leftEdgeMouse = new Vector2(10, 200);

            controller.HandleEdgeScrolling(leftEdgeMouse, GameGridRect, LargeGameArea, true, 0.20);
            controller.HandleEdgeScrolling(leftEdgeMouse, GameGridRect, LargeGameArea, true, 0.20);

            Assert.AreEqual(Vector2.Zero, controller.ScrollOffset,
                "Edge scrolling should be suppressed while over UI controls.");
        }

        [Test]
        public void EdgeScroll_Should_Move_When_Not_Over_UI_Controls()
        {
            var controller = new ViewportController(50, 30);
            var leftEdgeMouse = new Vector2(10, 200);

            controller.HandleEdgeScrolling(leftEdgeMouse, GameGridRect, LargeGameArea, false, 0.20);
            controller.HandleEdgeScrolling(leftEdgeMouse, GameGridRect, LargeGameArea, false, 0.20);

            Assert.AreNotEqual(Vector2.Zero, controller.ScrollOffset,
                "Edge scrolling should move when pointer is not over UI controls.");
        }

        [Test]
        public void EdgeScroll_Should_Not_Move_When_Mouse_Is_Outside_Game_Area()
        {
            var controller = new ViewportController(50, 30);
            var outsideMouse = new Vector2(700, 200);

            controller.HandleEdgeScrolling(outsideMouse, GameGridRect, LargeGameArea, false, 0.20);
            controller.HandleEdgeScrolling(outsideMouse, GameGridRect, LargeGameArea, false, 0.20);

            Assert.AreEqual(Vector2.Zero, controller.ScrollOffset,
                "Edge scrolling should not move when pointer is outside game area.");
        }

        [Test]
        public void PanGesture_Should_Not_Move_When_Mouse_Is_Over_UI_Controls()
        {
            var controller = new ViewportController(50, 30);
            var pan = new InputEventPanGesture
            {
                Position = new Vector2(100, 100),
                Delta = new Vector2(6, -4)
            };

            controller.HandlePanGesture(pan, LargeGameArea, _ => true);

            Assert.AreEqual(Vector2.Zero, controller.ScrollOffset,
                "Pan gesture should be suppressed while over UI controls.");
        }

        [Test]
        public void PanGesture_Should_Move_When_Mouse_Is_Not_Over_UI_Controls()
        {
            var controller = new ViewportController(50, 30);
            var pan = new InputEventPanGesture
            {
                Position = new Vector2(100, 100),
                Delta = new Vector2(6, -4)
            };

            controller.HandlePanGesture(pan, LargeGameArea, _ => false);

            Assert.AreNotEqual(Vector2.Zero, controller.ScrollOffset,
                "Pan gesture should scroll when pointer is not over UI controls.");
        }

        [Test]
        public void KeyboardScroll_Should_Not_Move_When_Map_Does_Not_Need_Scrolling()
        {
            var controller = new ViewportController(3, 2);
            var right = new InputEventKey { Keycode = Key.Right, Pressed = true };
            var handled = controller.HandleKeyboardInput(right, new Vector2(1920, 1080));

            Assert.IsFalse(handled, "Keyboard scroll should not be handled when map fits in game area.");
            Assert.AreEqual(Vector2.Zero, controller.ScrollOffset,
                "Scroll offset should remain unchanged when scrolling is not needed.");
        }
    }
}
