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
        var validDestinations = new List<Vector2I>();
        var movementBudget = unit.CurrentMovementPoints;
        
        // FIXED Dijkstra's algorithm - ensures complete exploration of all paths within budget
        var distances = new Dictionary<Vector2I, int>();
        var visited = new HashSet<Vector2I>();
        var priorityQueue = new PriorityQueue<DijkstraNode>();
        
        // Start with current position (cost 0)
        distances[currentPosition] = 0;
        priorityQueue.Enqueue(new DijkstraNode(0, currentPosition));
        
        while (priorityQueue.Count > 0)
        {
            // Get the position with lowest cost
            var currentNode = priorityQueue.Dequeue();
            var position = currentNode.Position;
            var currentCost = currentNode.Cost;
            
            // CRITICAL FIX: Only skip if we've already processed this position optimally
            // This prevents infinite loops while ensuring all optimal paths are explored
            if (visited.Contains(position))
                continue;
                
            // Mark as visited NOW - this is when we've found the optimal path to this position
            visited.Add(position);
            
            // CRITICAL FIX: Continue exploring even if current cost equals budget
            // This ensures we find all reachable tiles at exactly the budget limit
            if (currentCost > movementBudget)
                continue;
            
            // Check all adjacent positions
            var adjacentPositions = GetAdjacentPositions(position);
            
            foreach (var adjacentPos in adjacentPositions)
            {
                if (!gameMap.ContainsKey(adjacentPos))
                {
                    continue;
                }
                
                // CRITICAL FIX: Removed premature visited check here!
                // We should only skip visited nodes at dequeue time, not when considering adjacent nodes.
                // This allows Dijkstra's to find better paths to already-visited nodes.
                    
                var tile = gameMap[adjacentPos];
                var newCost = currentCost + tile.MovementCost;
                
                // CRITICAL FIX: Only skip if cost EXCEEDS budget, not equals
                // This allows tiles exactly at budget to be processed
                if (newCost > movementBudget)
                {
                    continue;
                }
                    
                // Skip if tile is occupied (can't end movement there)
                if (tile.IsOccupied())
                {
                    continue;
                }
                
                // If we found a better path to this position, update it
                if (!distances.ContainsKey(adjacentPos) || newCost < distances[adjacentPos])
                {
                    distances[adjacentPos] = newCost;
                    priorityQueue.Enqueue(new DijkstraNode(newCost, adjacentPos));
                }
            }
        }
        
        // Return all positions we can reach (excluding starting position)
        foreach (var kvp in distances)
        {
            if (kvp.Key != currentPosition && kvp.Value <= movementBudget)
            {
                validDestinations.Add(kvp.Key);
            }
        }

        // Pathfinding logging removed for cleaner output
        
        return validDestinations;
    }

    public Dictionary<Vector2I, int> GetPathCostsFromPosition(Unit unit, Vector2I currentPosition, Dictionary<Vector2I, HexTile> gameMap)
    {
        // Return the path costs dictionary that Dijkstra's algorithm calculates
        // This allows MovementCoordinator to get the actual path cost to any destination
        var movementBudget = unit.CurrentMovementPoints;
        
        var distances = new Dictionary<Vector2I, int>();
        var visited = new HashSet<Vector2I>();
        var priorityQueue = new PriorityQueue<DijkstraNode>();
        
        // Start with current position (cost 0)
        distances[currentPosition] = 0;
        priorityQueue.Enqueue(new DijkstraNode(0, currentPosition));
        
        while (priorityQueue.Count > 0)
        {
            // Get the position with lowest cost
            var currentNode = priorityQueue.Dequeue();
            var position = currentNode.Position;
            var currentCost = currentNode.Cost;
            
            // Only skip if we've already processed this position optimally
            if (visited.Contains(position))
                continue;
                
            // Mark as visited NOW - this is when we've found the optimal path to this position
            visited.Add(position);
            
            // Continue exploring even if current cost equals budget
            if (currentCost > movementBudget)
                continue;
            
            // Check all adjacent positions
            var adjacentPositions = GetAdjacentPositions(position);
            
            foreach (var adjacentPos in adjacentPositions)
            {
                if (!gameMap.ContainsKey(adjacentPos))
                {
                    continue;
                }
                
                // We should only skip visited nodes at dequeue time, not when considering adjacent nodes.
                // This allows Dijkstra's to find better paths to already-visited nodes.
                    
                var tile = gameMap[adjacentPos];
                var newCost = currentCost + tile.MovementCost;
                
                // Only skip if cost EXCEEDS budget, not equals
                if (newCost > movementBudget)
                {
                    continue;
                }
                    
                // Skip if tile is occupied (can't end movement there)
                if (tile.IsOccupied())
                {
                    continue;
                }
                
                // If we found a better path to this position, update it
                if (!distances.ContainsKey(adjacentPos) || newCost < distances[adjacentPos])
                {
                    distances[adjacentPos] = newCost;
                    priorityQueue.Enqueue(new DijkstraNode(newCost, adjacentPos));
                }
            }
        }
        
        return distances;
    }
} 