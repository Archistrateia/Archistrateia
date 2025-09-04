using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Archistrateia
{
    public class TileUnitCoordinator
    {
        private readonly Dictionary<string, Color> _playerColors = new()
        {
            { "Pharaoh", new Color(0.8f, 0.2f, 0.2f) },
            { "General", new Color(0.2f, 0.2f, 0.8f) },
            { "Default", new Color(0.5f, 0.5f, 0.5f) }
        };

        public Color GetPlayerColor(string playerName)
        {
            return _playerColors.ContainsKey(playerName) ? _playerColors[playerName] : _playerColors["Default"];
        }

        public HexTile FindTileWithUnit(Unit unit, Dictionary<Vector2I, HexTile> gameMap)
        {
            foreach (var kvp in gameMap)
            {
                var tile = kvp.Value;
                if (tile.IsOccupied() && ReferenceEquals(tile.OccupyingUnit, unit))
                {
                    return tile;
                }
            }
            return null;
        }

        public Vector2I? FindUnitPosition(Unit unit, Dictionary<Vector2I, HexTile> gameMap)
        {
            var tile = FindTileWithUnit(unit, gameMap);
            return tile?.Position;
        }

        public VisualUnit FindVisualUnit(Unit logicalUnit, IEnumerable<VisualUnit> visualUnits)
        {
            return visualUnits.FirstOrDefault(vu => ReferenceEquals(vu.LogicalUnit, logicalUnit));
        }

        public VisualHexTile FindVisualTile(Vector2I gridPosition, Node2D mapContainer)
        {
            if (mapContainer == null) return null;

            foreach (Node child in mapContainer.GetChildren())
            {
                if (child is VisualHexTile visualTile && visualTile.GridPosition == gridPosition)
                {
                    return visualTile;
                }
            }
            return null;
        }

        public void SynchronizeTileOccupation(Dictionary<Vector2I, HexTile> gameMap, Dictionary<Vector2I, VisualHexTile> visualTiles)
        {
            GD.Print($"🔄 SYNC: Synchronizing tile occupation for {gameMap.Count} tiles");
            
            foreach (var kvp in gameMap)
            {
                var logicalTile = kvp.Value;
                var gridPosition = kvp.Key;
                
                if (visualTiles.ContainsKey(gridPosition))
                {
                    var visualTile = visualTiles[gridPosition];
                    var isOccupied = logicalTile.IsOccupied();
                    
                    visualTile.SetOccupied(isOccupied);
                    
                    if (isOccupied)
                    {
                        var unit = logicalTile.OccupyingUnit;
                        GD.Print($"🔄 SYNC: Tile Grid({gridPosition.X},{gridPosition.Y}) occupied by {unit?.Name ?? "NULL"}");
                    }
                }
            }
        }

        public void UpdateAllVisualComponents(Node2D mapContainer, IEnumerable<VisualUnit> visualUnits)
        {
            // Update all tile visual components
            if (mapContainer != null)
            {
                foreach (Node child in mapContainer.GetChildren())
                {
                    if (child is VisualHexTile visualTile)
                    {
                        visualTile.UpdateVisualComponents();
                    }
                }
            }

            // Update all unit visual components
            foreach (var visualUnit in visualUnits)
            {
                visualUnit.UpdateVisualComponents();
            }
        }

        public void VerifyTileUnitAlignment(VisualUnit unit, Vector2I expectedGridPosition, Node2D mapContainer, VisualPositionManager positionManager)
        {
            var visualTile = FindVisualTile(expectedGridPosition, mapContainer);
            if (visualTile == null)
            {
                GD.Print($"⚠️  ALIGNMENT: Could not find visual tile at Grid({expectedGridPosition.X},{expectedGridPosition.Y})");
                return;
            }

            var unitWorldPosition = unit.Position;
            var tileWorldPosition = visualTile.Position;
            var offset = unitWorldPosition.DistanceTo(tileWorldPosition);

            GD.Print($"🔍 ALIGNMENT: Unit({unit.LogicalUnit.Name}) at Grid({expectedGridPosition.X},{expectedGridPosition.Y})");
            GD.Print($"   Unit World: ({unitWorldPosition.X:F1},{unitWorldPosition.Y:F1})");
            GD.Print($"   Tile World: ({tileWorldPosition.X:F1},{tileWorldPosition.Y:F1})");
            GD.Print($"   Offset: {offset:F1} pixels");

            if (offset > 5.0f)
            {
                GD.Print($"⚠️  ALIGNMENT WARNING: Unit is {offset:F1} pixels away from its tile!");
                
                // Auto-correct the alignment
                var correctedPosition = positionManager.CalculateWorldPosition(expectedGridPosition);
                unit.UpdatePosition(correctedPosition);
                GD.Print($"✅ ALIGNMENT CORRECTED: Unit moved to ({correctedPosition.X:F1},{correctedPosition.Y:F1})");
            }
        }

        public void CreateVisualUnitsForPlayers(
            List<Player> players, 
            Dictionary<Vector2I, HexTile> gameMap, 
            MapRenderer mapRenderer, 
            VisualPositionManager positionManager,
            Node2D mapContainer)
        {
            if (players == null || mapRenderer == null || positionManager == null) return;

            GD.Print($"🎨 Creating visual units for {players.Count} players");

            foreach (var player in players)
            {
                var playerColor = GetPlayerColor(player.Name);
                GD.Print($"🎨 Player {player.Name}: {player.Units.Count} units, color: {playerColor}");

                foreach (var unit in player.Units)
                {
                    var logicalTile = FindTileWithUnit(unit, gameMap);
                    if (logicalTile != null)
                    {
                        var worldPosition = positionManager.CalculateWorldPosition(logicalTile.Position);
                        var visualUnit = mapRenderer.CreateVisualUnit(unit, worldPosition, playerColor);
                        
                        // Verify alignment immediately after creation
                        VerifyTileUnitAlignment(visualUnit, logicalTile.Position, mapContainer, positionManager);
                    }
                    else
                    {
                        GD.Print($"⚠️  Could not find tile for unit {unit.Name}");
                    }
                }
            }
        }

        public void SynchronizeUnitMovement(
            Unit movedUnit, 
            Vector2I newGridPosition, 
            Dictionary<Vector2I, HexTile> gameMap,
            Dictionary<Vector2I, VisualHexTile> visualTiles,
            IEnumerable<VisualUnit> visualUnits,
            VisualPositionManager positionManager)
        {
            GD.Print($"🚀 SYNC MOVEMENT: Unit({movedUnit.Name}) moved to Grid({newGridPosition.X},{newGridPosition.Y})");

            // Update visual unit position
            var visualUnit = FindVisualUnit(movedUnit, visualUnits);
            if (visualUnit != null)
            {
                var newWorldPosition = positionManager.CalculateWorldPosition(newGridPosition);
                visualUnit.UpdatePosition(newWorldPosition);
                visualUnit.UpdateVisualComponents();
                GD.Print($"✅ Visual unit updated to World({newWorldPosition.X:F1},{newWorldPosition.Y:F1})");
            }

            // Synchronize tile occupation status
            SynchronizeTileOccupation(gameMap, visualTiles);
        }
    }
}
