using Godot;

public enum TerrainType
{
    Desert,
    Hill,
    River,
    Shoreline,
    Lagoon
}

public partial class HexTile : Node
{
    public Vector2I Position { get; set; }
    public TerrainType TerrainType { get; set; }
    public Unit OccupyingUnit { get; set; }
    public City City { get; set; }
    public int MovementCost { get; set; }
    public int DefenseBonus { get; set; }

    public HexTile(Vector2I position, TerrainType terrainType)
    {
        Position = position;
        TerrainType = terrainType;
        SetTerrainProperties();
    }

    private void SetTerrainProperties()
    {
        switch (TerrainType)
        {
            case TerrainType.Desert:
                MovementCost = 2;
                DefenseBonus = 0;
                break;
            case TerrainType.Hill:
                MovementCost = 2;
                DefenseBonus = 2;
                break;
            case TerrainType.River:
                MovementCost = 3;
                DefenseBonus = 1;
                break;
            case TerrainType.Shoreline:
                MovementCost = 1;
                DefenseBonus = 0;
                break;
            case TerrainType.Lagoon:
                MovementCost = 4;
                DefenseBonus = 0;
                break;
        }
    }

    public bool IsOccupied()
    {
        return OccupyingUnit != null;
    }

    public bool IsCity()
    {
        return City != null;
    }

    public bool CanMoveTo(Unit unit)
    {
        if (IsOccupied() && OccupyingUnit != unit)
        {
            return false;
        }

        return unit.CurrentMovementPoints >= MovementCost;
    }

    public bool MoveUnitTo(Unit unit)
    {
        if (!CanMoveTo(unit))
        {
            return false;
        }

        if (OccupyingUnit != null && OccupyingUnit != unit)
        {
            return false;
        }

        unit.Move(MovementCost);
        OccupyingUnit = unit;
        return true;
    }

    public void RemoveUnit()
    {
        OccupyingUnit = null;
    }

    public void SetCity(City city)
    {
        City = city;
    }

    public void RemoveCity()
    {
    }

    public int GetTotalDefenseBonus()
    {
        int bonus = DefenseBonus;
        return bonus;
    }

    public string GetDescription()
    {
        string description = $"{TerrainType} at {Position}";

        if (IsCity())
        {
            description += $" - City: {City.Name}";
        }

        if (IsOccupied())
        {
            description += $" - Unit: {OccupyingUnit.Name}";
        }

        return description;
    }

    public bool IsAdjacentTo(HexTile other)
    {
        Vector2I diff = Position - other.Position;
        int distance = Mathf.Abs(diff.X) + Mathf.Abs(diff.Y);
        return distance == 1;
    }
}