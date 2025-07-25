using Godot;
using System.Collections.Generic;

public class PlayerInteractionLogic
{
    private Unit _selectedUnit;

    public bool CanPlayerSelectUnit(Player player, Unit unit, GamePhase currentPhase)
    {
        // Only allow selection during Move phase
        if (currentPhase != GamePhase.Move)
        {
            return false;
        }

        // Player can only select their own units
        if (!player.Units.Contains(unit))
        {
            return false;
        }

        // Unit must have movement points to be selectable
        if (unit.CurrentMovementPoints <= 0)
        {
            return false;
        }

        return true;
    }

    public Unit GetSelectedUnit()
    {
        return _selectedUnit;
    }

    public bool SelectUnit(Player player, Unit unit, GamePhase currentPhase)
    {
        if (!CanPlayerSelectUnit(player, unit, currentPhase))
        {
            return false;
        }

        _selectedUnit = unit;
        return true;
    }

    public void DeselectUnit()
    {
        _selectedUnit = null;
    }
} 