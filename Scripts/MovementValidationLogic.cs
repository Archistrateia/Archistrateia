using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Archistrateia
{
    public class MovementValidationLogic
    {
        private static GodotMovementSystem _movementSystem;

        public static void SetMovementSystem(GodotMovementSystem movementSystem)
        {
            _movementSystem = movementSystem;
        }

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
            var adjacentPositions = GetAdjacentPositions(fromTile.Position);
            if (!adjacentPositions.ToList().Contains(toTile.Position))
            {
                return false;
            }
            
            return true;
        }

        public static Vector2I[] GetAdjacentPositions(Vector2I position)
        {
            // FIXED: Proper hex grid adjacency for pointy-top orientation using offset coordinates
            // Based on actual Godot hex grid layout where odd columns are offset downward
            
            var adjacents = new List<Vector2I>();
            
            // For pointy-top hex grid with offset coordinates, adjacency depends on even/odd columns
            if (position.X % 2 == 0) // Even column (like column 4)
            {
                // Even columns: diagonals go up-left and down-left relative to neighbors
                adjacents.Add(new Vector2I(position.X - 1, position.Y - 1)); // northwest
                adjacents.Add(new Vector2I(position.X - 1, position.Y));     // west
                adjacents.Add(new Vector2I(position.X, position.Y + 1));     // southwest  
                adjacents.Add(new Vector2I(position.X + 1, position.Y));     // southeast
                adjacents.Add(new Vector2I(position.X + 1, position.Y - 1)); // northeast
                adjacents.Add(new Vector2I(position.X, position.Y - 1));     // north
            }
            else // Odd column  
            {
                // Odd columns: diagonals go up-right and down-right relative to neighbors
                adjacents.Add(new Vector2I(position.X - 1, position.Y));     // northwest
                adjacents.Add(new Vector2I(position.X - 1, position.Y + 1)); // west
                adjacents.Add(new Vector2I(position.X, position.Y + 1));     // southwest
                adjacents.Add(new Vector2I(position.X + 1, position.Y + 1)); // southeast  
                adjacents.Add(new Vector2I(position.X + 1, position.Y));     // northeast
                adjacents.Add(new Vector2I(position.X, position.Y - 1));     // north
            }
            
            return adjacents.ToArray();
        }

        public static List<Vector2I> GetValidMovementDestinations(Unit unit, Vector2I currentPosition, Dictionary<Vector2I, HexTile> gameMap)
        {
            if (_movementSystem != null)
            {
                return _movementSystem.GetReachablePositions(currentPosition, unit.CurrentMovementPoints, gameMap);
            }
            
            // Auto-create a movement system for testing scenarios
            var autoSystem = new GodotMovementSystem(forTesting: true);
            autoSystem.InitializeNavigation(gameMap);
            var result = autoSystem.GetReachablePositions(currentPosition, unit.CurrentMovementPoints, gameMap);
            
            // Clean up the auto-created system
            autoSystem.QueueFree();
            return result;
        }

        public static Dictionary<Vector2I, int> GetPathCostsFromPosition(Unit unit, Vector2I currentPosition, Dictionary<Vector2I, HexTile> gameMap)
        {
            if (_movementSystem != null)
            {
                var pathCosts = new Dictionary<Vector2I, int>();
                var reachablePositions = _movementSystem.GetReachablePositions(currentPosition, unit.CurrentMovementPoints, gameMap);
                
                foreach (var pos in reachablePositions)
                {
                    var cost = (int)_movementSystem.GetPathCost(currentPosition, pos, gameMap);
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

        public static int GetOptimalPathCost(Vector2I from, Vector2I to, Dictionary<Vector2I, HexTile> gameMap)
        {
            if (_movementSystem != null)
            {
                var cost = (int)_movementSystem.GetPathCost(from, to, gameMap);
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