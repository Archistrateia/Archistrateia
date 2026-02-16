using Godot;
using NUnit.Framework;
using System.Reflection;
using Archistrateia;

[TestFixture]
public partial class GameInitializationPurchasePhaseTest : Node
{
    [TearDown]
    public void CleanupAfterEachTest()
    {
        foreach (var child in GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }
    }

    [Test]
    public void SetupInitialUnits_Should_NotCreateStartingUnits()
    {
        var gameManager = new GameManager();
        var playerOne = new Player("P1", 100);
        var playerTwo = new Player("P2", 100);
        gameManager.Players.Add(playerOne);
        gameManager.Players.Add(playerTwo);

        var method = typeof(GameManager).GetMethod("SetupInitialUnits", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, "SetupInitialUnits method should exist");

        method.Invoke(gameManager, null);

        Assert.AreEqual(0, playerOne.Units.Count, "Player 1 should start with no units");
        Assert.AreEqual(0, playerTwo.Units.Count, "Player 2 should start with no units");
    }

    [Test]
    public void TurnManager_Should_UseGlobalPhaseCycle()
    {
        var turnManager = new TurnManager();
        AddChild(turnManager);

        Assert.AreEqual(1, turnManager.CurrentTurn);
        Assert.AreEqual(GamePhase.Earn, turnManager.CurrentPhase);

        turnManager.AdvancePhase();
        Assert.AreEqual(GamePhase.Purchase, turnManager.CurrentPhase);
        Assert.AreEqual(1, turnManager.CurrentTurn, "Turn should not increment mid-cycle");

        turnManager.AdvancePhase();
        Assert.AreEqual(GamePhase.Move, turnManager.CurrentPhase);
        Assert.AreEqual(1, turnManager.CurrentTurn);

        turnManager.AdvancePhase();
        Assert.AreEqual(GamePhase.Combat, turnManager.CurrentPhase);
        Assert.AreEqual(1, turnManager.CurrentTurn);

        turnManager.AdvancePhase();
        Assert.AreEqual(GamePhase.Earn, turnManager.CurrentPhase);
        Assert.AreEqual(2, turnManager.CurrentTurn, "Turn should increment once per full global cycle");
    }
}
