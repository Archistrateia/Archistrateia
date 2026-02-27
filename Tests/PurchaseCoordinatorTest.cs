using Godot;
using NUnit.Framework;
using System.Collections.Generic;
using Archistrateia;

[TestFixture]
public class PurchaseCoordinatorTest
{
    [Test]
    public void BeginSelection_Should_Fail_WhenPlayerCannotAfford()
    {
        var map = BuildRectMap(20, 12);
        var player = new Player("P1", 0);
        var coordinator = new PurchaseCoordinator();

        var result = coordinator.BeginSelection(player, 0, UnitType.Charioteer, map, playerCount: 2);

        Assert.IsFalse(result.Success);
        StringAssert.Contains("Insufficient gold", result.ErrorMessage);
    }

    [Test]
    public void BeginSelection_Should_Fail_WhenNoDeployableTilesExist()
    {
        var map = BuildRectMap(20, 12);
        var player = new Player("P1", 100);
        var coordinator = new PurchaseCoordinator();
        var service = new SemicircleDeploymentService();

        var deployable = service.GetDeployableTilesForPlayer(map, playerIndex: 0, playerCount: 2);
        foreach (var tilePos in deployable)
        {
            map[tilePos].PlaceUnit(new Nakhtu());
        }

        var result = coordinator.BeginSelection(player, 0, UnitType.Nakhtu, map, playerCount: 2);

        Assert.IsFalse(result.Success);
        StringAssert.Contains("No valid deployment tiles", result.ErrorMessage);
    }

    [Test]
    public void TryPlacePendingUnit_Should_Succeed_OnValidDeployableTile()
    {
        var map = BuildRectMap(20, 12);
        var player = new Player("P1", 100);
        var coordinator = new PurchaseCoordinator();

        var selectResult = coordinator.BeginSelection(player, 0, UnitType.Nakhtu, map, playerCount: 2);
        Assert.IsTrue(selectResult.Success);
        Assert.IsTrue(selectResult.ValidPlacementTiles.Count > 0);

        int startingGold = player.Gold;
        int startingUnits = player.Units.Count;
        var targetTile = selectResult.ValidPlacementTiles[0];

        var placeResult = coordinator.TryPlacePendingUnit(player, 0, targetTile, map);

        Assert.IsTrue(placeResult.Success);
        Assert.AreEqual(startingUnits + 1, player.Units.Count, "Unit should be added to player");
        Assert.Less(player.Gold, startingGold, "Gold should be spent on successful placement");
        Assert.IsTrue(map[targetTile].IsOccupied(), "Target tile should be occupied after placement");
    }

    [Test]
    public void TryPlacePendingUnit_Should_Reject_OtherPlayerDeploymentZone()
    {
        var map = BuildRectMap(20, 12);
        var player = new Player("P1", 100);
        var coordinator = new PurchaseCoordinator();
        var service = new SemicircleDeploymentService();
        var playerTwoTiles = service.GetDeployableTilesForPlayer(map, playerIndex: 1, playerCount: 2);
        Assert.IsTrue(playerTwoTiles.Count > 0, "Player 2 should have deployable tiles");

        var selectResult = coordinator.BeginSelection(player, 0, UnitType.Nakhtu, map, playerCount: 2);
        Assert.IsTrue(selectResult.Success);

        var otherPlayerTile = playerTwoTiles[0];
        var placeResult = coordinator.TryPlacePendingUnit(player, 0, otherPlayerTile, map);

        Assert.IsFalse(placeResult.Success);
        StringAssert.Contains("deployment zone", placeResult.ErrorMessage);
    }

    [Test]
    public void TryPlacePendingUnit_Should_Reject_WhenDifferentPlayerAttemptsPlacement()
    {
        var map = BuildRectMap(20, 12);
        var playerOne = new Player("P1", 100);
        var playerTwo = new Player("P2", 100);
        var coordinator = new PurchaseCoordinator();

        var selectResult = coordinator.BeginSelection(playerOne, 0, UnitType.Nakhtu, map, playerCount: 2);
        Assert.IsTrue(selectResult.Success);

        var targetTile = selectResult.ValidPlacementTiles[0];
        var placeResult = coordinator.TryPlacePendingUnit(playerTwo, 1, targetTile, map);

        Assert.IsFalse(placeResult.Success);
        StringAssert.Contains("belongs to another player", placeResult.ErrorMessage);
    }

    private static Dictionary<Vector2I, HexTile> BuildRectMap(int width, int height)
    {
        var map = new Dictionary<Vector2I, HexTile>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pos = new Vector2I(x, y);
                map[pos] = new HexTile(pos, TerrainType.Grassland);
            }
        }

        return map;
    }
}
