using NUnit.Framework;
using Godot;

[TestFixture]
public partial class PlayerInteractionLogicTest : Node
{
    [Test]
    public void Should_Allow_Player_To_Select_Own_Units_During_Move_Phase()
    {
        var interactionLogic = new PlayerInteractionLogic();
        var pharaoh = new Player("Pharaoh", 100);
        var nakhtu = new Nakhtu();
        pharaoh.AddUnit(nakhtu);

        var canSelect = interactionLogic.CanPlayerSelectUnit(pharaoh, nakhtu, GamePhase.Move);

        Assert.IsTrue(canSelect, "Player should be able to select their own units during Move phase");
    }

    [Test]
    public void Should_Not_Allow_Player_To_Select_Enemy_Units()
    {
        var interactionLogic = new PlayerInteractionLogic();
        var pharaoh = new Player("Pharaoh", 100);
        var general = new Player("General", 100);
        var enemyUnit = new Nakhtu();
        general.AddUnit(enemyUnit);

        var canSelect = interactionLogic.CanPlayerSelectUnit(pharaoh, enemyUnit, GamePhase.Move);

        Assert.IsFalse(canSelect, "Player should not be able to select enemy units");
    }

    [Test]
    public void Should_Not_Allow_Unit_Selection_During_Non_Move_Phases()
    {
        var interactionLogic = new PlayerInteractionLogic();
        var pharaoh = new Player("Pharaoh", 100);
        var nakhtu = new Nakhtu();
        pharaoh.AddUnit(nakhtu);

        var canSelectEarn = interactionLogic.CanPlayerSelectUnit(pharaoh, nakhtu, GamePhase.Earn);
        var canSelectPurchase = interactionLogic.CanPlayerSelectUnit(pharaoh, nakhtu, GamePhase.Purchase);
        var canSelectCombat = interactionLogic.CanPlayerSelectUnit(pharaoh, nakhtu, GamePhase.Combat);

        Assert.IsFalse(canSelectEarn, "Should not select units during Earn phase");
        Assert.IsFalse(canSelectPurchase, "Should not select units during Purchase phase");
        Assert.IsFalse(canSelectCombat, "Should not select units during Combat phase");
    }

    [Test]
    public void Should_Track_Currently_Selected_Unit()
    {
        var interactionLogic = new PlayerInteractionLogic();
        var pharaoh = new Player("Pharaoh", 100);
        var nakhtu = new Nakhtu();
        pharaoh.AddUnit(nakhtu);

        Assert.IsNull(interactionLogic.GetSelectedUnit(), "No unit should be selected initially");

        interactionLogic.SelectUnit(pharaoh, nakhtu, GamePhase.Move);

        Assert.AreEqual(nakhtu, interactionLogic.GetSelectedUnit(), "Selected unit should be tracked");
    }

    [Test]
    public void Should_Deselect_Unit_When_Selecting_Another()
    {
        var interactionLogic = new PlayerInteractionLogic();
        var pharaoh = new Player("Pharaoh", 100);
        var nakhtu = new Nakhtu();
        var medjay = new Medjay();
        pharaoh.AddUnit(nakhtu);
        pharaoh.AddUnit(medjay);

        interactionLogic.SelectUnit(pharaoh, nakhtu, GamePhase.Move);
        Assert.AreEqual(nakhtu, interactionLogic.GetSelectedUnit());

        interactionLogic.SelectUnit(pharaoh, medjay, GamePhase.Move);
        Assert.AreEqual(medjay, interactionLogic.GetSelectedUnit(), "Should switch to newly selected unit");
    }

    [Test]
    public void Should_Allow_Deselecting_Unit()
    {
        var interactionLogic = new PlayerInteractionLogic();
        var pharaoh = new Player("Pharaoh", 100);
        var nakhtu = new Nakhtu();
        pharaoh.AddUnit(nakhtu);

        interactionLogic.SelectUnit(pharaoh, nakhtu, GamePhase.Move);
        Assert.AreEqual(nakhtu, interactionLogic.GetSelectedUnit());

        interactionLogic.DeselectUnit();
        Assert.IsNull(interactionLogic.GetSelectedUnit(), "Unit should be deselected");
    }

    [Test]
    public void Should_Only_Allow_Units_With_Movement_To_Be_Selected()
    {
        var interactionLogic = new PlayerInteractionLogic();
        var pharaoh = new Player("Pharaoh", 100);
        var nakhtu = new Nakhtu();
        pharaoh.AddUnit(nakhtu);

        // Unit with movement points
        nakhtu.CurrentMovementPoints = 2;
        var canSelectWithMovement = interactionLogic.CanPlayerSelectUnit(pharaoh, nakhtu, GamePhase.Move);

        // Unit with no movement points
        nakhtu.CurrentMovementPoints = 0;
        var canSelectWithoutMovement = interactionLogic.CanPlayerSelectUnit(pharaoh, nakhtu, GamePhase.Move);

        Assert.IsTrue(canSelectWithMovement, "Should be able to select unit with movement points");
        Assert.IsFalse(canSelectWithoutMovement, "Should not be able to select unit without movement points");
    }
} 