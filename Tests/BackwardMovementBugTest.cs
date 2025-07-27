using NUnit.Framework;
using Godot;
using System.Collections.Generic;

[TestFixture]
public class BackwardMovementBugTest
{
    [Test]
    public void Should_Allow_Unit_To_Move_Backwards_After_Forward_Move()
    {
        // Test scenario: Unit moves forward, then should be able to move back
        var coordinator = new MovementCoordinator();
        var gameMap = new Dictionary<Vector2I, HexTile>();
        
        // Create a simple straight line: A -> B -> C
        var positionA = new Vector2I(0, 0);
        var positionB = new Vector2I(1, 0);  // East of A
        var positionC = new Vector2I(2, 0);  // East of B
        
        gameMap[positionA] = new HexTile(positionA, TerrainType.Shoreline); // Cost 1
        gameMap[positionB] = new HexTile(positionB, TerrainType.Shoreline); // Cost 1
        gameMap[positionC] = new HexTile(positionC, TerrainType.Shoreline); // Cost 1
        
        var archer = new Archer(); // 4 MP
        GD.Print($"=== BACKWARD MOVEMENT TEST ===");
        GD.Print($"Archer starts with {archer.CurrentMovementPoints} MP");
        
        // Place archer at position A
        gameMap[positionA].PlaceUnit(archer);
        coordinator.SelectUnitForMovement(archer);
        
        // === STEP 1: Move A -> B ===
        GD.Print($"\n--- STEP 1: Moving from A{positionA} to B{positionB} ---");
        var validDestinationsFromA = coordinator.GetValidDestinations(positionA, gameMap);
        
        GD.Print($"Valid destinations from A: {validDestinationsFromA.Count}");
        foreach (var dest in validDestinationsFromA)
        {
            GD.Print($"  {dest}");
        }
        
        Assert.Contains(positionB, validDestinationsFromA, "Should be able to move from A to B");
        
        var moveResult1 = coordinator.TryMoveToDestination(positionA, positionB, gameMap);
        Assert.IsTrue(moveResult1.Success, $"Move A->B should succeed: {moveResult1.ErrorMessage}");
        
        GD.Print($"After move A->B: Archer has {archer.CurrentMovementPoints} MP remaining");
        
        // === STEP 2: Try to move B -> A (backwards) ===
        GD.Print($"\n--- STEP 2: Attempting to move backwards from B{positionB} to A{positionA} ---");
        
        // Re-select the unit (mimicking what happens in the UI)
        coordinator.SelectUnitForMovement(archer);
        var validDestinationsFromB = coordinator.GetValidDestinations(positionB, gameMap);
        
        GD.Print($"Valid destinations from B: {validDestinationsFromB.Count}");
        foreach (var dest in validDestinationsFromB)
        {
            GD.Print($"  {dest}");
        }
        
        // The critical test: Can the unit move backwards?
        Assert.Contains(positionA, validDestinationsFromB, 
            "BUG DETECTED: Unit should be able to move backwards from B to A");
        
        var moveResult2 = coordinator.TryMoveToDestination(positionB, positionA, gameMap);
        Assert.IsTrue(moveResult2.Success, 
            $"BUG DETECTED: Backward move B->A should succeed: {moveResult2.ErrorMessage}");
        
        GD.Print($"After move B->A: Archer has {archer.CurrentMovementPoints} MP remaining");
        
        // === STEP 3: Verify unit can move forward again ===
        GD.Print($"\n--- STEP 3: Verifying unit can move forward again A{positionA} to B{positionB} ---");
        
        coordinator.SelectUnitForMovement(archer);
        var validDestinationsFromAAgain = coordinator.GetValidDestinations(positionA, gameMap);
        
        Assert.Contains(positionB, validDestinationsFromAAgain, 
            "Unit should be able to move forward again after moving backwards");
        
        GD.Print($"✅ Backward movement test completed successfully");
    }
    
    [Test]
    public void Should_Allow_Unit_To_Backtrack_Multi_Step_Path()
    {
        // Test a more complex scenario: A -> B -> C, then C -> B -> A
        var coordinator = new MovementCoordinator();
        var gameMap = new Dictionary<Vector2I, HexTile>();
        
        // Create a path: A -> B -> C -> D
        var positionA = new Vector2I(0, 0);
        var positionB = new Vector2I(1, 0);
        var positionC = new Vector2I(2, 0);
        var positionD = new Vector2I(3, 0);
        
        gameMap[positionA] = new HexTile(positionA, TerrainType.Shoreline); // Cost 1
        gameMap[positionB] = new HexTile(positionB, TerrainType.Shoreline); // Cost 1
        gameMap[positionC] = new HexTile(positionC, TerrainType.Shoreline); // Cost 1
        gameMap[positionD] = new HexTile(positionD, TerrainType.Shoreline); // Cost 1
        
        var charioteer = new Charioteer(); // 8 MP
        GD.Print($"\n=== MULTI-STEP BACKTRACK TEST ===");
        GD.Print($"Charioteer starts with {charioteer.CurrentMovementPoints} MP");
        
        gameMap[positionA].PlaceUnit(charioteer);
        coordinator.SelectUnitForMovement(charioteer);
        
        // Move A -> B -> C
        var moveAB = coordinator.TryMoveToDestination(positionA, positionB, gameMap);
        Assert.IsTrue(moveAB.Success, "Move A->B should succeed");
        GD.Print($"After A->B: {charioteer.CurrentMovementPoints} MP remaining");
        
        coordinator.SelectUnitForMovement(charioteer);
        var moveBC = coordinator.TryMoveToDestination(positionB, positionC, gameMap);
        Assert.IsTrue(moveBC.Success, "Move B->C should succeed");
        GD.Print($"After B->C: {charioteer.CurrentMovementPoints} MP remaining");
        
        // Now try to backtrack: C -> B
        coordinator.SelectUnitForMovement(charioteer);
        var validFromC = coordinator.GetValidDestinations(positionC, gameMap);
        
        GD.Print($"Valid destinations from C: {validFromC.Count}");
        foreach (var dest in validFromC)
        {
            GD.Print($"  {dest}");
        }
        
        Assert.Contains(positionB, validFromC, 
            "BUG: Should be able to backtrack from C to B");
        
        var moveCB = coordinator.TryMoveToDestination(positionC, positionB, gameMap);
        Assert.IsTrue(moveCB.Success, 
            $"BUG: Backtrack C->B should succeed: {moveCB.ErrorMessage}");
        
        // Then B -> A
        coordinator.SelectUnitForMovement(charioteer);
        var validFromBAgain = coordinator.GetValidDestinations(positionB, gameMap);
        
        Assert.Contains(positionA, validFromBAgain, 
            "BUG: Should be able to complete backtrack from B to A");
        
        var moveBA = coordinator.TryMoveToDestination(positionB, positionA, gameMap);
        Assert.IsTrue(moveBA.Success, 
            $"BUG: Final backtrack B->A should succeed: {moveBA.ErrorMessage}");
        
        GD.Print($"✅ Multi-step backtrack completed. Final MP: {charioteer.CurrentMovementPoints}");
    }
} 