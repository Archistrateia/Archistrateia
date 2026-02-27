using Godot;
using NUnit.Framework;
using System.Collections.Generic;
using Archistrateia;

[TestFixture]
public class MapInteractionControllerTest
{
    [Test]
    public void HandleUnitClicked_Should_Select_CurrentPlayers_Unit_During_Move_Phase()
    {
        var player = new Player("Pharaoh", 100);
        var unit = new Nakhtu();
        player.AddUnit(unit);
        var controller = CreateController();

        var result = controller.HandleUnitClicked(player, GamePhase.Move, unit);

        Assert.IsTrue(result.WasSelected);
        Assert.AreEqual(unit, result.SelectedUnit);
        Assert.AreEqual(unit, controller.GetSelectedUnit());
    }

    [Test]
    public void HandleTileClicked_Should_Return_Purchase_Result_During_Purchase_Phase()
    {
        var controller = CreateController();
        var clickedPosition = new Vector2I(2, 1);

        var result = controller.HandleTileClicked(
            GamePhase.Purchase,
            clickedPosition,
            CreateTestMap(),
            _ => null);

        Assert.AreEqual(TileInteractionKind.PurchaseTileSelected, result.Kind);
        Assert.AreEqual(clickedPosition, result.NewPosition);
    }

    [Test]
    public void HandleTileClicked_Should_Move_Selected_Unit_When_Destination_Is_Valid()
    {
        var player = new Player("Pharaoh", 100);
        var unit = new Nakhtu();
        player.AddUnit(unit);
        var controller = CreateController();
        var gameMap = CreateTestMap();
        var startPosition = new Vector2I(1, 1);
        var destination = new Vector2I(1, 2);
        gameMap[startPosition].PlaceUnit(unit);

        controller.HandleUnitClicked(player, GamePhase.Move, unit);
        var result = controller.HandleTileClicked(
            GamePhase.Move,
            destination,
            gameMap,
            selectedUnit => FindUnitPosition(selectedUnit, gameMap));

        Assert.AreEqual(TileInteractionKind.MoveSucceeded, result.Kind);
        Assert.AreEqual(destination, result.NewPosition);
        Assert.AreEqual(unit, gameMap[destination].OccupyingUnit);
        Assert.IsFalse(gameMap[startPosition].IsOccupied());
    }

    [Test]
    public void HandleTileClicked_Should_Request_Deselect_When_Selected_Unit_Has_No_Movement()
    {
        var player = new Player("Pharaoh", 100);
        var unit = new Nakhtu();
        unit.CurrentMovementPoints = 0;
        player.AddUnit(unit);
        var controller = CreateController();
        var gameMap = CreateTestMap();
        var startPosition = new Vector2I(1, 1);
        gameMap[startPosition].PlaceUnit(unit);

        controller.HandleUnitClicked(player, GamePhase.Move, unit);
        var result = controller.HandleTileClicked(
            GamePhase.Move,
            new Vector2I(1, 2),
            gameMap,
            selectedUnit => FindUnitPosition(selectedUnit, gameMap));

        Assert.AreEqual(TileInteractionKind.Ignored, result.Kind, "Units without movement should not become selectable.");
    }

    [Test]
    public void HandleTileClicked_Should_Request_Deselect_When_Move_Fails()
    {
        var player = new Player("Pharaoh", 100);
        var unit = new Nakhtu();
        unit.CurrentMovementPoints = 1;
        player.AddUnit(unit);
        var controller = CreateController();
        var gameMap = CreateTestMap();
        var startPosition = new Vector2I(1, 1);
        var blockedDestination = new Vector2I(1, 2);
        gameMap[startPosition].PlaceUnit(unit);

        controller.HandleUnitClicked(player, GamePhase.Move, unit);
        var result = controller.HandleTileClicked(
            GamePhase.Move,
            blockedDestination,
            gameMap,
            selectedUnit => FindUnitPosition(selectedUnit, gameMap));

        Assert.AreEqual(TileInteractionKind.DeselectRequired, result.Kind);
        Assert.IsNotEmpty(result.ErrorMessage);
    }

    private static MapInteractionController CreateController()
    {
        return new MapInteractionController(
            new PlayerInteractionLogic(),
            new MovementCoordinator());
    }

    private static Dictionary<Vector2I, HexTile> CreateTestMap()
    {
        var map = new Dictionary<Vector2I, HexTile>();

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

    private static Vector2I? FindUnitPosition(Unit unit, Dictionary<Vector2I, HexTile> gameMap)
    {
        foreach (var entry in gameMap)
        {
            if (entry.Value.OccupyingUnit == unit)
            {
                return entry.Key;
            }
        }

        return null;
    }
}
