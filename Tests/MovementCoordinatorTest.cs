using Godot;
using NUnit.Framework;
using System.Collections.Generic;
using Archistrateia;

[TestFixture]
public partial class MovementCoordinatorTest : Node
{
    [Test]
    public void Should_Handle_Unit_Selection_For_Movement()
    {
        var coordinator = new MovementCoordinator();
        var gameMap = CreateTestMap();
        
        // Set up Godot's built-in movement system for this test map
        var movementSystem = new GodotMovementSystem(forTesting: true);
        movementSystem.InitializeNavigation(gameMap);
        MovementValidationLogic.SetMovementSystem(movementSystem);
        
        var nakhtu = new Nakhtu();
        
        coordinator.SelectUnitForMovement(nakhtu);
        
        Assert.AreEqual(nakhtu, coordinator.GetSelectedUnit());
        Assert.IsTrue(coordinator.IsAwaitingDestination());
    }

    [Test]
    public void Should_Clear_Selection_When_Deselecting()
    {
        var coordinator = new MovementCoordinator();
        var gameMap = CreateTestMap();
        
        // Set up Godot's built-in movement system for this test map
        var movementSystem = new GodotMovementSystem(forTesting: true);
        movementSystem.InitializeNavigation(gameMap);
        MovementValidationLogic.SetMovementSystem(movementSystem);
        
        var nakhtu = new Nakhtu(); // Has 4 movement points (doubled)
        
        coordinator.SelectUnitForMovement(nakhtu);
        coordinator.ClearSelection();
        
        Assert.IsNull(coordinator.GetSelectedUnit());
        Assert.IsFalse(coordinator.IsAwaitingDestination());
    }

    [Test]
    public void Should_Calculate_Valid_Destinations_For_Selected_Unit()
    {
        var coordinator = new MovementCoordinator();
        var gameMap = CreateTestMap();
        var nakhtu = new Nakhtu();
        var currentPosition = new Vector2I(1, 1);
        
        coordinator.SelectUnitForMovement(nakhtu);
        var validDestinations = coordinator.GetValidDestinations(currentPosition, gameMap);
        
        Assert.IsNotNull(validDestinations);
        Assert.IsTrue(validDestinations.Count > 0, "Should find valid destinations");
    }

    [Test]
    public void Should_Execute_Valid_Movement()
    {
        var coordinator = new MovementCoordinator();
        var gameMap = CreateTestMap();
        var nakhtu = new Nakhtu();
        var fromPosition = new Vector2I(1, 1);
        var toPosition = new Vector2I(1, 2);
        
        // Place unit on starting tile without consuming movement
        gameMap[fromPosition].PlaceUnit(nakhtu);
        
        coordinator.SelectUnitForMovement(nakhtu);
        var moveResult = coordinator.TryMoveToDestination(fromPosition, toPosition, gameMap);
        
        Assert.IsTrue(moveResult.Success, "Movement should succeed");
        Assert.AreEqual(toPosition, moveResult.NewPosition);
        // With doubled movement points: Nakhtu has 4 MP, Desert costs 2 MP, leaving 2 MP
        // Selection should remain active since unit still has movement points
        Assert.IsTrue(coordinator.IsAwaitingDestination(), "Should still await destination with 2 MP remaining");
    }

    [Test]
    public void Should_Reject_Invalid_Movement()
    {
        var coordinator = new MovementCoordinator();
        var gameMap = CreateTestMap();
        var nakhtu = new Nakhtu();
        nakhtu.CurrentMovementPoints = 1; // Only 1 movement point
        var fromPosition = new Vector2I(1, 1);
        var toPosition = new Vector2I(1, 2); // Costs 2 movement (desert terrain)
        
        // Place unit on starting tile without consuming movement
        gameMap[fromPosition].PlaceUnit(nakhtu);
        
        coordinator.SelectUnitForMovement(nakhtu);
        var moveResult = coordinator.TryMoveToDestination(fromPosition, toPosition, gameMap);
        
        Assert.IsFalse(moveResult.Success, "Movement should fail");
        Assert.IsNotNull(moveResult.ErrorMessage);
        Assert.IsTrue(coordinator.IsAwaitingDestination(), "Should still await valid destination");
    }

    [Test]
    public void Should_Handle_Clicking_Empty_Tile_When_Unit_Selected()
    {
        var coordinator = new MovementCoordinator();
        var gameMap = CreateTestMap();
        var nakhtu = new Nakhtu();
        var unitPosition = new Vector2I(1, 1);
        var clickPosition = new Vector2I(1, 2);
        
        gameMap[unitPosition].PlaceUnit(nakhtu);
        coordinator.SelectUnitForMovement(nakhtu);
        
        var clickResult = coordinator.HandleTileClick(clickPosition, gameMap);
        
        Assert.IsTrue(clickResult.IsMovementAttempt);
        Assert.AreEqual(clickPosition, clickResult.DestinationPosition);
    }

    [Test]
    public void Should_Handle_Clicking_Occupied_Tile_When_Unit_Selected()
    {
        var coordinator = new MovementCoordinator();
        var gameMap = CreateTestMap();
        var nakhtu = new Nakhtu();
        var otherUnit = new Medjay();
        var unitPosition = new Vector2I(1, 1);
        var occupiedPosition = new Vector2I(1, 2);
        
        gameMap[unitPosition].PlaceUnit(nakhtu);
        gameMap[occupiedPosition].PlaceUnit(otherUnit);
        coordinator.SelectUnitForMovement(nakhtu);
        
        var clickResult = coordinator.HandleTileClick(occupiedPosition, gameMap);
        
        Assert.IsFalse(clickResult.IsMovementAttempt);
        Assert.IsNotNull(clickResult.ErrorMessage);
    }

    [Test]
    public void Should_Reject_Movement_To_Invalid_Destination()
    {
        var coordinator = new MovementCoordinator();
        var gameMap = CreateTestMap();
        var nakhtu = new Nakhtu();
        var unitPosition = new Vector2I(1, 1);
        var invalidDestination = new Vector2I(2, 2); // Too far for unit with limited movement
        
        // Set unit to have only 1 movement point, but destination costs 2
        nakhtu.CurrentMovementPoints = 1;
        gameMap[unitPosition].PlaceUnit(nakhtu);
        gameMap[invalidDestination].MovementCost = 2;
        
        coordinator.SelectUnitForMovement(nakhtu);
        var validDestinations = coordinator.GetValidDestinations(unitPosition, gameMap);
        
        // Verify the destination is NOT in valid destinations
        Assert.IsFalse(validDestinations.Contains(invalidDestination), "Destination should not be in valid list");
        
        // Now try to move there anyway
        var moveResult = coordinator.TryMoveToDestination(unitPosition, invalidDestination, gameMap);
        
        Assert.IsFalse(moveResult.Success, "Movement to invalid destination should fail");
        Assert.IsNotNull(moveResult.ErrorMessage);
    }



    [Test]
    public void Should_Enforce_Valid_Destination_List_In_Coordinator()
    {
        var coordinator = new MovementCoordinator();
        var gameMap = CreateTestMap();
        var nakhtu = new Nakhtu();
        var unitPosition = new Vector2I(1, 1);
        
        // Set movement points to 0 to make ALL destinations invalid
        nakhtu.CurrentMovementPoints = 0;
        
        gameMap[unitPosition].PlaceUnit(nakhtu);
        coordinator.SelectUnitForMovement(nakhtu);
        
        var validDestinations = coordinator.GetValidDestinations(unitPosition, gameMap);
        
        // With 0 movement points, there should be no valid destinations
        Assert.AreEqual(0, validDestinations.Count, "Unit with 0 movement should have no valid destinations");
        
        // Try to move to each position in the map - all should fail
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var destination = new Vector2I(x, y);
                if (destination == unitPosition) continue; // Skip current position
                
                // Create fresh coordinator and unit for each test to avoid state conflicts
                var testCoordinator = new MovementCoordinator();
                var testGameMap = CreateTestMap();
                var testUnit = new Nakhtu();
                testUnit.CurrentMovementPoints = 0; // No movement allowed
                
                testGameMap[unitPosition].PlaceUnit(testUnit);
                testCoordinator.SelectUnitForMovement(testUnit);
                
                var moveResult = testCoordinator.TryMoveToDestination(unitPosition, destination, testGameMap);
                
                // ALL moves should fail with 0 movement points
                Assert.IsFalse(moveResult.Success, $"Movement to {destination} should fail with 0 movement points");
                Assert.IsNotNull(moveResult.ErrorMessage, $"Should have error message for failed move to {destination}");
            }
        }
        
        // Clean up
        MovementValidationLogic.SetMovementSystem(null);
    }

    [Test]
    public void Should_Clear_Selection_When_Unit_Exhausts_Movement_Points()
    {
        var coordinator = new MovementCoordinator();
        var gameMap = CreateTestMap();
        var nakhtu = new Nakhtu(); // Has 4 movement points (doubled)
        var startPosition = new Vector2I(1, 1);
        
        // Place unit on starting tile
        gameMap[startPosition].PlaceUnit(nakhtu);
        
        // Select unit and get initial destinations
        coordinator.SelectUnitForMovement(nakhtu);
        var initialDestinations = coordinator.GetValidDestinations(startPosition, gameMap);
        Assert.IsTrue(initialDestinations.Count > 0, "Should have destinations with full movement");
        
        // Manually set movement points to 2 to test exhaustion scenario
        nakhtu.CurrentMovementPoints = 2;
        var mpBeforeMove = nakhtu.CurrentMovementPoints;
        
        // Make a move that exhausts exactly all movement points (cost 2 to Desert)
        var destinationPosition = new Vector2I(2, 1); // Desert tile costs 2 MP
        var moveCost = gameMap[destinationPosition].MovementCost;
        var moveResult = coordinator.TryMoveToDestination(startPosition, destinationPosition, gameMap);
        
        Assert.IsTrue(moveResult.Success, "Move should succeed");
        var expectedRemainingMP = mpBeforeMove - moveCost;
        Assert.AreEqual(expectedRemainingMP, nakhtu.CurrentMovementPoints, $"Unit should have {expectedRemainingMP} movement points after move ({mpBeforeMove} - {moveCost})");
        
        // Coordinator should have cleared selection since unit has no movement left
        Assert.IsNull(coordinator.GetSelectedUnit(), "Unit should be deselected when movement exhausted");
        
        // Getting destinations should return empty list since unit has no movement
        var finalDestinations = coordinator.GetValidDestinations(destinationPosition, gameMap);
        Assert.AreEqual(0, finalDestinations.Count, "Should have no destinations with 0 movement points");
        
        // Clean up
        MovementValidationLogic.SetMovementSystem(null);
    }

    private Dictionary<Vector2I, HexTile> CreateTestMap()
    {
        var map = new Dictionary<Vector2I, HexTile>();
        
        // Create a 3x3 test map
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var position = new Vector2I(x, y);
                var terrainType = (x + y) % 2 == 0 ? TerrainType.Shoreline : TerrainType.Desert;
                map[position] = new HexTile(position, terrainType);
            }
        }
        
        return map;
    }
} 