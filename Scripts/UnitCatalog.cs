using System;
using System.Collections.Generic;
using System.Linq;

namespace Archistrateia
{
    public sealed class UnitBlueprint
    {
        public UnitType UnitType { get; }
        public string DisplayName { get; }
        public int Attack { get; }
        public int Defense { get; }
        public int MovementPoints { get; }
        public int Cost { get; }
        public float ValueScore { get; }

        public UnitBlueprint(UnitType unitType, string displayName, int attack, int defense, int movementPoints, int cost)
        {
            UnitType = unitType;
            DisplayName = displayName;
            Attack = attack;
            Defense = defense;
            MovementPoints = movementPoints;
            Cost = cost;
            ValueScore = UnitValuationPolicy.GetValueScore(attack, defense, movementPoints);
        }

        public Unit CreateUnit()
        {
            Unit unit = UnitCatalog.CreateUnit(UnitType);
            // Keep purchased unit metadata consistent with the catalog.
            unit.Cost = Cost;
            return unit;
        }
    }

    public static class UnitCatalog
    {
        private static readonly IReadOnlyList<UnitBlueprint> _blueprints = BuildBlueprints();
        private static readonly Dictionary<UnitType, UnitBlueprint> _byType = _blueprints.ToDictionary(bp => bp.UnitType);

        public static IReadOnlyList<UnitBlueprint> GetAll()
        {
            return _blueprints;
        }

        public static UnitBlueprint Get(UnitType unitType)
        {
            return _byType[unitType];
        }

        public static bool TryGet(UnitType unitType, out UnitBlueprint blueprint)
        {
            return _byType.TryGetValue(unitType, out blueprint);
        }

        public static Unit CreateUnit(UnitType unitType)
        {
            return unitType switch
            {
                UnitType.Nakhtu => new Nakhtu(),
                UnitType.Medjay => new Medjay(),
                UnitType.Archer => new Archer(),
                UnitType.Charioteer => new Charioteer(),
                _ => throw new ArgumentOutOfRangeException(nameof(unitType), unitType, "Unsupported unit type")
            };
        }

        private static IReadOnlyList<UnitBlueprint> BuildBlueprints()
        {
            var list = new List<UnitBlueprint>();
            foreach (UnitType unitType in Enum.GetValues(typeof(UnitType)))
            {
                var unit = CreateUnit(unitType);
                list.Add(new UnitBlueprint(
                    unitType,
                    unit.Name,
                    unit.Attack,
                    unit.Defense,
                    unit.MovementPoints,
                    unit.Cost
                ));
            }

            return list;
        }
    }
}
