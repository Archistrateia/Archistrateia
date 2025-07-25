using Godot;

public partial class City : Node
{
    public string Name { get; set; }
    public int ProductionValue { get; set; }
    public Player Owner { get; set; }
    public bool IsCapital { get; set; }
    public int Population { get; set; }

    public City(string name, int productionValue = 10, bool isCapital = false)
    {
        Name = name;
        ProductionValue = productionValue;
        IsCapital = isCapital;
        Population = 1000;
    }

    public void SetOwner(Player player)
    {
        if (Owner != null)
        {
            Owner.RemoveCity(this);
        }

        Owner = player;
        if (player != null)
        {
            player.AddCity(this);
        }
    }

    public bool CanProduceUnit(Unit unit, Player player)
    {
        return Owner == player && player.CanAffordUnit(unit);
    }

    public bool ProduceUnit(Unit unit, Player player)
    {
        if (CanProduceUnit(unit, player))
        {
            if (player.PurchaseUnit(unit))
            {
                GD.Print($"City {Name} produced {unit.Name}");
                return true;
            }
        }
        return false;
    }

    public void IncreaseProduction(int amount)
    {
        ProductionValue += amount;
        GD.Print($"City {Name} production increased to {ProductionValue}");
    }

    public void DecreaseProduction(int amount)
    {
        ProductionValue = Mathf.Max(0, ProductionValue - amount);
        GD.Print($"City {Name} production decreased to {ProductionValue}");
    }

    public void GrowPopulation(int amount)
    {
        Population += amount;
        GD.Print($"City {Name} population grew to {Population}");
    }

    public void ShrinkPopulation(int amount)
    {
        Population = Mathf.Max(0, Population - amount);
        GD.Print($"City {Name} population decreased to {Population}");
    }

    public int GetDefenseBonus()
    {
        return IsCapital ? 2 : 1;
    }

    public string GetStatus()
    {
        string ownerName = Owner?.Name ?? "Unclaimed";
        return $"{Name} (Owner: {ownerName}, Production: {ProductionValue}, Population: {Population})";
    }
}