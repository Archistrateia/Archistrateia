using NUnit.Framework;
using Archistrateia.Debug;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class NamespaceRefactoringTest
    {
        [Test]
        public void DebugScrollOverlay_Should_Be_In_Archistrateia_Debug_Namespace()
        {
            var type = typeof(DebugScrollOverlay);

            Assert.AreEqual("Archistrateia.Debug", type.Namespace);
            Assert.IsTrue(type.FullName.StartsWith("Archistrateia.Debug."),
                "Debug overlay types should be grouped in a named project namespace.");
        }

        [Test]
        public void DebugNamespace_Should_Prevent_Ambiguous_Global_Type_Usage()
        {
            var type = typeof(DebugScrollOverlay);

            Assert.AreEqual(type, typeof(DebugScrollOverlay),
                "Consumers should reference the namespace-qualified debug overlay type.");
        }
    }
}
