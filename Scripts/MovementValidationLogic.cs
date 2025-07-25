using Godot;
using System.Collections.Generic;
using System.Linq;

public class MovementValidationLogic
{
    public bool CanUnitMoveTo(Unit unit, HexTile fromTile, HexTile toTile)
    {
        // Check if unit has enough movement points
        if (unit.CurrentMovementPoints < toTile.MovementCost)
        {
            return false;
        }

        // Check if destination tile is occupied
        if (toTile.IsOccupied())
        {
            return false;
        }

        return true;
    }

    public Vector2I[] GetAdjacentPositions(Vector2I position)
    {
        // Hex grid adjacent positions (flat-top orientation)
        return new Vector2I[]
        {
            new Vector2I(position.X + 1, position.Y),     // East
            new Vector2I(position.X - 1, position.Y),     // West
            new Vector2I(position.X, position.Y + 1),     // Southeast
            new Vector2I(position.X, position.Y - 1),     // Northwest
            new Vector2I(position.X + 1, position.Y - 1), // Northeast
            new Vector2I(position.X - 1, position.Y + 1)  // Southwest
        };
    }

    public List<Vector2I> GetValidMovementDestinations(Unit unit, Vector2I currentPosition, Dictionary<Vector2I, HexTile> gameMap)
    {
        var validDestinations = new List<Vector2I>();
        var adjacentPositions = GetAdjacentPositions(currentPosition);

        foreach (var position in adjacentPositions)
        {
            if (gameMap.ContainsKey(position))
            {
                var tile = gameMap[position];
                if (CanUnitMoveTo(unit, gameMap[currentPosition], tile))
                {
                    validDestinations.Add(position);
                }
            }
        }

        return validDestinations;
    }
} 