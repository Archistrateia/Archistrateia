using Godot;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using Archistrateia;

[TestFixture]
public class MainRuntimeBootstrapControllerTest
{
    [Test]
    public void InitializeGameManager_Should_Set_GameManager_And_Defer_Connect()
    {
        var mapContainer = new Node2D();
        var logicalMap = BuildRectMap(2, 2);
        var gameManager = new GameManager();
        Node2D convertInput = null;
        Dictionary<Vector2I, HexTile> initializeInput = null;
        GameManager capturedManager = null;
        int deferredCalls = 0;
        var controller = CreateController(
            getMapContainer: () => mapContainer,
            convertVisualMapToGameMap: container =>
            {
                convertInput = container;
                return logicalMap;
            },
            initializeGameManagerCore: map =>
            {
                initializeInput = map;
                return gameManager;
            },
            setGameManager: manager => capturedManager = manager,
            deferConnectTurnManager: () => deferredCalls++);

        controller.InitializeGameManager();

        Assert.AreSame(mapContainer, convertInput);
        Assert.AreSame(logicalMap, initializeInput);
        Assert.AreSame(gameManager, capturedManager);
        Assert.AreEqual(1, deferredCalls);
    }

    [Test]
    public void ConnectTurnManager_Should_Set_And_Subscribe_Then_Initialize_Runtime_Components()
    {
        var turnManager = new TurnManager();
        var gameManager = new GameManager();
        SetTurnManager(gameManager, turnManager);

        TurnManager assignedTurnManager = null;
        int phaseChangedCalls = 0;
        int mapRendererInitCalls = 0;
        int phaseCoordinatorBuildCalls = 0;
        int phaseCoordinatorSetCalls = 0;
        var controller = CreateController(
            getGameManager: () => gameManager,
            setTurnManager: manager => assignedTurnManager = manager,
            onTurnManagerPhaseChanged: (_, _) => phaseChangedCalls++,
            initializeMapRendererCore: (_, _, _, _, _, _, _, _) =>
            {
                mapRendererInitCalls++;
                return new MapRenderer();
            },
            createPhaseTransitionCoordinator: () =>
            {
                phaseCoordinatorBuildCalls++;
                return new PhaseTransitionCoordinator(null, null, null, null, null, null, null, null, null);
            },
            setPhaseTransitionCoordinator: _ => phaseCoordinatorSetCalls++);

        controller.ConnectTurnManager();
        turnManager.SetPhase(GamePhase.Purchase);

        Assert.AreSame(turnManager, assignedTurnManager);
        Assert.AreEqual(1, mapRendererInitCalls);
        Assert.AreEqual(1, phaseCoordinatorBuildCalls);
        Assert.AreEqual(1, phaseCoordinatorSetCalls);
        Assert.AreEqual(1, phaseChangedCalls, "Subscribed phase handler should receive emitted phase change.");
    }

    private static MainRuntimeBootstrapController CreateController(
        System.Func<Node2D> getMapContainer = null,
        System.Func<Node2D, Dictionary<Vector2I, HexTile>> convertVisualMapToGameMap = null,
        System.Func<Dictionary<Vector2I, HexTile>, GameManager> initializeGameManagerCore = null,
        System.Action<GameManager> setGameManager = null,
        System.Action deferConnectTurnManager = null,
        System.Func<GameManager> getGameManager = null,
        System.Action<TurnManager> setTurnManager = null,
        System.Action<int, int> onTurnManagerPhaseChanged = null,
        System.Func<GameManager, TurnManager, Node2D, int, HexGridViewState, System.Action<Vector2I>, System.Action, System.Action, MapRenderer> initializeMapRendererCore = null,
        System.Func<TurnManager> getTurnManager = null,
        System.Func<int> getCurrentPlayerIndex = null,
        System.Func<HexGridViewState> getViewState = null,
        System.Action<Vector2I> onPurchaseTileClicked = null,
        System.Action updateTitleLabel = null,
        System.Action refreshPurchaseUI = null,
        System.Action<MapRenderer> setMapRenderer = null,
        System.Func<PhaseTransitionCoordinator> createPhaseTransitionCoordinator = null,
        System.Action<PhaseTransitionCoordinator> setPhaseTransitionCoordinator = null)
    {
        return new MainRuntimeBootstrapController(
            getMapContainer ?? (() => new Node2D()),
            convertVisualMapToGameMap ?? (_ => BuildRectMap(2, 2)),
            initializeGameManagerCore ?? (_ => new GameManager()),
            setGameManager ?? (_ => { }),
            deferConnectTurnManager ?? (() => { }),
            getGameManager ?? (() => new GameManager()),
            setTurnManager ?? (_ => { }),
            onTurnManagerPhaseChanged ?? ((_, _) => { }),
            initializeMapRendererCore ?? ((_, _, _, _, _, _, _, _) => new MapRenderer()),
            getTurnManager ?? (() => new TurnManager()),
            getCurrentPlayerIndex ?? (() => 0),
            getViewState ?? (() => new HexGridViewState()),
            onPurchaseTileClicked ?? (_ => { }),
            updateTitleLabel ?? (() => { }),
            refreshPurchaseUI ?? (() => { }),
            setMapRenderer ?? (_ => { }),
            createPhaseTransitionCoordinator ?? (() => new PhaseTransitionCoordinator(null, null, null, null, null, null, null, null, null)),
            setPhaseTransitionCoordinator ?? (_ => { }));
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

    private static void SetTurnManager(GameManager gameManager, TurnManager turnManager)
    {
        var field = typeof(GameManager).GetField("<TurnManager>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "Expected GameManager TurnManager backing field.");
        field.SetValue(gameManager, turnManager);
    }
}
