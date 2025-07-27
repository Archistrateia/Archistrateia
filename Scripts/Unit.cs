using Godot;

public enum UnitType
{
    Nakhtu,
    Medjay,
    Archer,
    Charioteer
}

public partial class Unit : Node
{
    public string Name { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int MovementPoints { get; set; }
    public int Cost { get; set; }
    public UnitType UnitType { get; set; }
    public int CurrentMovementPoints { get; set; }
    public bool HasMoved { get; set; } = false;

    public Unit(string name, int attack, int defense, int movementPoints, int cost, UnitType unitType)
    {
        Name = name;
        Attack = attack;
        Defense = defense;
        MovementPoints = movementPoints;
        CurrentMovementPoints = movementPoints;
        Cost = cost;
        UnitType = unitType;
    }

    public virtual void ResetMovement()
    {
        CurrentMovementPoints = MovementPoints;
        HasMoved = false;
    }

    public bool CanMove(int distance)
    {
        return CurrentMovementPoints >= distance && !HasMoved;
    }

    public void Move(int distance)
    {
        if (CanMove(distance))
        {
            CurrentMovementPoints -= distance;
            HasMoved = true;
        }
    }

    public int GetCombatPower()
    {
        return Attack + Defense;
    }
}

public partial class Nakhtu : Unit
{
    public Nakhtu() : base("Nakhtu", 3, 2, 4, 10, UnitType.Nakhtu)
    {
    }
}

public partial class Medjay : Unit
{
    public Medjay() : base("Medjay", 4, 3, 6, 15, UnitType.Medjay)
    {
    }
}

public partial class Archer : Unit
{
    public Archer() : base("Archer", 5, 1, 4, 12, UnitType.Archer)
    {
    }
}

public partial class Charioteer : Unit
{
    public Charioteer() : base("Charioteer", 6, 2, 8, 20, UnitType.Charioteer)
    {
    }
}