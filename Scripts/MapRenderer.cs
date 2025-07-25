using Godot;
using System.Collections.Generic;

public partial class MapRenderer : Node2D
{
    public GameManager GameManager { get; private set; }
    private List<VisualUnit> _visualUnits = new List<VisualUnit>();
    private PlayerInteractionLogic _interactionLogic = new PlayerInteractionLogic();
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
        UpdateVisualSelection();
    }
} 