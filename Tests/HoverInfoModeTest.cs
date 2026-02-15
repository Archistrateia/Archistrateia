using NUnit.Framework;
using Godot;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class HoverInfoModeTest
    {
        [Test]
        public void HoverInfoMode_Should_Be_Disabled_By_Default()
        {
            var mapRenderer = new MapRenderer();

            Assert.IsFalse(mapRenderer.IsHoverInfoModeEnabled(),
                "Hover info mode should default to OFF.");
        }

        [Test]
        public void HoverInfoMode_Should_Toggle_On_And_Off()
        {
            var mapRenderer = new MapRenderer();

            mapRenderer.ToggleHoverInfoMode();
            Assert.IsTrue(mapRenderer.IsHoverInfoModeEnabled(),
                "First toggle should enable hover info mode.");

            mapRenderer.ToggleHoverInfoMode();
            Assert.IsFalse(mapRenderer.IsHoverInfoModeEnabled(),
                "Second toggle should disable hover info mode.");
        }

        [Test]
        public void HoverInfoMode_Should_Not_Be_Affected_By_Left_Click_Input()
        {
            var mapRenderer = new MapRenderer();
            mapRenderer.ToggleHoverInfoMode(); // Enable inspect mode

            var click = new InputEventMouseButton
            {
                Pressed = true,
                ButtonIndex = MouseButton.Left
            };

            mapRenderer._Input(click);

            Assert.IsTrue(mapRenderer.IsHoverInfoModeEnabled(),
                "Left click should not disable hover info mode.");
        }
    }
}
