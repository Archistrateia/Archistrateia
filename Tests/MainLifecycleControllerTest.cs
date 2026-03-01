using NUnit.Framework;
using Archistrateia;

[TestFixture]
public class MainLifecycleControllerTest
{
    [Test]
    public void AdvancePhaseWithSideEffects_Should_Advance_TurnManager_Phase()
    {
        var turnManager = new TurnManager();
        var controller = CreateController(
            () => turnManager,
            () => null,
            () => 0,
            _ => { });

        controller.AdvancePhaseWithSideEffects();

        Assert.AreEqual(GamePhase.Purchase, turnManager.CurrentPhase);
    }

    [Test]
    public void SwitchToNextPlayer_Should_Rotate_Index_And_Refresh_UI()
    {
        var gameManager = new GameManager();
        gameManager.Players.Add(new Player("P1", 100));
        gameManager.Players.Add(new Player("P2", 100));

        int playerIndex = 0;
        Player lastRendererPlayer = null;
        int refreshCalls = 0;
        var controller = CreateController(
            () => new TurnManager(),
            () => gameManager,
            () => playerIndex,
            index => playerIndex = index,
            setCurrentPlayerOnMapRenderer: player => lastRendererPlayer = player,
            refreshPurchaseUI: () => refreshCalls++);

        controller.SwitchToNextPlayer();

        Assert.AreEqual(1, playerIndex);
        Assert.AreSame(gameManager.Players[1], lastRendererPlayer);
        Assert.AreEqual(1, refreshCalls);
    }

    [Test]
    public void HandleTurnManagerPhaseChanged_Should_ApplyTransition_And_UpdateTitle()
    {
        var turnManager = new TurnManager();
        turnManager.AdvancePhase(); // Purchase
        var gameManager = new GameManager();
        gameManager.Players.Add(new Player("Pharaoh", 100));

        GamePhase? oldPhase = null;
        GamePhase? newPhase = null;
        string titleText = null;
        string playerName = null;
        string phaseName = null;
        int turn = 0;
        var controller = CreateController(
            () => turnManager,
            () => gameManager,
            () => 0,
            _ => { },
            applyPhaseTransition: (oldValue, newValue) =>
            {
                oldPhase = oldValue;
                newPhase = newValue;
            },
            updateTopBarPlayerInfo: (name, phase, currentTurn) =>
            {
                playerName = name;
                phaseName = phase;
                turn = currentTurn;
            },
            setTitleText: text => titleText = text);

        controller.HandleTurnManagerPhaseChanged((int)GamePhase.Earn, (int)GamePhase.Purchase);

        Assert.AreEqual(GamePhase.Earn, oldPhase);
        Assert.AreEqual(GamePhase.Purchase, newPhase);
        Assert.AreEqual("Pharaoh", playerName);
        Assert.AreEqual("Purchase", phaseName);
        Assert.AreEqual(1, turn);
        Assert.AreEqual("Turn 1 - Purchase", titleText);
    }

    private static MainLifecycleController CreateController(
        System.Func<TurnManager> getTurnManager,
        System.Func<GameManager> getGameManager,
        System.Func<int> getCurrentPlayerIndex,
        System.Action<int> setCurrentPlayerIndex,
        System.Action<Player> setCurrentPlayerOnMapRenderer = null,
        System.Action<GamePhase, GamePhase> applyPhaseTransition = null,
        System.Action refreshPurchaseUI = null,
        System.Action<string, string, int> updateTopBarPlayerInfo = null,
        System.Action<string> setTitleText = null)
    {
        return new MainLifecycleController(
            getTurnManager,
            getGameManager,
            getCurrentPlayerIndex,
            setCurrentPlayerIndex,
            setCurrentPlayerOnMapRenderer,
            applyPhaseTransition,
            refreshPurchaseUI,
            updateTopBarPlayerInfo,
            setTitleText);
    }
}
