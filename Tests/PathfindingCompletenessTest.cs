using NUnit.Framework;
using Godot;
using System.Collections.Generic;

[TestFixture]
public class PathfindingCompletenessTest
{
    [Test]
    public void Should_Show_Complete_Reachable_Area_Initially()
    {
        // This test recreates the user's bug report:
        // Archer at (1,3) with 4 MP should see ALL destinations reachable within 4 MP
        // Including (2,5) which can be reached via (1,3) → (1,4) → (1,5) → (2,5)
        
        var logic = new MovementValidationLogic();
        var map = new Dictionary<Vector2I, HexTile>();
        
        // Create a test map that allows (2,5) to be reachable within 4 MP
        // Path: (1,3) → (1,4) → (1,5) → (2,5) = 1 + 1 + 1 = 3 total cost ≤ 4 MP
        map[new Vector2I(1, 3)] = new HexTile(new Vector2I(1, 3), TerrainType.Desert); // Start position  
        map[new Vector2I(1, 4)] = new HexTile(new Vector2I(1, 4), TerrainType.Shoreline); // Cost 1
        map[new Vector2I(1, 5)] = new HexTile(new Vector2I(1, 5), TerrainType.Shoreline); // Cost 1  
        map[new Vector2I(2, 5)] = new HexTile(new Vector2I(2, 5), TerrainType.Shoreline); // Cost 1
        map[new Vector2I(0, 5)] = new HexTile(new Vector2I(0, 5), TerrainType.Shoreline); // Cost 1
        map[new Vector2I(0, 4)] = new HexTile(new Vector2I(0, 4), TerrainType.Shoreline); // Cost 1
        map[new Vector2I(2, 3)] = new HexTile(new Vector2I(2, 3), TerrainType.Hill);    // Cost 2
        map[new Vector2I(0, 3)] = new HexTile(new Vector2I(0, 3), TerrainType.Hill);    // Cost 2
        map[new Vector2I(2, 4)] = new HexTile(new Vector2I(2, 4), TerrainType.Shoreline); // Cost 1
        
        // Create unit with 4 movement points (doubled as per user's test)
        var archer = new Archer();
        archer.CurrentMovementPoints = 4;
        
        GD.Print("=== TESTING PATHFINDING COMPLETENESS (FIXED) ===");
        
        // Get all destinations from starting position
        var initialDestinations = logic.GetValidMovementDestinations(archer, new Vector2I(1, 3), map);
        
        GD.Print($"🎯 Initial destinations from (1,3) with 4 MP: {initialDestinations.Count}");
        foreach (var dest in initialDestinations)
        {
            GD.Print($"  📍 {dest}");
        }
        
        // KEY TEST: (2,5) should be reachable from (1,3) within 4 MP
        // Path: (1,3) → (1,4) → (1,5) → (2,5) = 1 + 1 + 1 = 3 total cost ≤ 4 MP
        Assert.Contains(new Vector2I(2, 5), initialDestinations, 
            "BUG FOUND: (2,5) should be reachable from (1,3) via (1,4) → (1,5) within 4 MP but was not included in initial pathfinding!");
        
        // Also test that intermediate tiles are included
        Assert.Contains(new Vector2I(1, 4), initialDestinations, "(1,4) should be directly reachable");
        Assert.Contains(new Vector2I(1, 5), initialDestinations, "(1,5) should be reachable via (1,4)");
        Assert.Contains(new Vector2I(2, 3), initialDestinations, "(2,3) should be directly reachable");
        Assert.Contains(new Vector2I(0, 3), initialDestinations, "(0,3) should be directly reachable");
        
        GD.Print("✅ Pathfinding completeness test passed!");
    }
    
    [Test]
    public void Should_Match_Stepwise_Pathfinding()
    {
        // This test verifies that multi-step movement produces the same results
        // as calculating from intermediate positions
        
        var logic = new MovementValidationLogic();
        var map = new Dictionary<Vector2I, HexTile>();
        
        // Create simple test map
        map[new Vector2I(0, 0)] = new HexTile(new Vector2I(0, 0), TerrainType.Desert);
        map[new Vector2I(1, 0)] = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline); // Cost 1
        map[new Vector2I(2, 0)] = new HexTile(new Vector2I(2, 0), TerrainType.Shoreline); // Cost 1
        map[new Vector2I(3, 0)] = new HexTile(new Vector2I(3, 0), TerrainType.Shoreline); // Cost 1
        
        var unit = new Nakhtu();
        unit.CurrentMovementPoints = 3;
        
        // Get destinations from start with 3 MP
        var fromStart = logic.GetValidMovementDestinations(unit, new Vector2I(0, 0), map);
        
        // Simulate moving to (1,0) and getting destinations with 2 MP remaining
        unit.CurrentMovementPoints = 2;
        var fromIntermediate = logic.GetValidMovementDestinations(unit, new Vector2I(1, 0), map);
        
        // (2,0) and (3,0) should be reachable from both starting position and intermediate
        Assert.Contains(new Vector2I(2, 0), fromStart, "(2,0) should be in initial pathfinding");
        Assert.Contains(new Vector2I(3, 0), fromStart, "(3,0) should be in initial pathfinding");
        
        Assert.Contains(new Vector2I(2, 0), fromIntermediate, "(2,0) should be reachable from intermediate");
        Assert.Contains(new Vector2I(3, 0), fromIntermediate, "(3,0) should be reachable from intermediate");
        
        GD.Print("✅ Stepwise pathfinding consistency test passed!");
    }

    [Test]
    public void Should_Have_Correct_Hex_Adjacency()
    {
        // Test that hex adjacency calculation is correct
        var logic = new MovementValidationLogic();
        
        // Test adjacency from (1,5)
        var adjacent = logic.GetAdjacentPositions(new Vector2I(1, 5));
        
        GD.Print("=== TESTING HEX ADJACENCY ===");
        GD.Print($"Adjacent to (1,5): [{string.Join(", ", adjacent)}]");
        
        // (2,5) should be adjacent to (1,5)
        Assert.Contains(new Vector2I(2, 5), adjacent, "(2,5) should be adjacent to (1,5)");
        Assert.Contains(new Vector2I(0, 5), adjacent, "(0,5) should be adjacent to (1,5)");
        Assert.Contains(new Vector2I(1, 4), adjacent, "(1,4) should be adjacent to (1,5)");
        Assert.Contains(new Vector2I(1, 6), adjacent, "(1,6) should be adjacent to (1,5)");
        
        // Test adjacency from (1,3) 
        var adjacent13 = logic.GetAdjacentPositions(new Vector2I(1, 3));
        GD.Print($"Adjacent to (1,3): [{string.Join(", ", adjacent13)}]");
        
        Assert.Contains(new Vector2I(1, 4), adjacent13, "(1,4) should be adjacent to (1,3)");
        Assert.Contains(new Vector2I(2, 3), adjacent13, "(2,3) should be adjacent to (1,3)");
        Assert.Contains(new Vector2I(0, 3), adjacent13, "(0,3) should be adjacent to (1,3)");
        
        GD.Print("✅ Hex adjacency test passed!");
    }
} 