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

        [Test]
        public void ResolveInitialReferences_Should_Return_Existing_References_When_Provided()
        {
            var host = new Control();
            var start = new Button();
            var title = new Label();
            var controller = new MainUIBootstrapController();

            var references = controller.ResolveInitialReferences(host, start, title);

            Assert.AreSame(start, references.StartButton);
            Assert.AreSame(title, references.TitleLabel);
        }
    }
}
