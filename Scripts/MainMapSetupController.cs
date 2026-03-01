using Godot;
using System;

namespace Archistrateia
{
    public sealed class MainMapSetupController
    {
        private readonly Func<bool> _isGameStarted;
        private readonly Func<MapType> _getCurrentMapType;
        private readonly Action<MapType> _setCurrentMapType;
        private readonly Func<Node2D> _getMapContainer;
        private readonly Action<Node2D> _setMapContainer;
        private readonly Func<Node2D, MapType, Node2D> _generatePreviewMap;
        private readonly Func<float> _getZoomFactor;
        private readonly Action<float> _setZoomSliderValue;
        private readonly Action _updateZoomLabel;
        private readonly Action<string> _setMapTypeDescription;
        private readonly Action<string> _log;

        public MainMapSetupController(
            Func<bool> isGameStarted,
            Func<MapType> getCurrentMapType,
            Action<MapType> setCurrentMapType,
            Func<Node2D> getMapContainer,
            Action<Node2D> setMapContainer,
            Func<Node2D, MapType, Node2D> generatePreviewMap,
            Func<float> getZoomFactor,
            Action<float> setZoomSliderValue,
            Action updateZoomLabel,
            Action<string> setMapTypeDescription,
            Action<string> log = null)
        {
            _isGameStarted = isGameStarted ?? throw new ArgumentNullException(nameof(isGameStarted));
            _getCurrentMapType = getCurrentMapType ?? throw new ArgumentNullException(nameof(getCurrentMapType));
            _setCurrentMapType = setCurrentMapType ?? throw new ArgumentNullException(nameof(setCurrentMapType));
            _getMapContainer = getMapContainer ?? throw new ArgumentNullException(nameof(getMapContainer));
            _setMapContainer = setMapContainer ?? throw new ArgumentNullException(nameof(setMapContainer));
            _generatePreviewMap = generatePreviewMap ?? throw new ArgumentNullException(nameof(generatePreviewMap));
            _getZoomFactor = getZoomFactor ?? throw new ArgumentNullException(nameof(getZoomFactor));
            _setZoomSliderValue = setZoomSliderValue ?? (_ => { });
            _updateZoomLabel = updateZoomLabel ?? (() => { });
            _setMapTypeDescription = setMapTypeDescription ?? (_ => { });
            _log = log ?? (_ => { });
        }

        public void GenerateMap()
        {
            var mapContainer = _generatePreviewMap(_getMapContainer(), _getCurrentMapType());
            _setMapContainer(mapContainer);
            _setZoomSliderValue(_getZoomFactor());
            _updateZoomLabel();
        }

        public void OnMapTypeSelected(long index)
        {
            if (_isGameStarted())
            {
                return;
            }

            var mapTypes = Enum.GetValues<MapType>();
            if (index < 0 || index >= mapTypes.Length)
            {
                return;
            }

            var selectedType = mapTypes[index];
            _setCurrentMapType(selectedType);
            var config = MapTypeConfiguration.GetConfig(selectedType);
            _setMapTypeDescription(config.Description);
            _log($"🗺️ Selected map type: {config.Name}");
            GenerateMap();
        }

        public void OnRegenerateMapPressed()
        {
            if (_isGameStarted())
            {
                _log("⚠️ Map regeneration disabled during gameplay");
                return;
            }

            _log($"🔄 Regenerating map as {_getCurrentMapType()}");
            GenerateMap();
        }
    }
}
