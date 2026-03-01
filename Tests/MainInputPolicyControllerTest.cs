using Godot;
using NUnit.Framework;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class MainInputPolicyControllerTest
    {
        [Test]
        public void HandlePhaseInput_Should_Advance_On_Space()
        {
            int advanceCalls = 0;
            var controller = new MainInputPolicyController(
                _ => false,
                () => advanceCalls++);

            bool handled = controller.HandlePhaseInput(new InputEventKey { Keycode = Key.Space, Pressed = true });

            Assert.IsTrue(handled);
            Assert.AreEqual(1, advanceCalls);
        }

        [Test]
        public void HandleViewportInput_Should_Delegate_To_Injected_Handler()
        {
            InputEventKey captured = null;
            var controller = new MainInputPolicyController(
                key =>
                {
                    captured = key;
                    return true;
                },
                () => { });
            var keyEvent = new InputEventKey { Keycode = Key.Up, Pressed = true };

            bool handled = controller.HandleViewportInput(keyEvent);

            Assert.IsTrue(handled);
            Assert.AreSame(keyEvent, captured);
        }
    }
}
