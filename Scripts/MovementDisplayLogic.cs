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
            return isSelected;
        }
    }
} 