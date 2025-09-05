using Godot;
using System.Collections.Generic;

namespace Archistrateia
{
    public partial class MapRenderer : Node2D
    {
        public GameManager GameManager { get; private set; }
        private List<VisualUnit> _visualUnits = new List<VisualUnit>();
        private Dictionary<Vector2I, VisualHexTile> _visualTiles = new Dictionary<Vector2I, VisualHexTile>();
        private PlayerInteractionLogic _interactionLogic = new PlayerInteractionLogic();
        private MovementCoordinator _movementCoordinator = new MovementCoordinator();
        private Player _currentPlayer;
        private GamePhase _currentPhase;
        private InformationPanel _informationPanel;
        private TileUnitCoordinator _tileUnitCoordinator;
        private Node2D _mapContainer;

        public void Initialize(GameManager gameManager, TileUnitCoordinator tileUnitCoordinator = null, Node2D mapContainer = null)
        {
            GameManager = gameManager;
            _tileUnitCoordinator = tileUnitCoordinator ?? new TileUnitCoordinator();
            _mapContainer = mapContainer;
            ZIndex = 5; // Ensure MapRenderer is above tiles but below units
            CreateInformationPanel();
        }

        private void CreateInformationPanel()
        {
            _informationPanel = new InformationPanel();
            _informationPanel.Name = "InformationPanel";
            _informationPanel.ZIndex = 2000;
            GetTree().CurrentScene.AddChild(_informationPanel);
        }

        public void SetCurrentPlayer(Player player)
        {
            _currentPlayer = player;
        }

        public void SetCurrentPhase(GamePhase phase)
        {
            var oldPhase = _currentPhase;
            _currentPhase = phase;
            
            // Update movement phase state for all tiles
            bool isMovementPhase = (phase == GamePhase.Move);
            foreach (var tile in _visualTiles.Values)
            {
                tile.SetMovementPhase(isMovementPhase);
            }
            
            // Clear movement displays when leaving movement phase
            if (oldPhase == GamePhase.Move && phase != GamePhase.Move)
            {
                ClearAllHighlights();
                DeselectAll();
                ClearAllUnitMovementDisplays();
            }
        }

        public GamePhase GetCurrentPhase()
        {
            return _currentPhase;
        }
        
        public void ClearAllTiles()
        {
            GD.Print("🧹 MapRenderer: Clearing all tile references");
            _visualTiles.Clear();
            
            // Also clear any highlights or selections that reference old tiles
            ClearAllHighlights();
            DeselectAll();
            ClearAllUnitMovementDisplays();
        }
        
        public void ClearAllUnits()
        {
            GD.Print("🧹 MapRenderer: Clearing all visual units");
            
            // Disconnect signals and free visual units
            foreach (var visualUnit in _visualUnits)
            {
                if (visualUnit.IsConnected(VisualUnit.SignalName.UnitClicked, new Callable(this, MethodName.OnUnitClicked)))
                {
                    visualUnit.UnitClicked -= OnUnitClicked;
                }
                visualUnit.QueueFree();
            }
            
            _visualUnits.Clear();
            
            // Clear any unit-related selections
            _interactionLogic.DeselectUnit();
            _movementCoordinator.ClearSelection();
        }

        public Player GetCurrentPlayer()
        {
            return _currentPlayer;
        }

        public List<VisualUnit> GetVisualUnits()
        {
            return _visualUnits;
        }

        public VisualUnit CreateVisualUnit(Unit logicalUnit, Vector2 position, Color color)
        {
            var visualUnit = new VisualUnit();
            
            // Add units to the same container as tiles to ensure consistent coordinate system
            var mapContainer = GetMapContainer();
            if (mapContainer != null)
            {
                mapContainer.AddChild(visualUnit);
                GD.Print($"🔧 CONTAINER: Adding unit to map container");
            }
            else
            {
                AddChild(visualUnit);
                GD.Print($"⚠️ CONTAINER: No map container found, adding to MapRenderer");
            }
            
            visualUnit.Initialize(logicalUnit, position, color);
            visualUnit.SetMapRenderer(this); // Set the MapRenderer reference
            visualUnit.UnitClicked += OnUnitClicked;
            
            _visualUnits.Add(visualUnit);
            
            // Find where this unit is logically positioned
            var logicalPosition = FindUnitPosition(logicalUnit);
            GD.Print($"🔥 UNIT CREATE Debug: Unit({logicalUnit.Name}) LogicalGrid({logicalPosition?.X ?? -1},{logicalPosition?.Y ?? -1}) VisualWorld({position.X:F1},{position.Y:F1})");
            
            return visualUnit;
        }
        
        private Node2D GetMapContainer()
        {
            return _mapContainer;
        }

        public void OnUnitClicked(VisualUnit clickedUnit)
        {
            var gridPos = FindUnitPosition(clickedUnit.LogicalUnit) ?? new Vector2I(-1, -1);
            GD.Print($"🖱️ UNIT CLICK Debug: Unit({clickedUnit.LogicalUnit.Name}) at Grid({gridPos.X},{gridPos.Y}) World({clickedUnit.Position.X:F1},{clickedUnit.Position.Y:F1})");
            
            GD.Print($"🎯 MapRenderer: Unit {clickedUnit.LogicalUnit.Name} clicked!");
            GD.Print($"   Current Player: {_currentPlayer?.Name ?? "NULL"}");
            GD.Print($"   Current Phase: {_currentPhase}");
            GD.Print($"   Unit Owner: {GetUnitOwner(clickedUnit.LogicalUnit)?.Name ?? "UNKNOWN"}");
            
            if (_currentPlayer == null)
            {
                GD.Print("❌ No current player set - ignoring click");
                return;
            }

            // Try to select the unit
            var wasSelected = _interactionLogic.SelectUnit(_currentPlayer, clickedUnit.LogicalUnit, _currentPhase);
            
            if (wasSelected)
            {
                GD.Print($"✅ Successfully selected {clickedUnit.LogicalUnit.Name}");
                UpdateVisualSelection();
                ShowValidMovementDestinations(clickedUnit.LogicalUnit);
            }
            else
            {
                GD.Print($"❌ Cannot select {clickedUnit.LogicalUnit.Name} - invalid selection");
            }
        }

        private Player GetUnitOwner(Unit unit)
        {
            if (GameManager?.Players != null)
            {
                foreach (var player in GameManager.Players)
                {
                    if (player.Units.Contains(unit))
                    {
                        return player;
                    }
                }
            }
            return null;
        }

        private void UpdateVisualSelection()
        {
            var selectedUnit = _interactionLogic.GetSelectedUnit();
            
            // Update visual state for all units
            foreach (var visualUnit in _visualUnits)
            {
                var isSelected = visualUnit.LogicalUnit == selectedUnit;
                visualUnit.SetSelected(isSelected);
            }
        }

        public static void UpdateVisualUnitPosition(VisualUnit visualUnit, Vector2 newPosition)
        {
            visualUnit.UpdatePosition(newPosition);
        }

        public void RemoveVisualUnit(VisualUnit visualUnit)
        {
            if (_visualUnits.Remove(visualUnit))
            {
                visualUnit.QueueFree();
            }
        }

        public Unit GetSelectedUnit()
        {
            return _interactionLogic.GetSelectedUnit();
        }

        public void DeselectAll()
        {
            _interactionLogic.DeselectUnit();
            _movementCoordinator.ClearSelection();
            UpdateVisualSelection();
            ClearAllHighlights();
            ClearAllUnitMovementDisplays();
        }

        public void AddVisualTile(VisualHexTile visualTile)
        {
            _visualTiles[visualTile.GridPosition] = visualTile;
            visualTile.TileClicked += OnTileClicked;
            visualTile.TileHovered += OnTileHovered;
            visualTile.TileUnhovered += OnTileUnhovered;
        }

        private void OnTileClicked(VisualHexTile clickedTile)
        {
            GD.Print($"🖱️ CLICK Debug: Tile clicked at Grid({clickedTile.GridPosition.X},{clickedTile.GridPosition.Y}) World({clickedTile.Position.X:F1},{clickedTile.Position.Y:F1})");
            
            if (_currentPhase != GamePhase.Move)
            {
                GD.Print($"   ❌ Not in Move phase (current: {_currentPhase})");
                return;
            }

            var selectedUnit = _interactionLogic.GetSelectedUnit();
            if (selectedUnit == null)
            {
                GD.Print($"   ❌ No unit selected");
                return;
            }

            if (selectedUnit.CurrentMovementPoints <= 0)
            {
                DeselectAll();
                return;
            }

            var unitPosition = FindUnitPosition(selectedUnit);
            if (unitPosition == null)
            {
                return;
            }

            var selectedUnitId = selectedUnit?.GetInstanceId() ?? 0;
            var selectedUnitOwner = GetUnitOwner(selectedUnit)?.Name ?? "UNKNOWN";
            GD.Print($"🚀 MOVEMENT Debug: Attempting move from Grid({unitPosition.Value.X},{unitPosition.Value.Y}) to Grid({clickedTile.GridPosition.X},{clickedTile.GridPosition.Y})");
            GD.Print($"🚀 MOVING UNIT: Unit({selectedUnit?.Name ?? "NULL"}) ID({selectedUnitId}) Owner({selectedUnitOwner})");
            var moveResult = _movementCoordinator.TryMoveToDestination(unitPosition.Value, clickedTile.GridPosition, GameManager.GameMap);
            
            if (moveResult.Success)
            {
                GD.Print($"✅ MOVEMENT SUCCESS: Unit moved to Grid({moveResult.NewPosition.X},{moveResult.NewPosition.Y})");
                
                // Verify the logical position after movement
                var verifyLogicalPosition = FindUnitPosition(selectedUnit);
                GD.Print($"🔍 VERIFY LOGICAL: Unit({selectedUnit.Name}) is logically at Grid({verifyLogicalPosition?.X ?? -1},{verifyLogicalPosition?.Y ?? -1})");
                
                var visualUnit = FindVisualUnit(selectedUnit);
                if (visualUnit != null)
                {
                    // Use the actual final position from the move result, not the clicked tile
                    var actualFinalPosition = moveResult.NewPosition;
                    var newWorldPosition = _visualTiles[actualFinalPosition].Position;
                    GD.Print($"🎯 VISUAL UPDATE: Moving unit visual from World({visualUnit.Position.X:F1},{visualUnit.Position.Y:F1}) to World({newWorldPosition.X:F1},{newWorldPosition.Y:F1})");
                    GD.Print($"🎯 POSITION FIX: Using actual final position Grid({actualFinalPosition.X},{actualFinalPosition.Y}) instead of clicked Grid({clickedTile.GridPosition.X},{clickedTile.GridPosition.Y})");
                    
                    // Double-check which tile we're using for the world position
                    var targetTile = _visualTiles[actualFinalPosition];
                    GD.Print($"🎯 TARGET TILE: Grid({actualFinalPosition.X},{actualFinalPosition.Y}) -> World({targetTile.Position.X:F1},{targetTile.Position.Y:F1})");
                    
                    visualUnit.UpdatePosition(newWorldPosition);
                    
                    // Update the movement indicator after MP deduction
                    visualUnit.RefreshMovementDisplay();
                }
                
                // Update tile occupation status after movement
                UpdateTileOccupationStatus();
                
                if (selectedUnit.CurrentMovementPoints > 0)
                {
                    ShowValidMovementDestinations(selectedUnit);
                }
                else
                {
                    DeselectAll();
                }
            }
            else
            {
                // Move failed - deselect the unit and clear highlights
                GD.Print($"❌ Move failed: {moveResult.ErrorMessage} - Deselecting unit");
                DeselectAll();
            }
        }

        private void OnTileHovered(VisualHexTile hoveredTile)
        {
            if (_informationPanel == null) return;

            var gameMap = GameManager?.GameMap;
            if (gameMap != null && gameMap.ContainsKey(hoveredTile.GridPosition))
            {
                var tile = gameMap[hoveredTile.GridPosition];
                var mousePos = hoveredTile.GetGlobalMousePosition();
                _informationPanel.ShowTerrainInfo(tile.TerrainType, tile.MovementCost, mousePos);
            }
        }

        private void OnTileUnhovered(VisualHexTile unhoveredTile)
        {
            if (_informationPanel != null)
            {
                _informationPanel.Hide();
            }
        }

        private Vector2I? FindUnitPosition(Unit unit)
        {
            return _tileUnitCoordinator?.FindUnitPosition(unit, GameManager.GameMap);
        }

        private VisualUnit FindVisualUnit(Unit unit)
        {
            return _tileUnitCoordinator?.FindVisualUnit(unit, _visualUnits);
        }

        private void ClearAllHighlights()
        {
            foreach (var tile in _visualTiles.Values)
            {
                tile.SetHighlight(false);
                tile.SetGrayed(false);
                tile.SetBrightened(false);
                tile.SetOccupied(false);
                tile.SetUnavailable(false);
            }
        }

        private void ShowValidMovementDestinations(Unit selectedUnit)
        {
            if (selectedUnit == null) return;
            
            // Clear any existing highlights first
            ClearAllHighlights();
            
            var unitPosition = FindUnitPosition(selectedUnit);
            if (unitPosition == null) 
            {
                return;
            }

            _movementCoordinator.SelectUnitForMovement(selectedUnit);
            var validDestinations = _movementCoordinator.GetValidDestinations(unitPosition.Value, GameManager.GameMap);
            
            // Only apply highlighting if there are valid destinations
            if (validDestinations.Count > 0)
            {
                // Mark all tiles as unavailable first for maximum contrast
                foreach (var tile in _visualTiles.Values)
                {
                    tile.SetUnavailable(true);
                }
                
                // Mark occupied tiles as clearly unavailable with red overlay (only during movement phase)
                foreach (var kvp in GameManager.GameMap)
                {
                    if (kvp.Value.IsOccupied() && _visualTiles.ContainsKey(kvp.Key))
                    {
                        _visualTiles[kvp.Key].SetOccupied(true);
                    }
                }
                
                // Brighten valid destination tiles for maximum contrast
                foreach (var destination in validDestinations)
                {
                    if (_visualTiles.ContainsKey(destination))
                    {
                        var visualTile = _visualTiles[destination];
                        GD.Print($"🌟 HIGHLIGHT: Marking Grid({destination.X},{destination.Y}) as valid destination");
                        GD.Print($"   Visual tile at World({visualTile.Position.X:F1},{visualTile.Position.Y:F1})");
                        visualTile.SetUnavailable(false);  // Remove unavailable overlay
                        visualTile.SetBrightened(true);    // Add bright overlay
                    }
                }
            }
        }

        public void OnPhaseChanged(GamePhase newPhase)
        {
            // Capture the old phase before changing it
            var oldPhase = _currentPhase;
            
            GD.Print($"🔄 MapRenderer.OnPhaseChanged: {oldPhase} → {newPhase}");
            
            // Check if we're leaving the movement phase
            bool isLeavingMovementPhase = (oldPhase == GamePhase.Move && newPhase != GamePhase.Move);
            GD.Print($"   Leaving movement phase: {isLeavingMovementPhase}");
            
            // Update movement phase state for all tiles
            bool isMovementPhase = (newPhase == GamePhase.Move);
            foreach (var tile in _visualTiles.Values)
            {
                tile.SetMovementPhase(isMovementPhase);
            }
            
            // Clear all movement-related displays when moving out of movement phase
            if (isLeavingMovementPhase)
            {
                GD.Print("   🧹 Clearing movement displays and highlights");
                ClearAllHighlights();
                DeselectAll();
                ClearAllUnitMovementDisplays();
            }
            
            _currentPhase = newPhase;
            
            // Update all visual units to reflect the new phase
            UpdateAllVisualUnitsForPhase(newPhase, oldPhase);
        }
        
        private void UpdateAllVisualUnitsForPhase(GamePhase newPhase, GamePhase oldPhase)
        {
            GD.Print($"   📊 UpdateAllVisualUnitsForPhase: {oldPhase} → {newPhase}");
            
            if (oldPhase == GamePhase.Move && newPhase != GamePhase.Move)
            {
                GD.Print("   🎯 ENDING movement phase - resetting all MPs to 0");
                
                // End of movement phase: Set all MPs to 0
                if (GameManager?.Players != null)
                {
                    int totalUnits = 0;
                    foreach (var player in GameManager.Players)
                    {
                        foreach (var unit in player.Units)
                        {
                            unit.CurrentMovementPoints = 0;
                            totalUnits++;
                        }
                    }
                    GD.Print($"   ✅ Reset {totalUnits} units to 0 MP");
                }
                else
                {
                    GD.Print("   ❌ GameManager or Players is null!");
                }
                
                // Refresh all MP displays to reflect the new MP values
                foreach (var visualUnit in _visualUnits)
                {
                    visualUnit.RefreshMovementDisplay();
                }
            }
            else if (oldPhase != GamePhase.Move && newPhase == GamePhase.Move)
            {
                GD.Print("   🎯 STARTING movement phase - restoring all MPs to full");
                
                // Start of movement phase: Reset all MPs to full
                if (GameManager?.Players != null)
                {
                    int totalUnits = 0;
                    foreach (var player in GameManager.Players)
                    {
                        foreach (var unit in player.Units)
                        {
                            player.ResetUnitMovement();
                            totalUnits++;
                        }
                    }
                    GD.Print($"   ✅ Restored {totalUnits} units to full MP");
                }
                else
                {
                    GD.Print("   ❌ GameManager or Players is null!");
                }
                
                // Refresh all MP displays to reflect the new MP values
                foreach (var visualUnit in _visualUnits)
                {
                    visualUnit.RefreshMovementDisplay();
                }
            }
            else
            {
                GD.Print($"   ⏭️  Phase change doesn't affect movement: {oldPhase} → {newPhase}");
            }
        }

        private void ClearAllUnitMovementDisplays()
        {
            // Clear movement displays from all visual units
            foreach (var visualUnit in _visualUnits)
            {
                var movementDisplay = visualUnit.GetNodeOrNull("MovementDisplay");
                if (movementDisplay != null)
                {
                    movementDisplay.QueueFree();
                }
            }
        }

        public void UpdateTileOccupationStatus()
        {
            // Delegate to centralized tile-unit coordinator
            _tileUnitCoordinator?.SynchronizeTileOccupation(GameManager.GameMap, _visualTiles);
        }
     }
} 