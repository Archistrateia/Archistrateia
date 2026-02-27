using Godot;
using System.Collections.Generic;

namespace Archistrateia
{
    public sealed class MapPreviewController
    {
        private readonly Node _host;
        private readonly ModernUIManager _uiManager;
        private readonly VisualPositionManager _positionManager;
        private readonly ViewportController _viewportController;
        private readonly IReadOnlyDictionary<TerrainType, Color> _terrainColors;

        public MapPreviewController(
            Node host,
            ModernUIManager uiManager,
            VisualPositionManager positionManager,
            ViewportController viewportController,
            IReadOnlyDictionary<TerrainType, Color> terrainColors)
        {
            _host = host;
            _uiManager = uiManager;
            _positionManager = positionManager;
            _viewportController = viewportController;
            _terrainColors = terrainColors;
        }

        public Node2D GeneratePreviewMap(Node2D existingMapContainer, MapType mapType)
        {
            existingMapContainer?.QueueFree();

            var currentZoom = HexGridCalculator.ZoomFactor;
            if (Mathf.Abs(currentZoom - 0.0f) < 0.001f)
            {
                HexGridCalculator.SetZoom(1.0f);
            }

            _viewportController?.ResetScroll();

            var mapContainer = new Node2D
            {
                Name = "MapContainer",
                ZIndex = 1,
                Position = Vector2.Zero
            };

            var gameArea = _uiManager?.GetGameArea();
            if (gameArea != null)
            {
                gameArea.AddChild(mapContainer);
            }
            else
            {
                _host.AddChild(mapContainer);
            }

            var gameMap = MapGenerator.GenerateMap(MapConfiguration.MAP_WIDTH, MapConfiguration.MAP_HEIGHT, mapType);
            foreach (var kvp in gameMap)
            {
                var worldPosition = _positionManager.CalculateWorldPosition(kvp.Key);
                var visualTile = new VisualHexTile();
                visualTile.Initialize(kvp.Key, kvp.Value.TerrainType, _terrainColors[kvp.Value.TerrainType], worldPosition);
                mapContainer.AddChild(visualTile);
            }

            GD.Print($"🗺️ Generated map with {gameMap.Count} tiles of type {mapType}");
            return mapContainer;
        }

        public void HideMapGenerationControls()
        {
            if (_uiManager != null)
            {
                var regenerateButton = _uiManager.GetRegenerateMapButton();
                if (regenerateButton != null)
                {
                    regenerateButton.Visible = false;
                    GD.Print("🙈 Hidden regenerate map button");
                }

                var mapTypeSelector = _uiManager.GetMapTypeSelector();
                if (mapTypeSelector != null)
                {
                    mapTypeSelector.Visible = false;
                    GD.Print("🙈 Hidden map type selector");
                }

                GD.Print("✅ Zoom controls remain visible during gameplay");
                return;
            }

            foreach (Node child in _host.GetChildren())
            {
                if (child is not Panel panel || panel.GetChildCount() == 0)
                {
                    continue;
                }

                var container = panel.GetChild(0);
                if (container is not VBoxContainer vbox)
                {
                    continue;
                }

                foreach (Node vboxChild in vbox.GetChildren())
                {
                    if (vboxChild is OptionButton)
                    {
                        panel.Visible = false;
                        GD.Print("🙈 Hidden map generation controls (fallback)");
                        return;
                    }
                }
            }
        }

        public Dictionary<Vector2I, HexTile> ConvertVisualMapToGameMap(Node2D mapContainer)
        {
            var gameMap = new Dictionary<Vector2I, HexTile>();

            if (mapContainer != null)
            {
                foreach (Node child in mapContainer.GetChildren())
                {
                    if (child is VisualHexTile visualTile)
                    {
                        var logicalTile = new HexTile(visualTile.GridPosition, visualTile.TerrainType);
                        gameMap[visualTile.GridPosition] = logicalTile;
                    }
                }
            }

            GD.Print($"🔄 Converted visual map to logical game map with {gameMap.Count} tiles");
            return gameMap;
        }
    }
}
