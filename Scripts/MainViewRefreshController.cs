using Godot;
using System;
using System.Collections.Generic;

namespace Archistrateia
{
    public sealed class MainViewRefreshController
    {
        private readonly Func<float> _getZoomFactor;
        private readonly Func<double?> _getZoomSliderValue;
        private readonly Func<VisualPositionManager> _getPositionManager;
        private readonly Func<Node2D> _getMapContainer;
        private readonly Func<MapRenderer> _getMapRenderer;
        private readonly Func<GameManager> _getGameManager;
        private readonly Func<TileUnitCoordinator> _getTileUnitCoordinator;
        private readonly Action<string> _log;

        public MainViewRefreshController(
            Func<float> getZoomFactor,
            Func<double?> getZoomSliderValue,
            Func<VisualPositionManager> getPositionManager,
            Func<Node2D> getMapContainer,
            Func<MapRenderer> getMapRenderer,
            Func<GameManager> getGameManager,
            Func<TileUnitCoordinator> getTileUnitCoordinator,
            Action<string> log = null)
        {
            _getZoomFactor = getZoomFactor ?? throw new ArgumentNullException(nameof(getZoomFactor));
            _getZoomSliderValue = getZoomSliderValue ?? throw new ArgumentNullException(nameof(getZoomSliderValue));
            _getPositionManager = getPositionManager ?? throw new ArgumentNullException(nameof(getPositionManager));
            _getMapContainer = getMapContainer ?? throw new ArgumentNullException(nameof(getMapContainer));
            _getMapRenderer = getMapRenderer ?? throw new ArgumentNullException(nameof(getMapRenderer));
            _getGameManager = getGameManager ?? throw new ArgumentNullException(nameof(getGameManager));
            _getTileUnitCoordinator = getTileUnitCoordinator ?? throw new ArgumentNullException(nameof(getTileUnitCoordinator));
            _log = log ?? (_ => { });
        }

        public void RegenerateMapWithCurrentZoom()
        {
            _log($"🔍 REGENERATE DEBUG: Before UpdateAllPositions - Zoom: {_getZoomFactor():F2}x, Slider: {_getZoomSliderValue():F2}x");

            _getPositionManager()?.UpdateAllPositions(
                _getMapContainer(),
                _getMapRenderer()?.GetVisualUnits() ?? new List<VisualUnit>(),
                _getGameManager()?.GameMap,
                _getTileUnitCoordinator());

            _log($"🔍 REGENERATE DEBUG: After UpdateAllPositions - Zoom: {_getZoomFactor():F2}x, Slider: {_getZoomSliderValue():F2}x");
        }
    }
}
