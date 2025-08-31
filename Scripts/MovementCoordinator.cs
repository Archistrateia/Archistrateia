using Godot;
using System.Collections.Generic;
using Archistrateia;

public class MovementCoordinator
{
    private Unit _selectedUnit;
    private List<Vector2I> _validDestinations = new List<Vector2I>();
    private MovementValidationLogic _movementLogic = new MovementValidationLogic();

    public void SelectUnitForMovement(Unit unit)
    {
        _selectedUnit = unit;
        _validDestinations.Clear();
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

        _validDestinations.Clear();
        _validDestinations = MovementValidationLogic.GetValidMovementDestinations(_selectedUnit, currentPosition, gameMap);
        
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

        if (_validDestinations.Count == 0)
        {
            _validDestinations = MovementValidationLogic.GetValidMovementDestinations(_selectedUnit, fromPosition, gameMap);
        }

        if (!_validDestinations.Contains(toPosition))
        {
            return MoveResult.CreateFailure("Destination is not a valid move for this unit");
        }

        var fromTile = gameMap[fromPosition];
        var toTile = gameMap[toPosition];

        if (!MovementValidationLogic.CanUnitMoveTo(_selectedUnit, fromTile, toTile))
        {
            return MoveResult.CreateFailure("Cannot move to destination - insufficient movement or tile occupied");
        }

        var pathCost = GetPathCostToDestination(fromPosition, toPosition, gameMap);
        
        fromTile.RemoveUnit();
        toTile.PlaceUnit(_selectedUnit);
        _selectedUnit.CurrentMovementPoints -= pathCost;
        
        _validDestinations.Clear();
        
        if (_selectedUnit.CurrentMovementPoints <= 0)
        {
            ClearSelection();
        }

        return MoveResult.CreateSuccess(toPosition);
    }

    private int GetPathCostToDestination(Vector2I fromPosition, Vector2I toPosition, Dictionary<Vector2I, HexTile> gameMap)
    {
        // Use the new optimal path cost calculation for more accurate results
        var optimalPathCost = MovementValidationLogic.GetOptimalPathCost(fromPosition, toPosition, gameMap);
        
        if (optimalPathCost != int.MaxValue)
        {
            return optimalPathCost;
        }
        
        // Fallback to the old method if optimal path not found
        var pathCosts = MovementValidationLogic.GetPathCostsFromPosition(_selectedUnit, fromPosition, gameMap);
        
        if (pathCosts.ContainsKey(toPosition))
        {
            return pathCosts[toPosition];
        }
        
        return gameMap[toPosition].MovementCost;
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

        // Check if the clicked tile is a valid movement destination
        if (_validDestinations.Count == 0)
        {
            // Find the current position of the selected unit in the game map
            Vector2I currentPosition = Vector2I.Zero;
            foreach (var kvp in gameMap)
            {
                if (kvp.Value.IsOccupied() && kvp.Value.OccupyingUnit == _selectedUnit)
                {
                    currentPosition = kvp.Key;
                    break;
                }
            }
            
            _validDestinations = MovementValidationLogic.GetValidMovementDestinations(_selectedUnit, currentPosition, gameMap);
        }

        if (_validDestinations.Contains(clickPosition))
        {
            return TileClickResult.CreateMovementAttempt(clickPosition);
        }

        return TileClickResult.CreateError("Destination is not a valid move for this unit");
    }
} 