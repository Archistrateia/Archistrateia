using Godot;
using System;

namespace Archistrateia
{
    public sealed class GameStartController
    {
        private readonly Action _hideStartButton;
        private readonly Action _hideTitleLabel;
        private readonly Action _hideMapGenerationControls;
        private readonly Func<float> _getCurrentZoom;
        private readonly Action<float> _setZoomSliderValue;
        private readonly Action _updateZoomLabel;
        private readonly Action _initializeGameManager;
        private readonly Action _regenerateMapWithCurrentZoom;
        private readonly Action _updateUIPositions;
        private readonly Action _initializeDebugTools;

        public bool IsGameStarted { get; private set; }

        public GameStartController(
            Action hideStartButton,
            Action hideTitleLabel,
            Action hideMapGenerationControls,
            Func<float> getCurrentZoom,
            Action<float> setZoomSliderValue,
            Action updateZoomLabel,
            Action initializeGameManager,
            Action regenerateMapWithCurrentZoom,
            Action updateUIPositions,
            Action initializeDebugTools)
        {
            _hideStartButton = hideStartButton ?? (() => { });
            _hideTitleLabel = hideTitleLabel ?? (() => { });
            _hideMapGenerationControls = hideMapGenerationControls ?? (() => { });
            _getCurrentZoom = getCurrentZoom ?? (() => 1.0f);
            _setZoomSliderValue = setZoomSliderValue ?? (_ => { });
            _updateZoomLabel = updateZoomLabel ?? (() => { });
            _initializeGameManager = initializeGameManager ?? (() => { });
            _regenerateMapWithCurrentZoom = regenerateMapWithCurrentZoom ?? (() => { });
            _updateUIPositions = updateUIPositions ?? (() => { });
            _initializeDebugTools = initializeDebugTools ?? (() => { });
        }

        public void StartGame()
        {
            if (IsGameStarted)
            {
                return;
            }

            IsGameStarted = true;

            _hideStartButton();
            _hideTitleLabel();
            _hideMapGenerationControls();

            var currentZoom = _getCurrentZoom();
            _setZoomSliderValue(currentZoom);
            _updateZoomLabel();

            _initializeGameManager();

            currentZoom = _getCurrentZoom();
            _setZoomSliderValue(currentZoom);
            _updateZoomLabel();
            _regenerateMapWithCurrentZoom();
            _updateUIPositions();
            _initializeDebugTools();
        }
    }
}
