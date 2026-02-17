using Godot;
using NUnit.Framework;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class ViewportControllerRefactoringTest
    {
        private static readonly Vector2 LargeGameArea = new Vector2(600, 400);

        [Test]
        public void HandleKeyboardInput_Should_Zoom_In_With_Plus_Key()
        {
            var viewState = new HexGridViewState { ZoomFactor = 1.0f, ScrollOffset = Vector2.Zero };
            var controller = new ViewportController(50, 30, null, viewState);
            var plus = new InputEventKey { Keycode = Key.Plus, Pressed = true };
            var zoomBefore = viewState.ZoomFactor;

            var handled = controller.HandleKeyboardInput(plus, LargeGameArea);

            Assert.IsTrue(handled, "Plus key should be handled.");
            Assert.Greater(viewState.ZoomFactor, zoomBefore, "Plus key should increase zoom.");
        }

        [Test]
        public void HandleKeyboardInput_Should_Zoom_Out_With_Minus_Key()
        {
            var viewState = new HexGridViewState { ZoomFactor = 1.4f, ScrollOffset = Vector2.Zero };
            var controller = new ViewportController(50, 30, null, viewState);
            var minus = new InputEventKey { Keycode = Key.Minus, Pressed = true };
            var zoomBefore = viewState.ZoomFactor;

            var handled = controller.HandleKeyboardInput(minus, LargeGameArea);

            Assert.IsTrue(handled, "Minus key should be handled.");
            Assert.Less(viewState.ZoomFactor, zoomBefore, "Minus key should decrease zoom.");
        }

        [Test]
        public void HandleKeyboardInput_Should_Reset_Zoom_With_Zero_Key()
        {
            var viewState = new HexGridViewState { ZoomFactor = 1.7f, ScrollOffset = Vector2.Zero };
            var controller = new ViewportController(50, 30, null, viewState);
            var reset = new InputEventKey { Keycode = Key.Key0, Pressed = true };

            var handled = controller.HandleKeyboardInput(reset, LargeGameArea);

            Assert.IsTrue(handled, "0 key should be handled.");
            Assert.AreEqual(1.0f, viewState.ZoomFactor, "0 key should reset zoom to 1.0.");
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
            var viewState = new HexGridViewState { ZoomFactor = 1.0f, ScrollOffset = Vector2.Zero };
            var controller = new ViewportController(50, 30, null, viewState);
            var unrelated = new InputEventKey { Keycode = Key.Space, Pressed = true };
            var beforeZoom = viewState.ZoomFactor;
            var beforeScroll = controller.ScrollOffset;

            var handled = controller.HandleKeyboardInput(unrelated, LargeGameArea);

            Assert.IsFalse(handled, "Unrelated key should not be handled by viewport controller.");
            Assert.AreEqual(beforeZoom, viewState.ZoomFactor, "Unrelated key should not change zoom.");
            Assert.AreEqual(beforeScroll, controller.ScrollOffset, "Unrelated key should not change scroll.");
        }

        [Test]
        public void HandleKeyboardInput_Should_NotMutate_Global_ViewState_When_Injected_State_Is_Used()
        {
            var viewState = new HexGridViewState { ZoomFactor = 1.0f, ScrollOffset = Vector2.Zero };
            HexGridCalculator.SetZoom(1.0f);
            var controller = new ViewportController(50, 30, null, viewState);
            var plus = new InputEventKey { Keycode = Key.Plus, Pressed = true };

            controller.HandleKeyboardInput(plus, LargeGameArea);

            Assert.Greater(viewState.ZoomFactor, 1.0f, "Injected state should be updated.");
            Assert.AreEqual(1.0f, HexGridCalculator.ZoomFactor, "Global view state should remain unchanged.");
        }
    }
}
