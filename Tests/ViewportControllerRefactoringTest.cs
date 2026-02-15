using Godot;
using NUnit.Framework;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class ViewportControllerRefactoringTest
    {
        private static readonly Vector2 LargeGameArea = new Vector2(600, 400);

        [SetUp]
        public void SetUp()
        {
            HexGridCalculator.SetZoom(1.0f);
            HexGridCalculator.SetScrollOffset(Vector2.Zero);
        }

        [Test]
        public void HandleKeyboardInput_Should_Zoom_In_With_Plus_Key()
        {
            var controller = new ViewportController(50, 30);
            var plus = new InputEventKey { Keycode = Key.Plus, Pressed = true };
            var zoomBefore = HexGridCalculator.ZoomFactor;

            var handled = controller.HandleKeyboardInput(plus, LargeGameArea);

            Assert.IsTrue(handled, "Plus key should be handled.");
            Assert.Greater(HexGridCalculator.ZoomFactor, zoomBefore, "Plus key should increase zoom.");
        }

        [Test]
        public void HandleKeyboardInput_Should_Zoom_Out_With_Minus_Key()
        {
            var controller = new ViewportController(50, 30);
            HexGridCalculator.SetZoom(1.4f);
            var minus = new InputEventKey { Keycode = Key.Minus, Pressed = true };
            var zoomBefore = HexGridCalculator.ZoomFactor;

            var handled = controller.HandleKeyboardInput(minus, LargeGameArea);

            Assert.IsTrue(handled, "Minus key should be handled.");
            Assert.Less(HexGridCalculator.ZoomFactor, zoomBefore, "Minus key should decrease zoom.");
        }

        [Test]
        public void HandleKeyboardInput_Should_Reset_Zoom_With_Zero_Key()
        {
            var controller = new ViewportController(50, 30);
            HexGridCalculator.SetZoom(1.7f);
            var reset = new InputEventKey { Keycode = Key.Key0, Pressed = true };

            var handled = controller.HandleKeyboardInput(reset, LargeGameArea);

            Assert.IsTrue(handled, "0 key should be handled.");
            Assert.AreEqual(1.0f, HexGridCalculator.ZoomFactor, "0 key should reset zoom to 1.0.");
        }

        [Test]
        public void HandleKeyboardInput_Should_Move_Scroll_With_Arrow_Key_When_Scrolling_Is_Needed()
        {
            var controller = new ViewportController(50, 30);
            var right = new InputEventKey { Keycode = Key.Right, Pressed = true };

            var handled = controller.HandleKeyboardInput(right, LargeGameArea);

            Assert.IsTrue(handled, "Arrow key should be handled when scrolling is needed.");
            Assert.Greater(controller.ScrollOffset.X, 0.0f, "Right arrow should increase X scroll offset.");
        }

        [Test]
        public void HandleKeyboardInput_Should_Move_Scroll_With_WASD_When_Scrolling_Is_Needed()
        {
            var controller = new ViewportController(50, 30);
            var up = new InputEventKey { Keycode = Key.W, Pressed = true };

            var handled = controller.HandleKeyboardInput(up, LargeGameArea);

            Assert.IsTrue(handled, "W key should be handled when scrolling is needed.");
            Assert.Less(controller.ScrollOffset.Y, 0.0f, "W key should move scroll upward.");
        }

        [Test]
        public void HandleKeyboardInput_Should_Reset_Scroll_With_Home_Key()
        {
            var controller = new ViewportController(50, 30);
            controller.ApplyScrollDelta(new Vector2(100, 75), LargeGameArea);
            Assert.AreNotEqual(Vector2.Zero, controller.ScrollOffset, "Precondition: scroll should be non-zero.");

            var home = new InputEventKey { Keycode = Key.Home, Pressed = true };
            var handled = controller.HandleKeyboardInput(home, LargeGameArea);

            Assert.IsTrue(handled, "Home key should be handled.");
            Assert.AreEqual(Vector2.Zero, controller.ScrollOffset, "Home key should reset scroll to origin.");
        }

        [Test]
        public void HandleKeyboardInput_Should_Return_False_For_Unrelated_Key()
        {
            var controller = new ViewportController(50, 30);
            var unrelated = new InputEventKey { Keycode = Key.Space, Pressed = true };
            var beforeZoom = HexGridCalculator.ZoomFactor;
            var beforeScroll = controller.ScrollOffset;

            var handled = controller.HandleKeyboardInput(unrelated, LargeGameArea);

            Assert.IsFalse(handled, "Unrelated key should not be handled by viewport controller.");
            Assert.AreEqual(beforeZoom, HexGridCalculator.ZoomFactor, "Unrelated key should not change zoom.");
            Assert.AreEqual(beforeScroll, controller.ScrollOffset, "Unrelated key should not change scroll.");
        }
    }
}
