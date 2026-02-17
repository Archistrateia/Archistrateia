using System;

namespace Archistrateia
{
    public sealed class PhaseTransitionCoordinator
    {
        private readonly GameManager _gameManager;
        private readonly PurchaseCoordinator _purchaseCoordinator;
        private readonly Action<GamePhase> _applyMapPhase;
        private readonly Action<bool> _setPurchaseUIVisible;
        private readonly Action _updateSelectedPurchaseUnitDetails;
        private readonly Action<string> _setPurchaseStatus;
        private readonly Action _refreshPurchaseUI;
        private readonly Action _switchToNextPlayer;
        private readonly Action _deselectAllUnits;

        public PhaseTransitionCoordinator(
            GameManager gameManager,
            PurchaseCoordinator purchaseCoordinator,
            Action<GamePhase> applyMapPhase,
            Action<bool> setPurchaseUIVisible,
            Action updateSelectedPurchaseUnitDetails,
            Action<string> setPurchaseStatus,
            Action refreshPurchaseUI,
            Action switchToNextPlayer,
            Action deselectAllUnits)
        {
            _gameManager = gameManager;
            _purchaseCoordinator = purchaseCoordinator;
            _applyMapPhase = applyMapPhase;
            _setPurchaseUIVisible = setPurchaseUIVisible;
            _updateSelectedPurchaseUnitDetails = updateSelectedPurchaseUnitDetails;
            _setPurchaseStatus = setPurchaseStatus;
            _refreshPurchaseUI = refreshPurchaseUI;
            _switchToNextPlayer = switchToNextPlayer;
            _deselectAllUnits = deselectAllUnits;
        }

        public void ApplyTransition(GamePhase oldPhase, GamePhase newPhase)
        {
            _applyMapPhase?.Invoke(newPhase);

            switch (newPhase)
            {
                case GamePhase.Earn:
                    _purchaseCoordinator?.CancelPendingPurchase();
                    _setPurchaseUIVisible?.Invoke(false);
                    _gameManager?.ProcessEarnPhase();

                    // Only rotate active player at the end of a full cycle.
                    if (oldPhase == GamePhase.Combat)
                    {
                        _switchToNextPlayer?.Invoke();
                    }

                    _setPurchaseStatus?.Invoke("Earn phase");
                    break;
                case GamePhase.Purchase:
                    _purchaseCoordinator?.CancelPendingPurchase();
                    _setPurchaseUIVisible?.Invoke(true);
                    _updateSelectedPurchaseUnitDetails?.Invoke();
                    _setPurchaseStatus?.Invoke("Choose a unit to buy.");
                    break;
                case GamePhase.Move:
                    _purchaseCoordinator?.CancelPendingPurchase();
                    _setPurchaseUIVisible?.Invoke(false);

                    if (_gameManager?.Players != null)
                    {
                        foreach (var player in _gameManager.Players)
                        {
                            player.ResetUnitMovement();
                        }
                    }

                    _deselectAllUnits?.Invoke();
                    _setPurchaseStatus?.Invoke("Move phase");
                    break;
                case GamePhase.Combat:
                    _purchaseCoordinator?.CancelPendingPurchase();
                    _setPurchaseUIVisible?.Invoke(false);
                    _setPurchaseStatus?.Invoke("Combat phase");
                    break;
            }

            _refreshPurchaseUI?.Invoke();
        }
    }
}
