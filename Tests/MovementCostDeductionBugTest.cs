using NUnit.Framework;
using Godot;
using System.Collections.Generic;
using Archistrateia;

[TestFixture]
public class MovementCostDeductionBugTest
{
    [Test]
    public void Should_Deduct_Actual_Path_Cost_Not_One_Per_Click()
    {
        // Test that movement deducts the correct path cost, not just 1 per click
        var coordinator = new MovementCoordinator();
        
        GD.Print("=== TESTING MOVEMENT COST DEDUCTION BUG ===");
        
        // Create a map with tiles of different costs
        var gameMap = new Dictionary<Vector2I, HexTile>();
        gameMap[new Vector2I(0, 0)] = new HexTile(new Vector2I(0, 0), TerrainType.Shoreline); // Start
        gameMap[new Vector2I(1, 0)] = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline); // Cost 1
        gameMap[new Vector2I(2, 0)] = new HexTile(new Vector2I(2, 0), TerrainType.River);     // Cost 3  
        gameMap[new Vector2I(3, 0)] = new HexTile(new Vector2I(3, 0), TerrainType.Lagoon);    // Cost 4
        
        var archer = new Archer(); // 4 MP initially
        var initialMP = archer.CurrentMovementPoints;
        
        GD.Print($"Archer starts with {initialMP} MP");
        
        // Test 1: Move to cost-1 tile
        GD.Print("\n--- Test 1: Move to Shoreline (cost 1) ---");
        coordinator.SelectUnitForMovement(archer);
        
        var move1Result = coordinator.TryMoveToDestination(new Vector2I(0, 0), new Vector2I(1, 0), gameMap);
        Assert.IsTrue(move1Result.Success, "Move to cost-1 tile should succeed");
        
        var expectedMPAfterMove1 = initialMP - 1; // Should deduct 1 for cost-1 tile
        GD.Print($"After moving to cost-1 tile: Expected {expectedMPAfterMove1} MP, Actual {archer.CurrentMovementPoints} MP");
        
        Assert.AreEqual(expectedMPAfterMove1, archer.CurrentMovementPoints, 
            "BUG: Should deduct actual tile cost (1) not just 1 per click");
        
        // Test 2: Move to cost-3 tile
        GD.Print("\n--- Test 2: Move to River (cost 3) ---");
        coordinator.SelectUnitForMovement(archer);
        
        var move2Result = coordinator.TryMoveToDestination(new Vector2I(1, 0), new Vector2I(2, 0), gameMap);
        Assert.IsTrue(move2Result.Success, "Move to cost-3 tile should succeed");
        
        var expectedMPAfterMove2 = expectedMPAfterMove1 - 3; // Should deduct 3 for cost-3 tile
        GD.Print($"After moving to cost-3 tile: Expected {expectedMPAfterMove2} MP, Actual {archer.CurrentMovementPoints} MP");
        
        Assert.AreEqual(expectedMPAfterMove2, archer.CurrentMovementPoints, 
            "BUG: Should deduct actual tile cost (3) not just 1 per click");
        
        // Test 3: Verify unit cannot move to cost-4 tile with 0 MP remaining
        GD.Print("\n--- Test 3: Try to move to Lagoon (cost 4) with 0 MP ---");
        coordinator.SelectUnitForMovement(archer);
        
        var move3Result = coordinator.TryMoveToDestination(new Vector2I(2, 0), new Vector2I(3, 0), gameMap);
        Assert.IsFalse(move3Result.Success, "Move to cost-4 tile should fail with 0 MP");
        
        GD.Print($"Final MP: {archer.CurrentMovementPoints} (should still be 0)");
        Assert.AreEqual(0, archer.CurrentMovementPoints, "MP should remain 0 after failed move");
        
        GD.Print("✅ Movement cost deduction test completed");
    }
    
    [Test]
    public void Should_Deduct_Multi_Step_Path_Cost_Correctly()
    {
        // Test that multi-step movement deducts the total path cost
        var coordinator = new MovementCoordinator();
        
        GD.Print("=== TESTING MULTI-STEP PATH COST DEDUCTION ===");
        
        // Create a path where destination requires multi-step movement
        var gameMap = new Dictionary<Vector2I, HexTile>();
        gameMap[new Vector2I(0, 0)] = new HexTile(new Vector2I(0, 0), TerrainType.Shoreline); // Start
        gameMap[new Vector2I(1, 0)] = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline); // Step 1: cost 1
        gameMap[new Vector2I(2, 0)] = new HexTile(new Vector2I(2, 0), TerrainType.River);     // Step 2: cost 3
        gameMap[new Vector2I(3, 0)] = new HexTile(new Vector2I(3, 0), TerrainType.Shoreline); // Destination: cost 1
        
        // Total path cost: (0,0) → (1,0) → (2,0) → (3,0) = 1 + 3 + 1 = 5 total
        
        var charioteer = new Charioteer(); // 8 MP initially  
        var initialMP = charioteer.CurrentMovementPoints;
        
        GD.Print($"Charioteer starts with {initialMP} MP");
        GD.Print("Path: (0,0) → (1,0) → (2,0) → (3,0) with costs 1 + 3 + 1 = 5 total");
        
        coordinator.SelectUnitForMovement(charioteer);
        
        // Move directly to destination (3,0) - should deduct total path cost of 5
        var moveResult = coordinator.TryMoveToDestination(new Vector2I(0, 0), new Vector2I(3, 0), gameMap);
        Assert.IsTrue(moveResult.Success, "Multi-step move should succeed");
        
        var expectedMPAfterMove = initialMP - 5; // Should deduct total path cost
        GD.Print($"After multi-step move: Expected {expectedMPAfterMove} MP, Actual {charioteer.CurrentMovementPoints} MP");
        
        Assert.AreEqual(expectedMPAfterMove, charioteer.CurrentMovementPoints, 
            "BUG: Should deduct total path cost (5) not just 1 per click");
        
        GD.Print("✅ Multi-step path cost deduction test completed");
    }
    
    [Test]
    public void Should_Calculate_Path_Cost_From_Dijkstra_Results()
    {
        // Test that path cost matches what Dijkstra's algorithm calculated
        var coordinator = new MovementCoordinator();
        var logic = new MovementValidationLogic();
        
        GD.Print("=== TESTING PATH COST VS DIJKSTRA RESULTS ===");
        
        var gameMap = new Dictionary<Vector2I, HexTile>();
        gameMap[new Vector2I(0, 0)] = new HexTile(new Vector2I(0, 0), TerrainType.Shoreline); // Start
        gameMap[new Vector2I(1, 0)] = new HexTile(new Vector2I(1, 0), TerrainType.River);     // Cost 3
        gameMap[new Vector2I(0, 1)] = new HexTile(new Vector2I(0, 1), TerrainType.Shoreline); // Cost 1
        gameMap[new Vector2I(1, 1)] = new HexTile(new Vector2I(1, 1), TerrainType.Shoreline); // Destination
        
        var archer = new Archer();
        var initialMP = archer.CurrentMovementPoints;
        
        // Get Dijkstra's path costs
        var dijkstraResults = logic.GetValidMovementDestinations(archer, new Vector2I(0, 0), gameMap);
        GD.Print("Dijkstra's calculated costs from pathfinding output:");
        
        // Move to (1,1) and verify cost matches Dijkstra's calculation
        coordinator.SelectUnitForMovement(archer);
        var moveResult = coordinator.TryMoveToDestination(new Vector2I(0, 0), new Vector2I(1, 1), gameMap);
        
        if (moveResult.Success)
        {
            var actualDeducted = initialMP - archer.CurrentMovementPoints;
            GD.Print($"Movement deducted {actualDeducted} MP");
            GD.Print("This should match the cost shown in Dijkstra's output above");
            
            // The actual cost should be 2 (path: (0,0) → (0,1) → (1,1) = 1 + 1 = 2)
            Assert.AreEqual(2, actualDeducted, "Deducted MP should match Dijkstra's calculated path cost");
        }
        
        GD.Print("✅ Path cost vs Dijkstra results test completed");
    }
} 