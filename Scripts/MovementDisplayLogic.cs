public class MovementDisplayLogic
{
    public string GetMovementDisplayText(Unit unit)
    {
        return $"MP: {unit.CurrentMovementPoints}";
    }
    
    public bool ShouldShowMovementDisplay(Unit unit, bool isSelected)
    {
        return isSelected;
    }
} 