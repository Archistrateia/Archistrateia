using Godot;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using Archistrateia;

[TestFixture]
public class MapRendererInteractionBoundaryTest
{
    [Test]
    public void MapRenderer_Should_Depends_On_MapInteractionController_Instead_Of_Owning_Gameplay_Selection_State()
    {
        var rendererType = typeof(MapRenderer);

        Assert.IsNotNull(
            rendererType.GetField("_interactionController", BindingFlags.NonPublic | BindingFlags.Instance),
            "MapRenderer should delegate gameplay click decisions through a dedicated interaction controller.");
        Assert.IsNull(
            rendererType.GetField("_interactionLogic", BindingFlags.NonPublic | BindingFlags.Instance),
            "MapRenderer should no longer own PlayerInteractionLogic directly.");
        Assert.IsNull(
            rendererType.GetField("_movementCoordinator", BindingFlags.NonPublic | BindingFlags.Instance),
            "MapRenderer should no longer own MovementCoordinator directly.");
    }

    [Test]
    public void OnUnitClicked_Should_Delegate_To_Interaction_Controller()
    {
        var renderer = new MapRenderer();
        var fakeController = new FakeMapInteractionController();
        var player = new Player("Pharaoh", 100);
        var unit = new Nakhtu();
        player.AddUnit(unit);
        var visualUnit = new VisualUnit();
        visualUnit.Initialize(unit, Vector2.Zero, Colors.Red);

        SetInteractionController(renderer, fakeController);
        renderer.SetCurrentPlayer(player);
        renderer.SetCurrentPhase(GamePhase.Move);

        renderer.OnUnitClicked(visualUnit);

        Assert.AreSame(player, fakeController.LastPlayer);
        Assert.AreEqual(GamePhase.Move, fakeController.LastPhase);
        Assert.AreSame(unit, fakeController.LastUnitClicked);
        Assert.AreEqual(1, fakeController.UnitClickCalls);
    }

    [Test]
    public void OnTileClicked_Should_Emit_PurchaseTileClicked_When_Controller_Returns_Purchase_Result()
    {
        var renderer = new MapRenderer();
        var fakeController = new FakeMapInteractionController
        {
            NextTileResult = TileInteractionResult.CreatePurchaseTileSelected(new Vector2I(2, 3))
        };
        var clickedTile = new VisualHexTile();
        clickedTile.Initialize(new Vector2I(2, 3), TerrainType.Desert, Colors.Beige, Vector2.Zero);
        Vector2I? signaledPosition = null;

        SetInteractionController(renderer, fakeController);
        renderer.SetCurrentPhase(GamePhase.Purchase);
        renderer.PurchaseTileClicked += tilePosition => signaledPosition = tilePosition;

        InvokeOnTileClicked(renderer, clickedTile);

        Assert.AreEqual(new Vector2I(2, 3), signaledPosition);
        Assert.AreEqual(1, fakeController.TileClickCalls);
        Assert.AreEqual(GamePhase.Purchase, fakeController.LastTilePhase);
        Assert.AreEqual(new Vector2I(2, 3), fakeController.LastTilePosition);
    }

    private static void SetInteractionController(MapRenderer renderer, IMapInteractionController controller)
    {
        var field = typeof(MapRenderer).GetField("_interactionController", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "Expected MapRenderer interaction controller field to exist.");
        field.SetValue(renderer, controller);
    }

    private static void InvokeOnTileClicked(MapRenderer renderer, VisualHexTile tile)
    {
        var method = typeof(MapRenderer).GetMethod("OnTileClicked", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, "Expected MapRenderer tile click handler to exist.");
        method.Invoke(renderer, new object[] { tile });
    }

    private sealed class FakeMapInteractionController : IMapInteractionController
    {
        public int UnitClickCalls { get; private set; }
        public int TileClickCalls { get; private set; }
        public Player LastPlayer { get; private set; }
        public GamePhase LastPhase { get; private set; }
        public Unit LastUnitClicked { get; private set; }
        public GamePhase LastTilePhase { get; private set; }
        public Vector2I LastTilePosition { get; private set; }
        public TileInteractionResult NextTileResult { get; set; } = TileInteractionResult.CreateIgnored();

        public void ClearSelection()
        {
        }

        public Unit GetSelectedUnit()
        {
            return null;
        }

        public UnitSelectionResult HandleUnitClicked(Player currentPlayer, GamePhase currentPhase, Unit clickedUnit)
        {
            UnitClickCalls++;
            LastPlayer = currentPlayer;
            LastPhase = currentPhase;
            LastUnitClicked = clickedUnit;
            return UnitSelectionResult.Ignored;
        }

        public TileInteractionResult HandleTileClicked(
            GamePhase currentPhase,
            Vector2I clickedPosition,
            Dictionary<Vector2I, HexTile> gameMap,
            System.Func<Unit, Vector2I?> findUnitPosition)
        {
            TileClickCalls++;
            LastTilePhase = currentPhase;
            LastTilePosition = clickedPosition;
            return NextTileResult;
        }

        public List<Vector2I> GetValidMovementDestinations(Unit selectedUnit, Vector2I currentPosition, Dictionary<Vector2I, HexTile> gameMap)
        {
            return new List<Vector2I>();
        }
    }
}
