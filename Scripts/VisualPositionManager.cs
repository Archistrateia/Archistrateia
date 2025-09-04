using Godot;
using System.Collections.Generic;

namespace Archistrateia
{
    public class VisualPositionManager
    {
        private Vector2 _gameAreaSize;
        private readonly int _mapWidth;
        private readonly int _mapHeight;

        public VisualPositionManager(Vector2 gameAreaSize, int mapWidth, int mapHeight)
        {
            _gameAreaSize = gameAreaSize;
            _mapWidth = mapWidth;
            _mapHeight = mapHeight;
        }

        public void UpdateGameAreaSize(Vector2 gameAreaSize)
        {
            _gameAreaSize = gameAreaSize;
        }

        public Vector2 CalculateWorldPosition(Vector2I gridPosition)
        {
            return HexGridCalculator.CalculateHexPositionCentered(
                gridPosition.X, 
                gridPosition.Y, 
                _gameAreaSize, 
                _mapWidth, 
                _mapHeight
            );
        }

        public void UpdateTilePosition(VisualHexTile tile)
        {
            var worldPosition = CalculateWorldPosition(tile.GridPosition);
            GD.Print($"🔄 TILE POS UPDATE: Grid({tile.GridPosition.X},{tile.GridPosition.Y}) -> World({worldPosition.X:F1},{worldPosition.Y:F1}) | GameArea({_gameAreaSize.X}x{_gameAreaSize.Y})");
            tile.Position = worldPosition;
        }

        public void UpdateUnitPosition(VisualUnit unit, Vector2I gridPosition)
        {
            var worldPosition = CalculateWorldPosition(gridPosition);
            GD.Print($"🔄 UNIT POS UPDATE: Grid({gridPosition.X},{gridPosition.Y}) -> World({worldPosition.X:F1},{worldPosition.Y:F1}) | Unit({unit.LogicalUnit.Name}) | GameArea({_gameAreaSize.X}x{_gameAreaSize.Y})");
            unit.UpdatePosition(worldPosition);
        }

        public void UpdateAllTilePositions(Node2D mapContainer)
        {
            if (mapContainer == null) return;

            foreach (Node child in mapContainer.GetChildren())
            {
                if (child is VisualHexTile visualTile)
                {
                    UpdateTilePosition(visualTile);
                    visualTile.UpdateVisualComponents();
                }
            }
        }

        public void UpdateAllUnitPositions(IEnumerable<VisualUnit> visualUnits, Dictionary<Vector2I, HexTile> gameMap, TileUnitCoordinator coordinator)
        {
            foreach (var visualUnit in visualUnits)
            {
                var logicalTile = coordinator.FindTileWithUnit(visualUnit.LogicalUnit, gameMap);
                if (logicalTile != null)
                {
                    UpdateUnitPosition(visualUnit, logicalTile.Position);
                    visualUnit.UpdateVisualComponents();
                }
            }
        }

        public void UpdateAllPositions(Node2D mapContainer, IEnumerable<VisualUnit> visualUnits, Dictionary<Vector2I, HexTile> gameMap, TileUnitCoordinator coordinator)
        {
            GD.Print($"🔄 UPDATING ALL POSITIONS: GameArea({_gameAreaSize.X}x{_gameAreaSize.Y})");
            UpdateAllTilePositions(mapContainer);
            UpdateAllUnitPositions(visualUnits, gameMap, coordinator);
        }

        public void VerifyTileUnitAlignment(VisualUnit unit, Vector2I expectedGridPosition, Node2D mapContainer)
        {
            var unitWorldPosition = unit.Position;
            var expectedWorldPosition = CalculateWorldPosition(expectedGridPosition);
            
            Vector2 actualTilePosition = Vector2.Zero;
            if (mapContainer != null)
            {
                foreach (Node child in mapContainer.GetChildren())
                {
                    if (child is VisualHexTile visualTile && visualTile.GridPosition == expectedGridPosition)
                    {
                        actualTilePosition = visualTile.Position;
                        break;
                    }
                }
            }
            
            var unitTileOffset = unitWorldPosition.DistanceTo(actualTilePosition);
            var expectedOffset = expectedWorldPosition.DistanceTo(actualTilePosition);
            
            GD.Print($"🔍 ALIGNMENT CHECK: Unit({unit.LogicalUnit.Name}) at Grid({expectedGridPosition.X},{expectedGridPosition.Y})");
            GD.Print($"   Unit World: ({unitWorldPosition.X:F1},{unitWorldPosition.Y:F1})");
            GD.Print($"   Expected:   ({expectedWorldPosition.X:F1},{expectedWorldPosition.Y:F1})");
            GD.Print($"   Tile World: ({actualTilePosition.X:F1},{actualTilePosition.Y:F1})");
            GD.Print($"   Unit-Tile Offset: {unitTileOffset:F1} pixels");
            GD.Print($"   Expected-Tile Offset: {expectedOffset:F1} pixels");
            
            if (unitTileOffset > 5.0f)
            {
                GD.Print($"⚠️  ALIGNMENT WARNING: Unit is {unitTileOffset:F1} pixels away from its tile!");
            }
        }

    }
}
