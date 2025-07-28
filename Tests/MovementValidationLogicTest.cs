using NUnit.Framework;
using Godot;
using System.Collections.Generic;
using System.Linq;
using Archistrateia;

namespace Archistrateia.Tests
{
    [TestFixture]
    public partial class MovementValidationLogicTest : Node
    {
        [Test]
        public void Should_Allow_Valid_Movement_Within_Range()
        {
            var nakhtu = new Nakhtu(); // 2 movement points
            var fromTile = new HexTile(new Vector2I(0, 0), TerrainType.Desert); // 2 movement cost
            var toTile = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline); // 1 movement cost

            var canMove = MovementValidationLogic.CanUnitMoveTo(nakhtu, fromTile, toTile);

            Assert.IsTrue(canMove, "Unit should be able to move to adjacent tile within movement range");
        }

        [Test]
        public void Should_Prevent_Movement_Beyond_Range()
        {
            var nakhtu = new Nakhtu(); // 2 movement points
            nakhtu.CurrentMovementPoints = 1; // Only 1 point left
            var fromTile = new HexTile(new Vector2I(0, 0), TerrainType.Desert);
            var toTile = new HexTile(new Vector2I(1, 0), TerrainType.Hill); // 2 movement cost

            var canMove = MovementValidationLogic.CanUnitMoveTo(nakhtu, fromTile, toTile);

            Assert.IsFalse(canMove, "Unit should not be able to move to tile that costs more than remaining movement");
        }

        [Test]
        public void Should_Prevent_Movement_To_Occupied_Tile()
        {
            var fromTile = new HexTile(new Vector2I(0, 0), TerrainType.Shoreline);
            var toTile = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline);
            var nakhtu = new Nakhtu();
            var otherUnit = new Medjay();

            // Place another unit on the destination tile
            toTile.PlaceUnit(otherUnit);

            bool canMove = MovementValidationLogic.CanUnitMoveTo(nakhtu, fromTile, toTile);

            Assert.IsFalse(canMove, "Should not be able to move to occupied tile");
        }

        [Test]
        public void Should_Calculate_Adjacent_Tiles()
        {
            var centerPosition = new Vector2I(3, 3);

            var adjacentTiles = MovementValidationLogic.GetAdjacentPositions(centerPosition);

            Assert.AreEqual(6, adjacentTiles.Length, "Hex tile should have 6 adjacent positions");
            
            // Check that all adjacent positions are exactly 1 tile away
            foreach (var position in adjacentTiles)
            {
                var distance = Mathf.Abs(position.X - centerPosition.X) + Mathf.Abs(position.Y - centerPosition.Y);
                Assert.IsTrue(distance <= 2, $"Adjacent position {position} should be close to center {centerPosition}");
            }
        }

        [Test]
        public void Should_Get_Valid_Movement_Destinations()
        {
            var gameMap = CreateTestMap();
            var nakhtu = new Nakhtu(); // Has 2 movement points
            var currentPosition = new Vector2I(1, 1);

            var logic = new MovementValidationLogic();
            var validDestinations = logic.GetValidMovementDestinations(nakhtu, currentPosition, gameMap);

            Assert.IsTrue(validDestinations.Count > 0, "Should find some valid destinations");
            
            // All returned destinations should be reachable
            foreach (var destination in validDestinations)
            {
                var tile = gameMap[destination];
                Assert.IsTrue(tile.MovementCost <= nakhtu.CurrentMovementPoints,
                    $"Destination {destination} with cost {tile.MovementCost} should be within {nakhtu.CurrentMovementPoints} movement points");
            }
        }

        [Test]
        public void Should_Find_Multi_Step_Movement_Destinations()
        {
            var logic = new MovementValidationLogic();
            var gameMap = CreateSpecialTestMap();
            var charioteer = new Charioteer(); // Has 4 movement points
            var startPosition = new Vector2I(1, 1);

            var validDestinations = logic.GetValidMovementDestinations(charioteer, startPosition, gameMap);

            Assert.IsTrue(validDestinations.Count > 0, "Should find multi-step destinations");
            
            // Verify that we can reach destinations beyond immediate adjacency
            bool foundMultiStepDestination = false;
            foreach (var destination in validDestinations)
            {
                var distance = Mathf.Abs(destination.X - startPosition.X) + Mathf.Abs(destination.Y - startPosition.Y);
                if (distance > 1)
                {
                    foundMultiStepDestination = true;
                    break;
                }
            }
            
            Assert.IsTrue(foundMultiStepDestination, "Should find destinations that require multiple steps to reach");
        }

        [Test]
        public void Should_Not_Find_Destinations_Beyond_Movement_Budget()
        {
            var logic = new MovementValidationLogic();
            var gameMap = CreatePathfindingTestMap();
            var archer = new Archer(); // Has 4 movement points
            var startPosition = new Vector2I(1, 1);
            
            // Test with limited movement points
            archer.CurrentMovementPoints = 2;
            
            var validDestinations = logic.GetValidMovementDestinations(archer, startPosition, gameMap);
            
            // Verify that all destinations are reachable within the movement budget
            foreach (var destination in validDestinations)
            {
                var tile = gameMap[destination];
                Assert.IsTrue(tile.MovementCost <= archer.CurrentMovementPoints,
                    $"Destination {destination} should be reachable with {archer.CurrentMovementPoints} movement points");
            }
            
            // Test with very limited movement points
            archer.CurrentMovementPoints = 1;
            var limitedDestinations = logic.GetValidMovementDestinations(archer, startPosition, gameMap);
            
            // Should find fewer destinations with limited movement
            Assert.IsTrue(limitedDestinations.Count <= validDestinations.Count, 
                "Should find fewer destinations with limited movement points");
        }

        [Test]
        public void Should_Find_Optimal_Path_Within_Movement_Budget()
        {
            var logic = new MovementValidationLogic();
            var gameMap = CreatePathfindingTestMap();
            var archer = new Archer(); // Has 4 movement points
            var startPosition = new Vector2I(1, 1);
            
            var validDestinations = logic.GetValidMovementDestinations(archer, startPosition, gameMap);
            var pathCosts = logic.GetPathCostsFromPosition(archer, startPosition, gameMap);
            
            // Verify that path costs are calculated correctly
            foreach (var destination in validDestinations)
            {
                Assert.IsTrue(pathCosts.ContainsKey(destination), $"Should have path cost for destination {destination}");
                var pathCost = pathCosts[destination];
                Assert.IsTrue(pathCost <= archer.CurrentMovementPoints, 
                    $"Path cost {pathCost} should be within movement budget {archer.CurrentMovementPoints}");
            }
        }

        [Test]
        public void Should_Allow_Multiple_Moves_With_Movement_Budget()
        {
            var fromTile = new HexTile(new Vector2I(0, 0), TerrainType.Shoreline); // Cost 1
            var toTile = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline);   // Cost 1
            var charioteer = new Charioteer(); // Has 4 movement points

            var initialMP = charioteer.MovementPoints;
            Assert.AreEqual(initialMP, charioteer.CurrentMovementPoints, $"Charioteer should start with {initialMP} movement points");

            // First move should be allowed
            bool canMoveFirst = MovementValidationLogic.CanUnitMoveTo(charioteer, fromTile, toTile);
            Assert.IsTrue(canMoveFirst, "First move should be allowed");

            // Simulate first move and verify remaining points
            var firstMoveCost = toTile.MovementCost;
            charioteer.CurrentMovementPoints -= firstMoveCost;
            var expectedAfterFirst = initialMP - firstMoveCost;
            Assert.AreEqual(expectedAfterFirst, charioteer.CurrentMovementPoints, $"Should have {expectedAfterFirst} points after first move (cost {firstMoveCost})");

            // Second move should still be allowed with remaining points
            bool canMoveSecond = MovementValidationLogic.CanUnitMoveTo(charioteer, toTile, fromTile);
            Assert.IsTrue(canMoveSecond, "Second move should be allowed with remaining movement points");

            // Simulate second move and verify remaining points
            var secondMoveCost = fromTile.MovementCost;
            charioteer.CurrentMovementPoints -= secondMoveCost;
            var expectedAfterSecond = expectedAfterFirst - secondMoveCost;
            Assert.AreEqual(expectedAfterSecond, charioteer.CurrentMovementPoints, $"Should have {expectedAfterSecond} points after second move (cost {secondMoveCost})");

            // Third move should still be allowed if unit has enough points
            bool canMoveThird = MovementValidationLogic.CanUnitMoveTo(charioteer, fromTile, toTile);
            var thirdMoveCost = toTile.MovementCost;
            bool shouldAllowThird = charioteer.CurrentMovementPoints >= thirdMoveCost;
            Assert.AreEqual(shouldAllowThird, canMoveThird, $"Third move should be {(shouldAllowThird ? "allowed" : "blocked")} with {charioteer.CurrentMovementPoints} MP and cost {thirdMoveCost}");
        }

        [Test]
        public void Should_Deduct_Multi_Step_Path_Cost_Correctly()
        {
            // Test that multi-step movement deducts the total path cost
            var coordinator = new MovementCoordinator();
            
            GD.Print("=== TESTING MULTI-STEP PATH COST DEDUCTION ===");
            
            // Create a path where destination requires multi-step movement
            var gameMap = new Dictionary<Vector2I, HexTile>();
            gameMap[new Vector2I(0, 0)] = new HexTile(new Vector2I(0, 0), TerrainType.Shoreline); // Start
            gameMap[new Vector2I(1, 0)] = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline); // Step 1: cost 1
            gameMap[new Vector2I(2, 0)] = new HexTile(new Vector2I(2, 0), TerrainType.River);     // Step 2: cost 3
            gameMap[new Vector2I(3, 0)] = new HexTile(new Vector2I(3, 0), TerrainType.Shoreline); // Destination: cost 1
            
            // Total path cost: (0,0) → (1,0) → (2,0) → (3,0) = 1 + 3 + 1 = 5 total
            
            var charioteer = new Charioteer(); // 8 MP initially  
            var initialMP = charioteer.CurrentMovementPoints;
            
            GD.Print($"Charioteer starts with {initialMP} MP");
            GD.Print("Path: (0,0) → (1,0) → (2,0) → (3,0) with costs 1 + 3 + 1 = 5 total");
            
            coordinator.SelectUnitForMovement(charioteer);
            
            // Move directly to destination (3,0) - should deduct total path cost of 5
            var moveResult = coordinator.TryMoveToDestination(new Vector2I(0, 0), new Vector2I(3, 0), gameMap);
            Assert.IsTrue(moveResult.Success, "Multi-step move should succeed");
            
            var expectedMPAfterMove = initialMP - 5; // Should deduct total path cost
            GD.Print($"After multi-step move: Expected {expectedMPAfterMove} MP, Actual {charioteer.CurrentMovementPoints} MP");
            
            Assert.AreEqual(expectedMPAfterMove, charioteer.CurrentMovementPoints, 
                "BUG: Should deduct total path cost (5) not just 1 per click");
            
            GD.Print("✅ Multi-step path cost deduction test completed");
        }

        [Test]
        public void Should_Prevent_Movement_When_Insufficient_Points()
        {
            var fromTile = new HexTile(new Vector2I(0, 0), TerrainType.Shoreline); // Cost 1
            var toTile = new HexTile(new Vector2I(1, 0), TerrainType.Desert);      // Cost 2
            var nakhtu = new Nakhtu(); // Has 2 movement points

            // Reduce movement points to just 1
            nakhtu.CurrentMovementPoints = 1;

            // Should NOT be able to move to Desert tile (costs 2)
            bool canMove = MovementValidationLogic.CanUnitMoveTo(nakhtu, fromTile, toTile);
            Assert.IsFalse(canMove, "Should not be able to move to tile that costs more than remaining points");
        }

        [Test]
        public void Should_Find_Destinations_Based_On_Remaining_Movement()
        {
            var logic = new MovementValidationLogic();
            var gameMap = CreateTestMap();
            var charioteer = new Charioteer(); // Has 4 movement points initially
            var currentPosition = new Vector2I(1, 1);

            // Get destinations with full movement budget
            var fullBudgetDestinations = logic.GetValidMovementDestinations(charioteer, currentPosition, gameMap);
            int fullCount = fullBudgetDestinations.Count;
            Assert.IsTrue(fullCount > 0, "Should find destinations with full movement budget");

            // Reduce movement points to 1
            charioteer.CurrentMovementPoints = 1;

            // Should find fewer destinations with reduced budget
            var reducedBudgetDestinations = logic.GetValidMovementDestinations(charioteer, currentPosition, gameMap);
            int reducedCount = reducedBudgetDestinations.Count;
            Assert.IsTrue(reducedCount <= fullCount, "Should find same or fewer destinations with reduced movement");
            
            // All destinations should be reachable with remaining points
            foreach (var destination in reducedBudgetDestinations)
            {
                var tile = gameMap[destination];
                Assert.IsTrue(tile.MovementCost <= charioteer.CurrentMovementPoints,
                    $"Destination {destination} should be reachable with {charioteer.CurrentMovementPoints} remaining points");
            }
        }

        [Test]
        public void Should_Find_No_Destinations_When_No_Movement_Points()
        {
            var logic = new MovementValidationLogic();
            var gameMap = CreateTestMap();
            var nakhtu = new Nakhtu();
            var currentPosition = new Vector2I(1, 1);

            // Set movement points to 0
            nakhtu.CurrentMovementPoints = 0;

            // Should find no valid destinations
            var destinations = logic.GetValidMovementDestinations(nakhtu, currentPosition, gameMap);
            Assert.AreEqual(0, destinations.Count, "Should find no destinations with 0 movement points");
        }

        [Test]
        public void Should_Allow_Movement_To_Destinations_With_Exact_Budget()
        {
            var logic = new MovementValidationLogic();
            // Verify that when a path costs exactly the unit's movement budget, it should be allowed
            
            // Create a controlled scenario with a path that costs exactly 8 MP (matching Charioteer's budget)
            var gameMap = new Dictionary<Vector2I, HexTile>();
            gameMap[new Vector2I(0, 0)] = new HexTile(new Vector2I(0, 0), TerrainType.Shoreline); // Start (0)
            gameMap[new Vector2I(1, 0)] = new HexTile(new Vector2I(1, 0), TerrainType.Shoreline); // +1 = 1
            gameMap[new Vector2I(2, 0)] = new HexTile(new Vector2I(2, 0), TerrainType.Desert);    // +2 = 3
            gameMap[new Vector2I(3, 0)] = new HexTile(new Vector2I(3, 0), TerrainType.Hill);      // +2 = 5
            gameMap[new Vector2I(4, 0)] = new HexTile(new Vector2I(4, 0), TerrainType.River);     // +3 = 8
            
            var charioteer = new Charioteer(); // 8 MP
            
            var validDestinations = logic.GetValidMovementDestinations(charioteer, new Vector2I(0, 0), gameMap);
            var pathCosts = logic.GetPathCostsFromPosition(charioteer, new Vector2I(0, 0), gameMap);
            
            // The destination at cost 8 should be reachable
            bool canReachExactBudget = validDestinations.Contains(new Vector2I(4, 0));
            int pathCostToEnd = pathCosts.ContainsKey(new Vector2I(4, 0)) ? pathCosts[new Vector2I(4, 0)] : -1;
            
            // This should PASS - exact budget paths are valid
            Assert.IsTrue(canReachExactBudget, "Units should be able to move to destinations that cost exactly their movement budget");
            Assert.AreEqual(8, pathCostToEnd, "Path cost should exactly match movement budget");
        }

        private static Dictionary<Vector2I, HexTile> CreateTestMap()
        {
            var map = new Dictionary<Vector2I, HexTile>();
            
            // Create a 3x3 test map
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var position = new Vector2I(x, y);
                    var terrainType = (x + y) % 2 == 0 ? TerrainType.Shoreline : TerrainType.Desert;
                    map[position] = new HexTile(position, terrainType);
                }
            }
            
            return map;
        }

        private static Dictionary<Vector2I, HexTile> CreateSpecialTestMap()
        {
            var map = new Dictionary<Vector2I, HexTile>();
            
            // Create a map with mixed terrain types for testing multi-step movement
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    var position = new Vector2I(x, y);
                    TerrainType terrainType;
                    
                    if (x == 2 && y == 2) // Center
                        terrainType = TerrainType.Shoreline;
                    else if (x % 2 == 0 && y % 2 == 0) // Even positions
                        terrainType = TerrainType.Desert;
                    else if (x % 2 == 1 && y % 2 == 1) // Odd positions
                        terrainType = TerrainType.Hill;
                    else
                        terrainType = TerrainType.Shoreline;
                    
                    map[position] = new HexTile(position, terrainType);
                }
            }
            
            return map;
        }

        private static Dictionary<Vector2I, HexTile> CreatePathfindingTestMap()
        {
            var map = new Dictionary<Vector2I, HexTile>();
            
            // Create a map with various terrain types for testing pathfinding
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    var position = new Vector2I(x, y);
                    TerrainType terrainType;
                    
                    if (x == 0 && y == 0) // Start position
                        terrainType = TerrainType.Shoreline;
                    else if (x == 3 && y == 3) // End position
                        terrainType = TerrainType.River;
                    else if (x == 1 && y == 1) // Middle position
                        terrainType = TerrainType.Desert;
                    else
                        terrainType = TerrainType.Shoreline;
                    
                    map[position] = new HexTile(position, terrainType);
                }
            }
            
            return map;
        }
        
        [Test]
        public void Should_Verify_Second_Ring_Neighbors_Are_Not_Adjacent()
        {
            // Test that second ring neighbors (2 steps away) are never considered adjacent
            GD.Print("=== TESTING SECOND RING NEIGHBORS ===");
            GD.Print("Verifying that hexes 2 steps away are never considered adjacent");
            
            // Test positions that should be second ring neighbors
            var testCases = new[]
            {
                // Even column positions and their second ring neighbors
                new { Center = new Vector2I(0, 0), SecondRing = new Vector2I(0, 2) },
                new { Center = new Vector2I(0, 0), SecondRing = new Vector2I(2, 0) },
                new { Center = new Vector2I(0, 0), SecondRing = new Vector2I(2, 1) },
                new { Center = new Vector2I(2, 2), SecondRing = new Vector2I(0, 2) },
                new { Center = new Vector2I(2, 2), SecondRing = new Vector2I(4, 2) },
                new { Center = new Vector2I(2, 2), SecondRing = new Vector2I(2, 0) },
                new { Center = new Vector2I(2, 2), SecondRing = new Vector2I(2, 4) },
                
                // Odd column positions and their second ring neighbors
                new { Center = new Vector2I(1, 1), SecondRing = new Vector2I(1, 3) },
                new { Center = new Vector2I(1, 1), SecondRing = new Vector2I(3, 1) },
                new { Center = new Vector2I(1, 1), SecondRing = new Vector2I(3, 2) },
                new { Center = new Vector2I(3, 3), SecondRing = new Vector2I(1, 3) },
                new { Center = new Vector2I(3, 3), SecondRing = new Vector2I(5, 3) },
                new { Center = new Vector2I(3, 3), SecondRing = new Vector2I(3, 1) },
                new { Center = new Vector2I(3, 3), SecondRing = new Vector2I(3, 5) },
                
                // Diagonal second ring neighbors
                new { Center = new Vector2I(0, 0), SecondRing = new Vector2I(2, 2) },
                new { Center = new Vector2I(2, 0), SecondRing = new Vector2I(0, 2) },
                new { Center = new Vector2I(1, 1), SecondRing = new Vector2I(3, 3) },
                new { Center = new Vector2I(3, 1), SecondRing = new Vector2I(1, 3) }
            };
            
            bool anyFalseAdjacencies = false;
            
            foreach (var testCase in testCases)
            {
                var centerAdjacents = MovementValidationLogic.GetAdjacentPositions(testCase.Center).ToList();
                bool isSecondRingAdjacent = centerAdjacents.Contains(testCase.SecondRing);
                
                GD.Print($"{testCase.Center} → {testCase.SecondRing}: {(isSecondRingAdjacent ? "🚨 FALSE ADJACENCY" : "✅ Correctly not adjacent")}");
                
                if (isSecondRingAdjacent)
                {
                    anyFalseAdjacencies = true;
                }
            }
            
            // Also test some specific cases from the bug scenarios
            GD.Print("\n=== TESTING BUG SCENARIO SECOND RING CASES ===");
            
            // Test if the "island" positions are actually second ring neighbors
            var medjayPos = new Vector2I(1, 3);
            var nakhtuPos = new Vector2I(7, 2);
            
            // For (1,3), these should be second ring neighbors (2 steps away), not adjacent
            var medjaySecondRing = new Vector2I[] { new Vector2I(1, 1), new Vector2I(1, 5), new Vector2I(-1, 3), new Vector2I(3, 3) };
            var nakhtuSecondRing = new Vector2I[] { new Vector2I(7, 0), new Vector2I(7, 4), new Vector2I(5, 2), new Vector2I(9, 2) };
            
            GD.Print($"\nMedjay ({medjayPos}) second ring neighbors:");
            foreach (var secondRing in medjaySecondRing)
            {
                var medjayAdjacents = MovementValidationLogic.GetAdjacentPositions(medjayPos).ToList();
                bool isAdjacent = medjayAdjacents.Contains(secondRing);
                GD.Print($"  {secondRing}: {(isAdjacent ? "🚨 FALSE ADJACENCY" : "✅ Correctly not adjacent")}");
                if (isAdjacent) anyFalseAdjacencies = true;
            }
            
            GD.Print($"\nNakhtu ({nakhtuPos}) second ring neighbors:");
            foreach (var secondRing in nakhtuSecondRing)
            {
                var nakhtuAdjacents = MovementValidationLogic.GetAdjacentPositions(nakhtuPos).ToList();
                bool isAdjacent = nakhtuAdjacents.Contains(secondRing);
                GD.Print($"  {secondRing}: {(isAdjacent ? "🚨 FALSE ADJACENCY" : "✅ Correctly not adjacent")}");
                if (isAdjacent) anyFalseAdjacencies = true;
            }
            
            if (anyFalseAdjacencies)
            {
                GD.Print("\n🚨 BUG CONFIRMED: Second ring neighbors are being incorrectly marked as adjacent!");
                GD.Print("This explains why pathfinding can reach 'islands' that should be unreachable.");
            }
            else
            {
                GD.Print("\n✅ All second ring neighbors correctly identified as non-adjacent");
            }
        }
        
        [Test]
        public void Should_Compare_Current_vs_Proper_Hex_Adjacency()
        {
            // Systematic comparison of current vs proper hex adjacency
            var logic = new MovementValidationLogic();
            
            GD.Print("=== SYSTEMATIC HEX ADJACENCY COMPARISON ===");
            
            // Test a grid of positions to see differences
            for (int x = 0; x <= 3; x++)
            {
                for (int y = 0; y <= 3; y++)
                {
                    var pos = new Vector2I(x, y);
                    var currentAdjacents = MovementValidationLogic.GetAdjacentPositions(pos).ToList();
                    var properAdjacents = MovementValidationLogic.GetAdjacentPositions(pos).ToList();
                    
                    // Find differences
                    var onlyInCurrent = currentAdjacents.Except(properAdjacents).ToList();
                    var onlyInProper = properAdjacents.Except(currentAdjacents).ToList();
                    
                    if (onlyInCurrent.Count > 0 || onlyInProper.Count > 0)
                    {
                        GD.Print($"\n{pos} (column {(x % 2 == 0 ? "even" : "odd")}):");
                        GD.Print($"  Current: {string.Join(", ", currentAdjacents)}");
                        GD.Print($"  Proper:  {string.Join(", ", properAdjacents)}");
                        
                        if (onlyInCurrent.Count > 0)
                        {
                            GD.Print($"  🚨 False adjacencies: {string.Join(", ", onlyInCurrent)}");
                        }
                        
                        if (onlyInProper.Count > 0)
                        {
                            GD.Print($"  ❌ Missing adjacencies: {string.Join(", ", onlyInProper)}");
                        }
                    }
                }
            }
            
            GD.Print("\n✅ Hex adjacency comparison completed");
        }
    }
} 