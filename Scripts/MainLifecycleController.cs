using Godot;
using System;

namespace Archistrateia
{
    public sealed class MainLifecycleController
    {
        private readonly Func<TurnManager> _getTurnManager;
        private readonly Func<GameManager> _getGameManager;
        private readonly Func<int> _getCurrentPlayerIndex;
        private readonly Action<int> _setCurrentPlayerIndex;
        private readonly Action<Player> _setCurrentPlayerOnMapRenderer;
        private readonly Action<GamePhase, GamePhase> _applyPhaseTransition;
        private readonly Action _refreshPurchaseUI;
        private readonly Action<string, string, int> _updateTopBarPlayerInfo;
        private readonly Action<string> _setTitleText;

        public MainLifecycleController(
            Func<TurnManager> getTurnManager,
            Func<GameManager> getGameManager,
            Func<int> getCurrentPlayerIndex,
            Action<int> setCurrentPlayerIndex,
            Action<Player> setCurrentPlayerOnMapRenderer,
            Action<GamePhase, GamePhase> applyPhaseTransition,
            Action refreshPurchaseUI,
            Action<string, string, int> updateTopBarPlayerInfo,
            Action<string> setTitleText)
        {
            _getTurnManager = getTurnManager ?? throw new ArgumentNullException(nameof(getTurnManager));
            _getGameManager = getGameManager ?? throw new ArgumentNullException(nameof(getGameManager));
            _getCurrentPlayerIndex = getCurrentPlayerIndex ?? throw new ArgumentNullException(nameof(getCurrentPlayerIndex));
            _setCurrentPlayerIndex = setCurrentPlayerIndex ?? throw new ArgumentNullException(nameof(setCurrentPlayerIndex));
            _setCurrentPlayerOnMapRenderer = setCurrentPlayerOnMapRenderer ?? (_ => { });
            _applyPhaseTransition = applyPhaseTransition ?? ((_, _) => { });
            _refreshPurchaseUI = refreshPurchaseUI ?? (() => { });
            _updateTopBarPlayerInfo = updateTopBarPlayerInfo ?? ((_, _, _) => { });
            _setTitleText = setTitleText ?? (_ => { });
        }

        public void AdvancePhaseWithSideEffects()
        {
            var turnManager = _getTurnManager();
            if (turnManager == null)
            {
                GD.PrintErr("❌ TurnManager is null! Cannot advance phase.");
                return;
            }

            GD.Print($"📋 Current phase before advance: {turnManager.CurrentPhase}");

            try
            {
                turnManager.AdvancePhase();
                GD.Print($"📋 New phase after advance: {turnManager.CurrentPhase}");
                GD.Print("✅ Phase advance completed successfully");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"❌ Error advancing phase: {ex.Message}");
                GD.PrintErr($"Stack trace: {ex.StackTrace}");
            }
        }

        public void HandleTurnManagerPhaseChanged(int oldPhaseValue, int newPhaseValue)
        {
            var oldPhase = (GamePhase)oldPhaseValue;
            var newPhase = (GamePhase)newPhaseValue;
            GD.Print($"🔄 Main.OnTurnManagerPhaseChanged: {oldPhase} → {newPhase}");

            _applyPhaseTransition(oldPhase, newPhase);
            UpdateTitleLabel();
        }

        public void SwitchToNextPlayer()
        {
            var gameManager = _getGameManager();
            if (gameManager?.Players == null || gameManager.Players.Count == 0)
            {
                return;
            }

            int nextPlayerIndex = (_getCurrentPlayerIndex() + 1) % gameManager.Players.Count;
            _setCurrentPlayerIndex(nextPlayerIndex);

            var currentPlayer = gameManager.Players[nextPlayerIndex];
            GD.Print($"Switched to player: {currentPlayer.Name}");
            _setCurrentPlayerOnMapRenderer(currentPlayer);
            _refreshPurchaseUI();
        }

        public void UpdateTitleLabel()
        {
            var turnManager = _getTurnManager();
            if (turnManager == null)
            {
                return;
            }

            var currentPlayerName = "Unknown";
            var gameManager = _getGameManager();
            int currentPlayerIndex = _getCurrentPlayerIndex();
            if (gameManager?.Players?.Count > 0 && currentPlayerIndex >= 0 && currentPlayerIndex < gameManager.Players.Count)
            {
                currentPlayerName = gameManager.Players[currentPlayerIndex].Name;
            }

            _updateTopBarPlayerInfo(currentPlayerName, turnManager.CurrentPhase.ToString(), turnManager.CurrentTurn);
            _setTitleText($"Turn {turnManager.CurrentTurn} - {turnManager.CurrentPhase}");
        }
    }
}
