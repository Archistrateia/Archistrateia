using Godot;
using System.Collections.Generic;

public partial class Player : Node
{
    public string Name { get; set; }
    public List<Unit> Units { get; set; } = new List<Unit>();
    public int Gold { get; set; }
    public List<City> Cities { get; set; } = new List<City>();

    public Player(string name, int startingGold = 100)
    {
        Name = name;
        Gold = startingGold;
    }

    public void AddUnit(Unit unit)
    {
        Units.Add(unit);
        GD.Print($"Player {Name} recruited {unit.Name}");
    }

    public void RemoveUnit(Unit unit)
    {
        if (Units.Remove(unit))
        {
            GD.Print($"Player {Name} lost {unit.Name}");
        }
    }

    public bool CanAffordUnit(Unit unit)
    {
        return Gold >= unit.Cost;
    }

    public bool PurchaseUnit(Unit unit)
    {
        if (CanAffordUnit(unit))
        {
            Gold -= unit.Cost;
            AddUnit(unit);
            return true;
        }
        return false;
    }

    public void AddGold(int amount)
    {
        Gold += amount;
        GD.Print($"Player {Name} earned {amount} gold. Total: {Gold}");
    }

    public void SpendGold(int amount)
    {
        if (Gold >= amount)
        {
            Gold -= amount;
        }
    }

    public void AddCity(City city)
    {
        Cities.Add(city);
        GD.Print($"Player {Name} captured {city.Name}");
    }

    public void RemoveCity(City city)
    {
        if (Cities.Remove(city))
        {
            GD.Print($"Player {Name} lost {city.Name}");
        }
    }

    public void EarnFromCities()
    {
        int totalEarnings = 0;
        foreach (var city in Cities)
        {
            totalEarnings += city.ProductionValue;
        }
        AddGold(totalEarnings);
    }

    public void ResetUnitMovement()
    {
        foreach (var unit in Units)
        {
            unit.ResetMovement();
        }
    }

    public List<Unit> GetUnitsAtLocation(Vector2I position)
    {
        var unitsAtLocation = new List<Unit>();
        foreach (var unit in Units)
        {
            if (unit.GetParent() is HexTile tile && tile.Position == position)
            {
                unitsAtLocation.Add(unit);
            }
        }
        return unitsAtLocation;
    }
}