using Godot;
using System.Collections.Generic;

public class MovementCoordinator
{
    private Unit _selectedUnit;
    private List<Vector2I> _validDestinations = new List<Vector2I>();
    private MovementValidationLogic _movementLogic = new MovementValidationLogic();

    public void SelectUnitForMovement(Unit unit)
    {
        _selectedUnit = unit;
        _validDestinations.Clear(); // Clear previous destinations
    }

    public Unit GetSelectedUnit()
    {
        return _selectedUnit;
    }

    public bool IsAwaitingDestination()
    {
        return _selectedUnit != null;
    }

    public void ClearSelection()
    {
        _selectedUnit = null;
        _validDestinations.Clear();
    }

    public List<Vector2I> GetValidDestinations(Vector2I currentPosition, Dictionary<Vector2I, HexTile> gameMap)
    {
        if (_selectedUnit == null)
        {
            return new List<Vector2I>();
        }

        _validDestinations = _movementLogic.GetValidMovementDestinations(_selectedUnit, currentPosition, gameMap);
        return _validDestinations;
    }

    public MoveResult TryMoveToDestination(Vector2I fromPosition, Vector2I toPosition, Dictionary<Vector2I, HexTile> gameMap)
    {
        if (_selectedUnit == null)
        {
            return MoveResult.CreateFailure("No unit selected");
        }

        if (!gameMap.ContainsKey(fromPosition) || !gameMap.ContainsKey(toPosition))
        {
            return MoveResult.CreateFailure("Invalid position");
        }

        // Ensure valid destinations are calculated if not already done
        if (_validDestinations.Count == 0)
        {
            _validDestinations = _movementLogic.GetValidMovementDestinations(_selectedUnit, fromPosition, gameMap);
        }

        // Check if destination is in the list of valid destinations
        if (!_validDestinations.Contains(toPosition))
        {
            return MoveResult.CreateFailure("Destination is not a valid move for this unit");
        }

        var fromTile = gameMap[fromPosition];
        var toTile = gameMap[toPosition];

        if (!_movementLogic.CanUnitMoveTo(_selectedUnit, fromTile, toTile))
        {
            return MoveResult.CreateFailure("Cannot move to destination - insufficient movement or tile occupied");
        }

        // Execute the movement
        fromTile.RemoveUnit();
        toTile.MoveUnitTo(_selectedUnit);
        
        // Clear selection after successful move
        ClearSelection();

        return MoveResult.CreateSuccess(toPosition);
    }

    public TileClickResult HandleTileClick(Vector2I clickPosition, Dictionary<Vector2I, HexTile> gameMap)
    {
        if (_selectedUnit == null)
        {
            return TileClickResult.CreateError("No unit selected");
        }

        if (!gameMap.ContainsKey(clickPosition))
        {
            return TileClickResult.CreateError("Invalid tile position");
        }

        var tile = gameMap[clickPosition];
        
        if (tile.IsOccupied())
        {
            return TileClickResult.CreateError("Tile is occupied");
        }

        return TileClickResult.CreateMovementAttempt(clickPosition);
    }
} 