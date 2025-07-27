using NUnit.Framework;
using Godot;
using System.Collections.Generic;
using System.Linq;
using Archistrateia;

[TestFixture]
public class PathfindingJumpOverBarrierTest
{
    [Test]
    public void Should_Not_Include_Cheap_Hexes_Beyond_Expensive_Barriers()
    {
        // Test the user's hypothesis: algorithm incorrectly includes cheap hexes beyond expensive barriers
        var logic = new MovementValidationLogic();
        
        GD.Print("=== TESTING JUMP-OVER-BARRIER BUG ===");
        
        // Create a scenario with a cheap hex "island" beyond an expensive barrier
        var gameMap = new Dictionary<Vector2I, HexTile>();
        
        // Linear path with expensive barrier in the middle:
        // (0,0) → (1,0) → (2,0) → (3,0)
        //  Start   Cheap   EXPENSIVE   Cheap "Island"
        
        gameMap[new Vector2I(0, 0)] = new HexTile(new Vector2I(0, 0), TerrainType.Shoreline); // Start: cost 1
        gameMap[new Vector2I(1, 0)] = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline); // Reachable: cost 1
        gameMap[new Vector2I(2, 0)] = new HexTile(new Vector2I(2, 0), TerrainType.Lagoon);    // Barrier: cost 4
        gameMap[new Vector2I(3, 0)] = new HexTile(new Vector2I(3, 0), TerrainType.Shoreline); // "Island": cost 1
        
        // Path analysis:
        // To reach (3,0): (0,0) → (1,0) → (2,0) → (3,0) = 1 + 4 + 1 = 6 total cost
        // But unit only has 4 MP, so (3,0) should NOT be reachable
        
        var archer = new Archer(); // 4 MP
        GD.Print($"Archer has {archer.CurrentMovementPoints} MP");
        
        var validDestinations = logic.GetValidMovementDestinations(archer, new Vector2I(0, 0), gameMap);
        
        GD.Print("Pathfinding results:");
        foreach (var dest in validDestinations.OrderBy(p => p.X))
        {
            var tile = gameMap[dest];
            GD.Print($"  {dest}: {tile.TerrainType} (tile cost: {tile.MovementCost})");
        }
        
        // Verify what should be reachable
        var shouldBeReachable = new Vector2I(1, 0); // Cost 1 from start
        var shouldNotBeReachable = new Vector2I(3, 0); // Cost 6 total (beyond barrier)
        var expensiveBarrier = new Vector2I(2, 0); // Cost 5 total
        
        Assert.IsTrue(validDestinations.Contains(shouldBeReachable), 
            $"{shouldBeReachable} should be reachable with cost 1");
        
        Assert.IsFalse(validDestinations.Contains(expensiveBarrier), 
            $"{expensiveBarrier} should not be reachable (cost 5 > 4 MP)");
            
        Assert.IsFalse(validDestinations.Contains(shouldNotBeReachable), 
            $"BUG: {shouldNotBeReachable} marked as reachable but requires going through expensive barrier (total cost 6 > 4 MP)");
        
        GD.Print("✅ Jump-over-barrier test completed");
    }
    
    [Test]
    public void Should_Verify_Path_Connectivity_With_Manual_BFS()
    {
        // Manually verify path connectivity to confirm the Dijkstra bug
        var logic = new MovementValidationLogic();
        
        GD.Print("=== VERIFYING PATH CONNECTIVITY ===");
        
        // Same barrier scenario
        var gameMap = new Dictionary<Vector2I, HexTile>();
        gameMap[new Vector2I(0, 0)] = new HexTile(new Vector2I(0, 0), TerrainType.Shoreline); // Start
        gameMap[new Vector2I(1, 0)] = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline); // Cost 1
        gameMap[new Vector2I(2, 0)] = new HexTile(new Vector2I(2, 0), TerrainType.Lagoon);    // Cost 4 barrier
        gameMap[new Vector2I(3, 0)] = new HexTile(new Vector2I(3, 0), TerrainType.Shoreline); // Cost 1 "island"
        
        var archer = new Archer();
        var dijkstraResults = logic.GetValidMovementDestinations(archer, new Vector2I(0, 0), gameMap);
        
        // Manual verification: trace the actual path cost to each "reachable" destination
        foreach (var dest in dijkstraResults)
        {
            var actualPathCost = CalculateMinimumPathCost(new Vector2I(0, 0), dest, gameMap, logic);
            GD.Print($"Dijkstra says {dest} is reachable, actual minimum path cost: {actualPathCost}");
            
            if (actualPathCost > archer.CurrentMovementPoints)
            {
                GD.Print($"🚨 BUG CONFIRMED: {dest} is marked reachable but actual path cost ({actualPathCost}) > MP ({archer.CurrentMovementPoints})");
                Assert.Fail($"Dijkstra incorrectly included {dest} - actual path cost {actualPathCost} exceeds {archer.CurrentMovementPoints} MP");
            }
        }
        
        GD.Print("✅ Path connectivity verification completed");
    }
    
    [Test]
    public void Should_Handle_Complex_Multi_Path_Scenario()
    {
        // Test with a more complex scenario that matches the user's actual game
        var logic = new MovementValidationLogic();
        
        GD.Print("=== TESTING COMPLEX MULTI-PATH SCENARIO ===");
        
        // Create a realistic game map pattern like the user's screenshot
        var gameMap = new Dictionary<Vector2I, HexTile>();
        
        // Create a larger map with multiple potential paths, barriers, and cheap "islands"
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                var position = new Vector2I(x, y);
                TerrainType terrain;
                
                // Create a specific problematic pattern:
                // Expensive barriers that might allow algorithm to "jump over"
                if (x == 3 && y >= 1 && y <= 4) terrain = TerrainType.Lagoon; // Vertical barrier
                else if (x == 5 && y == 2) terrain = TerrainType.Lagoon; // Isolated expensive hex
                else if (x >= 6) terrain = TerrainType.Shoreline; // Cheap "island" beyond barriers
                else terrain = TerrainType.Shoreline; // Default cheap terrain
                
                gameMap[position] = new HexTile(position, terrain);
            }
        }
        
        var archer = new Archer(); // 4 MP
        var startPosition = new Vector2I(0, 2);
        
        GD.Print($"Testing complex scenario: Archer at {startPosition} with {archer.CurrentMovementPoints} MP");
        
        var validDestinations = logic.GetValidMovementDestinations(archer, startPosition, gameMap);
        
        GD.Print($"Found {validDestinations.Count} destinations:");
        foreach (var dest in validDestinations.OrderBy(p => p.X).ThenBy(p => p.Y))
        {
            var tile = gameMap[dest];
            GD.Print($"  {dest}: {tile.TerrainType} (cost: {tile.MovementCost})");
        }
        
        // Check for problematic "island" destinations beyond barriers
        var suspiciousDestinations = new List<Vector2I>();
        foreach (var dest in validDestinations)
        {
            // Check if destination is beyond expensive barriers
            if (dest.X >= 6) // Beyond the barrier zone
            {
                // Manually verify if this destination is actually reachable
                var actualCost = CalculateMinimumPathCost(startPosition, dest, gameMap, logic);
                GD.Print($"  Checking {dest}: Dijkstra included it, actual path cost = {actualCost}");
                
                if (actualCost > archer.CurrentMovementPoints)
                {
                    suspiciousDestinations.Add(dest);
                    GD.Print($"    🚨 SUSPICIOUS: {dest} marked as reachable but actual cost {actualCost} > {archer.CurrentMovementPoints} MP");
                }
            }
        }
        
        if (suspiciousDestinations.Count > 0)
        {
            Assert.Fail($"Complex scenario bug: {suspiciousDestinations.Count} destinations incorrectly marked as reachable: {string.Join(", ", suspiciousDestinations)}");
        }
        
        GD.Print("✅ Complex multi-path scenario test completed");
    }
    
    [Test]
    public void Should_Debug_Actual_Dijkstra_Algorithm_Execution()
    {
        // Add detailed debugging to understand what's happening in Dijkstra's
        var logic = new MovementValidationLogic();
        
        GD.Print("=== DEBUGGING DIJKSTRA'S ALGORITHM EXECUTION ===");
        
        // Create a scenario that might expose the jumping bug
        var gameMap = new Dictionary<Vector2I, HexTile>();
        
        // Pattern: Start → Cheap → Expensive → Cheap Island
        gameMap[new Vector2I(0, 0)] = new HexTile(new Vector2I(0, 0), TerrainType.Shoreline); // Start
        gameMap[new Vector2I(1, 0)] = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline); // Cheap: 1
        gameMap[new Vector2I(2, 0)] = new HexTile(new Vector2I(2, 0), TerrainType.Lagoon);    // Expensive: 4
        gameMap[new Vector2I(3, 0)] = new HexTile(new Vector2I(3, 0), TerrainType.Shoreline); // Island: 1
        
        // Add alternative paths that might confuse the algorithm
        gameMap[new Vector2I(0, 1)] = new HexTile(new Vector2I(0, 1), TerrainType.Shoreline); // Alt path
        gameMap[new Vector2I(1, 1)] = new HexTile(new Vector2I(1, 1), TerrainType.River);     // Alt path: 3
        gameMap[new Vector2I(2, 1)] = new HexTile(new Vector2I(2, 1), TerrainType.Shoreline); // Alt path
        gameMap[new Vector2I(3, 1)] = new HexTile(new Vector2I(3, 1), TerrainType.Shoreline); // Could reach (3,0)?
        
        var archer = new Archer(); // 4 MP
        
        GD.Print("Map layout:");
        for (int y = 0; y < 2; y++)
        {
            var row = "";
            for (int x = 0; x < 4; x++)
            {
                var pos = new Vector2I(x, y);
                if (gameMap.ContainsKey(pos))
                {
                    var tile = gameMap[pos];
                    var symbol = tile.TerrainType switch
                    {
                        TerrainType.Shoreline => "S",
                        TerrainType.Lagoon => "L",
                        TerrainType.River => "R",
                        _ => "?"
                    };
                    row += $"{symbol}({tile.MovementCost}) ";
                }
            }
            GD.Print($"  Row {y}: {row}");
        }
        
        var validDestinations = logic.GetValidMovementDestinations(archer, new Vector2I(0, 0), gameMap);
        
        GD.Print($"Dijkstra results for {archer.CurrentMovementPoints} MP:");
        foreach (var dest in validDestinations.OrderBy(p => p.X).ThenBy(p => p.Y))
        {
            var actualCost = CalculateMinimumPathCost(new Vector2I(0, 0), dest, gameMap, logic);
            var tile = gameMap[dest];
            GD.Print($"  {dest}: {tile.TerrainType} (actual min path cost: {actualCost})");
            
            if (actualCost > archer.CurrentMovementPoints)
            {
                GD.Print($"    🚨 BUG: Marked as reachable but costs {actualCost} > {archer.CurrentMovementPoints} MP");
            }
        }
        
        GD.Print("✅ Dijkstra debugging completed");
    }
    
    private int CalculateMinimumPathCost(Vector2I start, Vector2I target, Dictionary<Vector2I, HexTile> gameMap, MovementValidationLogic logic)
    {
        // Simple BFS to find actual minimum path cost (ignoring movement budget)
        var distances = new Dictionary<Vector2I, int>();
        var queue = new Queue<Vector2I>();
        
        distances[start] = 0;
        queue.Enqueue(start);
        
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            
            if (current == target)
            {
                return distances[current];
            }
            
            var adjacent = logic.GetAdjacentPositions(current);
            foreach (var adj in adjacent)
            {
                if (!gameMap.ContainsKey(adj)) continue;
                
                var newCost = distances[current] + gameMap[adj].MovementCost;
                
                if (!distances.ContainsKey(adj) || newCost < distances[adj])
                {
                    distances[adj] = newCost;
                    queue.Enqueue(adj);
                }
            }
        }
        
        return int.MaxValue; // No path found
    }
} 