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
        public void ContainsLocalPoint_Should_Exist_On_VisualHexTile()
        {
            var method = typeof(VisualHexTile).GetMethod("ContainsLocalPoint", BindingFlags.Public | BindingFlags.Instance);

            Assert.IsNotNull(method, "ContainsLocalPoint should exist as an instance helper on VisualHexTile.");
            Assert.AreEqual(typeof(bool), method.ReturnType);
            Assert.AreEqual(1, method.GetParameters().Length, "Method should only accept the point to test.");
            Assert.AreEqual(typeof(Vector2), method.GetParameters()[0].ParameterType);
        }

        [Test]
        public void ContainsLocalPoint_Should_Execute_With_Tile_Instance()
        {
            var method = typeof(VisualHexTile).GetMethod("ContainsLocalPoint", BindingFlags.Public | BindingFlags.Instance);

            var tile = new VisualHexTile();
            tile.Initialize(new Vector2I(0, 0), TerrainType.Desert, Colors.Beige, Vector2.Zero, new HexGridViewState());
            bool centerInside = (bool)method.Invoke(tile, new object[] { Vector2.Zero });
            bool farOutside = (bool)method.Invoke(tile, new object[] { new Vector2(5000, 5000) });

            Assert.IsTrue(centerInside, "Hex center should be inside.");
            Assert.IsFalse(farOutside, "Far point should be outside.");
        }
    }
}
