using Godot;
using System;

namespace Archistrateia
{
    public sealed class MainHoverInfoController
    {
        private readonly Func<MapRenderer> _getMapRenderer;
        private readonly Action _markInputHandled;
        private readonly Action<string> _log;

        public MainHoverInfoController(
            Func<MapRenderer> getMapRenderer,
            Action markInputHandled,
            Action<string> log = null)
        {
            _getMapRenderer = getMapRenderer ?? throw new ArgumentNullException(nameof(getMapRenderer));
            _markInputHandled = markInputHandled ?? throw new ArgumentNullException(nameof(markInputHandled));
            _log = log ?? (_ => { });
        }

        public bool HandleHoverInfoModeInput(InputEventKey keyEvent)
        {
            if (keyEvent.Keycode != Key.I)
            {
                return false;
            }

            var mapRenderer = _getMapRenderer();
            mapRenderer?.ToggleHoverInfoMode();
            bool enabled = mapRenderer?.IsHoverInfoModeEnabled() ?? false;
            _log($"Hover info mode: {(enabled ? "ON" : "OFF")}");
            _markInputHandled();
            return true;
        }
    }
}
