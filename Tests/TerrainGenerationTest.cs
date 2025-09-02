using Godot;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
public partial class TerrainGenerationTest : Node
{
    [Test]
    public void Should_Have_All_New_Terrain_Types_In_Enum()
    {
        var allTerrains = System.Enum.GetValues<TerrainType>();
        var expectedTerrains = new[] 
        { 
            TerrainType.Desert, TerrainType.Hill, TerrainType.River, 
            TerrainType.Shoreline, TerrainType.Lagoon, TerrainType.Grassland, 
            TerrainType.Mountain, TerrainType.Water 
        };
        
        foreach (var expectedTerrain in expectedTerrains)
        {
            Assert.Contains(expectedTerrain, allTerrains, $"Missing terrain type: {expectedTerrain}");
        }
    }
    
    [Test]
    public void Should_Have_Valid_Movement_Costs_For_All_Terrain_Types()
    {
        var allTerrains = System.Enum.GetValues<TerrainType>();
        
        foreach (var terrainType in allTerrains)
        {
            var tile = new HexTile(Vector2I.Zero, terrainType);
            
            Assert.That(tile.MovementCost, Is.GreaterThan(0).And.LessThanOrEqualTo(10), 
                $"Invalid movement cost for {terrainType}: {tile.MovementCost}");
        }
    }
    
    [Test]
    public void Should_Have_Valid_Defense_Bonuses_For_All_Terrain_Types()
    {
        var allTerrains = System.Enum.GetValues<TerrainType>();
        
        foreach (var terrainType in allTerrains)
        {
            var tile = new HexTile(Vector2I.Zero, terrainType);
            
            Assert.That(tile.DefenseBonus, Is.GreaterThanOrEqualTo(0).And.LessThanOrEqualTo(5), 
                $"Invalid defense bonus for {terrainType}: {tile.DefenseBonus}");
        }
    }
    
    [Test]
    public void Mountain_Should_Have_Highest_Defense_Bonus()
    {
        var mountainTile = new HexTile(Vector2I.Zero, TerrainType.Mountain);
        var allTerrains = System.Enum.GetValues<TerrainType>();
        
        foreach (var terrainType in allTerrains)
        {
            if (terrainType != TerrainType.Mountain)
            {
                var tile = new HexTile(Vector2I.Zero, terrainType);
                Assert.That(mountainTile.DefenseBonus, Is.GreaterThanOrEqualTo(tile.DefenseBonus), 
                    $"Mountain should have highest defense bonus, but {terrainType} has {tile.DefenseBonus} vs {mountainTile.DefenseBonus}");
            }
        }
    }
    
    [Test]
    public void Water_Should_Have_Highest_Movement_Cost()
    {
        var waterTile = new HexTile(Vector2I.Zero, TerrainType.Water);
        var allTerrains = System.Enum.GetValues<TerrainType>();
        
        foreach (var terrainType in allTerrains)
        {
            if (terrainType != TerrainType.Water)
            {
                var tile = new HexTile(Vector2I.Zero, terrainType);
                Assert.That(waterTile.MovementCost, Is.GreaterThanOrEqualTo(tile.MovementCost), 
                    $"Water should have highest movement cost, but {terrainType} has {tile.MovementCost} vs {waterTile.MovementCost}");
            }
        }
    }
    
    [Test]
    public void Grassland_Should_Have_Low_Movement_Cost()
    {
        var grasslandTile = new HexTile(Vector2I.Zero, TerrainType.Grassland);
        var shorelineTile = new HexTile(Vector2I.Zero, TerrainType.Shoreline);
        
        Assert.That(grasslandTile.MovementCost, Is.LessThanOrEqualTo(shorelineTile.MovementCost), 
            "Grassland should have low movement cost for easy traversal");
    }
    
    [Test]
    public void Should_Generate_Realistic_Terrain_Distribution_For_Each_Map_Type()
    {
        var testCases = new[]
        {
            new { MapType = MapType.Continental, ExpectedDominant = new[] { TerrainType.Desert, TerrainType.Grassland } },
            new { MapType = MapType.Desert, ExpectedDominant = new[] { TerrainType.Desert, TerrainType.Shoreline } },
            new { MapType = MapType.Highland, ExpectedDominant = new[] { TerrainType.Hill, TerrainType.Mountain, TerrainType.Desert } },
            new { MapType = MapType.Wetlands, ExpectedDominant = new[] { TerrainType.River, TerrainType.Grassland } }
        };
        
        foreach (var testCase in testCases)
        {
            var gameMap = MapGenerator.GenerateMap(15, 10, testCase.MapType, 12345);
            var distribution = GetTerrainDistribution(gameMap);
            
            var dominantTerrain = distribution.OrderByDescending(kvp => kvp.Value).First().Key;
            Assert.Contains(dominantTerrain, testCase.ExpectedDominant, 
                $"{testCase.MapType} should have {string.Join(" or ", testCase.ExpectedDominant)} as dominant terrain, but got {dominantTerrain}");
        }
    }
    
    [Test]
    public void Should_Have_Reasonable_Terrain_Variety()
    {
        var gameMap = MapGenerator.GenerateMap(20, 15, MapType.Continental, 12345);
        var uniqueTerrains = gameMap.Values.Select(t => t.TerrainType).Distinct().Count();
        
        Assert.That(uniqueTerrains, Is.GreaterThanOrEqualTo(3), 
            $"Continental map should have at least 3 different terrain types, got {uniqueTerrains}");
    }
    
    [Test]
    public void Archipelago_Should_Have_Island_Patterns()
    {
        var gameMap = MapGenerator.GenerateMap(20, 15, MapType.Archipelago, 12345);
        var waterTiles = gameMap.Values.Where(t => t.TerrainType == TerrainType.Water || t.TerrainType == TerrainType.Lagoon).Count();
        var totalTiles = gameMap.Count;
        var waterPercentage = (float)waterTiles / totalTiles;
        
        Assert.That(waterPercentage, Is.GreaterThan(0.25f), 
            $"Archipelago should have significant water coverage, got {waterPercentage:P}");
    }
    
    [Test]
    public void Desert_Should_Have_Minimal_Water_Features()
    {
        var gameMap = MapGenerator.GenerateMap(20, 15, MapType.Desert, 12345);
        var waterTiles = gameMap.Values.Where(t => 
            t.TerrainType == TerrainType.Water || 
            t.TerrainType == TerrainType.Lagoon || 
            t.TerrainType == TerrainType.River).Count();
        var totalTiles = gameMap.Count;
        var waterPercentage = (float)waterTiles / totalTiles;
        
        Assert.That(waterPercentage, Is.LessThan(0.3f), 
            $"Desert should have minimal water features, got {waterPercentage:P}");
    }
    
    [Test]
    public void Should_Maintain_Terrain_Consistency_Across_Regenerations()
    {
        const int numTests = 5;
        var distributions = new List<Dictionary<TerrainType, int>>();
        
        for (int i = 0; i < numTests; i++)
        {
            var gameMap = MapGenerator.GenerateMap(15, 10, MapType.Continental, 12345 + i);
            distributions.Add(GetTerrainDistribution(gameMap));
        }
        
        // Check that the most common terrain type is consistent
        var dominantTerrains = distributions.Select(d => d.OrderByDescending(kvp => kvp.Value).First().Key).ToList();
        var uniqueDominant = dominantTerrains.Distinct().Count();
        
        Assert.That(uniqueDominant, Is.LessThanOrEqualTo(2), 
            "Dominant terrain should be relatively consistent across different seeds");
    }
    
    [Test]
    public void Should_Generate_Connected_Land_Masses()
    {
        var gameMap = MapGenerator.GenerateMap(15, 10, MapType.Continental, 12345);
        var landTiles = gameMap.Values.Where(t => 
            t.TerrainType != TerrainType.Water && 
            t.TerrainType != TerrainType.Lagoon).ToList();
        
        // Simple connectivity check: ensure we don't have too many isolated single tiles
        var isolatedTiles = 0;
        foreach (var tile in landTiles)
        {
            var neighbors = GetLandNeighbors(tile.Position, gameMap);
            if (neighbors.Count == 0)
            {
                isolatedTiles++;
            }
        }
        
        var isolationRate = (float)isolatedTiles / landTiles.Count;
        Assert.That(isolationRate, Is.LessThan(0.1f), 
            $"Too many isolated land tiles: {isolationRate:P}");
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
    
    private List<HexTile> GetLandNeighbors(Vector2I position, Dictionary<Vector2I, HexTile> gameMap)
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
                var neighbor = gameMap[neighborPos];
                if (neighbor.TerrainType != TerrainType.Water && neighbor.TerrainType != TerrainType.Lagoon)
                {
                    neighbors.Add(neighbor);
                }
            }
        }
        
        return neighbors;
    }
}
