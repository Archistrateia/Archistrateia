using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Archistrateia
{
    public class MovementValidationLogic
    {
        public static bool CanUnitMoveTo(Unit unit, HexTile fromTile, HexTile toTile)
        {
            if (unit.CurrentMovementPoints < toTile.MovementCost)
            {
                return false;
            }
            
            if (toTile.IsOccupied())
            {
                return false;
            }
            
            // Check if the destination is actually adjacent (single-step movement only)
            if (!HexAdjacencyCalculator.ArePositionsAdjacent(fromTile.Position, toTile.Position))
            {
                return false;
            }
            
            return true;
        }

        public static Vector2I[] GetAdjacentPositions(Vector2I position)
        {
            // Delegate to centralized hex adjacency calculator
            return HexAdjacencyCalculator.GetAdjacentPositions(position);
        }

        public static List<Vector2I> GetValidMovementDestinations(
            Unit unit,
            Vector2I currentPosition,
            Dictionary<Vector2I, HexTile> gameMap,
            GodotMovementSystem movementSystem = null)
        {
            if (movementSystem != null)
            {
                return movementSystem.GetReachablePositions(currentPosition, unit.CurrentMovementPoints, gameMap);
            }
            
            // Auto-create a movement system for testing scenarios
            var autoSystem = new GodotMovementSystem(forTesting: true);
            autoSystem.InitializeNavigation(gameMap);
            var result = autoSystem.GetReachablePositions(currentPosition, unit.CurrentMovementPoints, gameMap);
            
            // Clean up the auto-created system
            autoSystem.QueueFree();
            return result;
        }

        public static Dictionary<Vector2I, int> GetPathCostsFromPosition(
            Unit unit,
            Vector2I currentPosition,
            Dictionary<Vector2I, HexTile> gameMap,
            GodotMovementSystem movementSystem = null)
        {
            if (movementSystem != null)
            {
                var pathCosts = new Dictionary<Vector2I, int>();
                var reachablePositions = movementSystem.GetReachablePositions(currentPosition, unit.CurrentMovementPoints, gameMap);
                
                foreach (var pos in reachablePositions)
                {
                    var cost = (int)movementSystem.GetPathCost(currentPosition, pos, gameMap);
                    pathCosts[pos] = cost;
                }
                
                return pathCosts;
            }
            
            // Auto-create a movement system for testing scenarios
            var autoSystem = new GodotMovementSystem(forTesting: true);
            autoSystem.InitializeNavigation(gameMap);
            var autoPathCosts = new Dictionary<Vector2I, int>();
            var autoReachablePositions = autoSystem.GetReachablePositions(currentPosition, unit.CurrentMovementPoints, gameMap);
            
            foreach (var pos in autoReachablePositions)
            {
                var autoCost = (int)autoSystem.GetPathCost(currentPosition, pos, gameMap);
                autoPathCosts[pos] = autoCost;
            }
            
            // Clean up the auto-created system
            autoSystem.QueueFree();
            return autoPathCosts;
        }

        public static int GetOptimalPathCost(
            Vector2I from,
            Vector2I to,
            Dictionary<Vector2I, HexTile> gameMap,
            GodotMovementSystem movementSystem = null)
        {
            if (movementSystem != null)
            {
                var cost = (int)movementSystem.GetPathCost(from, to, gameMap);
                return cost;
            }
            
            // Auto-create a movement system for testing scenarios
            var autoSystem = new GodotMovementSystem(forTesting: true);
            autoSystem.InitializeNavigation(gameMap);
            var autoCost = (int)autoSystem.GetPathCost(from, to, gameMap);
            
            // Clean up the auto-created system
            autoSystem.QueueFree();
            return autoCost;
        }
    }
} 
