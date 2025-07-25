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
        var movementLogic = new MovementValidationLogic();
        var nakhtu = new Nakhtu();
        var otherUnit = new Medjay();
        var fromTile = new HexTile(new Vector2I(0, 0), TerrainType.Desert);
        var toTile = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline);
        toTile.MoveUnitTo(otherUnit); // Occupy the destination

        var canMove = movementLogic.CanUnitMoveTo(nakhtu, fromTile, toTile);

        Assert.IsFalse(canMove, "Unit should not be able to move to occupied tile");
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
        var movementLogic = new MovementValidationLogic();
        var gameMap = CreateTestMap();
        var nakhtu = new Nakhtu(); // 2 movement points
        var currentPosition = new Vector2I(1, 1);

        var validDestinations = movementLogic.GetValidMovementDestinations(nakhtu, currentPosition, gameMap);

        Assert.IsNotNull(validDestinations);
        Assert.IsTrue(validDestinations.Count > 0, "Should find at least some valid destinations");
        
        // All destinations should be reachable
        foreach (var destination in validDestinations)
        {
            var tile = gameMap[destination];
            Assert.IsTrue(tile.MovementCost <= nakhtu.CurrentMovementPoints, 
                $"Destination {destination} should be reachable with {nakhtu.CurrentMovementPoints} movement points");
        }
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