using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Archistrateia;

public static class MapGenerator
{
    private const int BASE_SEA_LEVEL = 20;
    private const int SMOOTH_PASSES = 2;
    
    private static readonly Dictionary<TerrainType, int[]> ElevationRanges = new()
    {
        { TerrainType.Water, new[] { 0, 15 } },
        { TerrainType.Lagoon, new[] { 16, 20 } },
        { TerrainType.Shoreline, new[] { 21, 35 } },
        { TerrainType.River, new[] { 21, 40 } },
        { TerrainType.Desert, new[] { 41, 70 } },
        { TerrainType.Grassland, new[] { 36, 70 } },
        { TerrainType.Hill, new[] { 71, 90 } },
        { TerrainType.Mountain, new[] { 91, 100 } }
    };
    
    public static Dictionary<Vector2I, HexTile> GenerateMap(int width, int height, MapType mapType = MapType.Continental, int seed = -1)
    {
        if (seed == -1)
        {
            seed = GD.RandRange(0, int.MaxValue);
        }
        
        // Ensure deterministic generation by setting the global random seed
        GD.Seed((ulong)seed);
        
        var config = MapTypeConfiguration.GetConfig(mapType);
        GD.Print($"🗺️ Generating {config.Name} map with seed: {seed}");
        
        var elevationMap = GenerateElevationMap(width, height, seed, config);
        var terrainMap = GenerateInitialTerrain(elevationMap, width, height, config, seed);
        
        FlowWater(terrainMap, elevationMap, width, height, config, seed);
        EnforceTerrainAdjacency(terrainMap, elevationMap, width, height);
        
        var gameMap = CreateGameMap(terrainMap, width, height);
        
        GD.Print($"🗺️ {config.Name} map generation complete. Created {gameMap.Count} tiles");
        LogTerrainDistribution(gameMap);
        
        return gameMap;
    }
    
    private static float[,] GenerateElevationMap(int width, int height, int seed, MapGenerationConfig config)
    {
        var noise = new FastNoiseLite();
        noise.Seed = seed;
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        noise.Frequency = config.NoiseFrequency;
        
        var elevationMap = new float[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var noiseValue = noise.GetNoise2D(x, y);
                elevationMap[x, y] = (noiseValue + 1.0f) * 50.0f * config.ElevationMultiplier;
            }
        }
        
        for (int pass = 0; pass < SMOOTH_PASSES; pass++)
        {
            elevationMap = SmoothElevationMap(elevationMap, width, height);
        }
        
        return elevationMap;
    }
    
    private static float[,] SmoothElevationMap(float[,] elevationMap, int width, int height)
    {
        var smoothedMap = new float[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var neighbors = GetNeighborPositions(x, y, width, height);
                float totalElevation = elevationMap[x, y];
                int count = 1;
                
                foreach (var neighbor in neighbors)
                {
                    totalElevation += elevationMap[neighbor.X, neighbor.Y];
                    count++;
                }
                
                smoothedMap[x, y] = totalElevation / count;
            }
        }
        
        return smoothedMap;
    }
    
    private static TerrainType[,] GenerateInitialTerrain(float[,] elevationMap, int width, int height, MapGenerationConfig config, int seed)
    {
        var terrainMap = new TerrainType[width, height];
        var random = new System.Random(seed + 1000); // Offset seed for terrain generation
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var elevation = elevationMap[x, y];
                terrainMap[x, y] = GetTerrainForElevation(elevation, config, random);
            }
        }
        
        return terrainMap;
    }
    
    private static TerrainType GetTerrainForElevation(float elevation, MapGenerationConfig config, System.Random random)
    {
        var elevationInt = Mathf.RoundToInt(elevation);
        var possibleTerrains = new List<TerrainType>();
        
        foreach (var kvp in ElevationRanges)
        {
            if (elevationInt >= kvp.Value[0] && elevationInt <= kvp.Value[1])
            {
                possibleTerrains.Add(kvp.Key);
            }
        }
        
        if (possibleTerrains.Count == 0)
        {
            return TerrainType.Desert;
        }
        
        var weightedTerrains = new List<TerrainType>();
        foreach (var terrain in possibleTerrains)
        {
            var bias = config.TerrainBias.GetValueOrDefault(terrain, 1.0f);
            var weight = Mathf.Max(1, Mathf.RoundToInt(bias * 10));
            for (int i = 0; i < weight; i++)
            {
                weightedTerrains.Add(terrain);
            }
        }
        
        if (weightedTerrains.Count == 0)
        {
            return possibleTerrains[0];
        }
        
        return weightedTerrains[random.Next(0, weightedTerrains.Count)];
    }
    
    private static List<Vector2I> GetNeighborPositions(int x, int y, int width, int height)
    {
        var neighbors = new List<Vector2I>();
        
        var hexOffsets = y % 2 == 0 
            ? new[] { new Vector2I(-1, -1), new Vector2I(0, -1), new Vector2I(1, 0), new Vector2I(0, 1), new Vector2I(-1, 1), new Vector2I(-1, 0) }
            : new[] { new Vector2I(0, -1), new Vector2I(1, -1), new Vector2I(1, 0), new Vector2I(1, 1), new Vector2I(0, 1), new Vector2I(-1, 0) };
        
        foreach (var offset in hexOffsets)
        {
            var nx = x + offset.X;
            var ny = y + offset.Y;
            
            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
            {
                neighbors.Add(new Vector2I(nx, ny));
            }
        }
        
        return neighbors;
    }
    
    private static Dictionary<Vector2I, HexTile> CreateGameMap(TerrainType[,] terrainMap, int width, int height)
    {
        var gameMap = new Dictionary<Vector2I, HexTile>();
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var position = new Vector2I(x, y);
                var terrain = terrainMap[x, y];
                var tile = new HexTile(position, terrain);
                gameMap[position] = tile;
            }
        }
        
        return gameMap;
    }
    
    private static readonly Dictionary<TerrainType, TerrainType[]> TerrainAdjacencyRules = new()
    {
        { TerrainType.Water, new[] { TerrainType.Lagoon, TerrainType.Shoreline } },
        { TerrainType.Lagoon, new[] { TerrainType.Water, TerrainType.Shoreline } },
        { TerrainType.Shoreline, new[] { TerrainType.Water, TerrainType.Lagoon, TerrainType.Desert, TerrainType.Grassland, TerrainType.River } },
        { TerrainType.River, new[] { TerrainType.Shoreline, TerrainType.Desert, TerrainType.Grassland } },
        { TerrainType.Desert, new[] { TerrainType.Shoreline, TerrainType.River, TerrainType.Grassland, TerrainType.Hill } },
        { TerrainType.Grassland, new[] { TerrainType.Shoreline, TerrainType.River, TerrainType.Desert, TerrainType.Hill } },
        { TerrainType.Hill, new[] { TerrainType.Desert, TerrainType.Grassland, TerrainType.Mountain } },
        { TerrainType.Mountain, new[] { TerrainType.Hill } }
    };
    
    private static void FlowWater(TerrainType[,] terrainMap, float[,] elevationMap, int width, int height, MapGenerationConfig config, int seed)
    {
        var seaLevel = BASE_SEA_LEVEL + config.SeaLevelAdjustment;
        var waterSources = new List<Vector2I>();
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (elevationMap[x, y] <= seaLevel && terrainMap[x, y] != TerrainType.Water)
                {
                    waterSources.Add(new Vector2I(x, y));
                }
            }
        }
        
        foreach (var source in waterSources)
        {
            FlowWaterFromSource(terrainMap, elevationMap, source, width, height, config);
        }
        
        CreateRivers(terrainMap, elevationMap, width, height, config, seed);
    }
    
    private static void FlowWaterFromSource(TerrainType[,] terrainMap, float[,] elevationMap, Vector2I source, int width, int height, MapGenerationConfig config)
    {
        var seaLevel = BASE_SEA_LEVEL + config.SeaLevelAdjustment;
        var visited = new HashSet<Vector2I>();
        var queue = new Queue<Vector2I>();
        queue.Enqueue(source);
        
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (visited.Contains(current)) continue;
            visited.Add(current);
            
            var currentElevation = elevationMap[current.X, current.Y];
            
            if (currentElevation <= seaLevel - 5)
            {
                terrainMap[current.X, current.Y] = TerrainType.Water;
            }
            else if (currentElevation <= seaLevel)
            {
                terrainMap[current.X, current.Y] = TerrainType.Lagoon;
            }
            
            var neighbors = GetNeighborPositions(current.X, current.Y, width, height);
            foreach (var neighbor in neighbors)
            {
                var flowThreshold = 2.0f * config.WaterFlowIntensity;
                if (!visited.Contains(neighbor) && elevationMap[neighbor.X, neighbor.Y] <= currentElevation + flowThreshold)
                {
                    queue.Enqueue(neighbor);
                }
            }
        }
    }
    
    private static void CreateRivers(TerrainType[,] terrainMap, float[,] elevationMap, int width, int height, MapGenerationConfig config, int seed)
    {
        var seaLevel = BASE_SEA_LEVEL + config.SeaLevelAdjustment;
        var riverChance = Mathf.RoundToInt(config.RiverGenerationRate * 10);
        var random = new System.Random(seed + 2000); // Offset seed for river generation
        
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (IsValley(elevationMap, x, y, width, height) && 
                    elevationMap[x, y] > seaLevel && 
                    elevationMap[x, y] < 60 &&
                    random.Next(0, 10) < riverChance)
                {
                    terrainMap[x, y] = TerrainType.River;
                }
            }
        }
    }
    
    private static bool IsValley(float[,] elevationMap, int x, int y, int width, int height)
    {
        var currentElevation = elevationMap[x, y];
        var neighbors = GetNeighborPositions(x, y, width, height);
        
        int higherNeighbors = 0;
        foreach (var neighbor in neighbors)
        {
            if (elevationMap[neighbor.X, neighbor.Y] > currentElevation + 5)
            {
                higherNeighbors++;
            }
        }
        
        return higherNeighbors >= 3;
    }
    
    private static void EnforceTerrainAdjacency(TerrainType[,] terrainMap, float[,] elevationMap, int width, int height)
    {
        var changes = true;
        int iterations = 0;
        const int maxIterations = 5;
        
        while (changes && iterations < maxIterations)
        {
            changes = false;
            iterations++;
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var currentTerrain = terrainMap[x, y];
                    var neighbors = GetNeighborPositions(x, y, width, height);
                    
                    var adjacentTerrains = neighbors.Select(n => terrainMap[n.X, n.Y]).ToArray();
                    
                    if (!IsValidAdjacency(currentTerrain, adjacentTerrains))
                    {
                        var newTerrain = FindBestAdjacentTerrain(currentTerrain, adjacentTerrains, elevationMap[x, y]);
                        if (newTerrain != currentTerrain)
                        {
                            terrainMap[x, y] = newTerrain;
                            changes = true;
                        }
                    }
                }
            }
        }
    }
    
    private static bool IsValidAdjacency(TerrainType terrain, TerrainType[] neighbors)
    {
        if (!TerrainAdjacencyRules.ContainsKey(terrain)) return true;
        
        var validNeighbors = TerrainAdjacencyRules[terrain];
        return neighbors.Any(neighbor => validNeighbors.Contains(neighbor));
    }
    
    private static TerrainType FindBestAdjacentTerrain(TerrainType currentTerrain, TerrainType[] neighbors, float elevation)
    {
        var possibleTerrains = GetTerrainsForElevation(elevation);
        
        foreach (var neighbor in neighbors)
        {
            if (TerrainAdjacencyRules.ContainsKey(neighbor))
            {
                var validAdjacent = TerrainAdjacencyRules[neighbor];
                var bestMatch = validAdjacent.FirstOrDefault(t => possibleTerrains.Contains(t));
                if (bestMatch != default)
                {
                    return bestMatch;
                }
            }
        }
        
        return possibleTerrains.FirstOrDefault();
    }
    
    private static List<TerrainType> GetTerrainsForElevation(float elevation)
    {
        var elevationInt = Mathf.RoundToInt(elevation);
        var validTerrains = new List<TerrainType>();
        
        foreach (var kvp in ElevationRanges)
        {
            if (elevationInt >= kvp.Value[0] && elevationInt <= kvp.Value[1])
            {
                validTerrains.Add(kvp.Key);
            }
        }
        
        return validTerrains.Count > 0 ? validTerrains : new List<TerrainType> { TerrainType.Desert };
    }
    
    private static void LogTerrainDistribution(Dictionary<Vector2I, HexTile> gameMap)
    {
        var distribution = new Dictionary<TerrainType, int>();
        
        foreach (var tile in gameMap.Values)
        {
            if (!distribution.ContainsKey(tile.TerrainType))
            {
                distribution[tile.TerrainType] = 0;
            }
            distribution[tile.TerrainType]++;
        }
        
        GD.Print("🌍 Terrain Distribution:");
        foreach (var kvp in distribution.OrderByDescending(x => x.Value))
        {
            var percentage = (kvp.Value * 100.0f) / gameMap.Count;
            GD.Print($"   {kvp.Key}: {kvp.Value} tiles ({percentage:F1}%)");
        }
    }
}