using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Archistrateia
{
    public partial class GodotMovementSystem : Node
    {
        private NavigationRegion2D _navigation;
        private AStar2D _pathfinding;
        private Dictionary<Vector2I, Vector2> _gridToWorldMap;
        private Dictionary<Vector2, Vector2I> _worldToGridMap;
        private bool _isInitialized = false;
        
        public override void _Ready()
        {
            try
            {
                _navigation = new NavigationRegion2D();
                _pathfinding = new AStar2D();
                _gridToWorldMap = new Dictionary<Vector2I, Vector2>();
                _worldToGridMap = new Dictionary<Vector2, Vector2I>();
                
                AddChild(_navigation);
                _isInitialized = true;
            }
            catch (System.Exception e)
            {
                GD.PrintErr($"Failed to initialize GodotMovementSystem: {e.Message}");
                _isInitialized = false;
            }
        }
        
        // Constructor for test environments
        public GodotMovementSystem(bool forTesting = false)
        {
            if (forTesting)
            {
                _navigation = null;
                _pathfinding = new AStar2D();
                _gridToWorldMap = new Dictionary<Vector2I, Vector2>();
                _worldToGridMap = new Dictionary<Vector2, Vector2I>();
                _isInitialized = true;
            }
            else
            {
                // For non-testing, let _Ready() handle initialization
                _isInitialized = false;
            }
        }

        public void InitializeNavigation(Dictionary<Vector2I, HexTile> gameMap)
        {
            _gridToWorldMap.Clear();
            _worldToGridMap.Clear();
            
            // Only clear pathfinding if it exists and we're not in test mode
            if (_pathfinding != null)
            {
                _pathfinding.Clear();
            }
            
            foreach (var kvp in gameMap)
            {
                var gridPos = kvp.Key;
                var worldPos = HexGridCalculator.CalculateHexPosition(gridPos.X, gridPos.Y);
                
                _gridToWorldMap[gridPos] = worldPos;
                _worldToGridMap[worldPos] = gridPos;
                
                var pointId = _pathfinding.GetAvailablePointId();
                _pathfinding.AddPoint(pointId, worldPos);
                
                // Set the point weight based on terrain cost
                var terrainCost = kvp.Value.MovementCost;
                _pathfinding.SetPointWeightScale(pointId, terrainCost);
            }
            
            ConnectAdjacentTiles(gameMap);
        }

        private void ConnectAdjacentTiles(Dictionary<Vector2I, HexTile> gameMap)
        {
            foreach (var kvp in gameMap)
            {
                var gridPos = kvp.Key;
                var adjacentPositions = MovementValidationLogic.GetAdjacentPositions(gridPos);
                
                foreach (var adjacentPos in adjacentPositions)
                {
                    if (gameMap.ContainsKey(adjacentPos))
                    {
                        var fromWorldPos = _gridToWorldMap[gridPos];
                        var toWorldPos = _gridToWorldMap[adjacentPos];
                        
                        var fromPointId = _pathfinding.GetClosestPoint(fromWorldPos);
                        var toPointId = _pathfinding.GetClosestPoint(toWorldPos);
                        
                        if (fromPointId != -1 && toPointId != -1)
                        {
                            // Connect the points - AStar2D will use the point weights for cost calculation
                            _pathfinding.ConnectPoints(fromPointId, toPointId, true);
                        }
                    }
                }
            }
        }



        public List<Vector2I> FindPath(Vector2I from, Vector2I to, Dictionary<Vector2I, HexTile> gameMap)
        {
            if (!_gridToWorldMap.ContainsKey(from) || !_gridToWorldMap.ContainsKey(to))
            {
                return new List<Vector2I>();
            }

            var fromWorld = _gridToWorldMap[from];
            var toWorld = _gridToWorldMap[to];
            
            var fromPointId = _pathfinding.GetClosestPoint(fromWorld);
            var toPointId = _pathfinding.GetClosestPoint(toWorld);
            
            if (fromPointId == -1 || toPointId == -1)
            {
                return new List<Vector2I>();
            }

            var path = _pathfinding.GetPointPath(fromPointId, toPointId);
            var gridPath = new List<Vector2I>();
            
            foreach (var worldPoint in path)
            {
                if (_worldToGridMap.ContainsKey(worldPoint))
                {
                    gridPath.Add(_worldToGridMap[worldPoint]);
                }
            }
            
            return gridPath;
        }

        public float GetPathCost(Vector2I from, Vector2I to, Dictionary<Vector2I, HexTile> gameMap)
        {
            if (!_gridToWorldMap.ContainsKey(from) || !_gridToWorldMap.ContainsKey(to))
            {
                return float.MaxValue;
            }

            // For adjacent tiles, return the direct terrain cost
            if (IsAdjacent(from, to))
            {
                var toTile = gameMap[to];
                return toTile.MovementCost;
            }

            var fromWorld = _gridToWorldMap[from];
            var toWorld = _gridToWorldMap[to];
            
            var fromPointId = _pathfinding.GetClosestPoint(fromWorld);
            var toPointId = _pathfinding.GetClosestPoint(toWorld);
            
            if (fromPointId == -1 || toPointId == -1)
            {
                return float.MaxValue;
            }

            // Get the path and calculate total cost based on terrain
            var path = _pathfinding.GetPointPath(fromPointId, toPointId);
            if (path.Length < 2)
            {
                return 0;
            }

            // Calculate total cost by summing the terrain costs of the path
            float totalCost = 0;
            for (int i = 1; i < path.Length; i++) // Start from 1 to skip the starting point
            {
                var worldPoint = path[i];
                if (_worldToGridMap.ContainsKey(worldPoint))
                {
                    var gridPos = _worldToGridMap[worldPoint];
                    var tile = gameMap[gridPos];
                    totalCost += tile.MovementCost;
                }
            }
            
            return totalCost;
        }

        private bool IsAdjacent(Vector2I from, Vector2I to)
        {
            var adjacentPositions = MovementValidationLogic.GetAdjacentPositions(from);
            return adjacentPositions.Contains(to);
        }

        public List<Vector2I> GetReachablePositions(Vector2I from, int maxMovement, Dictionary<Vector2I, HexTile> gameMap)
        {
            var reachable = new List<Vector2I>();
            
            // If no movement points, no destinations are reachable (including current position)
            if (maxMovement <= 0)
            {
                return reachable;
            }
            
            var visited = new HashSet<Vector2I>();
            var queue = new Queue<(Vector2I pos, float cost)>();
            
            queue.Enqueue((from, 0));
            visited.Add(from);
            
            while (queue.Count > 0)
            {
                var (currentPos, currentCost) = queue.Dequeue();
                
                if (currentCost <= maxMovement)
                {
                    // Only add current position if it's not occupied (allows starting from occupied position)
                    if (currentPos == from || !gameMap[currentPos].IsOccupied())
                    {
                        reachable.Add(currentPos);
                    }
                    
                    var adjacentPositions = MovementValidationLogic.GetAdjacentPositions(currentPos);
                    foreach (var adjacentPos in adjacentPositions)
                    {
                        if (gameMap.ContainsKey(adjacentPos) && !visited.Contains(adjacentPos))
                        {
                            var tile = gameMap[adjacentPos];
                            
                            // Skip occupied tiles
                            if (tile.IsOccupied())
                            {
                                continue;
                            }
                            
                            var newCost = currentCost + tile.MovementCost;
                            
                            if (newCost <= maxMovement)
                            {
                                visited.Add(adjacentPos);
                                queue.Enqueue((adjacentPos, newCost));
                            }
                        }
                    }
                }
            }
            
            return reachable;
        }

        public async Task AnimateMovementAlongPath(VisualUnit visualUnit, List<Vector2I> path, float duration = 0.5f)
        {
            if (path.Count < 2)
            {
                return;
            }

            var tween = CreateTween();
            tween.SetLoops(0);
            
            for (int i = 1; i < path.Count; i++)
            {
                var worldPos = HexGridCalculator.CalculateHexPosition(path[i].X, path[i].Y);
                var segmentDuration = duration / (path.Count - 1);
                
                tween.TweenProperty(visualUnit, "position", worldPos, segmentDuration)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Quad);
                
                if (i < path.Count - 1)
                {
                    await ToSignal(tween, "finished");
                }
            }
        }
    }
}
