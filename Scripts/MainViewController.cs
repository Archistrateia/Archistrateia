using Godot;
using System;
using System.Collections.Generic;

namespace Archistrateia
{
    public sealed class MainViewController
    {
        private readonly VisualPositionManager _positionManager;
        private readonly TileUnitCoordinator _tileUnitCoordinator;
        private readonly HexGridViewState _viewState;
        private readonly HSlider _zoomSlider;
        private readonly Label _zoomLabel;
        private readonly Func<Vector2> _getGameAreaSize;
        private readonly Func<Vector2> _getViewportSize;
        private readonly Action<Vector2> _updateDebugButtonPosition;
        private int _viewChangedDebugCounter;
        private int _sliderDebugCounter;

        public MainViewController(
            VisualPositionManager positionManager,
            TileUnitCoordinator tileUnitCoordinator,
            HexGridViewState viewState,
            HSlider zoomSlider,
            Label zoomLabel,
            Func<Vector2> getGameAreaSize,
            Func<Vector2> getViewportSize,
            Action<Vector2> updateDebugButtonPosition)
        {
            _positionManager = positionManager ?? throw new ArgumentNullException(nameof(positionManager));
            _tileUnitCoordinator = tileUnitCoordinator ?? throw new ArgumentNullException(nameof(tileUnitCoordinator));
            _viewState = viewState ?? throw new ArgumentNullException(nameof(viewState));
            _zoomSlider = zoomSlider;
            _zoomLabel = zoomLabel;
            _getGameAreaSize = getGameAreaSize ?? throw new ArgumentNullException(nameof(getGameAreaSize));
            _getViewportSize = getViewportSize ?? throw new ArgumentNullException(nameof(getViewportSize));
            _updateDebugButtonPosition = updateDebugButtonPosition ?? (_ => { });
        }

        public void HandleViewChanged(Node2D mapContainer, MapRenderer mapRenderer, Dictionary<Vector2I, HexTile> gameMap)
        {
            _viewChangedDebugCounter++;
            if (_viewChangedDebugCounter % 60 == 0)
            {
                GD.Print($"🔍 VIEW CHANGED (Sample {_viewChangedDebugCounter}): Current zoom = {_viewState.ZoomFactor:F2}x, Slider = {_zoomSlider?.Value:F2}x");
            }

            _positionManager.UpdateGameAreaSize(_getGameAreaSize());

            if (mapContainer != null)
            {
                var visualUnits = mapRenderer?.GetVisualUnits() ?? new List<VisualUnit>();
                _positionManager.UpdateAllPositions(mapContainer, visualUnits, gameMap, _tileUnitCoordinator);
            }

            if (_zoomSlider != null)
            {
                var currentSliderValue = (float)_zoomSlider.Value;
                var currentZoom = _viewState.ZoomFactor;
                if (Mathf.Abs(currentSliderValue - currentZoom) > 0.001f)
                {
                    _sliderDebugCounter++;
                    if (_sliderDebugCounter % 60 == 0)
                    {
                        GD.Print($"🔍 VIEW CHANGED: Setting slider from {currentSliderValue:F2}x to {currentZoom:F2}x (Sample {_sliderDebugCounter})");
                    }
                    _zoomSlider.Value = currentZoom;
                }
                else
                {
                    _sliderDebugCounter++;
                    if (_sliderDebugCounter % 60 == 0)
                    {
                        GD.Print($"🔍 VIEW CHANGED: Slider already matches zoom ({currentZoom:F2}x) (Sample {_sliderDebugCounter})");
                    }
                }

                UpdateZoomLabel();
            }
        }

        public void UpdateZoomLabel()
        {
            if (_zoomLabel != null)
            {
                _zoomLabel.Text = $"Zoom: {_viewState.ZoomFactor:F1}x";
            }
        }

        public void UpdateUIPositions()
        {
            GD.Print($"🔍 UPDATE UI POSITIONS: Called with zoom {_viewState.ZoomFactor:F2}x, slider {_zoomSlider?.Value:F2}x");
            _updateDebugButtonPosition(_getViewportSize());
        }
    }
}
