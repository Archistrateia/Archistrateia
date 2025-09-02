using Godot;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Archistrateia;

[TestFixture]
public partial class MapGenerationIntegrationTest : Node
{
    [Test]
    public void Should_Generate_All_Map_Types_Successfully()
    {
        var allMapTypes = System.Enum.GetValues<MapType>();
        
        foreach (var mapType in allMapTypes)
        {
            var gameMap = MapGenerator.GenerateMap(15, 10, mapType, 12345);
            
            Assert.IsNotNull(gameMap, $"Failed to generate {mapType} map");
            Assert.AreEqual(150, gameMap.Count, $"{mapType} map should have correct tile count");
            Assert.IsTrue(gameMap.Values.All(t => t != null), $"All tiles in {mapType} map should be valid");
            
            GD.Print($"✅ {mapType} map generated successfully with {gameMap.Count} tiles");
        }
    }
    
    [Test]
    public void Should_Create_Distinct_Terrain_Patterns_For_Each_Map_Type()
    {
        var mapComparisons = new Dictionary<MapType, Dictionary<TerrainType, float>>();
        const int testSize = 20 * 15;
        
        // Generate all map types with same seed for fair comparison
        var allMapTypes = System.Enum.GetValues<MapType>();
        foreach (var mapType in allMapTypes)
        {
            var gameMap = MapGenerator.GenerateMap(20, 15, mapType, 12345);
            var distribution = GetTerrainPercentages(gameMap);
            mapComparisons[mapType] = distribution;
            
            GD.Print($"🌍 {mapType} terrain distribution:");
            foreach (var kvp in distribution.OrderByDescending(x => x.Value))
            {
                GD.Print($"   {kvp.Key}: {kvp.Value:P1}");
            }
        }
        
        // Verify each map type has distinct characteristics
        VerifyArchipelagoHasMoreWater(mapComparisons);
        VerifyHighlandHasMoreMountains(mapComparisons);
        VerifyDesertHasMoreDesert(mapComparisons);
        VerifyWetlandsHasMoreWaterFeatures(mapComparisons);
    }
    
    [Test]
    public void Should_Maintain_Game_Compatibility_With_New_Terrain_Types()
    {
        // Test that new terrain types work with existing game systems
        var gameMap = MapGenerator.GenerateMap(10, 8, MapType.Continental, 12345);
        
        // Test HexTile creation and properties
        foreach (var tile in gameMap.Values)
        {
            Assert.IsNotNull(tile.Position);
            Assert.That(tile.MovementCost, Is.GreaterThan(0));
            Assert.That(tile.DefenseBonus, Is.GreaterThanOrEqualTo(0));
            
            // Test that tiles can be used for unit placement logic
            Assert.IsFalse(tile.IsOccupied(), "New tiles should not be occupied initially");
            Assert.IsFalse(tile.IsCity(), "New tiles should not have cities initially");
        }
    }
    
    [Test]
    public void Should_Generate_Maps_With_Realistic_Strategic_Value()
    {
        var gameMap = MapGenerator.GenerateMap(15, 10, MapType.Continental, 12345);
        
        // Count strategic terrain features
        var defensiveTerrain = gameMap.Values.Where(t => t.DefenseBonus > 0).Count();
        var difficultTerrain = gameMap.Values.Where(t => t.MovementCost > 2).Count();
        var easyTerrain = gameMap.Values.Where(t => t.MovementCost == 1).Count();
        
        var totalTiles = gameMap.Count;
        
        Assert.That((float)defensiveTerrain / totalTiles, Is.GreaterThan(0.1f), 
            "Map should have some defensive terrain for strategic gameplay");
        Assert.That((float)difficultTerrain / totalTiles, Is.LessThan(0.5f), 
            "Map should not be mostly difficult terrain");
        Assert.That((float)easyTerrain / totalTiles, Is.GreaterThan(0.2f), 
            "Map should have sufficient easy terrain for movement");
        
        GD.Print($"🎯 Strategic terrain analysis:");
        GD.Print($"   Defensive terrain: {(float)defensiveTerrain / totalTiles:P1}");
        GD.Print($"   Difficult terrain: {(float)difficultTerrain / totalTiles:P1}");
        GD.Print($"   Easy terrain: {(float)easyTerrain / totalTiles:P1}");
    }
    
    [Test]
    public void Should_Handle_Edge_Cases_Gracefully()
    {
        // Test minimum map size
        var tinyMap = MapGenerator.GenerateMap(1, 1, MapType.Continental, 12345);
        Assert.AreEqual(1, tinyMap.Count);
        Assert.IsNotNull(tinyMap[Vector2I.Zero]);
        
        // Test with different seeds producing different results
        var map1 = MapGenerator.GenerateMap(5, 5, MapType.Continental, 1);
        var map2 = MapGenerator.GenerateMap(5, 5, MapType.Continental, 2);
        
        var differences = 0;
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var pos = new Vector2I(x, y);
                if (map1[pos].TerrainType != map2[pos].TerrainType)
                {
                    differences++;
                }
            }
        }
        
        Assert.That(differences, Is.GreaterThan(0), "Different seeds should produce different terrain");
    }
    
    [Test]
    public void Should_Integrate_With_Visual_System()
    {
        // Test that generated terrain types have corresponding visual colors
        var gameMap = MapGenerator.GenerateMap(10, 8, MapType.Archipelago, 12345);
        var visualTile = new VisualHexTile();
        
        var testedTerrains = new HashSet<TerrainType>();
        foreach (var tile in gameMap.Values.Take(10)) // Test a sample
        {
            if (testedTerrains.Contains(tile.TerrainType)) continue;
            testedTerrains.Add(tile.TerrainType);
            
            // This would test that the visual system can handle all terrain types
            try
            {
                visualTile.Initialize(tile.Position, tile.TerrainType, Colors.White, Vector2.Zero);
                // If we get here without exception, the terrain type is supported
                GD.Print($"✅ Terrain type {tile.TerrainType} is supported by visual system");
            }
            catch (System.Exception ex)
            {
                Assert.Fail($"Visual system doesn't support terrain type {tile.TerrainType}: {ex.Message}");
            }
        }
        
        Assert.That(testedTerrains.Count, Is.GreaterThan(0), "Should have tested at least one terrain type");
        
        visualTile.QueueFree();
    }
    
    [Test]
    public void Should_Perform_Well_With_Large_Maps()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var largeMap = MapGenerator.GenerateMap(50, 30, MapType.Continental, 12345);
        
        stopwatch.Stop();
        
        Assert.AreEqual(1500, largeMap.Count);
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000), 
            $"Large map generation should complete in reasonable time, took {stopwatch.ElapsedMilliseconds}ms");
        
        GD.Print($"⚡ Large map (50x30) generated in {stopwatch.ElapsedMilliseconds}ms");
    }
    
    [Test]
    public void Should_Support_Configuration_Driven_Generation()
    {
        // Test that map generation respects configuration parameters
        var config = MapTypeConfiguration.GetConfig(MapType.Desert);
        
        // Verify the configuration has the expected characteristics
        Assert.That(config.TerrainBias[TerrainType.Desert], Is.GreaterThan(2.0f), 
            "Desert configuration should heavily favor desert terrain");
        Assert.That(config.WaterFlowIntensity, Is.LessThan(0.5f), 
            "Desert configuration should have low water flow");
        
        // Generate map and verify it follows the configuration
        var desertMap = MapGenerator.GenerateMap(20, 15, MapType.Desert, 12345);
        var desertPercentage = GetTerrainPercentages(desertMap)[TerrainType.Desert];
        
        Assert.That(desertPercentage, Is.GreaterThan(0.25f), 
            $"Desert map should have significant desert terrain, got {desertPercentage:P}");
    }
    
    private Dictionary<TerrainType, float> GetTerrainPercentages(Dictionary<Vector2I, HexTile> gameMap)
    {
        var distribution = new Dictionary<TerrainType, float>();
        var totalTiles = gameMap.Count;
        
        foreach (var terrainType in System.Enum.GetValues<TerrainType>())
        {
            var count = gameMap.Values.Count(t => t.TerrainType == terrainType);
            distribution[terrainType] = (float)count / totalTiles;
        }
        
        return distribution;
    }
    
    private void VerifyArchipelagoHasMoreWater(Dictionary<MapType, Dictionary<TerrainType, float>> comparisons)
    {
        var archipelagoWater = comparisons[MapType.Archipelago][TerrainType.Water] + 
                             comparisons[MapType.Archipelago][TerrainType.Lagoon];
        var continentalWater = comparisons[MapType.Continental][TerrainType.Water] + 
                              comparisons[MapType.Continental][TerrainType.Lagoon];
        
        Assert.That(archipelagoWater, Is.GreaterThan(continentalWater), 
            $"Archipelago should have more water than Continental: {archipelagoWater:P} vs {continentalWater:P}");
    }
    
    private void VerifyHighlandHasMoreMountains(Dictionary<MapType, Dictionary<TerrainType, float>> comparisons)
    {
        var highlandMountains = comparisons[MapType.Highland][TerrainType.Mountain] + 
                               comparisons[MapType.Highland][TerrainType.Hill];
        var continentalMountains = comparisons[MapType.Continental][TerrainType.Mountain] + 
                                  comparisons[MapType.Continental][TerrainType.Hill];
        
        Assert.That(highlandMountains, Is.GreaterThanOrEqualTo(continentalMountains), 
            $"Highland should have at least as many mountains as Continental: {highlandMountains:P} vs {continentalMountains:P}");
    }
    
    private void VerifyDesertHasMoreDesert(Dictionary<MapType, Dictionary<TerrainType, float>> comparisons)
    {
        var desertDesert = comparisons[MapType.Desert][TerrainType.Desert];
        var continentalDesert = comparisons[MapType.Continental][TerrainType.Desert];
        
        Assert.That(desertDesert, Is.GreaterThan(continentalDesert), 
            $"Desert should have more desert terrain than Continental: {desertDesert:P} vs {continentalDesert:P}");
    }
    
    private void VerifyWetlandsHasMoreWaterFeatures(Dictionary<MapType, Dictionary<TerrainType, float>> comparisons)
    {
        var wetlandsWaterFeatures = comparisons[MapType.Wetlands][TerrainType.River] + 
                                   comparisons[MapType.Wetlands][TerrainType.Lagoon];
        var continentalWaterFeatures = comparisons[MapType.Continental][TerrainType.River] + 
                                      comparisons[MapType.Continental][TerrainType.Lagoon];
        
        Assert.That(wetlandsWaterFeatures, Is.GreaterThanOrEqualTo(continentalWaterFeatures), 
            $"Wetlands should have at least as many water features as Continental: {wetlandsWaterFeatures:P} vs {continentalWaterFeatures:P}");
    }
}
