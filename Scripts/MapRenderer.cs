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

        public void Initialize(GameManager gameManager)
        {
            GameManager = gameManager;
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
            AddChild(visualUnit);
            
            visualUnit.Initialize(logicalUnit, position, color);
            visualUnit.SetMapRenderer(this); // Set the MapRenderer reference
            visualUnit.UnitClicked += OnUnitClicked;
            
            _visualUnits.Add(visualUnit);
            
            return visualUnit;
        }

        public void OnUnitClicked(VisualUnit clickedUnit)
        {
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
        }

        private void OnTileClicked(VisualHexTile clickedTile)
        {
            if (_currentPhase != GamePhase.Move)
            {
                return;
            }

            var selectedUnit = _interactionLogic.GetSelectedUnit();
            if (selectedUnit == null)
            {
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

            var moveResult = _movementCoordinator.TryMoveToDestination(unitPosition.Value, clickedTile.GridPosition, GameManager.GameMap);
            
            if (moveResult.Success)
            {
                var visualUnit = FindVisualUnit(selectedUnit);
                if (visualUnit != null)
                {
                    var newWorldPosition = _visualTiles[clickedTile.GridPosition].Position;
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

        private Vector2I? FindUnitPosition(Unit unit)
        {
            foreach (var kvp in GameManager.GameMap)
            {
                if (kvp.Value.OccupyingUnit == unit)
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        private VisualUnit FindVisualUnit(Unit unit)
        {
            foreach (var visualUnit in _visualUnits)
            {
                if (visualUnit.LogicalUnit == unit)
                {
                    return visualUnit;
                }
            }
            return null;
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
                        _visualTiles[destination].SetUnavailable(false);  // Remove unavailable overlay
                        _visualTiles[destination].SetBrightened(true);    // Add bright overlay
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
            // Update visual occupation status for all tiles
            foreach (var kvp in GameManager.GameMap)
            {
                if (_visualTiles.ContainsKey(kvp.Key))
                {
                    var tile = kvp.Value;
                    var visualTile = _visualTiles[kvp.Key];
                    
                    if (tile.IsOccupied())
                    {
                        visualTile.SetOccupied(true);
                    }
                    else
                    {
                        visualTile.SetOccupied(false);
                                         }
                 }
             }
         }
     }
} 