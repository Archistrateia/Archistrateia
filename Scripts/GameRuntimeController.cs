using Godot;
using System;
using System.Collections.Generic;

namespace Archistrateia
{
    public sealed class GameRuntimeController
    {
        private readonly Node _host;
        private readonly TileUnitCoordinator _tileUnitCoordinator;
        private readonly VisualPositionManager _positionManager;

        public GameRuntimeController(
            Node host,
            TileUnitCoordinator tileUnitCoordinator,
            VisualPositionManager positionManager)
        {
            _host = host;
            _tileUnitCoordinator = tileUnitCoordinator;
            _positionManager = positionManager;
        }

        public GameManager InitializeGameManager(Dictionary<Vector2I, HexTile> logicalGameMap)
        {
            var gameManager = new GameManager();
            _host.AddChild(gameManager);
            gameManager.SetGameMap(logicalGameMap);
            gameManager.InitializeGame();
            return gameManager;
        }

        public MapRenderer InitializeMapRenderer(
            GameManager gameManager,
            TurnManager turnManager,
            Node2D mapContainer,
            int currentPlayerIndex,
            Action<Vector2I> onPurchaseTileClicked,
            Action updateTitleLabel,
            Action refreshPurchaseUI)
        {
            var mapRenderer = new MapRenderer();
            mapRenderer.Name = "MapRenderer";
            _host.AddChild(mapRenderer);
            mapRenderer.Initialize(gameManager, _tileUnitCoordinator, mapContainer);
            if (onPurchaseTileClicked != null)
            {
                mapRenderer.PurchaseTileClicked += tilePosition => onPurchaseTileClicked(tilePosition);
            }

            if (gameManager.Players.Count > currentPlayerIndex)
            {
                mapRenderer.SetCurrentPlayer(gameManager.Players[currentPlayerIndex]);
            }

            mapRenderer.SetCurrentPhase(turnManager.CurrentPhase);
            mapRenderer.UpdateTileOccupationStatus();
            updateTitleLabel?.Invoke();

            RegisterVisualTiles(mapContainer, mapRenderer);
            CreateVisualUnitsForPlayers(gameManager, mapRenderer, mapContainer);

            mapRenderer.UpdateTileOccupationStatus();
            refreshPurchaseUI?.Invoke();
            return mapRenderer;
        }

        private void RegisterVisualTiles(Node2D mapContainer, MapRenderer mapRenderer)
        {
            if (mapContainer == null || mapRenderer == null)
            {
                return;
            }

            foreach (Node child in mapContainer.GetChildren())
            {
                if (child is VisualHexTile visualTile)
                {
                    mapRenderer.AddVisualTile(visualTile);
                }
            }
        }

        private void CreateVisualUnitsForPlayers(GameManager gameManager, MapRenderer mapRenderer, Node2D mapContainer)
        {
            _tileUnitCoordinator?.CreateVisualUnitsForPlayers(
                gameManager?.Players,
                gameManager?.GameMap,
                mapRenderer,
                _positionManager,
                mapContainer
            );
        }
    }
}
