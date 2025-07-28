using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Archistrateia
{
    public class MovementValidationLogic
    {
        public static bool CanUnitMoveTo(Unit unit, HexTile fromTile, HexTile toTile)
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

        public static Vector2I[] GetAdjacentPositions(Vector2I position)
        {
            // FIXED: Proper hex grid adjacency for flat-top orientation
            // Adjacency depends on whether column is even or odd
            
            var adjacents = new List<Vector2I>();
            
            // Common neighbors (always the same)
            adjacents.Add(new Vector2I(position.X - 1, position.Y));     // West
            adjacents.Add(new Vector2I(position.X + 1, position.Y));     // East
            
            // Diagonal neighbors depend on even/odd column
            if (position.X % 2 == 0) // Even column
            {
                adjacents.Add(new Vector2I(position.X - 1, position.Y - 1)); // Northwest
                adjacents.Add(new Vector2I(position.X, position.Y - 1));     // Northeast
                adjacents.Add(new Vector2I(position.X - 1, position.Y + 1)); // Southwest  
                adjacents.Add(new Vector2I(position.X, position.Y + 1));     // Southeast
            }
            else // Odd column
            {
                adjacents.Add(new Vector2I(position.X, position.Y - 1));     // Northwest
                adjacents.Add(new Vector2I(position.X + 1, position.Y - 1)); // Northeast
                adjacents.Add(new Vector2I(position.X, position.Y + 1));     // Southwest
                adjacents.Add(new Vector2I(position.X + 1, position.Y + 1)); // Southeast
            }
            
            return adjacents.ToArray();
        }

        public List<Vector2I> GetValidMovementDestinations(Unit unit, Vector2I currentPosition, Dictionary<Vector2I, HexTile> gameMap)
        {
            var distances = RunDijkstraAlgorithm(unit, currentPosition, gameMap);
            return ExtractValidDestinations(distances, currentPosition, unit.CurrentMovementPoints);
        }

        public Dictionary<Vector2I, int> GetPathCostsFromPosition(Unit unit, Vector2I currentPosition, Dictionary<Vector2I, HexTile> gameMap)
        {
            return RunDijkstraAlgorithm(unit, currentPosition, gameMap);
        }

        private Dictionary<Vector2I, int> RunDijkstraAlgorithm(Unit unit, Vector2I currentPosition, Dictionary<Vector2I, HexTile> gameMap)
        {
            var movementBudget = unit.CurrentMovementPoints;
            var distances = new Dictionary<Vector2I, int>();
            var visited = new HashSet<Vector2I>();
            var priorityQueue = new PriorityQueue<DijkstraNode>();
            
            // Start with current position (cost 0)
            distances[currentPosition] = 0;
            priorityQueue.Enqueue(new DijkstraNode(0, currentPosition));
            
            while (priorityQueue.Count > 0)
            {
                var currentNode = priorityQueue.Dequeue();
                var position = currentNode.Position;
                var currentCost = currentNode.Cost;
                
                if (visited.Contains(position))
                    continue;
                    
                visited.Add(position);
                
                if (currentCost > movementBudget)
                    continue;
                
                ProcessAdjacentPositions(position, currentCost, movementBudget, gameMap, distances, visited, priorityQueue);
            }
            
            return distances;
        }

        private void ProcessAdjacentPositions(Vector2I position, int currentCost, int movementBudget, 
            Dictionary<Vector2I, HexTile> gameMap, Dictionary<Vector2I, int> distances, 
            HashSet<Vector2I> visited, PriorityQueue<DijkstraNode> priorityQueue)
        {
            var adjacentPositions = GetAdjacentPositions(position);
            
            foreach (var adjacentPos in adjacentPositions)
            {
                if (!gameMap.ContainsKey(adjacentPos))
                {
                    continue;
                }
                    
                var tile = gameMap[adjacentPos];
                var newCost = currentCost + tile.MovementCost;
                
                if (newCost > movementBudget)
                {
                    continue;
                }
                    
                if (tile.IsOccupied())
                {
                    continue;
                }
                
                if (!distances.ContainsKey(adjacentPos) || newCost < distances[adjacentPos])
                {
                    distances[adjacentPos] = newCost;
                    priorityQueue.Enqueue(new DijkstraNode(newCost, adjacentPos));
                }
            }
        }

        private List<Vector2I> ExtractValidDestinations(Dictionary<Vector2I, int> distances, Vector2I currentPosition, int movementBudget)
        {
            var validDestinations = new List<Vector2I>();
            
            foreach (var kvp in distances)
            {
                if (kvp.Key != currentPosition && kvp.Value <= movementBudget)
                {
                    validDestinations.Add(kvp.Key);
                }
            }
            
            return validDestinations;
        }
    }
} 