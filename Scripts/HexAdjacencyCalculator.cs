using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Archistrateia
{
    public static class HexAdjacencyCalculator
    {
        public static Vector2I[] GetAdjacentPositions(Vector2I position)
        {
            var adjacents = new List<Vector2I>();
            
            // For flat-top hex grid with offset coordinates, adjacency depends on even/odd columns
            // This matches the HexGridCalculator's flat-top hex positioning logic
            if (position.X % 2 == 0) // Even column
            {
                // Even columns: northwest and northeast neighbors are up one row
                adjacents.Add(new Vector2I(position.X - 1, position.Y - 1)); // northwest
                adjacents.Add(new Vector2I(position.X - 1, position.Y));     // west
                adjacents.Add(new Vector2I(position.X, position.Y + 1));     // southwest  
                adjacents.Add(new Vector2I(position.X + 1, position.Y));     // southeast
                adjacents.Add(new Vector2I(position.X + 1, position.Y - 1)); // northeast
                adjacents.Add(new Vector2I(position.X, position.Y - 1));     // north
            }
            else // Odd column  
            {
                // Odd columns: southwest and southeast neighbors are down one row
                adjacents.Add(new Vector2I(position.X - 1, position.Y));     // northwest
                adjacents.Add(new Vector2I(position.X - 1, position.Y + 1)); // west
                adjacents.Add(new Vector2I(position.X, position.Y + 1));     // southwest
                adjacents.Add(new Vector2I(position.X + 1, position.Y + 1)); // southeast  
                adjacents.Add(new Vector2I(position.X + 1, position.Y));     // northeast
                adjacents.Add(new Vector2I(position.X, position.Y - 1));     // north
            }
            
            return adjacents.ToArray();
        }

        public static List<Vector2I> GetNeighborPositions(Vector2I position, int mapWidth, int mapHeight)
        {
            var adjacents = GetAdjacentPositions(position);
            var validNeighbors = new List<Vector2I>();
            
            foreach (var adjacent in adjacents)
            {
                if (adjacent.X >= 0 && adjacent.X < mapWidth && adjacent.Y >= 0 && adjacent.Y < mapHeight)
                {
                    validNeighbors.Add(adjacent);
                }
            }
            
            return validNeighbors;
        }

        public static bool ArePositionsAdjacent(Vector2I pos1, Vector2I pos2)
        {
            var adjacents = GetAdjacentPositions(pos1);
            return adjacents.Contains(pos2);
        }

        public static int GetDistance(Vector2I pos1, Vector2I pos2)
        {
            // Convert offset coordinates to cube coordinates for distance calculation
            var cube1 = OffsetToCube(pos1);
            var cube2 = OffsetToCube(pos2);
            
            return (Math.Abs(cube1.X - cube2.X) + Math.Abs(cube1.Y - cube2.Y) + Math.Abs(cube1.Z - cube2.Z)) / 2;
        }

        private static Vector3I OffsetToCube(Vector2I offset)
        {
            var q = offset.X;
            var r = offset.Y - (offset.X - (offset.X & 1)) / 2;
            var s = -q - r;
            return new Vector3I(q, r, s);
        }

        public static Vector2I[] GetPositionsInRange(Vector2I center, int range, int mapWidth, int mapHeight)
        {
            var positions = new List<Vector2I>();
            
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    var pos = new Vector2I(x, y);
                    if (GetDistance(center, pos) <= range)
                    {
                        positions.Add(pos);
                    }
                }
            }
            
            return positions.ToArray();
        }
    }
}
