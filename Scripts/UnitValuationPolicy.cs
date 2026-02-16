using System;

namespace Archistrateia
{
    public static class UnitValuationPolicy
    {
        // A simple transparent score used to keep costs proportional as units evolve.
        public static float GetValueScore(int attack, int defense, int movementPoints)
        {
            return (attack * 1.5f) + (defense * 1.3f) + movementPoints;
        }

        public static int GetRecommendedCost(int attack, int defense, int movementPoints)
        {
            return Math.Max(5, (int)MathF.Round(GetValueScore(attack, defense, movementPoints)));
        }
    }
}
