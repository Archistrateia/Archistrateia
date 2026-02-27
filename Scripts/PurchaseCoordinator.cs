using Godot;
using System.Collections.Generic;

namespace Archistrateia
{
    public sealed class PurchaseResult
    {
        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; }
        public Unit PurchasedUnit { get; private set; }
        public Vector2I? PlacementPosition { get; private set; }
        public IReadOnlyList<Vector2I> ValidPlacementTiles { get; private set; }

        public static PurchaseResult CreateFailure(string message)
        {
            return new PurchaseResult
            {
                Success = false,
                ErrorMessage = message,
                ValidPlacementTiles = new List<Vector2I>()
            };
        }

        public static PurchaseResult CreateSelectionReady(IReadOnlyList<Vector2I> validPlacementTiles)
        {
            return new PurchaseResult
            {
                Success = true,
                ValidPlacementTiles = validPlacementTiles
            };
        }

        public static PurchaseResult CreatePlacementSuccess(Unit purchasedUnit, Vector2I placementPosition)
        {
            return new PurchaseResult
            {
                Success = true,
                PurchasedUnit = purchasedUnit,
                PlacementPosition = placementPosition,
                ValidPlacementTiles = new List<Vector2I>()
            };
        }
    }

    public sealed class PurchaseCoordinator
    {
        private readonly SemicircleDeploymentService _deploymentService = new();
        private UnitBlueprint _pendingBlueprint;
        private int _pendingPlayerIndex = -1;
        private List<Vector2I> _validPlacementTiles = new();

        public bool HasPendingPurchase => _pendingBlueprint != null;

        public UnitBlueprint GetPendingBlueprint()
        {
            return _pendingBlueprint;
        }

        public IReadOnlyList<Vector2I> GetValidPlacementTiles()
        {
            return _validPlacementTiles;
        }

        public PurchaseResult BeginSelection(
            Player player,
            int playerIndex,
            UnitType unitType,
            Dictionary<Vector2I, HexTile> gameMap,
            int playerCount)
        {
            if (player == null)
            {
                return PurchaseResult.CreateFailure("No active player");
            }

            if (!UnitCatalog.TryGet(unitType, out var blueprint))
            {
                return PurchaseResult.CreateFailure("Unknown unit type");
            }

            if (player.Gold < blueprint.Cost)
            {
                return PurchaseResult.CreateFailure("Insufficient gold");
            }

            var deployableTiles = _deploymentService.GetDeployableTilesForPlayer(gameMap, playerIndex, playerCount);
            if (deployableTiles.Count == 0)
            {
                return PurchaseResult.CreateFailure("No valid deployment tiles available");
            }

            _pendingBlueprint = blueprint;
            _pendingPlayerIndex = playerIndex;
            _validPlacementTiles = deployableTiles;

            return PurchaseResult.CreateSelectionReady(deployableTiles);
        }

        public PurchaseResult TryPlacePendingUnit(
            Player player,
            int playerIndex,
            Vector2I tilePosition,
            Dictionary<Vector2I, HexTile> gameMap)
        {
            if (!HasPendingPurchase)
            {
                return PurchaseResult.CreateFailure("No pending purchase");
            }

            if (_pendingPlayerIndex != playerIndex)
            {
                return PurchaseResult.CreateFailure("Pending purchase belongs to another player");
            }

            if (!gameMap.TryGetValue(tilePosition, out var tile))
            {
                return PurchaseResult.CreateFailure("Invalid tile position");
            }

            if (!_validPlacementTiles.Contains(tilePosition))
            {
                return PurchaseResult.CreateFailure("Tile is not in your deployment zone");
            }

            if (tile.IsOccupied())
            {
                return PurchaseResult.CreateFailure("Tile is occupied");
            }

            if (player.Gold < _pendingBlueprint.Cost)
            {
                return PurchaseResult.CreateFailure("Insufficient gold");
            }

            var unit = _pendingBlueprint.CreateUnit();
            player.SpendGold(_pendingBlueprint.Cost);
            player.AddUnit(unit);
            tile.PlaceUnit(unit);

            // Clear pending state after successful placement.
            _pendingBlueprint = null;
            _pendingPlayerIndex = -1;
            _validPlacementTiles = new List<Vector2I>();

            return PurchaseResult.CreatePlacementSuccess(unit, tilePosition);
        }

        public void CancelPendingPurchase()
        {
            _pendingBlueprint = null;
            _pendingPlayerIndex = -1;
            _validPlacementTiles = new List<Vector2I>();
        }
    }
}
