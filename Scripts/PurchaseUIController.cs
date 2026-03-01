using Godot;
using System;

namespace Archistrateia
{
    public sealed class PurchaseUIController
    {
        private readonly OptionButton _purchaseUnitSelector;
        private readonly Label _purchaseUnitDetailsLabel;
        private readonly Label _purchaseGoldLabel;
        private readonly Label _purchaseStatusLabel;
        private readonly Button _purchaseBuyButton;
        private readonly Button _purchaseCancelButton;
        private readonly PurchaseCoordinator _purchaseCoordinator;
        private readonly SemicircleDeploymentService _deploymentService;
        private readonly Func<GameManager> _getGameManager;
        private readonly Func<int> _getCurrentPlayerIndex;
        private readonly Func<TurnManager> _getTurnManager;
        private readonly Action<bool> _setPurchasePanelVisible;
        private readonly Action _clearPurchasePlacementTiles;
        private readonly Action<System.Collections.Generic.IReadOnlyList<Vector2I>> _showPurchasePlacementTiles;
        private readonly Func<Player, Color> _getPlayerColor;
        private readonly Func<Vector2I, Vector2> _calculateWorldPosition;
        private readonly Action<Unit, Vector2, Color> _createVisualUnit;
        private readonly Action _updateTileOccupationStatus;

        public PurchaseUIController(
            OptionButton purchaseUnitSelector,
            Label purchaseUnitDetailsLabel,
            Label purchaseGoldLabel,
            Label purchaseStatusLabel,
            Button purchaseBuyButton,
            Button purchaseCancelButton,
            PurchaseCoordinator purchaseCoordinator,
            SemicircleDeploymentService deploymentService,
            Func<GameManager> getGameManager,
            Func<int> getCurrentPlayerIndex,
            Func<TurnManager> getTurnManager,
            Action<bool> setPurchasePanelVisible,
            Action clearPurchasePlacementTiles,
            Action<System.Collections.Generic.IReadOnlyList<Vector2I>> showPurchasePlacementTiles,
            Func<Player, Color> getPlayerColor,
            Func<Vector2I, Vector2> calculateWorldPosition,
            Action<Unit, Vector2, Color> createVisualUnit,
            Action updateTileOccupationStatus)
        {
            _purchaseUnitSelector = purchaseUnitSelector;
            _purchaseUnitDetailsLabel = purchaseUnitDetailsLabel;
            _purchaseGoldLabel = purchaseGoldLabel;
            _purchaseStatusLabel = purchaseStatusLabel;
            _purchaseBuyButton = purchaseBuyButton;
            _purchaseCancelButton = purchaseCancelButton;
            _purchaseCoordinator = purchaseCoordinator ?? throw new ArgumentNullException(nameof(purchaseCoordinator));
            _deploymentService = deploymentService ?? throw new ArgumentNullException(nameof(deploymentService));
            _getGameManager = getGameManager ?? throw new ArgumentNullException(nameof(getGameManager));
            _getCurrentPlayerIndex = getCurrentPlayerIndex ?? throw new ArgumentNullException(nameof(getCurrentPlayerIndex));
            _getTurnManager = getTurnManager ?? throw new ArgumentNullException(nameof(getTurnManager));
            _setPurchasePanelVisible = setPurchasePanelVisible ?? (_ => { });
            _clearPurchasePlacementTiles = clearPurchasePlacementTiles ?? (() => { });
            _showPurchasePlacementTiles = showPurchasePlacementTiles ?? (_ => { });
            _getPlayerColor = getPlayerColor ?? (_ => Colors.White);
            _calculateWorldPosition = calculateWorldPosition ?? (_ => Vector2.Zero);
            _createVisualUnit = createVisualUnit ?? ((_, _, _) => { });
            _updateTileOccupationStatus = updateTileOccupationStatus ?? (() => { });
        }

        public void PopulatePurchaseUnitSelector()
        {
            _purchaseUnitSelector?.Clear();
            if (_purchaseUnitSelector == null)
            {
                return;
            }

            foreach (var blueprint in UnitCatalog.GetAll())
            {
                _purchaseUnitSelector.AddItem(blueprint.DisplayName);
            }

            _purchaseUnitSelector.Selected = 0;
            UpdateSelectedPurchaseUnitDetails();
        }

        public void OnPurchaseUnitSelected(long index)
        {
            _ = index;
            UpdateSelectedPurchaseUnitDetails();
            RefreshPurchaseUI();
        }

        public void UpdateSelectedPurchaseUnitDetails()
        {
            if (_purchaseUnitDetailsLabel == null)
            {
                return;
            }

            var blueprint = GetSelectedBlueprint();
            if (blueprint == null)
            {
                _purchaseUnitDetailsLabel.Text = "Select a unit";
                return;
            }

            int recommendedCost = UnitValuationPolicy.GetRecommendedCost(blueprint.Attack, blueprint.Defense, blueprint.MovementPoints);
            _purchaseUnitDetailsLabel.Text =
                $"ATK {blueprint.Attack} | DEF {blueprint.Defense} | MP {blueprint.MovementPoints}\n" +
                $"Cost {blueprint.Cost} | Value {blueprint.ValueScore:F1} (rec {recommendedCost})";
        }

        public void SetPurchaseStatus(string message)
        {
            if (_purchaseStatusLabel != null)
            {
                _purchaseStatusLabel.Text = message;
            }
        }

        public void RefreshPurchaseUI()
        {
            var currentPlayer = GetCurrentPlayer();
            if (_purchaseGoldLabel != null)
            {
                _purchaseGoldLabel.Text = $"Gold: {currentPlayer?.Gold ?? 0}";
            }

            var selectedBlueprint = GetSelectedBlueprint();
            bool canAfford = currentPlayer != null && selectedBlueprint != null && currentPlayer.Gold >= selectedBlueprint.Cost;
            bool isPurchasePhase = _getTurnManager()?.CurrentPhase == GamePhase.Purchase;

            bool hasValidTiles = false;
            var gameManager = _getGameManager();
            int currentPlayerIndex = _getCurrentPlayerIndex();
            if (gameManager != null && isPurchasePhase && currentPlayer != null)
            {
                hasValidTiles = _deploymentService.GetDeployableTilesForPlayer(
                    gameManager.GameMap,
                    currentPlayerIndex,
                    gameManager.Players.Count
                ).Count > 0;
            }

            if (_purchaseBuyButton != null)
            {
                _purchaseBuyButton.Disabled = !isPurchasePhase || !canAfford || !hasValidTiles;
            }

            if (_purchaseCancelButton != null)
            {
                _purchaseCancelButton.Disabled = !_purchaseCoordinator.HasPendingPurchase;
            }

            if (isPurchasePhase && !hasValidTiles && !_purchaseCoordinator.HasPendingPurchase)
            {
                SetPurchaseStatus("No valid deployment tiles available.");
            }
        }

        public void SetPurchaseUIVisible(bool visible)
        {
            _setPurchasePanelVisible(visible);
            if (!visible)
            {
                _clearPurchasePlacementTiles();
            }
        }

        public void OnPurchaseBuyPressed()
        {
            var turnManager = _getTurnManager();
            var gameManager = _getGameManager();
            if (turnManager == null || gameManager == null || turnManager.CurrentPhase != GamePhase.Purchase)
            {
                SetPurchaseStatus("Purchase is only available during the Purchase phase.");
                return;
            }

            var currentPlayer = GetCurrentPlayer();
            var selectedBlueprint = GetSelectedBlueprint();
            if (currentPlayer == null || selectedBlueprint == null)
            {
                SetPurchaseStatus("Unable to start purchase.");
                return;
            }

            int currentPlayerIndex = _getCurrentPlayerIndex();
            var selectionResult = _purchaseCoordinator.BeginSelection(
                currentPlayer,
                currentPlayerIndex,
                selectedBlueprint.UnitType,
                gameManager.GameMap,
                gameManager.Players.Count
            );

            if (!selectionResult.Success)
            {
                _clearPurchasePlacementTiles();
                SetPurchaseStatus(selectionResult.ErrorMessage);
                RefreshPurchaseUI();
                return;
            }

            _showPurchasePlacementTiles(selectionResult.ValidPlacementTiles);
            SetPurchaseStatus("Select a highlighted deployment tile to place your unit.");
            RefreshPurchaseUI();
        }

        public void OnPurchaseCancelPressed()
        {
            _purchaseCoordinator.CancelPendingPurchase();
            _clearPurchasePlacementTiles();
            SetPurchaseStatus("Purchase cancelled.");
            RefreshPurchaseUI();
        }

        public void OnPurchaseTileClicked(Vector2I tilePosition)
        {
            var turnManager = _getTurnManager();
            var gameManager = _getGameManager();
            if (turnManager == null || gameManager == null || turnManager.CurrentPhase != GamePhase.Purchase)
            {
                return;
            }

            if (!_purchaseCoordinator.HasPendingPurchase)
            {
                SetPurchaseStatus("Choose a unit and press Buy + Place first.");
                return;
            }

            var currentPlayer = GetCurrentPlayer();
            if (currentPlayer == null)
            {
                SetPurchaseStatus("No active player.");
                return;
            }

            int currentPlayerIndex = _getCurrentPlayerIndex();
            var placeResult = _purchaseCoordinator.TryPlacePendingUnit(
                currentPlayer,
                currentPlayerIndex,
                tilePosition,
                gameManager.GameMap
            );

            if (!placeResult.Success)
            {
                SetPurchaseStatus(placeResult.ErrorMessage);
                RefreshPurchaseUI();
                return;
            }

            var playerColor = _getPlayerColor(currentPlayer);
            var worldPosition = _calculateWorldPosition(tilePosition);
            _createVisualUnit(placeResult.PurchasedUnit, worldPosition, playerColor);
            _updateTileOccupationStatus();
            _clearPurchasePlacementTiles();

            SetPurchaseStatus($"Placed {placeResult.PurchasedUnit.Name}.");
            RefreshPurchaseUI();
        }

        private Player GetCurrentPlayer()
        {
            var gameManager = _getGameManager();
            if (gameManager == null || gameManager.Players.Count == 0)
            {
                return null;
            }

            int currentPlayerIndex = _getCurrentPlayerIndex();
            if (currentPlayerIndex < 0 || currentPlayerIndex >= gameManager.Players.Count)
            {
                return null;
            }

            return gameManager.Players[currentPlayerIndex];
        }

        private UnitBlueprint GetSelectedBlueprint()
        {
            if (_purchaseUnitSelector == null || _purchaseUnitSelector.Selected < 0)
            {
                return null;
            }

            var selectedIndex = (int)_purchaseUnitSelector.Selected;
            var all = UnitCatalog.GetAll();
            if (selectedIndex >= all.Count)
            {
                return null;
            }

            return all[selectedIndex];
        }
    }
}
