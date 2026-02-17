using Godot;
using NUnit.Framework;
using System.Reflection;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class DuplicateCodeRefactoringTest
    {
        [Test]
        public void ViewportInputHandlers_Should_Share_Common_Signature()
        {
            var mainType = typeof(Main);
            var viewportInput = mainType.GetMethod("HandleViewportInput", BindingFlags.NonPublic | BindingFlags.Instance);
            var zoomInput = mainType.GetMethod("HandleZoomInput", BindingFlags.NonPublic | BindingFlags.Instance);
            var scrollInput = mainType.GetMethod("HandleScrollInput", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(viewportInput, "Main should have a common HandleViewportInput method.");
            Assert.IsNotNull(zoomInput, "Main should keep a HandleZoomInput wrapper.");
            Assert.IsNotNull(scrollInput, "Main should keep a HandleScrollInput wrapper.");

            Assert.AreEqual(typeof(bool), viewportInput.ReturnType);
            Assert.AreEqual(typeof(bool), zoomInput.ReturnType);
            Assert.AreEqual(typeof(bool), scrollInput.ReturnType);

            Assert.AreEqual(typeof(InputEventKey), viewportInput.GetParameters()[0].ParameterType);
            Assert.AreEqual(typeof(InputEventKey), zoomInput.GetParameters()[0].ParameterType);
            Assert.AreEqual(typeof(InputEventKey), scrollInput.GetParameters()[0].ParameterType);
        }

        [Test]
        public void ZoomAndScrollWrappers_Should_Behave_Consistently_WhenViewportUnavailable()
        {
            var main = new Main();
            var mainType = typeof(Main);
            var zoomInput = mainType.GetMethod("HandleZoomInput", BindingFlags.NonPublic | BindingFlags.Instance);
            var scrollInput = mainType.GetMethod("HandleScrollInput", BindingFlags.NonPublic | BindingFlags.Instance);
            var key = new InputEventKey { Keycode = Key.Space, Pressed = true };

            bool zoomHandled = (bool)zoomInput.Invoke(main, new object[] { key });
            bool scrollHandled = (bool)scrollInput.Invoke(main, new object[] { key });

            Assert.AreEqual(zoomHandled, scrollHandled, "Zoom and scroll wrappers should delegate consistently.");
            Assert.IsFalse(zoomHandled, "Without a viewport/controller, wrappers should safely return false.");
        }
    }
}
