using Godot;
using NUnit.Framework;
using System.Collections.Generic;
using Archistrateia;

[TestFixture]
public class PurchaseUIControllerTest
{
    [Test]
    public void RefreshPurchaseUI_Should_Disable_Buy_When_Not_In_Purchase_Phase()
    {
        var turnManager = new TurnManager();
        var gameManager = CreateGameManagerWithSinglePlayer();
        var unitSelector = new OptionButton();
        var details = new Label();
        var gold = new Label();
        var status = new Label();
        var buy = new Button();
        var cancel = new Button();
        var controller = CreateController(
            unitSelector,
            details,
            gold,
            status,
            buy,
            cancel,
            new PurchaseCoordinator(),
            new SemicircleDeploymentService(),
            () => gameManager,
            () => 0,
            () => turnManager);

        controller.PopulatePurchaseUnitSelector();
        controller.RefreshPurchaseUI();

        Assert.IsTrue(buy.Disabled, "Buy button should stay disabled outside Purchase phase.");
    }

    [Test]
    public void Buy_Then_Place_Should_Create_Unit_And_Update_Status()
    {
        var turnManager = new TurnManager();
        turnManager.SetPhase(GamePhase.Purchase);
        var gameManager = CreateGameManagerWithSinglePlayer();
        var player = gameManager.Players[0];
        var unitSelector = new OptionButton();
        var details = new Label();
        var gold = new Label();
        var status = new Label();
        var buy = new Button();
        var cancel = new Button();

        int createVisualUnitCalls = 0;
        int updateOccupationCalls = 0;
        int clearPlacementCalls = 0;
        IReadOnlyList<Vector2I> shownTiles = null;

        var controller = CreateController(
            unitSelector,
            details,
            gold,
            status,
            buy,
            cancel,
            new PurchaseCoordinator(),
            new SemicircleDeploymentService(),
            () => gameManager,
            () => 0,
            () => turnManager,
            _ => { },
            () => clearPlacementCalls++,
            tiles => shownTiles = tiles,
            _ => Colors.Red,
            _ => Vector2.Zero,
            (_, _, _) => createVisualUnitCalls++,
            () => updateOccupationCalls++);

        controller.PopulatePurchaseUnitSelector();
        controller.OnPurchaseBuyPressed();

        Assert.IsNotNull(shownTiles);
        Assert.IsTrue(shownTiles.Count > 0, "Selection should expose valid deployment tiles.");
        StringAssert.Contains("Select a highlighted deployment tile", status.Text);

        var targetTile = shownTiles[0];
        controller.OnPurchaseTileClicked(targetTile);

        Assert.AreEqual(1, player.Units.Count, "Placed purchase should add a unit to the active player.");
        Assert.IsTrue(gameManager.GameMap[targetTile].IsOccupied(), "Placed purchase should occupy selected tile.");
        Assert.AreEqual(1, createVisualUnitCalls, "Successful placement should emit visual unit creation once.");
        Assert.AreEqual(1, updateOccupationCalls, "Successful placement should refresh tile occupation status.");
        Assert.GreaterOrEqual(clearPlacementCalls, 1, "Successful placement should clear placement highlights.");
        StringAssert.Contains("Placed", status.Text);
    }

    [Test]
    public void Cancel_Should_Clear_Pending_Purchase_And_Update_Status()
    {
        var turnManager = new TurnManager();
        turnManager.SetPhase(GamePhase.Purchase);
        var gameManager = CreateGameManagerWithSinglePlayer();
        var status = new Label();
        int clearPlacementCalls = 0;

        var controller = CreateController(
            new OptionButton(),
            new Label(),
            new Label(),
            status,
            new Button(),
            new Button(),
            new PurchaseCoordinator(),
            new SemicircleDeploymentService(),
            () => gameManager,
            () => 0,
            () => turnManager,
            _ => { },
            () => clearPlacementCalls++);

        controller.PopulatePurchaseUnitSelector();
        controller.OnPurchaseBuyPressed();
        controller.OnPurchaseCancelPressed();

        Assert.AreEqual(1, clearPlacementCalls, "Cancel should clear highlighted deployment tiles exactly once.");
        Assert.AreEqual("Purchase cancelled.", status.Text);
    }

    private static PurchaseUIController CreateController(
        OptionButton unitSelector,
        Label details,
        Label gold,
        Label status,
        Button buy,
        Button cancel,
        PurchaseCoordinator purchaseCoordinator,
        SemicircleDeploymentService deploymentService,
        System.Func<GameManager> getGameManager,
        System.Func<int> getCurrentPlayerIndex,
        System.Func<TurnManager> getTurnManager,
        System.Action<bool> setPanelVisible = null,
        System.Action clearPlacementTiles = null,
        System.Action<IReadOnlyList<Vector2I>> showPlacementTiles = null,
        System.Func<Player, Color> getPlayerColor = null,
        System.Func<Vector2I, Vector2> calculateWorldPosition = null,
        System.Action<Unit, Vector2, Color> createVisualUnit = null,
        System.Action updateTileOccupationStatus = null)
    {
        return new PurchaseUIController(
            unitSelector,
            details,
            gold,
            status,
            buy,
            cancel,
            purchaseCoordinator,
            deploymentService,
            getGameManager,
            getCurrentPlayerIndex,
            getTurnManager,
            setPanelVisible,
            clearPlacementTiles,
            showPlacementTiles,
            getPlayerColor,
            calculateWorldPosition,
            createVisualUnit,
            updateTileOccupationStatus);
    }

    private static GameManager CreateGameManagerWithSinglePlayer()
    {
        var gameManager = new GameManager();
        gameManager.SetGameMap(BuildRectMap(20, 12));
        gameManager.Players.Add(new Player("P1", 150));
        gameManager.Players.Add(new Player("P2", 150));
        return gameManager;
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
