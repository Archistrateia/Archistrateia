using Godot;
using NUnit.Framework;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class MainUIBootstrapControllerTest
    {
        [Test]
        public void CreateAndAttachUI_Should_Add_UIManager_And_DebugOverlay_To_Host()
        {
            var host = new Control();
            var controller = new MainUIBootstrapController();

            var result = controller.CreateAndAttachUI(host);

            Assert.IsNotNull(result.UIManager);
            Assert.IsNotNull(result.DebugScrollOverlay);
            Assert.AreEqual(host, result.UIManager.GetParent());
            Assert.AreEqual(host, result.DebugScrollOverlay.GetParent());
        }
    }
}
