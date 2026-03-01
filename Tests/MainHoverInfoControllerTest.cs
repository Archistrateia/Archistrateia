using Godot;
using NUnit.Framework;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class MainHoverInfoControllerTest
    {
        [Test]
        public void HandleHoverInfoModeInput_Should_Ignore_NonI_Keys()
        {
            int handledCalls = 0;
            var controller = new MainHoverInfoController(
                () => null,
                () => handledCalls++);

            bool handled = controller.HandleHoverInfoModeInput(new InputEventKey { Keycode = Key.Space, Pressed = true });

            Assert.IsFalse(handled);
            Assert.AreEqual(0, handledCalls);
        }

        [Test]
        public void HandleHoverInfoModeInput_Should_Handle_I_Key_And_Mark_Input()
        {
            int handledCalls = 0;
            var controller = new MainHoverInfoController(
                () => null,
                () => handledCalls++);

            bool handled = controller.HandleHoverInfoModeInput(new InputEventKey { Keycode = Key.I, Pressed = true });

            Assert.IsTrue(handled);
            Assert.AreEqual(1, handledCalls);
        }
    }
}
