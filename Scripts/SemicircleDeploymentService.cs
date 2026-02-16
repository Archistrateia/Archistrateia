using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Archistrateia
{
    public sealed class SemicircleDeploymentService
    {
        public List<Vector2I> GetDeployableTilesForPlayer(Dictionary<Vector2I, HexTile> gameMap, int playerIndex, int playerCount)
        {
            if (gameMap == null || gameMap.Count == 0 || playerCount <= 0 || playerIndex < 0 || playerIndex >= playerCount)
            {
                return new List<Vector2I>();
            }

            int maxX = gameMap.Keys.Max(p => p.X);
            int maxY = gameMap.Keys.Max(p => p.Y);
            var anchor = GetPlayerAnchor(playerIndex, playerCount, maxX, maxY);
            var center = new Vector2(maxX / 2.0f, maxY / 2.0f);
            var inward = (center - anchor).Normalized();
            float radius = GetDeploymentRadius(maxX + 1, maxY + 1);

            var deployable = new List<Vector2I>();
            foreach (var kvp in gameMap)
            {
                var pos = kvp.Key;
                var tile = kvp.Value;
                var point = new Vector2(pos.X, pos.Y);
                var fromAnchor = point - anchor;

                // Semicircle condition: inside circle and toward map interior.
                bool inRadius = fromAnchor.Length() <= radius;
                bool inHalf = fromAnchor.Dot(inward) >= 0.0f;

                if (inRadius && inHalf && !tile.IsOccupied())
                {
                    deployable.Add(pos);
                }
            }

            deployable.Sort((a, b) =>
            {
                float da = anchor.DistanceTo(new Vector2(a.X, a.Y));
                float db = anchor.DistanceTo(new Vector2(b.X, b.Y));
                return da.CompareTo(db);
            });

            return deployable;
        }

        public Vector2 GetPlayerAnchor(int playerIndex, int playerCount, int maxX, int maxY)
        {
            var center = new Vector2(maxX / 2.0f, maxY / 2.0f);
            float angle = -Mathf.Pi + (Mathf.Tau * playerIndex / Math.Max(1, playerCount));
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            return IntersectRayWithRect(center, direction, maxX, maxY);
        }

        public float GetDeploymentRadius(int mapWidth, int mapHeight)
        {
            float minDim = Math.Max(1, Math.Min(mapWidth, mapHeight));
            return Mathf.Max(2.0f, minDim * 0.28f);
        }

        private static Vector2 IntersectRayWithRect(Vector2 origin, Vector2 direction, int maxX, int maxY)
        {
            float tx = float.PositiveInfinity;
            float ty = float.PositiveInfinity;

            if (Mathf.Abs(direction.X) > 0.0001f)
            {
                tx = direction.X > 0
                    ? (maxX - origin.X) / direction.X
                    : (0 - origin.X) / direction.X;
            }

            if (Mathf.Abs(direction.Y) > 0.0001f)
            {
                ty = direction.Y > 0
                    ? (maxY - origin.Y) / direction.Y
                    : (0 - origin.Y) / direction.Y;
            }

            float t = Math.Min(tx, ty);
            if (float.IsInfinity(t) || t < 0)
            {
                return origin;
            }

            return origin + (direction * t);
        }
    }
}
