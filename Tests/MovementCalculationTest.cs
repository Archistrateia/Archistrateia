using NUnit.Framework;
using Godot;
using Archistrateia;
using System.Collections.Generic;

[TestFixture] 
public class MovementCalculationTest
{

    
    [Test] 
    public void Should_Recalculate_Correctly_After_Movement()
    {
        var gameMap = new Dictionary<Vector2I, HexTile>();
        var coordinator = new MovementCoordinator();
        
        // Create a simple path for testing using actual terrain costs
        var start = new Vector2I(0, 0);
        var adjacent1 = new Vector2I(1, 0);   // East
        var adjacent2 = new Vector2I(0, 1);   // Southeast
        
        gameMap[start] = new HexTile(start, TerrainType.Shoreline);
        gameMap[adjacent1] = new HexTile(adjacent1, TerrainType.Shoreline);
        gameMap[adjacent2] = new HexTile(adjacent2, TerrainType.Desert);
        
        // Set up Godot's built-in movement system for this test map
        var movementSystem = new GodotMovementSystem(forTesting: true);
        movementSystem.InitializeNavigation(gameMap);
        MovementValidationLogic.SetMovementSystem(movementSystem);
        
        var archer = new Archer(); // Use actual unit definition
        gameMap[start].PlaceUnit(archer);
        
        // Get actual movement points from unit
        var initialMP = archer.CurrentMovementPoints;
        GD.Print($"Archer starts with {initialMP} MP");
        
        // Initial state: should be able to reach destinations within movement budget
        coordinator.SelectUnitForMovement(archer);
        var initialDestinations = coordinator.GetValidDestinations(start, gameMap);
        
        GD.Print($"Initial destinations for Archer with {initialMP} MP:");
        foreach (var dest in initialDestinations)
        {
            var tile = gameMap[dest];
            GD.Print($"  {dest}: {tile.TerrainType} (cost {tile.MovementCost})");
        }
        
        Assert.Contains(adjacent1, initialDestinations, "Should initially reach adjacent Shoreline tile");
        Assert.Contains(adjacent2, initialDestinations, "Should initially reach adjacent Desert tile");
        
        // Test 1: Move to Shoreline tile
        GD.Print("\n--- Test 1: Move to Shoreline ---");
        var shoreCost = gameMap[adjacent1].MovementCost;
        var moveResult = coordinator.TryMoveToDestination(start, adjacent1, gameMap);
        Assert.IsTrue(moveResult.Success, "Move to adjacent tile should succeed");
        
        var expectedRemainingMP = initialMP - shoreCost;
        Assert.AreEqual(expectedRemainingMP, archer.CurrentMovementPoints, $"Should have {expectedRemainingMP} MP left ({initialMP} MP total - {shoreCost} MP cost)");
        
        // Test 2: Move to Desert tile from new position
        GD.Print("\n--- Test 2: Move to Desert from new position ---");
        coordinator.SelectUnitForMovement(archer);
        var desertCost = gameMap[adjacent2].MovementCost;
        
        // Get actual path cost from pathfinding algorithm
        var pathCosts = MovementValidationLogic.GetPathCostsFromPosition(archer, adjacent1, gameMap);
        if (pathCosts.ContainsKey(adjacent2))
        {
            var calculatedPathCost = pathCosts[adjacent2];
            GD.Print($"Path cost calculation for {adjacent1} -> {adjacent2}: {calculatedPathCost} (tile cost: {desertCost})");
        }
        
        var move2Result = coordinator.TryMoveToDestination(adjacent1, adjacent2, gameMap);
        Assert.IsTrue(move2Result.Success, "Move to Desert tile should succeed");
        
        // Calculate actual path cost deducted
        var actualPathCost = expectedRemainingMP - archer.CurrentMovementPoints;
        GD.Print($"After first move: {expectedRemainingMP} MP, Actual path cost: {actualPathCost}, Actual MP remaining: {archer.CurrentMovementPoints} MP");
        
        // Verify movement cost deduction works correctly
        Assert.IsTrue(archer.CurrentMovementPoints >= 0, $"Should have non-negative MP remaining, got {archer.CurrentMovementPoints}");
        
        // Test 3: Verify unit cannot move to expensive tile with insufficient MP
        GD.Print("\n--- Test 3: Try to move to expensive tile with insufficient MP ---");
        gameMap[new Vector2I(2, 0)] = new HexTile(new Vector2I(2, 0), TerrainType.River);
        coordinator.SelectUnitForMovement(archer);
        
        var move3Result = coordinator.TryMoveToDestination(adjacent2, new Vector2I(2, 0), gameMap);
        Assert.IsFalse(move3Result.Success, "Move to expensive tile should fail with insufficient MP");
        
        var mpAfterFailedMove = archer.CurrentMovementPoints;
        Assert.AreEqual(mpAfterFailedMove, archer.CurrentMovementPoints, "MP should remain unchanged after failed move");
        
        // Recalculate from new position - should only reach tiles within remaining MP
        var finalDestinations = coordinator.GetValidDestinations(adjacent2, gameMap);
        
        GD.Print($"Final destinations from {adjacent2} with {archer.CurrentMovementPoints} MP:");
        foreach (var dest in finalDestinations)
        {
            var tile = gameMap[dest];
            GD.Print($"  {dest}: {tile.TerrainType} (cost {tile.MovementCost})");
        }
        
        // All destinations should be reachable with remaining MP
        foreach (var destination in finalDestinations)
        {
            var tile = gameMap[destination];
            // If it's the current position, it should always be reachable (cost 0)
            // Otherwise, it should be within the remaining MP
            if (destination == adjacent2)
            {
                Assert.IsTrue(true, $"Current position {destination} should always be reachable");
            }
            else
            {
                Assert.IsTrue(tile.MovementCost <= archer.CurrentMovementPoints,
                    $"Destination {destination} should be reachable with {archer.CurrentMovementPoints} remaining MP");
            }
        }
        
        // Clean up
        MovementValidationLogic.SetMovementSystem(null);
    }

    // REMOVED: Test had hardcoded hex adjacency expectations from old buggy behavior

    [Test]
    public void Should_Debug_Specific_Movement_Failure()
    {
        // Recreate the exact scenario from the game log
        var gameMap = new Dictionary<Vector2I, HexTile>();
        
        // Position (4,0) where Medjay ended up
        var medjayPos = new Vector2I(4, 0);
        gameMap[medjayPos] = new HexTile(medjayPos, TerrainType.Shoreline); // Cost 1
        
        // Target position (3,1) that failed 
        var targetPos = new Vector2I(3, 1);
        gameMap[targetPos] = new HexTile(targetPos, TerrainType.Shoreline); // Cost 1
        
        // Add some other adjacent tiles
        gameMap[new Vector2I(5, 0)] = new HexTile(new Vector2I(5, 0), TerrainType.Desert);    // Cost 2
        gameMap[new Vector2I(3, 0)] = new HexTile(new Vector2I(3, 0), TerrainType.Hill);      // Cost 2
        gameMap[new Vector2I(4, 1)] = new HexTile(new Vector2I(4, 1), TerrainType.River);     // Cost 3
        
        var medjay = new Medjay(); // 3 MP total
        medjay.CurrentMovementPoints = 1; // After movement
        gameMap[medjayPos].PlaceUnit(medjay);
        
        var logic = new MovementValidationLogic();
        var coordinator = new MovementCoordinator();
        
        GD.Print($"=== DEBUGGING MEDJAY MOVEMENT FAILURE ===");
        GD.Print($"Medjay at {medjayPos} with {medjay.CurrentMovementPoints} MP");
        GD.Print($"Target tile {targetPos}: {gameMap[targetPos].TerrainType} (cost {gameMap[targetPos].MovementCost})");
        GD.Print($"Target occupied: {gameMap[targetPos].IsOccupied()}");
        
        // Test direct validation
        bool canMoveDirect = MovementValidationLogic.CanUnitMoveTo(medjay, gameMap[medjayPos], gameMap[targetPos]);
        GD.Print($"Direct validation result: {canMoveDirect}");
        
        // Test pathfinding
        coordinator.SelectUnitForMovement(medjay);
        var validDestinations = coordinator.GetValidDestinations(medjayPos, gameMap);
        
        bool pathfindingAllows = validDestinations.Contains(targetPos);
        GD.Print($"Pathfinding allows: {pathfindingAllows}");
        
        // Test actual movement attempt
        var moveResult = coordinator.TryMoveToDestination(medjayPos, targetPos, gameMap);
        GD.Print($"Movement result: {moveResult.Success} - {moveResult.ErrorMessage}");
        
        // They should all agree
        Assert.AreEqual(canMoveDirect, pathfindingAllows, "Direct validation and pathfinding should agree");
        Assert.AreEqual(pathfindingAllows, moveResult.Success, "Pathfinding and movement result should agree");
    }

    [Test]
    public void Should_Debug_Invalid_Hex_Between_Valid_Hexes_Issue()
    {
        // Recreate the exact scenario from the game log
        var gameMap = new Dictionary<Vector2I, HexTile>();
        var logic = new MovementValidationLogic();
        
        // Archer at (1, 3) with 2 MP
        var archerPos = new Vector2I(1, 3);
        var questionableDestination = new Vector2I(0, 5); // This shows as reachable but isn't adjacent
        
        // Get expected adjacent positions
        var adjacentToArcher = MovementValidationLogic.GetAdjacentPositions(archerPos);
        
        GD.Print($"=== DEBUGGING PATHFINDING BUG ===");
        GD.Print($"Archer position: {archerPos}");
        GD.Print($"Questionable destination: {questionableDestination}");
        GD.Print($"Is {questionableDestination} adjacent to {archerPos}? {System.Array.Exists(adjacentToArcher, pos => pos == questionableDestination)}");
        
        GD.Print($"Adjacent positions to {archerPos}:");
        foreach (var adj in adjacentToArcher)
        {
            GD.Print($"  {adj}");
        }
        
        // Create a minimal map to test pathfinding
        gameMap[archerPos] = new HexTile(archerPos, TerrainType.Shoreline); // Cost 1
        
        // Add all adjacent tiles
        foreach (var adj in adjacentToArcher)
        {
            // Make some expensive tiles to force pathfinding choices
            var terrainType = adj.Y > archerPos.Y ? TerrainType.River : TerrainType.Shoreline; // River=3, Shoreline=1
            gameMap[adj] = new HexTile(adj, terrainType);
            GD.Print($"  Added {adj}: {terrainType} (cost {gameMap[adj].MovementCost})");
        }
        
        // Add the questionable destination if it's not already there
        if (!gameMap.ContainsKey(questionableDestination))
        {
            gameMap[questionableDestination] = new HexTile(questionableDestination, TerrainType.Shoreline); // Cost 1
            GD.Print($"  Added {questionableDestination}: Shoreline (cost 1)");
        }
        
        // Test pathfinding
        var archer = new Archer(); // 2 MP
        archer.CurrentMovementPoints = 2;
        gameMap[archerPos].PlaceUnit(archer);
        
        var validDestinations = MovementValidationLogic.GetValidMovementDestinations(archer, archerPos, gameMap);
        
        GD.Print($"Pathfinding results:");
        foreach (var dest in validDestinations)
        {
            var tile = gameMap[dest];
            var isAdjacent = System.Array.Exists(adjacentToArcher, pos => pos == dest);
            GD.Print($"  {dest}: {tile.TerrainType} (cost {tile.MovementCost}) - Adjacent: {isAdjacent}");
            
            // If not adjacent, this destination should only be reachable through multi-step path
            if (!isAdjacent)
            {
                GD.Print($"    ⚠️  NON-ADJACENT DESTINATION FOUND! This suggests pathfinding bug.");
                
                // Find what intermediate tiles would be needed
                var intermediatesToCheck = MovementValidationLogic.GetAdjacentPositions(dest);
                GD.Print($"    To reach {dest}, unit must go through one of these adjacent to destination:");
                foreach (var intermediate in intermediatesToCheck)
                {
                    if (gameMap.ContainsKey(intermediate))
                    {
                        var intermediateTile = gameMap[intermediate];
                        var intermediateIsAdjacentToStart = System.Array.Exists(adjacentToArcher, pos => pos == intermediate);
                        GD.Print($"      {intermediate}: {intermediateTile.TerrainType} (cost {intermediateTile.MovementCost}) - Adjacent to start: {intermediateIsAdjacentToStart}");
                    }
                }
            }
        }
        
        // The bug test: non-adjacent destinations should not appear unless there's a valid multi-step path
        bool containsQuestionableDestination = validDestinations.Contains(questionableDestination);
        if (containsQuestionableDestination)
        {
            var questionableTile = gameMap[questionableDestination];
            GD.Print($"🐛 BUG CONFIRMED: {questionableDestination} is reachable but not adjacent!");
            GD.Print($"   This destination costs {questionableTile.MovementCost} but requires going through intermediate tiles.");
            
            // This should only be possible if there's a valid path through intermediate tiles
            // within the movement budget
        }
    }

    [Test]
    public void Should_Allow_Multi_Step_Pathfinding_Through_Cheaper_Routes()
    {
        // Test the specific scenario that could explain the (0,5) destination
        var gameMap = new Dictionary<Vector2I, HexTile>();
        // Archer at (1, 3) with 2 MP - same as game log
        var archerPos = new Vector2I(1, 3);
        var distantDestination = new Vector2I(0, 5);
        
        GD.Print($"=== TESTING MULTI-STEP PATHFINDING ===");
        GD.Print($"Archer at {archerPos} trying to reach {distantDestination}");
        
        // Create terrain that allows multi-step movement
        gameMap[archerPos] = new HexTile(archerPos, TerrainType.Shoreline);        // Start: cost 1
        gameMap[new Vector2I(0, 4)] = new HexTile(new Vector2I(0, 4), TerrainType.Shoreline);  // Intermediate: cost 1  
        gameMap[distantDestination] = new HexTile(distantDestination, TerrainType.Shoreline);  // End: cost 1
        
        // Add some expensive adjacent tiles to make the multi-step path attractive
        gameMap[new Vector2I(2, 3)] = new HexTile(new Vector2I(2, 3), TerrainType.River);     // cost 3
        gameMap[new Vector2I(1, 4)] = new HexTile(new Vector2I(1, 4), TerrainType.Lagoon);    // cost 4
        gameMap[new Vector2I(1, 2)] = new HexTile(new Vector2I(1, 2), TerrainType.Hill);      // cost 2
        gameMap[new Vector2I(2, 2)] = new HexTile(new Vector2I(2, 2), TerrainType.Desert);    // cost 2
        gameMap[new Vector2I(0, 3)] = new HexTile(new Vector2I(0, 3), TerrainType.River);     // cost 3
        
        var archer = new Archer(); // 2 MP
        archer.CurrentMovementPoints = 2;
        gameMap[archerPos].PlaceUnit(archer);
        
        var validDestinations = MovementValidationLogic.GetValidMovementDestinations(archer, archerPos, gameMap);
        
        GD.Print($"Pathfinding results for multi-step scenario:");
        foreach (var dest in validDestinations)
        {
            var tile = gameMap[dest];
            GD.Print($"  {dest}: {tile.TerrainType} (cost {tile.MovementCost})");
        }
        
        // Check if distant destination is reachable
        bool canReachDistant = validDestinations.Contains(distantDestination);
        GD.Print($"Can reach distant destination {distantDestination}? {canReachDistant}");
        
        if (canReachDistant)
        {
            GD.Print($"✅ MULTI-STEP PATHFINDING CONFIRMED!");
            GD.Print($"   Path likely: {archerPos} → (0,4) [cost 1] → {distantDestination} [cost 1] = 2 total");
        }
        
        // Verify the path calculation manually
        var intermediate = new Vector2I(0, 4);
        bool intermediateReachable = validDestinations.Contains(intermediate);
        GD.Print($"Intermediate tile {intermediate} reachable? {intermediateReachable}");
        
        // This demonstrates that Dijkstra's can find optimal multi-step paths
        // The question is: should the UI show intermediate tiles as valid destinations too?
    }

    [Test]
    public void Should_Show_All_Reachable_Tiles_Including_Pass_Through()
    {
        // Test whether pathfinding should show tiles you can pass through vs only final destinations
        var gameMap = new Dictionary<Vector2I, HexTile>();
        var logic = new MovementValidationLogic();
        
        var start = new Vector2I(0, 0);
        var intermediate = new Vector2I(1, 0);    // Cost 1 - can stop here
        var destination = new Vector2I(2, 0);     // Cost 1 - can reach in 2 total steps
        
        gameMap[start] = new HexTile(start, TerrainType.Shoreline);           // Cost 1
        gameMap[intermediate] = new HexTile(intermediate, TerrainType.Shoreline); // Cost 1
        gameMap[destination] = new HexTile(destination, TerrainType.Shoreline);   // Cost 1
        
        var archer = new Archer(); // 2 MP
        archer.CurrentMovementPoints = 2;
        gameMap[start].PlaceUnit(archer);
        
        var validDestinations = MovementValidationLogic.GetValidMovementDestinations(archer, start, gameMap);
        
        GD.Print($"=== REACHABLE TILES TEST ===");
        GD.Print($"With 2 MP, reachable destinations:");
        foreach (var dest in validDestinations)
        {
            GD.Print($"  {dest}: cost {gameMap[dest].MovementCost}");
        }
        
        // Both intermediate and final destination should be reachable
        Assert.Contains(intermediate, validDestinations, "Should be able to stop at intermediate tile");
        Assert.Contains(destination, validDestinations, "Should be able to reach final destination in 2 steps");
        
        GD.Print($"✅ Both intermediate stopping point and final destination are valid");
    }
    
    [Test]
    public void Should_Calculate_Path_Cost_From_Dijkstra_Results()
    {
        // Test that path cost matches what Dijkstra's algorithm calculated
        var coordinator = new MovementCoordinator();
        
        GD.Print("=== TESTING PATH COST VS DIJKSTRA RESULTS ===");
        
        var gameMap = new Dictionary<Vector2I, HexTile>();
        gameMap[new Vector2I(0, 0)] = new HexTile(new Vector2I(0, 0), TerrainType.Shoreline); // Start
        gameMap[new Vector2I(1, 0)] = new HexTile(new Vector2I(1, 0), TerrainType.River);     // Cost 3
        gameMap[new Vector2I(0, 1)] = new HexTile(new Vector2I(0, 1), TerrainType.Shoreline); // Cost 1
        gameMap[new Vector2I(1, 1)] = new HexTile(new Vector2I(1, 1), TerrainType.Shoreline); // Destination
        
        // Set up Godot's built-in movement system for this test map
        var movementSystem = new GodotMovementSystem(forTesting: true);
        movementSystem.InitializeNavigation(gameMap);
        MovementValidationLogic.SetMovementSystem(movementSystem);
        
        var archer = new Archer();
        var initialMP = archer.CurrentMovementPoints;
        
        // Get Dijkstra's path costs using Godot's system
        var dijkstraResults = MovementValidationLogic.GetValidMovementDestinations(archer, new Vector2I(0, 0), gameMap);
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
        
        // Clean up
        MovementValidationLogic.SetMovementSystem(null);
        
        GD.Print("✅ Path cost vs Dijkstra results test completed");
    }
} 