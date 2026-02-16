using Godot;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Archistrateia;

[TestFixture]
public class SemicircleDeploymentServiceTest
{
    [Test]
    public void GetDeployableTilesForPlayer_Should_ExcludeOccupiedTiles()
    {
        var map = BuildRectMap(20, 12);
        var service = new SemicircleDeploymentService();

        var deployable = service.GetDeployableTilesForPlayer(map, playerIndex: 0, playerCount: 2);
        Assert.IsTrue(deployable.Count > 0, "Expected at least one deployable tile");

        map[deployable[0]].PlaceUnit(new Nakhtu());

        var afterOccupy = service.GetDeployableTilesForPlayer(map, playerIndex: 0, playerCount: 2);
        Assert.Less(afterOccupy.Count, deployable.Count, "Occupied deployment tiles should not stay deployable");
    }

    [Test]
    public void GetDeployableTilesForTwoPlayers_Should_BiasTowardsOppositeEdges()
    {
        var map = BuildRectMap(20, 12);
        var service = new SemicircleDeploymentService();

        var p1Tiles = service.GetDeployableTilesForPlayer(map, playerIndex: 0, playerCount: 2);
        var p2Tiles = service.GetDeployableTilesForPlayer(map, playerIndex: 1, playerCount: 2);
        Assert.IsTrue(p1Tiles.Count > 0 && p2Tiles.Count > 0, "Each player should have deployable tiles");

        float p1AvgX = 0;
        foreach (var tile in p1Tiles) p1AvgX += tile.X;
        p1AvgX /= p1Tiles.Count;

        float p2AvgX = 0;
        foreach (var tile in p2Tiles) p2AvgX += tile.X;
        p2AvgX /= p2Tiles.Count;

        float centerX = (20 - 1) / 2.0f;
        Assert.Less(p1AvgX, centerX, "Player 1 deployment should bias toward left side");
        Assert.Greater(p2AvgX, centerX, "Player 2 deployment should bias toward right side");
    }

    [Test]
    public void GetDeployableTilesForFourPlayers_Should_GiveEachPlayerUsableZone()
    {
        var map = BuildRectMap(50, 30);
        var service = new SemicircleDeploymentService();

        for (int playerIndex = 0; playerIndex < 4; playerIndex++)
        {
            var tiles = service.GetDeployableTilesForPlayer(map, playerIndex, playerCount: 4);
            Assert.GreaterOrEqual(tiles.Count, 10, $"Player {playerIndex} should have a usable deployment zone");
        }
    }

    [Test]
    public void AdjacentPlayers_Should_Not_HaveHeavyDeploymentOverlap()
    {
        var map = BuildRectMap(50, 30);
        var service = new SemicircleDeploymentService();

        var player0 = service.GetDeployableTilesForPlayer(map, 0, playerCount: 4);
        var player1 = service.GetDeployableTilesForPlayer(map, 1, playerCount: 4);

        Assert.IsTrue(player0.Count > 0 && player1.Count > 0, "Expected non-empty deployment zones");

        var set0 = new HashSet<Vector2I>(player0);
        int overlap = player1.Count(tile => set0.Contains(tile));
        float overlapRatio = overlap / (float)Mathf.Max(1, Mathf.Min(player0.Count, player1.Count));

        Assert.Less(overlapRatio, 0.20f, $"Deployment overlap too high: {overlapRatio:P1}");
    }

    private static Dictionary<Vector2I, HexTile> BuildRectMap(int width, int height)
    {
        var map = new Dictionary<Vector2I, HexTile>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pos = new Vector2I(x, y);
                map[pos] = new HexTile(pos, TerrainType.Grassland);
            }
        }

        return map;
    }
}
