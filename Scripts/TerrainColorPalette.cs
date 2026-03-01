using Godot;
using System.Collections.Generic;

namespace Archistrateia
{
    public static class TerrainColorPalette
    {
        public static readonly IReadOnlyDictionary<TerrainType, Color> Default = new Dictionary<TerrainType, Color>
        {
            { TerrainType.Desert, new Color(0.9f, 0.8f, 0.6f) },
            { TerrainType.Hill, new Color(0.6f, 0.5f, 0.3f) },
            { TerrainType.River, new Color(0.3f, 0.6f, 0.9f) },
            { TerrainType.Shoreline, new Color(0.8f, 0.7f, 0.5f) },
            { TerrainType.Lagoon, new Color(0.2f, 0.5f, 0.7f) },
            { TerrainType.Grassland, new Color(0.4f, 0.8f, 0.3f) },
            { TerrainType.Mountain, new Color(0.5f, 0.4f, 0.4f) },
            { TerrainType.Water, new Color(0.1f, 0.4f, 0.8f) }
        };
    }
}
