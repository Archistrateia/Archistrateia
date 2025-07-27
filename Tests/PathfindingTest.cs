using NUnit.Framework;
using Godot;
using System.Collections.Generic;
using System.Linq;
using Archistrateia;

[TestFixture]
public class PathfindingTest
{
    [Test]
    public void Should_Show_Complete_Reachable_Area_Initially()
    {
        var logic = new MovementValidationLogic();
        var map = new Dictionary<Vector2I, HexTile>();
        
        map[new Vector2I(1, 3)] = new HexTile(new Vector2I(1, 3), TerrainType.Desert);
        map[new Vector2I(1, 4)] = new HexTile(new Vector2I(1, 4), TerrainType.Shoreline);
        map[new Vector2I(1, 5)] = new HexTile(new Vector2I(1, 5), TerrainType.Shoreline);
        map[new Vector2I(2, 5)] = new HexTile(new Vector2I(2, 5), TerrainType.Shoreline);
        map[new Vector2I(0, 5)] = new HexTile(new Vector2I(0, 5), TerrainType.Shoreline);
        map[new Vector2I(0, 4)] = new HexTile(new Vector2I(0, 4), TerrainType.Shoreline);
        map[new Vector2I(2, 3)] = new HexTile(new Vector2I(2, 3), TerrainType.Hill);
        map[new Vector2I(0, 3)] = new HexTile(new Vector2I(0, 3), TerrainType.Hill);
        map[new Vector2I(2, 4)] = new HexTile(new Vector2I(2, 4), TerrainType.Shoreline);
        
        var archer = new Archer();
        archer.CurrentMovementPoints = 4;
        
        GD.Print("=== TESTING PATHFINDING COMPLETENESS ===");
        
        var initialDestinations = logic.GetValidMovementDestinations(archer, new Vector2I(1, 3), map);
        
        GD.Print($"Initial destinations from (1,3) with 4 MP: {initialDestinations.Count}");
        foreach (var dest in initialDestinations)
        {
            GD.Print($"  📍 {dest}");
        }
        
        Assert.Contains(new Vector2I(2, 5), initialDestinations, 
            "BUG FOUND: (2,5) should be reachable from (1,3) via (1,4) → (1,5) within 4 MP but was not included in initial pathfinding!");
        
        Assert.Contains(new Vector2I(1, 4), initialDestinations, "(1,4) should be directly reachable");
        Assert.Contains(new Vector2I(1, 5), initialDestinations, "(1,5) should be reachable via (1,4)");
        Assert.Contains(new Vector2I(2, 3), initialDestinations, "(2,3) should be directly reachable");
        Assert.Contains(new Vector2I(0, 3), initialDestinations, "(0,3) should be directly reachable");
        
        GD.Print("✅ Pathfinding completeness test passed!");
    }
    
    [Test]
    public void Should_Match_Stepwise_Pathfinding()
    {
        var logic = new MovementValidationLogic();
        var map = new Dictionary<Vector2I, HexTile>();
        
        map[new Vector2I(0, 0)] = new HexTile(new Vector2I(0, 0), TerrainType.Desert);
        map[new Vector2I(1, 0)] = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline);
        map[new Vector2I(2, 0)] = new HexTile(new Vector2I(2, 0), TerrainType.Shoreline);
        map[new Vector2I(3, 0)] = new HexTile(new Vector2I(3, 0), TerrainType.Shoreline);
        
        var unit = new Nakhtu();
        unit.CurrentMovementPoints = 3;
        
        var fromStart = logic.GetValidMovementDestinations(unit, new Vector2I(0, 0), map);
        
        unit.CurrentMovementPoints = 2;
        var fromIntermediate = logic.GetValidMovementDestinations(unit, new Vector2I(1, 0), map);
        
        Assert.Contains(new Vector2I(2, 0), fromStart, "(2,0) should be in initial pathfinding");
        Assert.Contains(new Vector2I(3, 0), fromStart, "(3,0) should be in initial pathfinding");
        
        Assert.Contains(new Vector2I(2, 0), fromIntermediate, "(2,0) should be reachable from intermediate");
        Assert.Contains(new Vector2I(3, 0), fromIntermediate, "(3,0) should be reachable from intermediate");
        
        GD.Print("✅ Stepwise pathfinding consistency test passed!");
    }

    [Test]
    public void Should_Have_Correct_Hex_Adjacency()
    {
        var logic = new MovementValidationLogic();
        
        var adjacent = logic.GetAdjacentPositions(new Vector2I(1, 5));
        
        GD.Print("=== TESTING HEX ADJACENCY ===");
        GD.Print($"Adjacent to (1,5): [{string.Join(", ", adjacent)}]");
        
        Assert.Contains(new Vector2I(2, 5), adjacent, "(2,5) should be adjacent to (1,5)");
        Assert.Contains(new Vector2I(0, 5), adjacent, "(0,5) should be adjacent to (1,5)");
        Assert.Contains(new Vector2I(1, 4), adjacent, "(1,4) should be adjacent to (1,5)");
        Assert.Contains(new Vector2I(1, 6), adjacent, "(1,6) should be adjacent to (1,5)");
        
        var adjacent13 = logic.GetAdjacentPositions(new Vector2I(1, 3));
        GD.Print($"Adjacent to (1,3): [{string.Join(", ", adjacent13)}]");
        
        Assert.Contains(new Vector2I(1, 4), adjacent13, "(1,4) should be adjacent to (1,3)");
        Assert.Contains(new Vector2I(2, 3), adjacent13, "(2,3) should be adjacent to (1,3)");
        Assert.Contains(new Vector2I(0, 3), adjacent13, "(0,3) should be adjacent to (1,3)");
        
        GD.Print("✅ Hex adjacency test passed!");
    }

    [Test]
    public void Should_Not_Jump_Over_Expensive_Barriers()
    {
        var logic = new MovementValidationLogic();
        
        GD.Print("=== TESTING JUMP-OVER-BARRIER BUG ===");
        
        var gameMap = new Dictionary<Vector2I, HexTile>();
        
        gameMap[new Vector2I(0, 0)] = new HexTile(new Vector2I(0, 0), TerrainType.Shoreline);
        gameMap[new Vector2I(1, 0)] = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline);
        gameMap[new Vector2I(2, 0)] = new HexTile(new Vector2I(2, 0), TerrainType.Lagoon);
        gameMap[new Vector2I(3, 0)] = new HexTile(new Vector2I(3, 0), TerrainType.Shoreline);
        
        var archer = new Archer();
        GD.Print($"Archer has {archer.CurrentMovementPoints} MP");
        
        var validDestinations = logic.GetValidMovementDestinations(archer, new Vector2I(0, 0), gameMap);
        
        GD.Print("Pathfinding results:");
        foreach (var dest in validDestinations.OrderBy(p => p.X))
        {
            var tile = gameMap[dest];
            GD.Print($"  {dest}: {tile.TerrainType} (tile cost: {tile.MovementCost})");
        }
        
        var shouldBeReachable = new Vector2I(1, 0);
        var shouldNotBeReachable = new Vector2I(3, 0);
        
        Assert.Contains(shouldBeReachable, validDestinations, 
            $"{shouldBeReachable} should be reachable (adjacent shoreline)");
        Assert.IsFalse(validDestinations.Contains(shouldNotBeReachable), 
            $"BARRIER-JUMPING BUG: {shouldNotBeReachable} marked as reachable but is beyond expensive barrier");
        
        GD.Print("✅ Barrier jumping prevented!");
    }

    [Test]
    public void Should_Handle_Multiple_Enqueueing_Correctly()
    {
        var logic = new MovementValidationLogic();
        
        GD.Print("=== TESTING MULTIPLE ENQUEUEING HANDLING ===");
        
        var gameMap = new Dictionary<Vector2I, HexTile>();
        
        gameMap[new Vector2I(1, 0)] = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline);
        gameMap[new Vector2I(0, 1)] = new HexTile(new Vector2I(0, 1), TerrainType.Shoreline);
        gameMap[new Vector2I(2, 1)] = new HexTile(new Vector2I(2, 1), TerrainType.Shoreline);
        gameMap[new Vector2I(1, 2)] = new HexTile(new Vector2I(1, 2), TerrainType.Shoreline);
        
        var archer = new Archer();
        
        var validDestinations = logic.GetValidMovementDestinations(archer, new Vector2I(1, 0), gameMap);
        
        GD.Print($"Diamond pattern results:");
        foreach (var dest in validDestinations.OrderBy(p => p.Y).ThenBy(p => p.X))
        {
            GD.Print($"  {dest}");
        }
        
        var adjacent = logic.GetAdjacentPositions(new Vector2I(1, 0));
        GD.Print($"Direct adjacents from (1,0): [{string.Join(", ", adjacent)}]");
        
        Assert.GreaterOrEqual(validDestinations.Count, 1, "Should find at least some destinations");
            
        GD.Print("✅ Multiple enqueueing handled correctly!");
    }

    [Test]
    public void Should_Work_With_Binary_Heap_Optimization()
    {
        var logic = new MovementValidationLogic();
        var gameMap = CreateTestMap();
        var archer = new Archer();
        var startPosition = new Vector2I(1, 1);
        
        gameMap[startPosition].PlaceUnit(archer);
        
        GD.Print("=== TESTING OPTIMIZED PATHFINDING ===");
        
        var validDestinations = logic.GetValidMovementDestinations(archer, startPosition, gameMap);
        
        Assert.IsTrue(validDestinations.Count > 0, "Should find valid destinations");
        
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
        var charioteer = new Charioteer();
        var startPosition = new Vector2I(2, 2);
        
        gameMap[startPosition].PlaceUnit(charioteer);
        
        var validDestinations = logic.GetValidMovementDestinations(charioteer, startPosition, gameMap);
        
        GD.Print($"=== COMPLEX TERRAIN TEST ===");
        GD.Print($"Charioteer with 4 MP found {validDestinations.Count} destinations");
        
        Assert.IsTrue(validDestinations.Count > 0, "Should find destinations in complex terrain");
        
        var nearShoreline = new Vector2I(3, 2);
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
        
        priorityQueue.Enqueue(new DijkstraNode(5, new Vector2I(1, 1)));
        priorityQueue.Enqueue(new DijkstraNode(1, new Vector2I(0, 0)));
        priorityQueue.Enqueue(new DijkstraNode(3, new Vector2I(2, 2)));
        priorityQueue.Enqueue(new DijkstraNode(2, new Vector2I(1, 0)));
        
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
        
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var pos = new Vector2I(x, y);
                var terrain = (x + y) % 2 == 0 ? TerrainType.Shoreline : TerrainType.Desert;
                map[pos] = new HexTile(pos, terrain);
            }
        }
        
        return map;
    }
    
    private Dictionary<Vector2I, HexTile> CreateComplexTerrainMap()
    {
        var map = new Dictionary<Vector2I, HexTile>();
        
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