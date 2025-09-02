using Godot;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Archistrateia;

[TestFixture]
public partial class MapGeneratorTest : Node
{
    private const int TEST_WIDTH = 10;
    private const int TEST_HEIGHT = 8;
    private const int SEED = 12345;
    
    [Test]
    public void Should_Generate_Map_With_Correct_Dimensions()
    {
        var gameMap = MapGenerator.GenerateMap(TEST_WIDTH, TEST_HEIGHT, MapType.Continental, SEED);
        
        Assert.AreEqual(TEST_WIDTH * TEST_HEIGHT, gameMap.Count);
        
        for (int x = 0; x < TEST_WIDTH; x++)
        {
            for (int y = 0; y < TEST_HEIGHT; y++)
            {
                var position = new Vector2I(x, y);
                Assert.IsTrue(gameMap.ContainsKey(position), $"Missing tile at position {position}");
                Assert.AreEqual(position, gameMap[position].Position);
            }
        }
    }
    
    [Test]
    public void Should_Generate_Deterministic_Maps_With_Same_Seed()
    {
        var map1 = MapGenerator.GenerateMap(TEST_WIDTH, TEST_HEIGHT, MapType.Continental, SEED);
        var map2 = MapGenerator.GenerateMap(TEST_WIDTH, TEST_HEIGHT, MapType.Continental, SEED);
        
        Assert.AreEqual(map1.Count, map2.Count);
        
        // Check that the overall terrain distribution is the same (deterministic)
        var distribution1 = GetTerrainDistribution(map1);
        var distribution2 = GetTerrainDistribution(map2);
        
        foreach (var terrainType in System.Enum.GetValues<TerrainType>())
        {
            var count1 = distribution1.GetValueOrDefault(terrainType, 0);
            var count2 = distribution2.GetValueOrDefault(terrainType, 0);
            Assert.AreEqual(count1, count2, $"Terrain count mismatch for {terrainType}: {count1} vs {count2}");
        }
    }
    
    [Test]
    public void Should_Generate_Different_Maps_With_Different_Seeds()
    {
        var map1 = MapGenerator.GenerateMap(TEST_WIDTH, TEST_HEIGHT, MapType.Continental, SEED);
        var map2 = MapGenerator.GenerateMap(TEST_WIDTH, TEST_HEIGHT, MapType.Continental, SEED + 1);
        
        int differentTiles = 0;
        foreach (var kvp in map1)
        {
            var position = kvp.Key;
            if (map1[position].TerrainType != map2[position].TerrainType)
            {
                differentTiles++;
            }
        }
        
        Assert.That(differentTiles, Is.GreaterThan(0), "Maps with different seeds should have some different terrain");
    }
    
    [Test]
    public void Should_Respect_Terrain_Adjacency_Rules()
    {
        var gameMap = MapGenerator.GenerateMap(TEST_WIDTH, TEST_HEIGHT, MapType.Continental, SEED);
        var violations = new List<string>();
        
        var adjacencyRules = new Dictionary<TerrainType, TerrainType[]>
        {
            { TerrainType.Water, new[] { TerrainType.Lagoon, TerrainType.Shoreline } },
            { TerrainType.Mountain, new[] { TerrainType.Hill } }
        };
        
        foreach (var kvp in gameMap)
        {
            var position = kvp.Key;
            var tile = kvp.Value;
            
            if (adjacencyRules.ContainsKey(tile.TerrainType))
            {
                var neighbors = GetNeighbors(position, gameMap);
                var validNeighbors = adjacencyRules[tile.TerrainType];
                
                if (neighbors.Count > 0 && !neighbors.Any(n => validNeighbors.Contains(n.TerrainType)))
                {
                    violations.Add($"{tile.TerrainType} at {position} has no valid adjacent terrain");
                }
            }
        }
        
        if (violations.Count > 0)
        {
            Assert.Fail($"Adjacency rule violations:\n{string.Join("\n", violations)}");
        }
    }
    
    [Test]
    public void Archipelago_Should_Have_More_Water_Than_Continental()
    {
        var continentalMap = MapGenerator.GenerateMap(TEST_WIDTH, TEST_HEIGHT, MapType.Continental, SEED);
        var archipelagoMap = MapGenerator.GenerateMap(TEST_WIDTH, TEST_HEIGHT, MapType.Archipelago, SEED);
        
        var continentalWater = CountTerrainType(continentalMap, TerrainType.Water, TerrainType.Lagoon);
        var archipelagoWater = CountTerrainType(archipelagoMap, TerrainType.Water, TerrainType.Lagoon);
        
        Assert.That(archipelagoWater, Is.GreaterThan(continentalWater), 
            $"Archipelago should have more water tiles: {archipelagoWater} vs {continentalWater}");
    }
    
    [Test]
    public void Highland_Should_Have_More_Mountains_Than_Continental()
    {
        var continentalMap = MapGenerator.GenerateMap(TEST_WIDTH, TEST_HEIGHT, MapType.Continental, SEED);
        var highlangMap = MapGenerator.GenerateMap(TEST_WIDTH, TEST_HEIGHT, MapType.Highland, SEED);
        
        var continentalMountains = CountTerrainType(continentalMap, TerrainType.Mountain, TerrainType.Hill);
        var highlandMountains = CountTerrainType(highlangMap, TerrainType.Mountain, TerrainType.Hill);
        
        Assert.That(highlandMountains, Is.GreaterThanOrEqualTo(continentalMountains), 
            $"Highland should have at least as many mountain tiles: {highlandMountains} vs {continentalMountains}");
    }
    
    [Test]
    public void Desert_Should_Have_More_Desert_Terrain_Than_Continental()
    {
        var continentalMap = MapGenerator.GenerateMap(TEST_WIDTH, TEST_HEIGHT, MapType.Continental, SEED);
        var desertMap = MapGenerator.GenerateMap(TEST_WIDTH, TEST_HEIGHT, MapType.Desert, SEED);
        
        var continentalDesert = CountTerrainType(continentalMap, TerrainType.Desert);
        var desertDesert = CountTerrainType(desertMap, TerrainType.Desert);
        
        Assert.That(desertDesert, Is.GreaterThan(continentalDesert), 
            $"Desert map should have more desert tiles: {desertDesert} vs {continentalDesert}");
    }
    
    [Test]
    public void Wetlands_Should_Have_More_Rivers_Than_Continental()
    {
        var continentalMap = MapGenerator.GenerateMap(TEST_WIDTH, TEST_HEIGHT, MapType.Continental, SEED);
        var wetlandsMap = MapGenerator.GenerateMap(TEST_WIDTH, TEST_HEIGHT, MapType.Wetlands, SEED);
        
        var continentalRivers = CountTerrainType(continentalMap, TerrainType.River);
        var wetlandsRivers = CountTerrainType(wetlandsMap, TerrainType.River);
        
        Assert.That(wetlandsRivers, Is.GreaterThanOrEqualTo(continentalRivers), 
            $"Wetlands should have at least as many rivers: {wetlandsRivers} vs {continentalRivers}");
    }
    
    [Test]
    public void Should_Generate_All_Basic_Terrain_Types_In_Large_Map()
    {
        var largeMap = MapGenerator.GenerateMap(20, 15, MapType.Continental, SEED);
        var presentTerrains = largeMap.Values.Select(t => t.TerrainType).Distinct().ToList();
        
        var expectedTerrains = new[] { TerrainType.Desert, TerrainType.Grassland, TerrainType.Hill };
        
        foreach (var expectedTerrain in expectedTerrains)
        {
            Assert.Contains(expectedTerrain, presentTerrains, 
                $"Large continental map should contain {expectedTerrain}");
        }
    }
    
    [Test]
    public void Should_Have_Valid_Terrain_Properties()
    {
        var gameMap = MapGenerator.GenerateMap(TEST_WIDTH, TEST_HEIGHT, MapType.Continental, SEED);
        
        foreach (var tile in gameMap.Values)
        {
            Assert.That(tile.MovementCost, Is.GreaterThan(0), 
                $"Invalid movement cost for {tile.TerrainType}: {tile.MovementCost}");
            
            Assert.That(tile.DefenseBonus, Is.GreaterThanOrEqualTo(0), 
                $"Invalid defense bonus for {tile.TerrainType}: {tile.DefenseBonus}");
        }
    }
    
    [Test]
    public void Should_Handle_Small_Map_Sizes()
    {
        var smallMap = MapGenerator.GenerateMap(3, 3, MapType.Continental, SEED);
        
        Assert.AreEqual(9, smallMap.Count);
        Assert.IsTrue(smallMap.Values.All(t => t != null));
    }
    
    [Test]
    public void Should_Handle_Large_Map_Sizes()
    {
        var largeMap = MapGenerator.GenerateMap(50, 30, MapType.Continental, SEED);
        
        Assert.AreEqual(1500, largeMap.Count);
        Assert.IsTrue(largeMap.Values.All(t => t != null));
    }
    
    private List<HexTile> GetNeighbors(Vector2I position, Dictionary<Vector2I, HexTile> gameMap)
    {
        var neighbors = new List<HexTile>();
        var offsets = position.Y % 2 == 0 
            ? new[] { new Vector2I(-1, -1), new Vector2I(0, -1), new Vector2I(1, 0), 
                     new Vector2I(0, 1), new Vector2I(-1, 1), new Vector2I(-1, 0) }
            : new[] { new Vector2I(0, -1), new Vector2I(1, -1), new Vector2I(1, 0), 
                     new Vector2I(1, 1), new Vector2I(0, 1), new Vector2I(-1, 0) };
        
        foreach (var offset in offsets)
        {
            var neighborPos = position + offset;
            if (gameMap.ContainsKey(neighborPos))
            {
                neighbors.Add(gameMap[neighborPos]);
            }
        }
        
        return neighbors;
    }
    
    private int CountTerrainType(Dictionary<Vector2I, HexTile> gameMap, params TerrainType[] terrainTypes)
    {
        return gameMap.Values.Count(tile => terrainTypes.Contains(tile.TerrainType));
    }
    
    private Dictionary<TerrainType, int> GetTerrainDistribution(Dictionary<Vector2I, HexTile> gameMap)
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
        
        return distribution;
    }
}
