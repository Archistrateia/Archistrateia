using Godot;
using System;
using System.Collections.Generic;

namespace Archistrateia
{
    public interface IMapInteractionController
    {
        Unit GetSelectedUnit();
        void ClearSelection();
        UnitSelectionResult HandleUnitClicked(Player currentPlayer, GamePhase currentPhase, Unit clickedUnit);
        TileInteractionResult HandleTileClicked(
            GamePhase currentPhase,
            Vector2I clickedPosition,
            Dictionary<Vector2I, HexTile> gameMap,
            Func<Unit, Vector2I?> findUnitPosition);
        List<Vector2I> GetValidMovementDestinations(Unit selectedUnit, Vector2I currentPosition, Dictionary<Vector2I, HexTile> gameMap);
    }

    public enum TileInteractionKind
    {
        Ignored,
        PurchaseTileSelected,
        MoveSucceeded,
        DeselectRequired
    }

    public sealed class UnitSelectionResult
    {
        public static readonly UnitSelectionResult Ignored = new(false, null);

        public bool WasSelected { get; }
        public Unit SelectedUnit { get; }

        private UnitSelectionResult(bool wasSelected, Unit selectedUnit)
        {
            WasSelected = wasSelected;
            SelectedUnit = selectedUnit;
        }

        public static UnitSelectionResult CreateSelected(Unit unit)
        {
            return new UnitSelectionResult(true, unit);
        }
    }

    public sealed class TileInteractionResult
    {
        public TileInteractionKind Kind { get; }
        public Unit SelectedUnit { get; }
        public Vector2I NewPosition { get; }
        public string ErrorMessage { get; }

        private TileInteractionResult(TileInteractionKind kind, Unit selectedUnit, Vector2I newPosition, string errorMessage)
        {
            Kind = kind;
            SelectedUnit = selectedUnit;
            NewPosition = newPosition;
            ErrorMessage = errorMessage;
        }

        public static TileInteractionResult CreateIgnored()
        {
            return new TileInteractionResult(TileInteractionKind.Ignored, null, Vector2I.Zero, string.Empty);
        }

        public static TileInteractionResult CreatePurchaseTileSelected(Vector2I tilePosition)
        {
            return new TileInteractionResult(TileInteractionKind.PurchaseTileSelected, null, tilePosition, string.Empty);
        }

        public static TileInteractionResult CreateMoveSucceeded(Unit selectedUnit, Vector2I newPosition)
        {
            return new TileInteractionResult(TileInteractionKind.MoveSucceeded, selectedUnit, newPosition, string.Empty);
        }

        public static TileInteractionResult CreateDeselectRequired(Unit selectedUnit, string errorMessage = "")
        {
            return new TileInteractionResult(TileInteractionKind.DeselectRequired, selectedUnit, Vector2I.Zero, errorMessage ?? string.Empty);
        }
    }

    public sealed class MapInteractionController : IMapInteractionController
    {
        private readonly PlayerInteractionLogic _interactionLogic;
        private readonly MovementCoordinator _movementCoordinator;

        public MapInteractionController()
            : this(new PlayerInteractionLogic(), new MovementCoordinator())
        {
        }

        public MapInteractionController(PlayerInteractionLogic interactionLogic, MovementCoordinator movementCoordinator)
        {
            _interactionLogic = interactionLogic ?? throw new ArgumentNullException(nameof(interactionLogic));
            _movementCoordinator = movementCoordinator ?? throw new ArgumentNullException(nameof(movementCoordinator));
        }

        public Unit GetSelectedUnit()
        {
            return _interactionLogic.GetSelectedUnit();
        }

        public void ClearSelection()
        {
            _interactionLogic.DeselectUnit();
            _movementCoordinator.ClearSelection();
        }

        public UnitSelectionResult HandleUnitClicked(Player currentPlayer, GamePhase currentPhase, Unit clickedUnit)
        {
            if (currentPlayer == null || clickedUnit == null)
            {
                return UnitSelectionResult.Ignored;
            }

            var wasSelected = _interactionLogic.SelectUnit(currentPlayer, clickedUnit, currentPhase);
            if (!wasSelected)
            {
                return UnitSelectionResult.Ignored;
            }

            _movementCoordinator.SelectUnitForMovement(clickedUnit);
            return UnitSelectionResult.CreateSelected(clickedUnit);
        }

        public TileInteractionResult HandleTileClicked(
            GamePhase currentPhase,
            Vector2I clickedPosition,
            Dictionary<Vector2I, HexTile> gameMap,
            Func<Unit, Vector2I?> findUnitPosition)
        {
            if (currentPhase == GamePhase.Purchase)
            {
                return TileInteractionResult.CreatePurchaseTileSelected(clickedPosition);
            }

            if (currentPhase != GamePhase.Move)
            {
                return TileInteractionResult.CreateIgnored();
            }

            var selectedUnit = _interactionLogic.GetSelectedUnit();
            if (selectedUnit == null)
            {
                return TileInteractionResult.CreateIgnored();
            }

            if (selectedUnit.CurrentMovementPoints <= 0)
            {
                return TileInteractionResult.CreateDeselectRequired(selectedUnit);
            }

            var unitPosition = findUnitPosition?.Invoke(selectedUnit);
            if (unitPosition == null || gameMap == null)
            {
                return TileInteractionResult.CreateIgnored();
            }

            var moveResult = _movementCoordinator.TryMoveToDestination(unitPosition.Value, clickedPosition, gameMap);
            if (!moveResult.Success)
            {
                return TileInteractionResult.CreateDeselectRequired(selectedUnit, moveResult.ErrorMessage);
            }

            if (selectedUnit.CurrentMovementPoints <= 0)
            {
                ClearSelection();
            }

            return TileInteractionResult.CreateMoveSucceeded(selectedUnit, moveResult.NewPosition);
        }

        public List<Vector2I> GetValidMovementDestinations(
            Unit selectedUnit,
            Vector2I currentPosition,
            Dictionary<Vector2I, HexTile> gameMap)
        {
            if (selectedUnit == null)
            {
                return new List<Vector2I>();
            }

            _movementCoordinator.SelectUnitForMovement(selectedUnit);
            return _movementCoordinator.GetValidDestinations(currentPosition, gameMap);
        }
    }
}
