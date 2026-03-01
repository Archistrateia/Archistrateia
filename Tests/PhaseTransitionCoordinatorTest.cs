using NUnit.Framework;
using Archistrateia;

[TestFixture]
public class PhaseTransitionCoordinatorTest
{
    [Test]
    public void ApplyTransition_Should_ApplyMapPhase_ExactlyOnce()
    {
        var gameManager = new GameManager();
        var purchaseCoordinator = new PurchaseCoordinator();
        int mapPhaseCalls = 0;

        var coordinator = CreateCoordinator(
            gameManager,
            purchaseCoordinator,
            _ => mapPhaseCalls++);

        coordinator.ApplyTransition(GamePhase.Earn, GamePhase.Purchase);

        Assert.AreEqual(1, mapPhaseCalls, "Phase transition should update map visuals through a single path.");
    }

    [Test]
    public void ApplyTransition_CombatToEarn_Should_SwitchPlayer()
    {
        var gameManager = new GameManager();
        var purchaseCoordinator = new PurchaseCoordinator();
        int switchCalls = 0;

        var coordinator = CreateCoordinator(
            gameManager,
            purchaseCoordinator,
            _ => { },
            switchToNextPlayer: () => switchCalls++);

        coordinator.ApplyTransition(GamePhase.Combat, GamePhase.Earn);

        Assert.AreEqual(1, switchCalls, "Player rotation should happen only at full-cycle boundary.");
    }

    [Test]
    public void ApplyTransition_PurchaseToEarn_Should_NotSwitchPlayer()
    {
        var gameManager = new GameManager();
        var purchaseCoordinator = new PurchaseCoordinator();
        int switchCalls = 0;

        var coordinator = CreateCoordinator(
            gameManager,
            purchaseCoordinator,
            _ => { },
            switchToNextPlayer: () => switchCalls++);

        coordinator.ApplyTransition(GamePhase.Purchase, GamePhase.Earn);

        Assert.AreEqual(0, switchCalls, "Entering Earn from non-Combat phase should not rotate player.");
    }

    [Test]
    public void ApplyTransition_ToMove_Should_ResetMovement_AndDeselect()
    {
        var gameManager = new GameManager();
        var player = new Player("Pharaoh", 100);
        var unit = new Nakhtu();
        player.AddUnit(unit);
        unit.CurrentMovementPoints = 1;
        gameManager.Players.Add(player);

        var purchaseCoordinator = new PurchaseCoordinator();
        int deselectCalls = 0;

        var coordinator = CreateCoordinator(
            gameManager,
            purchaseCoordinator,
            _ => { },
            deselectAllUnits: () => deselectCalls++);

        coordinator.ApplyTransition(GamePhase.Purchase, GamePhase.Move);

        Assert.AreEqual(unit.MovementPoints, unit.CurrentMovementPoints, "Move phase should restore full movement points.");
        Assert.AreEqual(1, deselectCalls, "Move phase should clear current selection.");
    }

    [Test]
    public void ApplyTransition_ToPurchase_Should_ShowPurchaseUi_AndUpdateDetails()
    {
        var gameManager = new GameManager();
        var purchaseCoordinator = new PurchaseCoordinator();
        bool? purchaseUiVisible = null;
        int detailRefreshCalls = 0;
        string statusMessage = string.Empty;
        int purchaseRefreshCalls = 0;

        var coordinator = new PhaseTransitionCoordinator(
            gameManager,
            purchaseCoordinator,
            _ => { },
            visible => purchaseUiVisible = visible,
            () => detailRefreshCalls++,
            message => statusMessage = message,
            () => purchaseRefreshCalls++,
            () => { },
            () => { });

        coordinator.ApplyTransition(GamePhase.Earn, GamePhase.Purchase);

        Assert.That(purchaseUiVisible, Is.True, "Purchase phase should show purchase controls.");
        Assert.AreEqual(1, detailRefreshCalls, "Purchase phase should refresh selected unit details.");
        Assert.AreEqual("Choose a unit to buy.", statusMessage);
        Assert.AreEqual(1, purchaseRefreshCalls, "Transition should refresh purchase UI exactly once.");
    }

    private static PhaseTransitionCoordinator CreateCoordinator(
        GameManager gameManager,
        PurchaseCoordinator purchaseCoordinator,
        System.Action<GamePhase> applyMapPhase,
        System.Action switchToNextPlayer = null,
        System.Action deselectAllUnits = null)
    {
        return new PhaseTransitionCoordinator(
            gameManager,
            purchaseCoordinator,
            applyMapPhase,
            _ => { },
            () => { },
            _ => { },
            () => { },
            switchToNextPlayer ?? (() => { }),
            deselectAllUnits ?? (() => { }));
    }
}
