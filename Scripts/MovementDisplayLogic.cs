namespace Archistrateia
{
    public class MovementDisplayLogic
    {
        public static string GetMovementDisplayText(Unit unit)
        {
            return $"MP: {unit.CurrentMovementPoints}";
        }
        
        public static bool ShouldShowMovementDisplay(Unit unit, bool isSelected)
        {
            // Show MP display if unit is selected (regardless of MP value)
            // This helps players understand why they can't move (MP: 0) vs not being selected
            return isSelected;
        }
    }
} 