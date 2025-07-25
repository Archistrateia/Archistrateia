using Godot;
using System.Collections.Generic;

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
        GD.Print($"🏗️ Creating visual unit for {logicalUnit.Name} at {position}");
        
        var visualUnit = new VisualUnit();
        AddChild(visualUnit);
        GD.Print($"   Added VisualUnit as child to MapRenderer");
        
        visualUnit.Initialize(logicalUnit, position, color);
        visualUnit.UnitClicked += OnUnitClicked;
        GD.Print($"   Connected UnitClicked signal");
        
        _visualUnits.Add(visualUnit);
        GD.Print($"   Added to _visualUnits list. Total units: {_visualUnits.Count}");
        
        // Debug the scene tree structure
        GD.Print($"   VisualUnit scene path: {visualUnit.GetPath()}");
        GD.Print($"   VisualUnit children count: {visualUnit.GetChildCount()}");
        
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
        GD.Print($"🔄 Updating visual selection. Selected unit: {selectedUnit?.Name ?? "NONE"}");
        
        // Update visual state for all units
        foreach (var visualUnit in _visualUnits)
        {
            var isSelected = visualUnit.LogicalUnit == selectedUnit;
            visualUnit.SetSelected(isSelected);
            
            if (isSelected)
            {
                GD.Print($"   🔵 {visualUnit.LogicalUnit.Name} - SELECTED (showing ring)");
            }
            else
            {
                GD.Print($"   ⚪ {visualUnit.LogicalUnit.Name} - deselected");
            }
        }
    }

    public void UpdateVisualUnitPosition(VisualUnit visualUnit, Vector2 newPosition)
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
        GD.Print("🔄 Deselected all units and cleared highlights");
    }

    public void AddVisualTile(VisualHexTile visualTile)
    {
        _visualTiles[visualTile.GridPosition] = visualTile;
        visualTile.TileClicked += OnTileClicked;
        GD.Print($"🗺️ Added visual tile at {visualTile.GridPosition}");
    }

    private void OnTileClicked(VisualHexTile clickedTile)
    {
        GD.Print($"🎯 MapRenderer: OnTileClicked called for tile at {clickedTile.GridPosition}");
        
        if (_currentPhase != GamePhase.Move)
        {
            GD.Print($"❌ Not in Move phase (current: {_currentPhase}) - ignoring tile click");
            return;
        }

        var selectedUnit = _interactionLogic.GetSelectedUnit();
        if (selectedUnit == null)
        {
            GD.Print("❌ No unit selected - ignoring tile click");
            return;
        }

        GD.Print($"🎯 Unit {selectedUnit.Name} is selected, attempting movement...");

        // Find the unit's current position
        var unitPosition = FindUnitPosition(selectedUnit);
        if (unitPosition == null)
        {
            GD.Print("❌ Could not find unit position in game map");
            return;
        }

        GD.Print($"🎯 Attempting to move {selectedUnit.Name} from {unitPosition} to {clickedTile.GridPosition}");

        // Try to move the unit
        var moveResult = _movementCoordinator.TryMoveToDestination(unitPosition.Value, clickedTile.GridPosition, GameManager.GameMap);
        
        if (moveResult.Success)
        {
            GD.Print($"✅ Movement successful! Unit moved to {moveResult.NewPosition}");
            
            // Update visual unit position
            var visualUnit = FindVisualUnit(selectedUnit);
            if (visualUnit != null)
            {
                var newWorldPosition = _visualTiles[clickedTile.GridPosition].Position;
                visualUnit.UpdatePosition(newWorldPosition);
                GD.Print($"🔄 Updated visual unit position to {newWorldPosition}");
            }
            else
            {
                GD.PrintErr("❌ Could not find visual unit for movement update");
            }
            
            // Clear selection and highlights after successful move
            DeselectAll();
        }
        else
        {
            GD.Print($"❌ Movement failed: {moveResult.ErrorMessage}");
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
        if (unitPosition == null) return;

        _movementCoordinator.SelectUnitForMovement(selectedUnit);
        var validDestinations = _movementCoordinator.GetValidDestinations(unitPosition.Value, GameManager.GameMap);
        
        GD.Print($"🎯 Creating high contrast for {validDestinations.Count} valid destinations");
        
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