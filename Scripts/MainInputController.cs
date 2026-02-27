using Godot;
using System;

namespace Archistrateia
{
    public sealed class MainInputController
    {
        private readonly IViewportInputController _viewportController;
        private readonly Archistrateia.Debug.DebugScrollOverlay _debugScrollOverlay;
        private readonly Func<Vector2> _getMousePosition;
        private readonly Func<Vector2> _getGameAreaSize;
        private readonly Func<Rect2> _getGameGridRect;
        private readonly Func<Vector2, bool> _isMouseOverUIControls;
        private readonly Func<Vector2, bool> _isMouseOverGameArea;
        private readonly Action<Vector2> _debugMousePosition;
        private readonly Func<bool> _isDebugAdjacentModeEnabled;
        private readonly Action<Vector2> _handleDebugAdjacentHover;
        private readonly Action _markInputHandled;

        public MainInputController(
            IViewportInputController viewportController,
            Archistrateia.Debug.DebugScrollOverlay debugScrollOverlay,
            Func<Vector2> getMousePosition,
            Func<Vector2> getGameAreaSize,
            Func<Rect2> getGameGridRect,
            Func<Vector2, bool> isMouseOverUIControls,
            Func<Vector2, bool> isMouseOverGameArea,
            Action<Vector2> debugMousePosition,
            Func<bool> isDebugAdjacentModeEnabled,
            Action<Vector2> handleDebugAdjacentHover,
            Action markInputHandled)
        {
            _viewportController = viewportController;
            _debugScrollOverlay = debugScrollOverlay;
            _getMousePosition = getMousePosition ?? throw new ArgumentNullException(nameof(getMousePosition));
            _getGameAreaSize = getGameAreaSize ?? throw new ArgumentNullException(nameof(getGameAreaSize));
            _getGameGridRect = getGameGridRect ?? throw new ArgumentNullException(nameof(getGameGridRect));
            _isMouseOverUIControls = isMouseOverUIControls ?? throw new ArgumentNullException(nameof(isMouseOverUIControls));
            _isMouseOverGameArea = isMouseOverGameArea ?? throw new ArgumentNullException(nameof(isMouseOverGameArea));
            _debugMousePosition = debugMousePosition ?? throw new ArgumentNullException(nameof(debugMousePosition));
            _isDebugAdjacentModeEnabled = isDebugAdjacentModeEnabled ?? throw new ArgumentNullException(nameof(isDebugAdjacentModeEnabled));
            _handleDebugAdjacentHover = handleDebugAdjacentHover ?? throw new ArgumentNullException(nameof(handleDebugAdjacentHover));
            _markInputHandled = markInputHandled ?? throw new ArgumentNullException(nameof(markInputHandled));
        }

        public void HandleInput(InputEvent @event)
        {
            if (@event == null)
            {
                return;
            }

            if (_isDebugAdjacentModeEnabled() && @event is InputEventMouseMotion)
            {
                _handleDebugAdjacentHover(_getMousePosition());
            }

            if (@event is InputEventMouseButton mouseEvent)
            {
                var mousePosition = _getMousePosition();
                if (!_isMouseOverUIControls(mousePosition))
                {
                    var handled = _viewportController?.HandleMouseInput(mouseEvent, _getGameAreaSize()) ?? false;
                    if (handled)
                    {
                        _markInputHandled();
                    }
                }

                if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    _debugMousePosition(mousePosition);
                }
            }

            if (@event is InputEventPanGesture panGesture)
            {
                _viewportController?.HandlePanGesture(panGesture, _getGameAreaSize(), _isMouseOverUIControls, _isMouseOverGameArea);
                _markInputHandled();
            }
        }

        public void HandleProcess(double delta)
        {
            _viewportController?.Update(delta);

            var mousePosition = _getMousePosition();
            var gameGridRect = _getGameGridRect();
            var gameAreaSize = _getGameAreaSize();
            var isScrollingNeeded = _viewportController != null && _viewportController.IsScrollingNeeded(gameAreaSize);
            var isOverUIControls = _isMouseOverUIControls(mousePosition);
            var edgeScrollThreshold = _viewportController?.EdgeScrollThreshold ?? 50.0f;

            _debugScrollOverlay?.UpdateScrollAreas(gameGridRect.Size, edgeScrollThreshold, isScrollingNeeded, gameGridRect.Position);
            _debugScrollOverlay?.UpdateUIExclusions(mousePosition, isOverUIControls);
            _viewportController?.HandleEdgeScrolling(mousePosition, gameGridRect, gameAreaSize, isOverUIControls, delta);
        }

        public void HandleUnhandledInput(
            InputEvent @event,
            Func<InputEventKey, bool> handleDebugInput,
            Func<InputEventKey, bool> handlePhaseInput,
            Func<InputEventKey, bool> handleHoverInfoModeInput,
            Func<InputEventKey, bool> handleZoomInput,
            Func<InputEventKey, bool> handleScrollInput)
        {
            if (@event is not InputEventKey keyEvent || !keyEvent.Pressed)
            {
                return;
            }

            if (handleDebugInput?.Invoke(keyEvent) == true) return;
            if (handlePhaseInput?.Invoke(keyEvent) == true) return;
            if (handleHoverInfoModeInput?.Invoke(keyEvent) == true) return;
            if (handleZoomInput?.Invoke(keyEvent) == true) return;
            handleScrollInput?.Invoke(keyEvent);
        }
    }
}
