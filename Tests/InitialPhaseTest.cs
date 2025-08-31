using NUnit.Framework;
using Godot;
using System;

[TestFixture]
public partial class InitialPhaseTest : Node
{
    [TearDown]
    public void CleanupAfterEachTest()
    {
        // Clean up all children to prevent resource leaks
        foreach (var child in GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }
        
        // Force cleanup of any remaining resources
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [Test]
    public void Should_Start_With_Earn_Phase_Not_Movement()
    {
        var turnManager = new TurnManager();
        AddChild(turnManager);
        
        Assert.AreEqual(GamePhase.Earn, turnManager.CurrentPhase, "Game should start with Earn phase, not Movement");
        Assert.AreEqual(1, turnManager.CurrentTurn, "Game should start with Turn 1");
    }
    
    [Test]
    public void Should_Advance_From_Earn_To_Purchase_Phase()
    {
        var turnManager = new TurnManager();
        AddChild(turnManager);
        
        Assert.AreEqual(GamePhase.Earn, turnManager.CurrentPhase, "Should start with Earn phase");
        
        turnManager.AdvancePhase();
        Assert.AreEqual(GamePhase.Purchase, turnManager.CurrentPhase, "Should advance to Purchase phase");
    }
    
    [Test]
    public void Should_Complete_Full_Turn_Cycle()
    {
        var turnManager = new TurnManager();
        AddChild(turnManager);
        
        Assert.AreEqual(GamePhase.Earn, turnManager.CurrentPhase, "Should start with Earn phase");
        Assert.AreEqual(1, turnManager.CurrentTurn, "Should start with Turn 1");
        
        turnManager.AdvancePhase();
        Assert.AreEqual(GamePhase.Purchase, turnManager.CurrentPhase, "Should advance to Purchase phase");
        
        turnManager.AdvancePhase();
        Assert.AreEqual(GamePhase.Move, turnManager.CurrentPhase, "Should advance to Move phase");
        
        turnManager.AdvancePhase();
        Assert.AreEqual(GamePhase.Combat, turnManager.CurrentPhase, "Should advance to Combat phase");
        
        turnManager.AdvancePhase();
        Assert.AreEqual(GamePhase.Earn, turnManager.CurrentPhase, "Should return to Earn phase");
        Assert.AreEqual(2, turnManager.CurrentTurn, "Should advance to Turn 2");
    }
} 