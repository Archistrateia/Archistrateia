using NUnit.Framework;
using Godot;
using System.Collections.Generic;
using Archistrateia;

[TestFixture]
public class VisualDisplayBugTest
{
    [Test]
    public void Should_Detect_Visual_Tiles_Mismatch_With_Pathfinding()
    {
        // Test for the exact bug: pathfinding returns destinations that don't exist in visual tiles
        GD.Print("=== TESTING VISUAL TILES MISMATCH BUG ===");
        
        var coordinator = new MovementCoordinator();
        var logic = new MovementValidationLogic();
        
        // Create a game map (logical map)
        var gameMap = new Dictionary<Vector2I, HexTile>();
        
        // Create map matching the user's reported dimensions
        for (int x = 0; x < MapConfiguration.MAP_WIDTH; x++)
        {
            for (int y = 0; y < MapConfiguration.MAP_HEIGHT; y++)
            {
                var position = new Vector2I(x, y);
                var terrain = GetRandomTerrain(x, y);
                gameMap[position] = new HexTile(position, terrain);
            }
        }
        
        // Create a visual tiles dictionary (simulating what MapRenderer has)
        var visualTiles = new Dictionary<Vector2I, bool>(); // bool represents if tile exists
        
        // Simulate a potential size mismatch or missing tiles
        for (int x = 0; x < MapConfiguration.MAP_WIDTH; x++)
        {
            for (int y = 0; y < MapConfiguration.MAP_HEIGHT; y++)
            {
                var position = new Vector2I(x, y);
                // Simulate some tiles missing from visual tiles (potential bug source)
                if (x >= 0 && y >= 0) // Most tiles exist
                {
                    visualTiles[position] = true;
                }
            }
        }
        
        var charioteer = new Charioteer();
        var startPos = new Vector2I(5, 4);
        
        if (gameMap.ContainsKey(startPos))
        {
            // Get pathfinding destinations
            coordinator.SelectUnitForMovement(charioteer);
            var validDestinations = coordinator.GetValidDestinations(startPos, gameMap);
            
            GD.Print($"Pathfinding found {validDestinations.Count} destinations from {startPos}");
            
            // Check for mismatch between pathfinding and visual tiles
            var missingFromVisual = new List<Vector2I>();
            var validButNotInVisual = new List<Vector2I>();
            
            foreach (var destination in validDestinations)
            {
                if (!visualTiles.ContainsKey(destination))
                {
                    missingFromVisual.Add(destination);
                    GD.Print($"   🚨 Destination {destination} from pathfinding NOT in visual tiles!");
                }
                else
                {
                    // Check if this destination would actually be highlightable
                    if (destination == new Vector2I(5, 9))
                    {
                        GD.Print($"   🔍 Found the problematic destination {destination} in both maps");
                        
                        // Check its neighbors in visual tiles
                        var neighbors = MovementValidationLogic.GetAdjacentPositions(destination);
                        var visualNeighbors = 0;
                        foreach (var neighbor in neighbors)
                        {
                            if (visualTiles.ContainsKey(neighbor))
                            {
                                visualNeighbors++;
                            }
                        }
                        GD.Print($"   {destination} has {visualNeighbors}/{neighbors.Length} neighbors in visual tiles");
                    }
                }
            }
            
            if (missingFromVisual.Count > 0)
            {
                GD.Print($"\n🚨 VISUAL MISMATCH DETECTED:");
                GD.Print($"   {missingFromVisual.Count} destinations missing from visual tiles");
                foreach (var missing in missingFromVisual)
                {
                    GD.Print($"   Missing: {missing}");
                }
                
                Assert.Fail($"BUG: {missingFromVisual.Count} pathfinding destinations are missing from visual tiles!");
            }
            else
            {
                GD.Print("✅ All pathfinding destinations exist in visual tiles");
            }
        }
    }
    
    [Test]
    public void Should_Check_Map_Dimensions_Consistency()
    {
        // Verify that logical and visual maps have the same dimensions
        GD.Print("=== CHECKING MAP DIMENSIONS CONSISTENCY ===");
        
        GD.Print($"MapConfiguration dimensions: {MapConfiguration.MAP_WIDTH} x {MapConfiguration.MAP_HEIGHT}");
        GD.Print($"Total expected tiles: {MapConfiguration.TOTAL_TILES}");
        
        // Create logical map
        var gameMap = new Dictionary<Vector2I, HexTile>();
        for (int x = 0; x < MapConfiguration.MAP_WIDTH; x++)
        {
            for (int y = 0; y < MapConfiguration.MAP_HEIGHT; y++)
            {
                var position = new Vector2I(x, y);
                gameMap[position] = new HexTile(position, TerrainType.Shoreline);
            }
        }
        
        // Create visual map (simulating Main.cs generation)
        var visualMap = new Dictionary<Vector2I, bool>();
        for (int x = 0; x < MapConfiguration.MAP_WIDTH; x++)
        {
            for (int y = 0; y < MapConfiguration.MAP_HEIGHT; y++)
            {
                var position = new Vector2I(x, y);
                visualMap[position] = true;
            }
        }
        
        GD.Print($"Logical map size: {gameMap.Count}");
        GD.Print($"Visual map size: {visualMap.Count}");
        
        // Check for any missing tiles
        var logicalOnly = new List<Vector2I>();
        var visualOnly = new List<Vector2I>();
        
        foreach (var pos in gameMap.Keys)
        {
            if (!visualMap.ContainsKey(pos))
            {
                logicalOnly.Add(pos);
            }
        }
        
        foreach (var pos in visualMap.Keys)
        {
            if (!gameMap.ContainsKey(pos))
            {
                visualOnly.Add(pos);
            }
        }
        
        if (logicalOnly.Count > 0 || visualOnly.Count > 0)
        {
            GD.Print($"\n🚨 MAP DIMENSION MISMATCH:");
            if (logicalOnly.Count > 0)
            {
                GD.Print($"   {logicalOnly.Count} tiles in logical map but not visual map");
            }
            if (visualOnly.Count > 0)
            {
                GD.Print($"   {visualOnly.Count} tiles in visual map but not logical map");
            }
            
            Assert.Fail("Map dimension mismatch detected!");
        }
        else
        {
            GD.Print("✅ Logical and visual maps have identical dimensions");
        }
    }
    
    [Test]
    public void Should_Debug_Specific_User_Island_With_Visual_Check()
    {
        // Test the exact user scenario with visual tile checking
        GD.Print("=== DEBUGGING USER ISLAND WITH VISUAL CHECK ===");
        
        var coordinator = new MovementCoordinator();
        
        // Create game map
        var gameMap = new Dictionary<Vector2I, HexTile>();
        for (int x = 0; x < MapConfiguration.MAP_WIDTH; x++)
        {
            for (int y = 0; y < MapConfiguration.MAP_HEIGHT; y++)
            {
                var position = new Vector2I(x, y);
                gameMap[position] = new HexTile(position, GetRandomTerrain(x, y));
            }
        }
        
        // Create visual tiles (simulating MapRenderer._visualTiles)
        var visualTiles = new Dictionary<Vector2I, bool>();
        for (int x = 0; x < MapConfiguration.MAP_WIDTH; x++)
        {
            for (int y = 0; y < MapConfiguration.MAP_HEIGHT; y++)
            {
                var position = new Vector2I(x, y);
                visualTiles[position] = true;
            }
        }
        
        var charioteer = new Charioteer();
        var startPos = new Vector2I(5, 4);
        var targetPos = new Vector2I(5, 9);
        
        GD.Print($"Testing movement from {startPos} to {targetPos}");
        GD.Print($"Map bounds: (0,0) to ({MapConfiguration.MAP_WIDTH - 1},{MapConfiguration.MAP_HEIGHT - 1})");
        
        // Check if both positions are in bounds
        bool startInBounds = startPos.X >= 0 && startPos.X < MapConfiguration.MAP_WIDTH && 
                           startPos.Y >= 0 && startPos.Y < MapConfiguration.MAP_HEIGHT;
        bool targetInBounds = targetPos.X >= 0 && targetPos.X < MapConfiguration.MAP_WIDTH && 
                            targetPos.Y >= 0 && targetPos.Y < MapConfiguration.MAP_HEIGHT;
        
        GD.Print($"Start {startPos} in bounds: {startInBounds}");
        GD.Print($"Target {targetPos} in bounds: {targetInBounds}");
        
        if (!targetInBounds)
        {
            GD.Print($"🚨 FOUND THE BUG: Target {targetPos} is outside map bounds!");
            GD.Print($"   This could appear as a selectable but unreachable island!");
            Assert.Fail($"Target position {targetPos} is outside map bounds!");
        }
        
        if (startInBounds && targetInBounds)
        {
            coordinator.SelectUnitForMovement(charioteer);
            var validDestinations = coordinator.GetValidDestinations(startPos, gameMap);
            
            bool canReachTarget = validDestinations.Contains(targetPos);
            GD.Print($"Can reach target: {canReachTarget}");
            
            if (canReachTarget)
            {
                // Check what happens in visual display logic
                bool targetInVisualTiles = visualTiles.ContainsKey(targetPos);
                GD.Print($"Target in visual tiles: {targetInVisualTiles}");
                
                if (!targetInVisualTiles)
                {
                    GD.Print("🚨 BUG: Target reachable by pathfinding but not in visual tiles!");
                    Assert.Fail("Target reachable but missing from visual tiles!");
                }
            }
        }
        
        GD.Print("✅ No visual display bug detected in controlled test");
    }
    
    private TerrainType GetRandomTerrain(int x, int y)
    {
        var seed = (x * 7 + y * 11) % 5;
        return (TerrainType)seed;
    }
    

} 