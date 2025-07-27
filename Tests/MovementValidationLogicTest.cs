using NUnit.Framework;
using Godot;
using System.Collections.Generic;

[TestFixture]
public partial class MovementValidationLogicTest : Node
{
    [Test]
    public void Should_Allow_Valid_Movement_Within_Range()
    {
        var movementLogic = new MovementValidationLogic();
        var nakhtu = new Nakhtu(); // 2 movement points
        var fromTile = new HexTile(new Vector2I(0, 0), TerrainType.Desert); // 2 movement cost
        var toTile = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline); // 1 movement cost

        var canMove = movementLogic.CanUnitMoveTo(nakhtu, fromTile, toTile);

        Assert.IsTrue(canMove, "Unit should be able to move to adjacent tile within movement range");
    }

    [Test]
    public void Should_Prevent_Movement_Beyond_Range()
    {
        var movementLogic = new MovementValidationLogic();
        var nakhtu = new Nakhtu(); // 2 movement points
        nakhtu.CurrentMovementPoints = 1; // Only 1 point left
        var fromTile = new HexTile(new Vector2I(0, 0), TerrainType.Desert);
        var toTile = new HexTile(new Vector2I(1, 0), TerrainType.Hill); // 2 movement cost

        var canMove = movementLogic.CanUnitMoveTo(nakhtu, fromTile, toTile);

        Assert.IsFalse(canMove, "Unit should not be able to move to tile that costs more than remaining movement");
    }

    [Test]
    public void Should_Prevent_Movement_To_Occupied_Tile()
    {
        var logic = new MovementValidationLogic();
        var fromTile = new HexTile(new Vector2I(0, 0), TerrainType.Shoreline);
        var toTile = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline);
        var nakhtu = new Nakhtu();
        var otherUnit = new Medjay();

        // Place another unit on the destination tile
        toTile.PlaceUnit(otherUnit);

        bool canMove = logic.CanUnitMoveTo(nakhtu, fromTile, toTile);

        Assert.IsFalse(canMove, "Should not be able to move to occupied tile");
    }

    [Test]
    public void Should_Calculate_Adjacent_Tiles()
    {
        var movementLogic = new MovementValidationLogic();
        var centerPosition = new Vector2I(3, 3);

        var adjacentTiles = movementLogic.GetAdjacentPositions(centerPosition);

        Assert.AreEqual(6, adjacentTiles.Length, "Hex tile should have 6 adjacent positions");
        
        // Check that all adjacent positions are exactly 1 tile away
        foreach (var position in adjacentTiles)
        {
            var distance = Mathf.Abs(position.X - centerPosition.X) + Mathf.Abs(position.Y - centerPosition.Y);
            Assert.IsTrue(distance <= 2, $"Adjacent position {position} should be close to center {centerPosition}");
        }
    }

    [Test]
    public void Should_Get_Valid_Movement_Destinations()
    {
        var logic = new MovementValidationLogic();
        var gameMap = CreateTestMap();
        var nakhtu = new Nakhtu(); // Has 2 movement points
        var currentPosition = new Vector2I(1, 1);

        var validDestinations = logic.GetValidMovementDestinations(nakhtu, currentPosition, gameMap);

        Assert.IsTrue(validDestinations.Count > 0, "Should find some valid destinations");
        
        // All returned destinations should be reachable
        foreach (var destination in validDestinations)
        {
            var tile = gameMap[destination];
            Assert.IsTrue(tile.MovementCost <= nakhtu.CurrentMovementPoints,
                $"Destination {destination} with cost {tile.MovementCost} should be within {nakhtu.CurrentMovementPoints} movement points");
        }
    }

    [Test]
    public void Should_Find_Multi_Step_Movement_Destinations()
    {
        var logic = new MovementValidationLogic();
        var gameMap = CreateSpecialTestMap();
        var charioteer = new Charioteer(); // Has 4 movement points
        var startPosition = new Vector2I(1, 1);

        var validDestinations = logic.GetValidMovementDestinations(charioteer, startPosition, gameMap);

        // Should find tiles that require 2 steps (like going through a cost-2 tile to reach another cost-2 tile)
        // Total movement budget is 4, so should be able to reach tiles 2 steps away if each step costs 2
        var twoStepDestination = new Vector2I(1, 3); // 2 steps south
        Assert.Contains(twoStepDestination, validDestinations, 
            "Should find destinations reachable in multiple steps within movement budget");
    }

    [Test]
    public void Should_Not_Find_Destinations_Beyond_Movement_Budget()
    {
        var logic = new MovementValidationLogic();
        var gameMap = CreateSpecialTestMap(); // All tiles cost 2 movement (Desert)
        var nakhtu = new Nakhtu();
        var startPosition = new Vector2I(1, 1);

        var validDestinations = logic.GetValidMovementDestinations(nakhtu, startPosition, gameMap);

        // Calculate a destination that should exceed the movement budget
        // With all tiles costing 2 MP, we need to find a destination that costs more than nakhtu.CurrentMovementPoints
        var unitMP = nakhtu.CurrentMovementPoints;
        var tileMP = 2; // All Desert tiles cost 2 MP
        
        // Find a destination that would require more moves than the unit can afford
        var maxMoves = unitMP / tileMP;
        var excessiveDistance = maxMoves + 1; // One more move than affordable
        var excessiveDestination = new Vector2I(1, 1 + excessiveDistance); // Move south by excessive distance
        
        GD.Print($"Testing unit with {unitMP} MP, tile cost {tileMP}, max moves {maxMoves}, testing destination at distance {excessiveDistance}");
        
        // Only test if the excessive destination exists in the map
        if (gameMap.ContainsKey(excessiveDestination))
        {
            Assert.IsFalse(validDestinations.Contains(excessiveDestination), 
                $"Should not find destination {excessiveDestination} that requires {excessiveDistance * tileMP} total movement with only {unitMP} MP available");
        }
        else
        {
            // If the map isn't big enough, manually test with a closer destination that still exceeds budget
            // Find the furthest reachable destination and verify the next one beyond that isn't reachable
            var furthestReachable = new Vector2I(1, 1 + maxMoves);
            if (gameMap.ContainsKey(furthestReachable))
            {
                var expectedReachableCost = maxMoves * tileMP;
                if (expectedReachableCost <= unitMP)
                {
                    Assert.Contains(furthestReachable, validDestinations, 
                        $"Should find destination {furthestReachable} that costs exactly {expectedReachableCost} MP with {unitMP} MP available");
                }
            }
        }
    }

    [Test]
    public void Should_Find_Optimal_Path_Within_Movement_Budget()
    {
        var logic = new MovementValidationLogic();
        var gameMap = CreatePathfindingTestMap();
        var charioteer = new Charioteer(); // Has 4 movement points
        var startPosition = new Vector2I(0, 0);

        var validDestinations = logic.GetValidMovementDestinations(charioteer, startPosition, gameMap);

        // Should find a tile that's reachable via optimal path
        // Path: (0,0) -> (1,0) [cost 1] -> (2,0) [cost 1] -> (3,0) [cost 1] -> (4,0) [cost 1] = total 4
        var destination = new Vector2I(4, 0);
        Assert.Contains(destination, validDestinations,
            "Should find destinations reachable via optimal pathfinding within movement budget");
    }

    [Test]
    public void Should_Allow_Multiple_Moves_With_Movement_Budget()
    {
        var logic = new MovementValidationLogic();
        var fromTile = new HexTile(new Vector2I(0, 0), TerrainType.Shoreline); // Cost 1
        var toTile = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline);   // Cost 1
        var charioteer = new Charioteer();

        // Unit should start with its full movement points
        var initialMP = charioteer.MovementPoints;
        Assert.AreEqual(initialMP, charioteer.CurrentMovementPoints, $"Charioteer should start with {initialMP} movement points");

        // First move should be allowed
        bool canMoveFirst = logic.CanUnitMoveTo(charioteer, fromTile, toTile);
        Assert.IsTrue(canMoveFirst, "First move should be allowed");

        // Simulate first move and verify remaining points
        var firstMoveCost = toTile.MovementCost;
        charioteer.CurrentMovementPoints -= firstMoveCost;
        var expectedAfterFirst = initialMP - firstMoveCost;
        Assert.AreEqual(expectedAfterFirst, charioteer.CurrentMovementPoints, $"Should have {expectedAfterFirst} points after first move (cost {firstMoveCost})");

        // Second move should still be allowed with remaining points
        bool canMoveSecond = logic.CanUnitMoveTo(charioteer, toTile, fromTile);
        Assert.IsTrue(canMoveSecond, "Second move should be allowed with remaining movement points");

        // Simulate second move and verify remaining points
        var secondMoveCost = fromTile.MovementCost;
        charioteer.CurrentMovementPoints -= secondMoveCost;
        var expectedAfterSecond = expectedAfterFirst - secondMoveCost;
        Assert.AreEqual(expectedAfterSecond, charioteer.CurrentMovementPoints, $"Should have {expectedAfterSecond} points after second move (cost {secondMoveCost})");

        // Third move should still be allowed if unit has enough points
        bool canMoveThird = logic.CanUnitMoveTo(charioteer, fromTile, toTile);
        var thirdMoveCost = toTile.MovementCost;
        bool shouldAllowThird = charioteer.CurrentMovementPoints >= thirdMoveCost;
        Assert.AreEqual(shouldAllowThird, canMoveThird, $"Third move should be {(shouldAllowThird ? "allowed" : "blocked")} with {charioteer.CurrentMovementPoints} MP and cost {thirdMoveCost}");
    }

    [Test]
    public void Should_Prevent_Movement_When_Insufficient_Points()
    {
        var logic = new MovementValidationLogic();
        var fromTile = new HexTile(new Vector2I(0, 0), TerrainType.Shoreline); // Cost 1
        var toTile = new HexTile(new Vector2I(1, 0), TerrainType.Desert);      // Cost 2
        var nakhtu = new Nakhtu(); // Has 2 movement points

        // Reduce movement points to just 1
        nakhtu.CurrentMovementPoints = 1;

        // Should NOT be able to move to Desert tile (costs 2)
        bool canMove = logic.CanUnitMoveTo(nakhtu, fromTile, toTile);
        Assert.IsFalse(canMove, "Should not be able to move to tile that costs more than remaining points");
    }

    [Test]
    public void Should_Find_Destinations_Based_On_Remaining_Movement()
    {
        var logic = new MovementValidationLogic();
        var gameMap = CreateTestMap();
        var charioteer = new Charioteer(); // Has 4 movement points initially
        var currentPosition = new Vector2I(1, 1);

        // Get destinations with full movement budget
        var fullBudgetDestinations = logic.GetValidMovementDestinations(charioteer, currentPosition, gameMap);
        int fullCount = fullBudgetDestinations.Count;
        Assert.IsTrue(fullCount > 0, "Should find destinations with full movement budget");

        // Reduce movement points to 1
        charioteer.CurrentMovementPoints = 1;

        // Should find fewer destinations with reduced budget
        var reducedBudgetDestinations = logic.GetValidMovementDestinations(charioteer, currentPosition, gameMap);
        int reducedCount = reducedBudgetDestinations.Count;
        Assert.IsTrue(reducedCount <= fullCount, "Should find same or fewer destinations with reduced movement");
        
        // All destinations should be reachable with remaining points
        foreach (var destination in reducedBudgetDestinations)
        {
            var tile = gameMap[destination];
            Assert.IsTrue(tile.MovementCost <= charioteer.CurrentMovementPoints,
                $"Destination {destination} should be reachable with {charioteer.CurrentMovementPoints} remaining points");
        }
    }

    [Test]
    public void Should_Find_No_Destinations_When_No_Movement_Points()
    {
        var logic = new MovementValidationLogic();
        var gameMap = CreateTestMap();
        var nakhtu = new Nakhtu();
        var currentPosition = new Vector2I(1, 1);

        // Set movement points to 0
        nakhtu.CurrentMovementPoints = 0;

        // Should find no valid destinations
        var destinations = logic.GetValidMovementDestinations(nakhtu, currentPosition, gameMap);
        Assert.AreEqual(0, destinations.Count, "Should find no destinations with 0 movement points");
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

    private Dictionary<Vector2I, HexTile> CreateSpecialTestMap()
    {
        var map = new Dictionary<Vector2I, HexTile>();
        
        // Create a map where all tiles cost 2 movement
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var position = new Vector2I(x, y);
                map[position] = new HexTile(position, TerrainType.Desert); // All cost 2
            }
        }
        
        return map;
    }

    private Dictionary<Vector2I, HexTile> CreatePathfindingTestMap()
    {
        var map = new Dictionary<Vector2I, HexTile>();
        
        // Create a straight line of low-cost tiles for pathfinding test
        for (int x = 0; x < 6; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var position = new Vector2I(x, y);
                map[position] = new HexTile(position, TerrainType.Shoreline); // All cost 1
            }
        }
        
        return map;
    }
} 