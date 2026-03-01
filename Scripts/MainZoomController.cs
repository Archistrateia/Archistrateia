using Godot;
using System;

namespace Archistrateia
{
    public sealed class MainZoomController
    {
        private readonly Func<HSlider> _getZoomSlider;
        private readonly Action<float> _setViewportZoom;
        private readonly Action _updateTitleLabel;
        private readonly Action _updateUIPositions;
        private readonly Action<string> _log;

        public MainZoomController(
            Func<HSlider> getZoomSlider,
            Action<float> setViewportZoom,
            Action updateTitleLabel,
            Action updateUIPositions,
            Action<string> log = null)
        {
            _getZoomSlider = getZoomSlider ?? throw new ArgumentNullException(nameof(getZoomSlider));
            _setViewportZoom = setViewportZoom ?? throw new ArgumentNullException(nameof(setViewportZoom));
            _updateTitleLabel = updateTitleLabel ?? throw new ArgumentNullException(nameof(updateTitleLabel));
            _updateUIPositions = updateUIPositions ?? throw new ArgumentNullException(nameof(updateUIPositions));
            _log = log ?? (_ => { });
        }

        public void OnZoomSliderChanged(double value)
        {
            _log($"🔍 SLIDER CHANGED: {value:F2}x");
            _setViewportZoom((float)value);
            _updateTitleLabel();
            _updateUIPositions();
        }

        public void OnZoomSliderInput(InputEvent @event, Action<double> applyZoomChange)
        {
            _log($"Zoom slider received input event: {@event.GetType().Name}");

            var zoomSlider = _getZoomSlider();
            if (zoomSlider == null)
            {
                return;
            }

            if (@event is not InputEventMouseButton mouseEvent || !mouseEvent.Pressed)
            {
                return;
            }

            _log($"Zoom slider mouse button pressed: {mouseEvent.ButtonIndex}");
            var localPos = zoomSlider.GetLocalMousePosition();
            var sliderWidth = zoomSlider.Size.X;
            if (sliderWidth <= 0.001f)
            {
                return;
            }

            var normalizedPos = localPos.X / sliderWidth;
            var newValue = zoomSlider.MinValue + (normalizedPos * (zoomSlider.MaxValue - zoomSlider.MinValue));
            newValue = Mathf.Clamp(newValue, zoomSlider.MinValue, zoomSlider.MaxValue);

            _log($"Calculated new zoom value: {newValue} from mouse position {localPos}");
            zoomSlider.Value = newValue;
            applyZoomChange?.Invoke(newValue);
        }
    }
}
