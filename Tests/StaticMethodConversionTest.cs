using Godot;
using NUnit.Framework;
using System.Reflection;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class StaticMethodConversionTest
    {
        [Test]
        public void CreateStartButton_Should_Be_Static_Utility_Method()
        {
            var method = typeof(Main).GetMethod("CreateStartButton", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(method, "CreateStartButton should exist as a static method.");
            Assert.AreEqual(typeof(void), method.ReturnType);
            Assert.AreEqual(0, method.GetParameters().Length);
        }

        [Test]
        public void IsPointInHexagon_Should_Be_Static_With_Single_Point_Parameter()
        {
            var method = typeof(Main).GetMethod("IsPointInHexagon", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(method, "IsPointInHexagon should exist as a static method.");
            Assert.AreEqual(typeof(bool), method.ReturnType);
            Assert.AreEqual(1, method.GetParameters().Length, "Method should only accept the point to test.");
            Assert.AreEqual(typeof(Vector2), method.GetParameters()[0].ParameterType);
        }

        [Test]
        public void IsPointInHexagon_Should_Execute_Without_Main_Instance()
        {
            var method = typeof(Main).GetMethod("IsPointInHexagon", BindingFlags.NonPublic | BindingFlags.Static);

            bool centerInside = (bool)method.Invoke(null, new object[] { Vector2.Zero });
            bool farOutside = (bool)method.Invoke(null, new object[] { new Vector2(5000, 5000) });

            Assert.IsTrue(centerInside, "Hex center should be inside.");
            Assert.IsFalse(farOutside, "Far point should be outside.");
        }
    }
}
