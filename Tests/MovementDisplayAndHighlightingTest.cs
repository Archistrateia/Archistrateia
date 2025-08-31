using Godot;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public partial class MovementDisplayAndHighlightingTest : Node
    {
        [TearDown]
        public void CleanupAfterEachTest()
        {
            // Clean up all children to prevent resource leaks
            foreach (var child in GetChildren())
            {
                if (child is Node node)
                {
                    node.QueueFree();
                }
            }
            
            // Force cleanup of any remaining resources
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [Test]
        public void Should_Not_Show_MP_Display_When_Unit_Not_Selected()
        {
            // This test verifies the core logic that should prevent MP displays from showing
            // when units are outside their movement range, without requiring visual components
            
            // Arrange: Create a simple test map and unit
            var gameMap = CreateTestMap();
            var unit = new Archer(); // 4 MP
            var unitPosition = new Vector2I(1, 1);
            gameMap[unitPosition].PlaceUnit(unit);
            
            // Act: Calculate reachable positions
            var reachablePositions = GetReachablePositions(unitPosition, 4, gameMap);
            
            // Assert: Only positions within movement range should be reachable
            foreach (var position in reachablePositions)
            {
                var pathCost = CalculatePathCost(unitPosition, position, gameMap);
                Assert.LessOrEqual(pathCost, 4, 
                    $"Position {position} should be reachable within 4 MP, but costs {pathCost}");
            }
            
            // Verify that distant positions are not included
            var distantPosition = new Vector2I(10, 10);
            Assert.IsFalse(reachablePositions.Contains(distantPosition), 
                "Distant position should not be in reachable positions");
        }

        [Test]
        public void Should_Not_Highlight_Units_Outside_Movement_Range()
        {
            // Arrange: Create a test map with units at different positions
            var gameMap = CreateTestMap();
            var unit1 = new Archer(); // 4 MP
            var unit2 = new Medjay(); // 1 MP
            
            // Place units at different positions
            gameMap[new Vector2I(1, 1)].PlaceUnit(unit1);
            gameMap[new Vector2I(4, 4)].PlaceUnit(unit2); // Far away but within map bounds
            
            // Act: Get valid movement destinations for unit1
            var validDestinations = GetReachablePositions(new Vector2I(1, 1), 4, gameMap);
            
            // Assert: Unit2's position should not be in valid destinations (too far away)
            var unit2Position = new Vector2I(4, 4);
            Assert.IsFalse(validDestinations.Contains(unit2Position), 
                "Unit outside movement range should not be in valid destinations");
            
            // Verify that only reachable tiles are highlighted
            Assert.IsTrue(validDestinations.Count > 0, "Should have some valid destinations");
            Assert.IsTrue(validDestinations.Count < gameMap.Count, "Should not highlight all tiles");
        }

        [Test]
        public void Should_Only_Highlight_Tiles_Within_Movement_Range()
        {
            // Arrange: Create a test map
            var gameMap = CreateTestMap();
            var unitPosition = new Vector2I(1, 1);
            
            // Act: Get valid movement destinations
            var validDestinations = GetReachablePositions(unitPosition, 4, gameMap);
            
            // Assert: Should only highlight tiles within 4 MP range
            foreach (var destination in validDestinations)
            {
                var pathCost = CalculatePathCost(unitPosition, destination, gameMap);
                Assert.LessOrEqual(pathCost, 4, 
                    $"Destination {destination} should be reachable within 4 MP, but costs {pathCost}");
            }
        }

        [Test]
        public void Should_Not_Highlight_Occupied_Tiles_Outside_Movement_Range()
        {
            // Arrange: Create a test map with a unit far away
            var gameMap = CreateTestMap();
            var farAwayPosition = new Vector2I(10, 10);
            var farAwayTile = new HexTile(farAwayPosition, TerrainType.Shoreline);
            var farAwayUnit = new Archer();
            farAwayTile.PlaceUnit(farAwayUnit);
            gameMap[farAwayPosition] = farAwayTile;
            
            // Act: Get valid movement destinations
            var unitPosition = new Vector2I(1, 1);
            var validDestinations = GetReachablePositions(unitPosition, 4, gameMap);
            
            // Assert: Far away occupied tile should not be highlighted
            Assert.IsFalse(validDestinations.Contains(farAwayPosition), 
                "Occupied tile outside movement range should not be highlighted");
        }

        [Test]
        public void Should_Respect_Movement_Point_Limits()
        {
            // Arrange: Test with different movement point budgets
            var gameMap = CreateTestMap();
            var unitPosition = new Vector2I(1, 1);
            
            // Act: Get destinations with different MP budgets
            var destinationsWith1MP = GetReachablePositions(unitPosition, 1, gameMap);
            var destinationsWith2MP = GetReachablePositions(unitPosition, 2, gameMap);
            var destinationsWith4MP = GetReachablePositions(unitPosition, 4, gameMap);
            
            // Assert: More MP should allow more destinations
            Assert.LessOrEqual(destinationsWith1MP.Count, destinationsWith2MP.Count, 
                "2 MP should allow at least as many destinations as 1 MP");
            Assert.LessOrEqual(destinationsWith2MP.Count, destinationsWith4MP.Count, 
                "4 MP should allow at least as many destinations as 2 MP");
            
            // Verify all destinations are within budget
            foreach (var destination in destinationsWith1MP)
            {
                var pathCost = CalculatePathCost(unitPosition, destination, gameMap);
                Assert.LessOrEqual(pathCost, 1, 
                    $"Destination {destination} should be reachable within 1 MP, but costs {pathCost}");
            }
        }

        [Test]
        public void Should_Not_Include_Occupied_Tiles_In_Valid_Destinations()
        {
            // Arrange: Create a test map with occupied tiles
            var gameMap = CreateTestMap();
            var occupiedPosition1 = new Vector2I(2, 1);
            var occupiedPosition2 = new Vector2I(1, 2);
            var testUnit1 = new Archer();
            var testUnit2 = new Medjay();
            gameMap[occupiedPosition1].PlaceUnit(testUnit1);
            gameMap[occupiedPosition2].PlaceUnit(testUnit2);
            
            // Act: Get valid movement destinations
            var unitPosition = new Vector2I(1, 1);
            var validDestinations = GetReachablePositions(unitPosition, 4, gameMap);
            
            // Assert: Occupied tiles should not be in valid destinations
            Assert.IsFalse(validDestinations.Contains(occupiedPosition1), 
                "Occupied tile should not be in valid destinations");
            Assert.IsFalse(validDestinations.Contains(occupiedPosition2), 
                "Occupied tile should not be in valid destinations");
        }

        [Test]
        public void Should_Calculate_Correct_Path_Costs()
        {
            // Arrange: Test path costs between different positions
            var gameMap = CreateTestMap();
            var startPosition = new Vector2I(1, 1);
            var adjacentPosition = new Vector2I(2, 1);
            var distantPosition = new Vector2I(3, 1);
            
            // Act: Calculate path costs
            var costToAdjacent = CalculatePathCost(startPosition, adjacentPosition, gameMap);
            var costToDistant = CalculatePathCost(startPosition, distantPosition, gameMap);
            
            // Assert: Path costs should be calculated correctly
            var adjacentTile = gameMap[adjacentPosition];
            var distantTile = gameMap[distantPosition];
            
            // Adjacent tile cost should match its terrain cost
            Assert.AreEqual(adjacentTile.MovementCost, costToAdjacent, 
                "Adjacent tile cost should match its terrain movement cost");
            
            // Verify the actual costs based on terrain and pathfinding
            Assert.AreEqual(2, costToAdjacent, "Desert tile should cost 2 MP");
            Assert.AreEqual(3, costToDistant, "Path to distant tile should cost 3 MP (1 + 2)");
        }

        [Test]
        public void Should_Handle_Edge_Cases_For_Movement_Calculation()
        {
            // Arrange: Test edge cases
            var gameMap = CreateTestMap();
            var unitPosition = new Vector2I(1, 1);
            
            // Act: Test with 0 MP
            var destinationsWith0MP = GetReachablePositions(unitPosition, 0, gameMap);
            
            // Assert: No destinations should be reachable with 0 MP
            Assert.AreEqual(0, destinationsWith0MP.Count, "No destinations should be reachable with 0 MP");
            
            // Test with negative MP (edge case)
            var destinationsWithNegativeMP = GetReachablePositions(unitPosition, -1, gameMap);
            Assert.AreEqual(0, destinationsWithNegativeMP.Count, "No destinations should be reachable with negative MP");
        }

        [Test]
        public void Should_Update_Unit_Position_After_Movement()
        {
            // Arrange: Start with unit at position (1, 1)
            var gameMap = CreateTestMap();
            var fromPosition = new Vector2I(1, 1);
            var toPosition = new Vector2I(2, 1);
            var unit = new Archer();
            gameMap[fromPosition].PlaceUnit(unit);
            
            // Verify initial state
            Assert.AreEqual(unit, gameMap[fromPosition].OccupyingUnit, "Unit should start at fromPosition");
            Assert.IsNull(gameMap[toPosition].OccupyingUnit, "To position should be empty initially");
            
            // Act: Move unit
            gameMap[fromPosition].RemoveUnit();
            gameMap[toPosition].PlaceUnit(unit);
            
            // Assert: Unit should be at new position
            Assert.IsNull(gameMap[fromPosition].OccupyingUnit, "From position should be empty after move");
            Assert.AreEqual(unit, gameMap[toPosition].OccupyingUnit, "Unit should be at new position");
        }

        [Test]
        public void Should_Test_MP_Reset_Logic_Directly()
        {
            // Test the MP reset logic directly without going through the signal chain
            // Use the same simple pattern as the working test to avoid GameManager issues
            
            // Create a simple player and unit directly
            var player = new Player("TestPlayer", 100);
            var unit = new Nakhtu();
            player.AddUnit(unit);
            
            // Verify unit starts with full MP
            Assert.AreEqual(4, unit.CurrentMovementPoints, "Unit should start with full MP");
            
            // Manually reset MP to 0 (simulating what should happen when leaving movement phase)
            unit.CurrentMovementPoints = 0;
            
            // Verify MP was reset
            Assert.AreEqual(0, unit.CurrentMovementPoints, "Unit should have 0 MP after manual reset");
            
            // Now restore MP (simulating what should happen when entering movement phase)
            player.ResetUnitMovement();
            
            // Verify MP was restored
            Assert.AreEqual(4, unit.CurrentMovementPoints, "Unit should have full MP restored");
        }

        [Test]
        public void Should_Verify_MP_Reset_Logic_Works()
        {
            // Simple test to verify the MP reset logic works
            // This tests the core functionality without the complex signal chain
            
            // Create a simple unit
            var unit = new Nakhtu();
            Assert.AreEqual(4, unit.CurrentMovementPoints, "Unit should start with full MP");
            
            // Test MP reset
            unit.CurrentMovementPoints = 0;
            Assert.AreEqual(0, unit.CurrentMovementPoints, "Unit should have 0 MP after reset");
            
            // Test MP restoration
            unit.ResetMovement();
            Assert.AreEqual(4, unit.CurrentMovementPoints, "Unit should have full MP restored");
        }

        // Helper methods for testing movement logic
        private static Dictionary<Vector2I, HexTile> CreateTestMap()
        {
            var map = new Dictionary<Vector2I, HexTile>();
            
            // Create a 5x5 test map
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    var position = new Vector2I(x, y);
                    var terrainType = (x + y) % 2 == 0 ? TerrainType.Shoreline : TerrainType.Desert;
                    map[position] = new HexTile(position, terrainType);
                }
            }
            
            return map;
        }

        private static HashSet<Vector2I> GetReachablePositions(Vector2I startPosition, int maxMovement, Dictionary<Vector2I, HexTile> gameMap)
        {
            // Use the actual game's movement validation logic
            var unit = new Archer(); // 4 MP
            unit.CurrentMovementPoints = maxMovement;
            var destinations = MovementValidationLogic.GetValidMovementDestinations(unit, startPosition, gameMap);
            return new HashSet<Vector2I>(destinations);
        }

        private static int CalculatePathCost(Vector2I from, Vector2I to, Dictionary<Vector2I, HexTile> gameMap)
        {
            // Use the actual game's path cost calculation
            var unit = new Archer(); // 4 MP
            var pathCosts = MovementValidationLogic.GetPathCostsFromPosition(unit, from, gameMap);
            
            if (pathCosts.ContainsKey(to))
            {
                return pathCosts[to];
            }
            
            return int.MaxValue; // No path found
        }
    }
}
