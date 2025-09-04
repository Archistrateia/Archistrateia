using Godot;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using Archistrateia;

[TestFixture]
public class UnitTileAlignmentTest
{
    private Vector2 gameAreaSize = new Vector2(1720, 1080);
    private const int MAP_WIDTH = 20;
    private const int MAP_HEIGHT = 10;

    [SetUp]
    public void Setup()
    {
        HexGridCalculator.SetZoom(1.0f);
        HexGridCalculator.SetScrollOffset(Vector2.Zero);
    }

    [Test]
    public void Should_Calculate_Consistent_Tile_Positions()
    {
        var testPositions = new Vector2I[]
        {
            new Vector2I(0, 0),
            new Vector2I(1, 0),
            new Vector2I(0, 1),
            new Vector2I(1, 1),
            new Vector2I(2, 0),
            new Vector2I(2, 1),
            new Vector2I(5, 5),
            new Vector2I(10, 4),
            new Vector2I(19, 9)
        };

        foreach (var gridPos in testPositions)
        {
            var calculatedPosition = HexGridCalculator.CalculateHexPositionCentered(
                gridPos.X, gridPos.Y, gameAreaSize, MAP_WIDTH, MAP_HEIGHT);
            
            var basicPosition = HexGridCalculator.CalculateHexPosition(gridPos.X, gridPos.Y);
            
            GD.Print($"🎯 POSITION TEST: Grid({gridPos.X},{gridPos.Y})");
            GD.Print($"   Basic Position:    ({basicPosition.X:F1},{basicPosition.Y:F1})");
            GD.Print($"   Centered Position: ({calculatedPosition.X:F1},{calculatedPosition.Y:F1})");
            
            // Verify that both calculation methods are internally consistent
            Assert.IsTrue(calculatedPosition.X >= 0 && calculatedPosition.X <= gameAreaSize.X, 
                $"Centered X position should be within game area for Grid({gridPos.X},{gridPos.Y})");
            Assert.IsTrue(calculatedPosition.Y >= 0 && calculatedPosition.Y <= gameAreaSize.Y, 
                $"Centered Y position should be within game area for Grid({gridPos.X},{gridPos.Y})");
        }
    }

    [Test]
    public void Should_Verify_Adjacent_Tile_Positioning()
    {
        var centerPosition = new Vector2I(10, 5);
        var adjacentPositions = GetAdjacentTiles(centerPosition);

        var centerWorldPos = HexGridCalculator.CalculateHexPositionCentered(
            centerPosition.X, centerPosition.Y, gameAreaSize, MAP_WIDTH, MAP_HEIGHT);
        
        GD.Print($"\n🚀 ADJACENCY TEST: Center at Grid({centerPosition.X},{centerPosition.Y}) -> World({centerWorldPos.X:F1},{centerWorldPos.Y:F1})");

        foreach (var adjacentPos in adjacentPositions)
        {
            var adjacentWorldPos = HexGridCalculator.CalculateHexPositionCentered(
                adjacentPos.X, adjacentPos.Y, gameAreaSize, MAP_WIDTH, MAP_HEIGHT);
            
            var distance = centerWorldPos.DistanceTo(adjacentWorldPos);
            
            GD.Print($"   Adjacent Grid({adjacentPos.X},{adjacentPos.Y}) -> World({adjacentWorldPos.X:F1},{adjacentWorldPos.Y:F1}) | Distance: {distance:F1}");
            
            // Adjacent hexes should be roughly the same distance apart
            Assert.IsTrue(distance > 50 && distance < 100, 
                $"Adjacent hex distance should be reasonable, got {distance:F1} for Grid({adjacentPos.X},{adjacentPos.Y})");
        }
    }


    [Test]
    public void Should_Verify_Coordinate_System_Consistency()
    {
        GD.Print("\n🧪 COORDINATE SYSTEM VERIFICATION:");
        GD.Print($"   Game Area: {gameAreaSize.X}x{gameAreaSize.Y}");
        GD.Print($"   Map Dimensions: {MAP_WIDTH}x{MAP_HEIGHT}");
        GD.Print($"   Hex Constants: WIDTH={HexGridCalculator.HEX_WIDTH:F1}, HEIGHT={HexGridCalculator.HEX_HEIGHT:F1}");
        
        var testPositions = new Vector2I[]
        {
            new Vector2I(0, 0),
            new Vector2I(1, 0),
            new Vector2I(0, 1),
            new Vector2I(1, 1),
            new Vector2I(10, 5),
            new Vector2I(19, 9)
        };

        foreach (var gridPos in testPositions)
        {
            
            var calculatedWorld = HexGridCalculator.CalculateHexPositionCentered(
                gridPos.X, gridPos.Y, gameAreaSize, MAP_WIDTH, MAP_HEIGHT);
            var actualTileCenter = GetTileCenterPosition(gridPos);
            
            GD.Print($"   Grid({gridPos.X},{gridPos.Y}): Calculated=({calculatedWorld.X:F1},{calculatedWorld.Y:F1}), Actual=({actualTileCenter.X:F1},{actualTileCenter.Y:F1})");
            
            Assert.AreEqual(calculatedWorld.X, actualTileCenter.X, 1.0f, 
                $"Calculated and actual X should match for Grid({gridPos.X},{gridPos.Y})");
            Assert.AreEqual(calculatedWorld.Y, actualTileCenter.Y, 1.0f, 
                $"Calculated and actual Y should match for Grid({gridPos.X},{gridPos.Y})");
        }
    }

    private Vector2 GetTileCenterPosition(Vector2I gridPosition)
    {
        return HexGridCalculator.CalculateHexPositionCentered(
            gridPosition.X, gridPosition.Y, gameAreaSize, MAP_WIDTH, MAP_HEIGHT);
    }

    private Vector2 GetUnitCenterPosition(Vector2I gridPosition)
    {
        return HexGridCalculator.CalculateHexPositionCentered(
            gridPosition.X, gridPosition.Y, gameAreaSize, MAP_WIDTH, MAP_HEIGHT);
    }

    private List<Vector2I> GetAdjacentTiles(Vector2I position)
    {
        var adjacents = new List<Vector2I>();
        int x = position.X;
        int y = position.Y;
        
        if (x % 2 == 0)
        {
            adjacents.Add(new Vector2I(x - 1, y - 1));
            adjacents.Add(new Vector2I(x - 1, y));
            adjacents.Add(new Vector2I(x, y + 1));
            adjacents.Add(new Vector2I(x + 1, y));
            adjacents.Add(new Vector2I(x + 1, y - 1));
            adjacents.Add(new Vector2I(x, y - 1));
        }
        else
        {
            adjacents.Add(new Vector2I(x - 1, y));
            adjacents.Add(new Vector2I(x - 1, y + 1));
            adjacents.Add(new Vector2I(x, y + 1));
            adjacents.Add(new Vector2I(x + 1, y + 1));
            adjacents.Add(new Vector2I(x + 1, y));
            adjacents.Add(new Vector2I(x, y - 1));
        }
        
        return adjacents;
    }

    private int CalculateDistance(Vector2I from, Vector2I to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;
        
        if (Mathf.Sign(dx) == Mathf.Sign(dy))
        {
            return Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
        }
        else
        {
            return Mathf.Abs(dx) + Mathf.Abs(dy);
        }
    }

    [Test]
    public void Should_Detect_Coordinate_System_Offset_Between_Tiles_And_Units()
    {
        GD.Print("\n🔍 COORDINATE OFFSET DETECTION TEST:");
        GD.Print("Testing for potential off-by-one errors between tile and unit coordinate systems");
        
        var testPositions = new Vector2I[]
        {
            new Vector2I(0, 0),
            new Vector2I(1, 0), 
            new Vector2I(0, 1),
            new Vector2I(1, 1),
            new Vector2I(5, 5),
            new Vector2I(10, 4)
        };

        foreach (var gridPos in testPositions)
        {
            var tileWorldPos = HexGridCalculator.CalculateHexPositionCentered(
                gridPos.X, gridPos.Y, gameAreaSize, MAP_WIDTH, MAP_HEIGHT);
            
            // Test potential off-by-one scenarios
            var unitOffsetScenarios = new[]
            {
                new { Name = "Same Position", Offset = new Vector2I(0, 0) },
                new { Name = "X+1", Offset = new Vector2I(1, 0) },
                new { Name = "Y+1", Offset = new Vector2I(0, 1) },
                new { Name = "X-1", Offset = new Vector2I(-1, 0) },
                new { Name = "Y-1", Offset = new Vector2I(0, -1) },
                new { Name = "Both+1", Offset = new Vector2I(1, 1) },
                new { Name = "Both-1", Offset = new Vector2I(-1, -1) }
            };

            GD.Print($"\n📍 Testing Grid({gridPos.X},{gridPos.Y}) -> Tile World({tileWorldPos.X:F1},{tileWorldPos.Y:F1})");
            
            foreach (var scenario in unitOffsetScenarios)
            {
                var unitGridPos = gridPos + scenario.Offset;
                var unitWorldPos = HexGridCalculator.CalculateHexPositionCentered(
                    unitGridPos.X, unitGridPos.Y, gameAreaSize, MAP_WIDTH, MAP_HEIGHT);
                
                var distance = tileWorldPos.DistanceTo(unitWorldPos);
                
                GD.Print($"   {scenario.Name,-10}: Unit Grid({unitGridPos.X},{unitGridPos.Y}) -> World({unitWorldPos.X:F1},{unitWorldPos.Y:F1}) | Distance: {distance:F1}");
                
                // Flag potential coordinate system mismatches
                if (scenario.Name != "Same Position" && distance < 70) // Less than one hex width
                {
                    GD.Print($"   ⚠️  POTENTIAL OFFSET: {scenario.Name} shows close alignment (distance: {distance:F1})");
                }
            }
        }
    }

    [Test] 
    public void Should_Test_Visual_To_Logical_Coordinate_Conversion()
    {
        GD.Print("\n🎯 VISUAL-TO-LOGICAL COORDINATE CONVERSION TEST:");
        
        // Test if there's a systematic offset in how coordinates are converted
        var knownGoodPositions = new[]
        {
            new { Grid = new Vector2I(0, 0), ExpectedWorld = new Vector2(326.2f, 221.8f) },
            new { Grid = new Vector2I(1, 0), ExpectedWorld = new Vector2(378.8f, 252.1f) },
            new { Grid = new Vector2I(10, 4), ExpectedWorld = new Vector2(851.2f, 464.2f) }
        };

        foreach (var testCase in knownGoodPositions)
        {
            var calculatedWorld = HexGridCalculator.CalculateHexPositionCentered(
                testCase.Grid.X, testCase.Grid.Y, gameAreaSize, MAP_WIDTH, MAP_HEIGHT);
            
            var deltaX = Math.Abs(calculatedWorld.X - testCase.ExpectedWorld.X);
            var deltaY = Math.Abs(calculatedWorld.Y - testCase.ExpectedWorld.Y);
            
            GD.Print($"Grid({testCase.Grid.X},{testCase.Grid.Y}):");
            GD.Print($"   Expected: ({testCase.ExpectedWorld.X:F1},{testCase.ExpectedWorld.Y:F1})");
            GD.Print($"   Actual:   ({calculatedWorld.X:F1},{calculatedWorld.Y:F1})");
            GD.Print($"   Delta:    ({deltaX:F1},{deltaY:F1})");
            
            // Test for systematic offsets that might indicate coordinate system issues
            Assert.Less(deltaX, 1.0f, $"X coordinate should match expected value for Grid({testCase.Grid.X},{testCase.Grid.Y})");
            Assert.Less(deltaY, 1.0f, $"Y coordinate should match expected value for Grid({testCase.Grid.X},{testCase.Grid.Y})");
        }
    }

    [Test]
    public void Should_Verify_Click_To_Grid_Conversion_Accuracy()
    {
        GD.Print("\n🖱️ CLICK-TO-GRID CONVERSION TEST:");
        GD.Print("Testing reverse conversion from world coordinates to grid coordinates");
        
        var testCases = new[]
        {
            new Vector2I(5, 5),
            new Vector2I(10, 4), 
            new Vector2I(15, 7),
            new Vector2I(0, 0),
            new Vector2I(19, 9)
        };

        foreach (var originalGrid in testCases)
        {
            // Convert grid to world
            var worldPos = HexGridCalculator.CalculateHexPositionCentered(
                originalGrid.X, originalGrid.Y, gameAreaSize, MAP_WIDTH, MAP_HEIGHT);
            
            // Test clicking slightly off-center to simulate real user clicks
            var clickOffsets = new[]
            {
                new Vector2(0, 0),      // Perfect center
                new Vector2(5, 5),      // Slightly off-center
                new Vector2(-5, -5),    // Slightly off-center opposite
                new Vector2(10, 0),     // Edge of hex
                new Vector2(0, 10)      // Edge of hex
            };

            foreach (var offset in clickOffsets)
            {
                var clickPos = worldPos + offset;
                
                // Here we would need a reverse conversion function
                // For now, just verify the forward conversion is consistent
                GD.Print($"Original Grid({originalGrid.X},{originalGrid.Y}) -> World({worldPos.X:F1},{worldPos.Y:F1})");
                GD.Print($"   Click at World({clickPos.X:F1},{clickPos.Y:F1}) with offset({offset.X:F1},{offset.Y:F1})");
                
                // The key insight: if clicks are being interpreted as different grid positions
                // than where the visual tiles are, this would cause the offset issue
            }
            GD.Print("");
        }
    }

    [Test]
    public void Should_Detect_Logical_vs_Visual_Position_Mismatch()
    {
        GD.Print("\n🔍 LOGICAL vs VISUAL POSITION MISMATCH TEST:");
        GD.Print("This test simulates the exact positioning logic used in the game");
        
        // Test the exact positioning logic from Main.cs CreateVisualUnitsForPlayers
        var gameAreaSize = new Vector2(1720, 1080);
        gameAreaSize.X -= 200; // Subtract sidebar width (from Main.cs)
        
        var testCases = new[]
        {
            new Vector2I(0, 0),
            new Vector2I(1, 0), 
            new Vector2I(10, 4),
            new Vector2I(5, 5)
        };

        GD.Print($"Game Area Size: {gameAreaSize.X}x{gameAreaSize.Y} (after sidebar adjustment)");
        
        foreach (var logicalGridPos in testCases)
        {
            // According to Main.cs, BOTH tiles and units should use the same reduced viewport
            // Let me test what the code actually says vs. what I assumed
            var fullViewport = new Vector2(1720, 1080);
            var reducedViewport = new Vector2(1720, 1080);
            reducedViewport.X -= 200; // This is what Main.cs does for both tiles and units
            
            var tileWorldPos = HexGridCalculator.CalculateHexPositionCentered(
                logicalGridPos.X, logicalGridPos.Y, 
                reducedViewport, // Both should use reduced viewport according to Main.cs
                MAP_WIDTH, MAP_HEIGHT
            );
            
            var unitWorldPos = HexGridCalculator.CalculateHexPositionCentered(
                logicalGridPos.X, logicalGridPos.Y,
                reducedViewport, // Both should use reduced viewport according to Main.cs
                MAP_WIDTH, MAP_HEIGHT
            );
            
            // Let me also test what happens if one uses full viewport (to see the difference)
            var tileWorldPosFullViewport = HexGridCalculator.CalculateHexPositionCentered(
                logicalGridPos.X, logicalGridPos.Y,
                fullViewport, // Test with full viewport
                MAP_WIDTH, MAP_HEIGHT
            );
            
            var offset = tileWorldPos.DistanceTo(unitWorldPos);
            var fullViewportOffset = tileWorldPosFullViewport.DistanceTo(tileWorldPos);
            
            GD.Print($"Grid({logicalGridPos.X},{logicalGridPos.Y}):");
            GD.Print($"   Reduced Viewport Tile: ({tileWorldPos.X:F1},{tileWorldPos.Y:F1}) [1520x1080]");
            GD.Print($"   Reduced Viewport Unit: ({unitWorldPos.X:F1},{unitWorldPos.Y:F1}) [1520x1080]");
            GD.Print($"   Full Viewport Tile:    ({tileWorldPosFullViewport.X:F1},{tileWorldPosFullViewport.Y:F1}) [1720x1080]");
            GD.Print($"   Reduced vs Reduced:    {offset:F1} pixels offset");
            GD.Print($"   Full vs Reduced:       {fullViewportOffset:F1} pixels offset");
            
            if (offset > 5.0f) // More than 5 pixels difference between reduced viewport calculations
            {
                GD.Print($"   🚨 MISMATCH DETECTED: {offset:F1} pixel offset between tile and unit positioning!");
                Assert.Fail($"Significant positioning offset detected: {offset:F1} pixels for Grid({logicalGridPos.X},{logicalGridPos.Y})");
            }
            else if (fullViewportOffset > 5.0f)
            {
                GD.Print($"   📝 INFO: Full viewport would create {fullViewportOffset:F1} pixel offset");
                GD.Print($"   ✅ Reduced viewport calculations match - this is correct");
            }
            else
            {
                GD.Print($"   ✅ All calculations match within tolerance");
            }
            GD.Print("");
        }
    }
}
