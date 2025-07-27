using NUnit.Framework;
using Godot;
using System.Collections.Generic;
using System.Diagnostics;

[TestFixture]
public class OptimizedPathfindingTest
{
    [Test]
    public void Should_Work_Correctly_With_Binary_Heap_Optimization()
    {
        var logic = new MovementValidationLogic();
        var gameMap = CreateTestMap();
        var archer = new Archer(); // 2 MP
        var startPosition = new Vector2I(1, 1);
        
        gameMap[startPosition].PlaceUnit(archer);
        
        GD.Print("=== TESTING OPTIMIZED PATHFINDING ===");
        
        var validDestinations = logic.GetValidMovementDestinations(archer, startPosition, gameMap);
        
        Assert.IsTrue(validDestinations.Count > 0, "Should find valid destinations");
        
        // Verify all destinations are within movement budget
        foreach (var destination in validDestinations)
        {
            var tile = gameMap[destination];
            Assert.IsTrue(tile.MovementCost <= archer.CurrentMovementPoints, 
                $"Destination {destination} with cost {tile.MovementCost} should be within {archer.CurrentMovementPoints} MP");
        }
        
        GD.Print($"✅ Optimized pathfinding found {validDestinations.Count} valid destinations");
    }
    
    [Test]
    public void Should_Handle_Complex_Terrain_Correctly()
    {
        var logic = new MovementValidationLogic();
        var gameMap = CreateComplexTerrainMap();
        var charioteer = new Charioteer(); // 4 MP
        var startPosition = new Vector2I(2, 2); // Center position
        
        gameMap[startPosition].PlaceUnit(charioteer);
        
        var validDestinations = logic.GetValidMovementDestinations(charioteer, startPosition, gameMap);
        
        GD.Print($"=== COMPLEX TERRAIN TEST ===");
        GD.Print($"Charioteer with 4 MP found {validDestinations.Count} destinations");
        
        // Should be able to reach some shoreline tiles (cost 1) and some desert/hill tiles (cost 2)
        Assert.IsTrue(validDestinations.Count > 0, "Should find destinations in complex terrain");
        
        // Test specific expected reachable positions
        var nearShoreline = new Vector2I(3, 2); // Adjacent shoreline should be reachable
        if (gameMap.ContainsKey(nearShoreline) && gameMap[nearShoreline].TerrainType == TerrainType.Shoreline)
        {
            Assert.Contains(nearShoreline, validDestinations, "Adjacent shoreline should be reachable");
        }
        
        GD.Print("✅ Complex terrain pathfinding working correctly");
    }
    
    [Test]
    public void Should_Verify_Priority_Queue_Functionality()
    {
        var priorityQueue = new PriorityQueue<DijkstraNode>();
        
        // Add nodes in non-optimal order
        priorityQueue.Enqueue(new DijkstraNode(5, new Vector2I(1, 1)));
        priorityQueue.Enqueue(new DijkstraNode(1, new Vector2I(0, 0)));
        priorityQueue.Enqueue(new DijkstraNode(3, new Vector2I(2, 2)));
        priorityQueue.Enqueue(new DijkstraNode(2, new Vector2I(1, 0)));
        
        // Should come out in order of lowest cost first
        var first = priorityQueue.Dequeue();
        var second = priorityQueue.Dequeue();
        var third = priorityQueue.Dequeue();
        var fourth = priorityQueue.Dequeue();
        
        Assert.AreEqual(1, first.Cost, "First dequeued should have cost 1");
        Assert.AreEqual(2, second.Cost, "Second dequeued should have cost 2");
        Assert.AreEqual(3, third.Cost, "Third dequeued should have cost 3");
        Assert.AreEqual(5, fourth.Cost, "Fourth dequeued should have cost 5");
        
        GD.Print("✅ Priority queue binary heap working correctly");
    }
    
    private Dictionary<Vector2I, HexTile> CreateTestMap()
    {
        var map = new Dictionary<Vector2I, HexTile>();
        
        // Create a 3x3 test map
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var pos = new Vector2I(x, y);
                // Mix of terrain types
                var terrain = (x + y) % 2 == 0 ? TerrainType.Shoreline : TerrainType.Desert;
                map[pos] = new HexTile(pos, terrain);
            }
        }
        
        return map;
    }
    
    private Dictionary<Vector2I, HexTile> CreateComplexTerrainMap()
    {
        var map = new Dictionary<Vector2I, HexTile>();
        
        // Create a 5x5 map with varied terrain
        var terrainPattern = new TerrainType[,]
        {
            { TerrainType.Shoreline, TerrainType.Desert, TerrainType.Hill, TerrainType.River, TerrainType.Lagoon },
            { TerrainType.Desert, TerrainType.Shoreline, TerrainType.Desert, TerrainType.Hill, TerrainType.River },
            { TerrainType.Hill, TerrainType.Desert, TerrainType.Shoreline, TerrainType.Desert, TerrainType.Hill },
            { TerrainType.River, TerrainType.Hill, TerrainType.Desert, TerrainType.Shoreline, TerrainType.Desert },
            { TerrainType.Lagoon, TerrainType.River, TerrainType.Hill, TerrainType.Desert, TerrainType.Shoreline }
        };
        
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var pos = new Vector2I(x, y);
                map[pos] = new HexTile(pos, terrainPattern[x, y]);
            }
        }
        
        return map;
    }
} 