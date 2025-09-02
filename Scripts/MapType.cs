using Godot;
using System.Collections.Generic;

public enum MapType
{
    Continental,
    Archipelago,
    Highland,
    Desert,
    Wetlands,
    Volcanic
}

public static class MapTypeConfiguration
{
    public static readonly Dictionary<MapType, MapGenerationConfig> Configurations = new()
    {
        {
            MapType.Continental,
            new MapGenerationConfig
            {
                Name = "Continental",
                Description = "Large landmasses with varied terrain, rivers, and moderate water coverage",
                SeaLevelAdjustment = 0,
                NoiseFrequency = 0.08f,
                ElevationMultiplier = 1.0f,
                WaterFlowIntensity = 1.0f,
                RiverGenerationRate = 0.3f,
                TerrainBias = new Dictionary<TerrainType, float>
                {
                    { TerrainType.Desert, 1.0f },
                    { TerrainType.Grassland, 1.2f },
                    { TerrainType.Hill, 1.0f },
                    { TerrainType.Mountain, 0.8f },
                    { TerrainType.Water, 1.0f }
                }
            }
        },
        {
            MapType.Archipelago,
            new MapGenerationConfig
            {
                Name = "Archipelago",
                Description = "Many islands separated by water with coastal terrain focus",
                SeaLevelAdjustment = 20,
                NoiseFrequency = 0.12f,
                ElevationMultiplier = 0.7f,
                WaterFlowIntensity = 2.0f,
                RiverGenerationRate = 0.1f,
                TerrainBias = new Dictionary<TerrainType, float>
                {
                    { TerrainType.Desert, 0.5f },
                    { TerrainType.Grassland, 1.2f },
                    { TerrainType.Hill, 0.6f },
                    { TerrainType.Mountain, 0.4f },
                    { TerrainType.Water, 3.0f },
                    { TerrainType.Lagoon, 2.5f },
                    { TerrainType.Shoreline, 2.0f },
                    { TerrainType.River, 0.5f }
                }
            }
        },
        {
            MapType.Highland,
            new MapGenerationConfig
            {
                Name = "Highland",
                Description = "Mountainous terrain with high elevation and deep valleys",
                SeaLevelAdjustment = -10,
                NoiseFrequency = 0.06f,
                ElevationMultiplier = 1.4f,
                WaterFlowIntensity = 0.8f,
                RiverGenerationRate = 0.4f,
                TerrainBias = new Dictionary<TerrainType, float>
                {
                    { TerrainType.Desert, 0.7f },
                    { TerrainType.Grassland, 0.9f },
                    { TerrainType.Hill, 1.5f },
                    { TerrainType.Mountain, 2.0f },
                    { TerrainType.Water, 0.6f }
                }
            }
        },
        {
            MapType.Desert,
            new MapGenerationConfig
            {
                Name = "Desert",
                Description = "Arid landscape with minimal water and sandy terrain",
                SeaLevelAdjustment = -10,
                NoiseFrequency = 0.1f,
                ElevationMultiplier = 0.8f,
                WaterFlowIntensity = 0.2f,
                RiverGenerationRate = 0.05f,
                TerrainBias = new Dictionary<TerrainType, float>
                {
                    { TerrainType.Desert, 4.0f },
                    { TerrainType.Grassland, 0.2f },
                    { TerrainType.Hill, 1.0f },
                    { TerrainType.Mountain, 0.6f },
                    { TerrainType.Water, 0.1f },
                    { TerrainType.River, 0.2f },
                    { TerrainType.Shoreline, 0.5f },
                    { TerrainType.Lagoon, 0.1f }
                }
            }
        },
        {
            MapType.Wetlands,
            new MapGenerationConfig
            {
                Name = "Wetlands",
                Description = "Low-lying areas with rivers, marshes, and abundant water",
                SeaLevelAdjustment = 8,
                NoiseFrequency = 0.09f,
                ElevationMultiplier = 0.6f,
                WaterFlowIntensity = 1.8f,
                RiverGenerationRate = 0.6f,
                TerrainBias = new Dictionary<TerrainType, float>
                {
                    { TerrainType.Desert, 0.4f },
                    { TerrainType.Grassland, 1.6f },
                    { TerrainType.Hill, 0.6f },
                    { TerrainType.Mountain, 0.3f },
                    { TerrainType.Water, 1.5f },
                    { TerrainType.River, 2.0f },
                    { TerrainType.Lagoon, 1.4f }
                }
            }
        },
        {
            MapType.Volcanic,
            new MapGenerationConfig
            {
                Name = "Volcanic",
                Description = "Dramatic terrain with extreme elevation changes and island chains",
                SeaLevelAdjustment = 5,
                NoiseFrequency = 0.15f,
                ElevationMultiplier = 1.6f,
                WaterFlowIntensity = 1.2f,
                RiverGenerationRate = 0.2f,
                TerrainBias = new Dictionary<TerrainType, float>
                {
                    { TerrainType.Desert, 0.8f },
                    { TerrainType.Grassland, 0.7f },
                    { TerrainType.Hill, 1.3f },
                    { TerrainType.Mountain, 1.8f },
                    { TerrainType.Water, 1.3f }
                }
            }
        }
    };
    
    public static MapGenerationConfig GetConfig(MapType mapType)
    {
        return Configurations.TryGetValue(mapType, out var config) ? config : Configurations[MapType.Continental];
    }
}

public class MapGenerationConfig
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int SeaLevelAdjustment { get; set; } = 0;
    public float NoiseFrequency { get; set; } = 0.1f;
    public float ElevationMultiplier { get; set; } = 1.0f;
    public float WaterFlowIntensity { get; set; } = 1.0f;
    public float RiverGenerationRate { get; set; } = 0.3f;
    public Dictionary<TerrainType, float> TerrainBias { get; set; } = new();
}
