using NUnit.Framework;

[TestFixture]
public class MovementDisplayLogicTest
{
    [Test]
    public void Should_Format_Movement_Points_Display_Text()
    {
        var displayLogic = new MovementDisplayLogic();
        var unit = new Charioteer();
        
        var displayText = displayLogic.GetMovementDisplayText(unit);
        
        // Test should use the unit's actual movement points, not hardcoded values
        var expectedText = $"MP: {unit.CurrentMovementPoints}";
        Assert.AreEqual(expectedText, displayText);
    }
    
    [Test]
    public void Should_Update_Display_Text_After_Movement()
    {
        var displayLogic = new MovementDisplayLogic();
        var unit = new Nakhtu();
        
        // Initial display should show unit's full movement points
        var initialText = displayLogic.GetMovementDisplayText(unit);
        var expectedInitialText = $"MP: {unit.CurrentMovementPoints}";
        Assert.AreEqual(expectedInitialText, initialText);
        
        // After consuming 1 movement point
        var originalMP = unit.CurrentMovementPoints;
        unit.CurrentMovementPoints = originalMP - 1;
        var updatedText = displayLogic.GetMovementDisplayText(unit);
        var expectedUpdatedText = $"MP: {unit.CurrentMovementPoints}";
        Assert.AreEqual(expectedUpdatedText, updatedText);
        
        // After consuming all movement
        unit.CurrentMovementPoints = 0;
        var finalText = displayLogic.GetMovementDisplayText(unit);
        Assert.AreEqual("MP: 0", finalText);
    }
    
    [Test]
    public void Should_Determine_When_To_Show_Movement_Display()
    {
        var displayLogic = new MovementDisplayLogic();
        var unit = new Charioteer();
        
        // Should show when unit is selected and has movement
        bool shouldShow = displayLogic.ShouldShowMovementDisplay(unit, isSelected: true);
        Assert.IsTrue(shouldShow);
        
        // Should not show when unit is not selected
        bool shouldNotShow = displayLogic.ShouldShowMovementDisplay(unit, isSelected: false);
        Assert.IsFalse(shouldNotShow);
        
        // Should show even when unit has 0 movement (to indicate exhaustion)
        unit.CurrentMovementPoints = 0;
        bool shouldShowEmpty = displayLogic.ShouldShowMovementDisplay(unit, isSelected: true);
        Assert.IsTrue(shouldShowEmpty);
    }
} 