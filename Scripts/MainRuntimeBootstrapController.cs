using Godot;
using System;
using System.Collections.Generic;

namespace Archistrateia
{
    public sealed class MainRuntimeBootstrapController
    {
        private readonly Func<Node2D> _getMapContainer;
        private readonly Func<Node2D, Dictionary<Vector2I, HexTile>> _convertVisualMapToGameMap;
        private readonly Func<Dictionary<Vector2I, HexTile>, GameManager> _initializeGameManagerCore;
        private readonly Action<GameManager> _setGameManager;
        private readonly Action _deferConnectTurnManager;

        private readonly Func<GameManager> _getGameManager;
        private readonly Action<TurnManager> _setTurnManager;
        private readonly Action<int, int> _onTurnManagerPhaseChanged;

        private readonly Func<GameManager, TurnManager, Node2D, int, HexGridViewState, Action<Vector2I>, Action, Action, MapRenderer> _initializeMapRendererCore;
        private readonly Func<TurnManager> _getTurnManager;
        private readonly Func<int> _getCurrentPlayerIndex;
        private readonly Func<HexGridViewState> _getViewState;
        private readonly Action<Vector2I> _onPurchaseTileClicked;
        private readonly Action _updateTitleLabel;
        private readonly Action _refreshPurchaseUI;
        private readonly Action<MapRenderer> _setMapRenderer;

        private readonly Func<PhaseTransitionCoordinator> _createPhaseTransitionCoordinator;
        private readonly Action<PhaseTransitionCoordinator> _setPhaseTransitionCoordinator;

        public MainRuntimeBootstrapController(
            Func<Node2D> getMapContainer,
            Func<Node2D, Dictionary<Vector2I, HexTile>> convertVisualMapToGameMap,
            Func<Dictionary<Vector2I, HexTile>, GameManager> initializeGameManagerCore,
            Action<GameManager> setGameManager,
            Action deferConnectTurnManager,
            Func<GameManager> getGameManager,
            Action<TurnManager> setTurnManager,
            Action<int, int> onTurnManagerPhaseChanged,
            Func<GameManager, TurnManager, Node2D, int, HexGridViewState, Action<Vector2I>, Action, Action, MapRenderer> initializeMapRendererCore,
            Func<TurnManager> getTurnManager,
            Func<int> getCurrentPlayerIndex,
            Func<HexGridViewState> getViewState,
            Action<Vector2I> onPurchaseTileClicked,
            Action updateTitleLabel,
            Action refreshPurchaseUI,
            Action<MapRenderer> setMapRenderer,
            Func<PhaseTransitionCoordinator> createPhaseTransitionCoordinator,
            Action<PhaseTransitionCoordinator> setPhaseTransitionCoordinator)
        {
            _getMapContainer = getMapContainer ?? throw new ArgumentNullException(nameof(getMapContainer));
            _convertVisualMapToGameMap = convertVisualMapToGameMap ?? throw new ArgumentNullException(nameof(convertVisualMapToGameMap));
            _initializeGameManagerCore = initializeGameManagerCore ?? throw new ArgumentNullException(nameof(initializeGameManagerCore));
            _setGameManager = setGameManager ?? throw new ArgumentNullException(nameof(setGameManager));
            _deferConnectTurnManager = deferConnectTurnManager ?? throw new ArgumentNullException(nameof(deferConnectTurnManager));
            _getGameManager = getGameManager ?? throw new ArgumentNullException(nameof(getGameManager));
            _setTurnManager = setTurnManager ?? throw new ArgumentNullException(nameof(setTurnManager));
            _onTurnManagerPhaseChanged = onTurnManagerPhaseChanged ?? throw new ArgumentNullException(nameof(onTurnManagerPhaseChanged));
            _initializeMapRendererCore = initializeMapRendererCore ?? throw new ArgumentNullException(nameof(initializeMapRendererCore));
            _getTurnManager = getTurnManager ?? throw new ArgumentNullException(nameof(getTurnManager));
            _getCurrentPlayerIndex = getCurrentPlayerIndex ?? throw new ArgumentNullException(nameof(getCurrentPlayerIndex));
            _getViewState = getViewState ?? throw new ArgumentNullException(nameof(getViewState));
            _onPurchaseTileClicked = onPurchaseTileClicked ?? throw new ArgumentNullException(nameof(onPurchaseTileClicked));
            _updateTitleLabel = updateTitleLabel ?? throw new ArgumentNullException(nameof(updateTitleLabel));
            _refreshPurchaseUI = refreshPurchaseUI ?? throw new ArgumentNullException(nameof(refreshPurchaseUI));
            _setMapRenderer = setMapRenderer ?? throw new ArgumentNullException(nameof(setMapRenderer));
            _createPhaseTransitionCoordinator = createPhaseTransitionCoordinator ?? throw new ArgumentNullException(nameof(createPhaseTransitionCoordinator));
            _setPhaseTransitionCoordinator = setPhaseTransitionCoordinator ?? throw new ArgumentNullException(nameof(setPhaseTransitionCoordinator));
        }

        public void InitializeGameManager()
        {
            var logicalGameMap = _convertVisualMapToGameMap(_getMapContainer());
            var gameManager = _initializeGameManagerCore(logicalGameMap);
            _setGameManager(gameManager);
            _deferConnectTurnManager();
        }

        public void ConnectTurnManager()
        {
            var turnManager = _getGameManager()?.TurnManager;
            if (turnManager == null)
            {
                return;
            }

            _setTurnManager(turnManager);
            turnManager.PhaseChanged += (oldPhase, newPhase) => _onTurnManagerPhaseChanged(oldPhase, newPhase);
            InitializeMapRenderer();
            InitializePhaseTransitionCoordinator();
        }

        public void InitializeMapRenderer()
        {
            var mapRenderer = _initializeMapRendererCore(
                _getGameManager(),
                _getTurnManager(),
                _getMapContainer(),
                _getCurrentPlayerIndex(),
                _getViewState(),
                _onPurchaseTileClicked,
                _updateTitleLabel,
                _refreshPurchaseUI);
            _setMapRenderer(mapRenderer);
        }

        public void InitializePhaseTransitionCoordinator()
        {
            _setPhaseTransitionCoordinator(_createPhaseTransitionCoordinator());
        }
    }
}
