using NUnit.Framework;
using Godot;
using System.Collections.Generic;
using System.Linq;
using Archistrateia;

[TestFixture]
public class PathfindingConsistencyBugTest
{
    [Test]
    public void Should_Detect_Unreachable_Island_Consistency_Bug()
    {
        // Test for the exact bug the user reported: a tile marked as reachable 
        // but surrounded by unreachable tiles (creating an "island")
        var logic = new MovementValidationLogic();
        
        GD.Print("=== DETECTING PATHFINDING CONSISTENCY BUG ===");
        GD.Print("Checking for destinations marked reachable but with unreachable paths");
        
        // Create a scenario that might reproduce the island bug
        var gameMap = new Dictionary<Vector2I, HexTile>();
        
        // Create a larger map to test connectivity
        for (int x = 0; x <= 15; x++)
        {
            for (int y = 0; y <= 10; y++)
            {
                var position = new Vector2I(x, y);
                // Mix terrain types to create potential pathfinding issues
                var terrain = GetVariedTerrain(x, y);
                gameMap[position] = new HexTile(position, terrain);
            }
        }
        
        var charioteer = new Charioteer(); // 8 MP
        var startPos = new Vector2I(5, 4);
        
        // Get pathfinding results
        var validDestinations = logic.GetValidMovementDestinations(charioteer, startPos, gameMap);
        var pathCosts = logic.GetPathCostsFromPosition(charioteer, startPos, gameMap);
        
        GD.Print($"\nCharioteer at {startPos} with {charioteer.CurrentMovementPoints} MP");
        GD.Print($"Found {validDestinations.Count} valid destinations");
        
        // Check each valid destination for consistency
        var inconsistentDestinations = new List<Vector2I>();
        
        foreach (var destination in validDestinations)
        {
            if (pathCosts.ContainsKey(destination))
            {
                var pathCost = pathCosts[destination];
                GD.Print($"\nAnalyzing destination {destination} (cost {pathCost}):");
                
                // Check if this destination has unreachable neighbors
                var hasUnreachableNeighbors = CheckForUnreachableNeighbors(destination, validDestinations, gameMap);
                
                if (hasUnreachableNeighbors)
                {
                    GD.Print($"   🚨 INCONSISTENCY: {destination} is reachable but has unreachable neighbors!");
                    inconsistentDestinations.Add(destination);
                    
                    // Try to trace the path to see where the inconsistency lies
                    var pathExists = VerifyPathExistsToDestination(startPos, destination, validDestinations, gameMap);
                    GD.Print($"   Path verification: {pathExists}");
                }
            }
        }
        
        if (inconsistentDestinations.Count > 0)
        {
            GD.Print($"\n🚨 FOUND {inconsistentDestinations.Count} INCONSISTENT DESTINATIONS:");
            foreach (var dest in inconsistentDestinations)
            {
                GD.Print($"   {dest}: Marked reachable but surrounded by unreachable tiles");
            }
            
            Assert.Fail($"PATHFINDING CONSISTENCY BUG: {inconsistentDestinations.Count} destinations are marked reachable but have unreachable paths!");
        }
        else
        {
            GD.Print("\n✅ No pathfinding consistency issues detected");
        }
    }
    
    private bool CheckForUnreachableNeighbors(Vector2I destination, List<Vector2I> validDestinations, Dictionary<Vector2I, HexTile> gameMap)
    {
        var adjacentPositions = GetAdjacentPositions(destination);
        var reachableNeighbors = 0;
        var totalNeighbors = 0;
        
        foreach (var adjacent in adjacentPositions)
        {
            if (gameMap.ContainsKey(adjacent))
            {
                totalNeighbors++;
                if (validDestinations.Contains(adjacent))
                {
                    reachableNeighbors++;
                }
                else
                {
                    var terrain = gameMap[adjacent].TerrainType;
                    GD.Print($"     Unreachable neighbor: {adjacent} ({terrain})");
                }
            }
        }
        
        GD.Print($"     Neighbors: {reachableNeighbors}/{totalNeighbors} reachable");
        
        // If ALL neighbors are unreachable, this might be an island
        return reachableNeighbors == 0 && totalNeighbors > 0;
    }
    
    private bool VerifyPathExistsToDestination(Vector2I start, Vector2I destination, List<Vector2I> validDestinations, Dictionary<Vector2I, HexTile> gameMap)
    {
        // Manual BFS using only tiles marked as valid destinations
        var queue = new Queue<Vector2I>();
        var visited = new HashSet<Vector2I>();
        
        queue.Enqueue(start);
        visited.Add(start);
        
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            
            if (current == destination)
            {
                return true;
            }
            
            var adjacents = GetAdjacentPositions(current);
            foreach (var adjacent in adjacents)
            {
                if (gameMap.ContainsKey(adjacent) && 
                    validDestinations.Contains(adjacent) && 
                    !visited.Contains(adjacent))
                {
                    visited.Add(adjacent);
                    queue.Enqueue(adjacent);
                }
            }
        }
        
        return false;
    }
    
    [Test]
    public void Should_Debug_User_Specific_Island_Scenario()
    {
        // Specifically test the user's reported scenario: (5,4) to (5,9) 
        var logic = new MovementValidationLogic();
        
        GD.Print("=== DEBUGGING USER'S SPECIFIC ISLAND SCENARIO ===");
        GD.Print("Testing (5,4) to (5,9) with surrounding tiles");
        
        // Create a map that reproduces the user's situation
        var gameMap = new Dictionary<Vector2I, HexTile>();
        
        // Create terrain around the problem area
        for (int x = 3; x <= 7; x++)
        {
            for (int y = 2; y <= 10; y++)
            {
                var position = new Vector2I(x, y);
                var terrain = GetRealisticTerrain(x, y);
                gameMap[position] = new HexTile(position, terrain);
            }
        }
        
        var charioteer = new Charioteer(); // 8 MP
        var startPos = new Vector2I(5, 4);
        var targetPos = new Vector2I(5, 9);
        
        var validDestinations = logic.GetValidMovementDestinations(charioteer, startPos, gameMap);
        var pathCosts = logic.GetPathCostsFromPosition(charioteer, startPos, gameMap);
        
        GD.Print($"\nPathfinding from {startPos} to {targetPos}:");
        
        bool isTargetReachable = validDestinations.Contains(targetPos);
        var targetCost = pathCosts.ContainsKey(targetPos) ? pathCosts[targetPos] : -1;
        
        GD.Print($"Target {targetPos} reachable: {isTargetReachable} (cost {targetCost})");
        
        if (isTargetReachable)
        {
            GD.Print("\nAnalyzing surrounding tiles of target:");
            
            // Check the tiles around (5,9) to see if they're marked as reachable
            var surroundingTiles = GetAdjacentPositions(targetPos);
            var reachableSurrounding = 0;
            
            foreach (var surrounding in surroundingTiles)
            {
                if (gameMap.ContainsKey(surrounding))
                {
                    bool isReachable = validDestinations.Contains(surrounding);
                    var cost = pathCosts.ContainsKey(surrounding) ? pathCosts[surrounding] : -1;
                    var terrain = gameMap[surrounding].TerrainType;
                    
                    GD.Print($"   {surrounding}: {terrain} - {(isReachable ? "REACHABLE" : "unreachable")} (cost {cost})");
                    
                    if (isReachable) reachableSurrounding++;
                }
            }
            
            if (reachableSurrounding == 0)
            {
                GD.Print($"\n🚨 ISLAND DETECTED: {targetPos} is reachable but NO surrounding tiles are!");
                GD.Print("This confirms the user's bug report - pathfinding inconsistency");
                
                // Try to find how the algorithm thinks it can reach this tile
                GD.Print("\nTracing path backwards from island:");
                TracePathBackwards(targetPos, startPos, pathCosts, gameMap, validDestinations);
                
                Assert.Fail("CONFIRMED BUG: Target is reachable but all surrounding tiles are unreachable!");
            }
            else
            {
                GD.Print($"\n✅ {reachableSurrounding} surrounding tiles are reachable - no island detected");
            }
        }
        else
        {
            GD.Print("✅ Target correctly identified as unreachable");
        }
    }
    
    private void TracePathBackwards(Vector2I island, Vector2I start, Dictionary<Vector2I, int> pathCosts, Dictionary<Vector2I, HexTile> gameMap, List<Vector2I> validDestinations)
    {
        var islandCost = pathCosts[island];
        GD.Print($"Island {island} has cost {islandCost}");
        
        // Look for adjacent tiles that could lead to this island
        var adjacents = GetAdjacentPositions(island);
        foreach (var adjacent in adjacents)
        {
            if (gameMap.ContainsKey(adjacent))
            {
                var adjacentCost = pathCosts.ContainsKey(adjacent) ? pathCosts[adjacent] : -1;
                var isAdjacenReachable = validDestinations.Contains(adjacent);
                var terrain = gameMap[adjacent].TerrainType;
                var tileCost = gameMap[island].MovementCost;
                
                GD.Print($"   Checking {adjacent}: cost {adjacentCost}, reachable {isAdjacenReachable}");
                
                if (adjacentCost >= 0 && adjacentCost + tileCost == islandCost)
                {
                    GD.Print($"   🔍 Potential path step: {adjacent} → {island} (cost {adjacentCost} + {tileCost} = {islandCost})");
                    
                    if (!isAdjacenReachable)
                    {
                        GD.Print($"   🚨 BUG: Path goes through unreachable tile {adjacent}!");
                    }
                }
            }
        }
    }
    
    private TerrainType GetVariedTerrain(int x, int y)
    {
        // Create varied terrain that might expose pathfinding bugs
        var seed = (x * 13 + y * 7) % 20;
        
        if (seed < 5) return TerrainType.Shoreline;  // 25% - cheap
        if (seed < 10) return TerrainType.Desert;    // 25% - medium
        if (seed < 14) return TerrainType.Hill;      // 20% - medium
        if (seed < 17) return TerrainType.River;     // 15% - expensive
        return TerrainType.Lagoon;                   // 15% - very expensive
    }
    
    private TerrainType GetRealisticTerrain(int x, int y)
    {
        // Create more realistic terrain distribution
        if (x == 5 && y == 4) return TerrainType.Shoreline; // Start
        if (x == 5 && y == 9) return TerrainType.Shoreline; // Target
        
        // Create some expensive barriers
        if (y == 6 || y == 7) return TerrainType.Lagoon; // Expensive barrier
        
        return TerrainType.Shoreline; // Mostly cheap terrain
    }
    
    private List<Vector2I> GetAdjacentPositions(Vector2I position)
    {
        // Hex grid adjacency
        return new List<Vector2I>
        {
            new Vector2I(position.X + 1, position.Y),     // East
            new Vector2I(position.X - 1, position.Y),     // West
            new Vector2I(position.X, position.Y + 1),     // South
            new Vector2I(position.X, position.Y - 1),     // North
            new Vector2I(position.X + 1, position.Y - 1), // NorthEast
            new Vector2I(position.X - 1, position.Y + 1)  // SouthWest
        };
    }
} 