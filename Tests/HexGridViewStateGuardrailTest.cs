using NUnit.Framework;
using System.Reflection;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class HexGridViewStateGuardrailTest
    {
        [Test]
        public void HexGridCalculator_Should_Not_Expose_Global_ViewState_Bridge()
        {
            var calculatorType = typeof(HexGridCalculator);

            Assert.IsNull(calculatorType.GetMethod("SetGlobalViewState", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNull(calculatorType.GetMethod("GetGlobalViewState", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNull(calculatorType.GetProperty("ZoomFactor", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNull(calculatorType.GetProperty("ScrollOffset", BindingFlags.Public | BindingFlags.Static));
        }
    }
}
