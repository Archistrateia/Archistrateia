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
            _currentPhase = phase;
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
            visualUnit.UnitClicked += OnUnitClicked;
            
            _visualUnits.Add(visualUnit);
            
            return visualUnit;
        }

        private void OnUnitClicked(VisualUnit clickedUnit)
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
                }
                
                if (selectedUnit.CurrentMovementPoints > 0)
                {
                    ShowValidMovementDestinations(selectedUnit);
                }
                else
                {
                    DeselectAll();
                }
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
                tile.SetGrayed(false);      // Remove graying
                tile.SetBrightened(false);  // Remove brightening
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
                // Gray out all tiles first for dark background
                foreach (var tile in _visualTiles.Values)
                {
                    tile.SetGrayed(true);
                }
                
                // Brighten valid destination tiles for maximum contrast
                foreach (var destination in validDestinations)
                {
                    if (_visualTiles.ContainsKey(destination))
                    {
                        _visualTiles[destination].SetGrayed(false);      // Remove dark overlay
                        _visualTiles[destination].SetBrightened(true);   // Add bright overlay
                    }
                }
            }
        }
    }
} 