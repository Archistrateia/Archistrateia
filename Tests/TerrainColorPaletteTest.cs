using NUnit.Framework;
using System;
using System.Linq;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class TerrainColorPaletteTest
    {
        [Test]
        public void Default_Should_Define_Color_For_Each_TerrainType()
        {
            var missing = Enum.GetValues<TerrainType>()
                .Where(terrain => !TerrainColorPalette.Default.ContainsKey(terrain))
                .ToList();

            Assert.IsEmpty(missing, "TerrainColorPalette.Default should include every terrain type.");
        }
    }
}
