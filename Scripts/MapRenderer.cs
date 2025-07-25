using Godot;
using System.Collections.Generic;

public partial class MapRenderer : Node2D
{
    public GameManager GameManager { get; private set; }
    private List<VisualUnit> _visualUnits = new List<VisualUnit>();

    public void Initialize(GameManager gameManager)
    {
        GameManager = gameManager;
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
        _visualUnits.Add(visualUnit);
        return visualUnit;
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
} 