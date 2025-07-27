using NUnit.Framework;
using Godot;
using System.Collections.Generic;
using System.Linq;
using Archistrateia;

[TestFixture]
public class HexAdjacencyBugTest
{
    [Test]
    public void Should_Verify_Hex_Adjacency_Is_Causing_Island_Bug()
    {
        // Test that the current hex adjacency calculation is wrong and causes false connectivity
        var logic = new MovementValidationLogic();
        
        GD.Print("=== TESTING HEX ADJACENCY BUG ===");
        GD.Print("Verifying that incorrect adjacency causes pathfinding islands");
        
        // Test specific positions from the user's bug report
        var testPositions = new Vector2I[]
        {
            new Vector2I(1, 3), // Medjay position
            new Vector2I(0, 3), // "Island" that was marked reachable
            new Vector2I(7, 2), // Nakhtu position  
            new Vector2I(7, 3), // "Island" that was marked reachable
            new Vector2I(8, 1)  // "Island" that was marked reachable
        };
        
        GD.Print("\nCurrent adjacency calculations:");
        foreach (var pos in testPositions)
        {
            var adjacents = logic.GetAdjacentPositions(pos);
            GD.Print($"{pos} adjacent to: {string.Join(", ", adjacents.Select(a => a.ToString()))}");
        }
        
        // Check specific problematic adjacencies from the bug scenarios
        GD.Print("\n=== ANALYZING PROBLEMATIC ADJACENCIES ===");
        
        // Case 1: (1,3) and (0,3) - are they actually adjacent in a hex grid?
        var medjayPos = new Vector2I(1, 3);
        var island1 = new Vector2I(0, 3);
        var medjayAdjacents = logic.GetAdjacentPositions(medjayPos);
        bool isMedjayConnectedToIsland1 = medjayAdjacents.Contains(island1);
        
        GD.Print($"Is {medjayPos} adjacent to {island1}? {isMedjayConnectedToIsland1}");
        
        // Case 2: (7,2) and (7,3) - are they actually adjacent in a hex grid?
        var nakhtuPos = new Vector2I(7, 2);
        var island2 = new Vector2I(7, 3);
        var nakhtuAdjacents = logic.GetAdjacentPositions(nakhtuPos);
        bool isNakhtuConnectedToIsland2 = nakhtuAdjacents.Contains(island2);
        
        GD.Print($"Is {nakhtuPos} adjacent to {island2}? {isNakhtuConnectedToIsland2}");
        
        // Case 3: (7,2) and (8,1) - are they actually adjacent in a hex grid?
        var island3 = new Vector2I(8, 1);
        bool isNakhtuConnectedToIsland3 = nakhtuAdjacents.Contains(island3);
        
        GD.Print($"Is {nakhtuPos} adjacent to {island3}? {isNakhtuConnectedToIsland3}");
        
        // Verify proper hex adjacency
        GD.Print("\n=== CHECKING PROPER HEX ADJACENCY ===");
        
        // For a flat-top hex grid, proper adjacency should depend on even/odd columns
        var properAdjacents13 = GetProperHexAdjacents(new Vector2I(1, 3));
        var properAdjacents72 = GetProperHexAdjacents(new Vector2I(7, 2));
        
        GD.Print($"Proper hex adjacents for (1,3): {string.Join(", ", properAdjacents13.Select(a => a.ToString()))}");
        GD.Print($"Proper hex adjacents for (7,2): {string.Join(", ", properAdjacents72.Select(a => a.ToString()))}");
        
        bool properConnection13to03 = properAdjacents13.Contains(new Vector2I(0, 3));
        bool properConnection72to73 = properAdjacents72.Contains(new Vector2I(7, 3));
        bool properConnection72to81 = properAdjacents72.Contains(new Vector2I(8, 1));
        
        GD.Print($"\nProper hex adjacency results:");
        GD.Print($"  (1,3) → (0,3): {properConnection13to03}");
        GD.Print($"  (7,2) → (7,3): {properConnection72to73}");
        GD.Print($"  (7,2) → (8,1): {properConnection72to81}");
        
        // If current algorithm shows connections that proper hex doesn't, that's the bug
        if (isMedjayConnectedToIsland1 && !properConnection13to03)
        {
            GD.Print("🚨 BUG DETECTED: Current algorithm shows false connection (1,3) → (0,3)");
        }
        
        if (isNakhtuConnectedToIsland2 && !properConnection72to73)
        {
            GD.Print("🚨 BUG DETECTED: Current algorithm shows false connection (7,2) → (7,3)");
        }
        
        if (isNakhtuConnectedToIsland3 && !properConnection72to81)
        {
            GD.Print("🚨 BUG DETECTED: Current algorithm shows false connection (7,2) → (8,1)");
        }
        
        // If any false connections exist, that explains the island bug
        bool hasFalseConnections = (isMedjayConnectedToIsland1 && !properConnection13to03) ||
                                  (isNakhtuConnectedToIsland2 && !properConnection72to73) ||
                                  (isNakhtuConnectedToIsland3 && !properConnection72to81);
        
        if (hasFalseConnections)
        {
            GD.Print("\n✅ CONFIRMED: Incorrect hex adjacency is causing the island pathfinding bug!");
            GD.Print("The pathfinding algorithm thinks tiles are adjacent when they're not,");
            GD.Print("creating phantom paths to unreachable 'islands'.");
        }
        else
        {
            GD.Print("\n❓ Hex adjacency appears correct - the bug might be elsewhere");
        }
    }
    
    private List<Vector2I> GetProperHexAdjacents(Vector2I position)
    {
        // Proper hex adjacency for flat-top orientation
        // Depends on whether column is even or odd
        var adjacents = new List<Vector2I>();
        
        // Common neighbors (always the same)
        adjacents.Add(new Vector2I(position.X - 1, position.Y));     // West
        adjacents.Add(new Vector2I(position.X + 1, position.Y));     // East
        
        // Diagonal neighbors depend on even/odd column
        if (position.X % 2 == 0) // Even column
        {
            adjacents.Add(new Vector2I(position.X - 1, position.Y - 1)); // Northwest
            adjacents.Add(new Vector2I(position.X, position.Y - 1));     // Northeast
            adjacents.Add(new Vector2I(position.X - 1, position.Y + 1)); // Southwest  
            adjacents.Add(new Vector2I(position.X, position.Y + 1));     // Southeast
        }
        else // Odd column
        {
            adjacents.Add(new Vector2I(position.X, position.Y - 1));     // Northwest
            adjacents.Add(new Vector2I(position.X + 1, position.Y - 1)); // Northeast
            adjacents.Add(new Vector2I(position.X, position.Y + 1));     // Southwest
            adjacents.Add(new Vector2I(position.X + 1, position.Y + 1)); // Southeast
        }
        
        return adjacents;
    }
    
    [Test]
    public void Should_Compare_Current_vs_Proper_Hex_Adjacency()
    {
        // Systematic comparison of current vs proper hex adjacency
        var logic = new MovementValidationLogic();
        
        GD.Print("=== SYSTEMATIC HEX ADJACENCY COMPARISON ===");
        
        // Test a grid of positions to see differences
        for (int x = 0; x <= 3; x++)
        {
            for (int y = 0; y <= 3; y++)
            {
                var pos = new Vector2I(x, y);
                var currentAdjacents = logic.GetAdjacentPositions(pos).ToList();
                var properAdjacents = GetProperHexAdjacents(pos);
                
                // Find differences
                var onlyInCurrent = currentAdjacents.Except(properAdjacents).ToList();
                var onlyInProper = properAdjacents.Except(currentAdjacents).ToList();
                
                if (onlyInCurrent.Count > 0 || onlyInProper.Count > 0)
                {
                    GD.Print($"\n{pos} (column {(x % 2 == 0 ? "even" : "odd")}):");
                    GD.Print($"  Current: {string.Join(", ", currentAdjacents)}");
                    GD.Print($"  Proper:  {string.Join(", ", properAdjacents)}");
                    
                    if (onlyInCurrent.Count > 0)
                    {
                        GD.Print($"  🚨 False adjacencies: {string.Join(", ", onlyInCurrent)}");
                    }
                    
                    if (onlyInProper.Count > 0)
                    {
                        GD.Print($"  ❌ Missing adjacencies: {string.Join(", ", onlyInProper)}");
                    }
                }
            }
        }
        
        GD.Print("\n✅ Hex adjacency comparison completed");
    }
} 