using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Archistrateia
{
    public interface IDebugHexTile
    {
        Vector2I GridPosition { get; }
        Vector2 ToLocal(Vector2 globalPosition);
        void SetHighlight(bool highlight, Color highlightColor = default);
    }

    public sealed class DebugToolsController
    {
        private static readonly Color HoverHighlightColor = new(1.0f, 1.0f, 0.0f, 0.5f);
        private static readonly Color AdjacentHighlightColor = new(0.0f, 1.0f, 0.0f, 0.5f);

        private readonly Func<IEnumerable<IDebugHexTile>> _getTiles;
        private readonly Func<IDebugHexTile, Vector2, bool> _isMouseOverTile;
        private readonly Action<string> _log;
        private bool _debugAdjacentMode;
        private IDebugHexTile _lastHoveredTile;

        public DebugToolsController(
            Func<IEnumerable<IDebugHexTile>> getTiles,
            Func<IDebugHexTile, Vector2, bool> isMouseOverTile = null,
            Action<string> log = null)
        {
            _getTiles = getTiles ?? throw new ArgumentNullException(nameof(getTiles));
            _isMouseOverTile = isMouseOverTile ?? DefaultIsMouseOverTile;
            _log = log ?? GD.Print;
        }

        public bool IsDebugAdjacentModeEnabled()
        {
            return _debugAdjacentMode;
        }

        public Button CreateDebugAdjacentButton(Node parent, Func<Vector2> getViewportSize)
        {
            var button = new Button();
            button.Text = "Debug Adjacent";
            button.Position = new Vector2(10, getViewportSize().Y - 50);
            button.Size = new Vector2(120, 40);
            button.AddThemeFontSizeOverride("font_size", 16);
            button.ZIndex = 1000;
            button.MouseFilter = Control.MouseFilterEnum.Stop;
            button.Pressed += () => ToggleDebugAdjacentMode(button);
            parent.AddChild(button);
            return button;
        }

        public void UpdateDebugButtonPosition(Button button, Vector2 viewportSize)
        {
            if (button != null)
            {
                button.Position = new Vector2(10, viewportSize.Y - 50);
            }
        }

        public bool HandleDebugInput(InputEventKey keyEvent, Archistrateia.Debug.DebugScrollOverlay debugScrollOverlay)
        {
            if (keyEvent.Keycode == Key.F3)
            {
                debugScrollOverlay?.ToggleVisibility();
                return true;
            }

            return false;
        }

        public void ToggleDebugAdjacentMode(Button button)
        {
            _debugAdjacentMode = !_debugAdjacentMode;
            if (button != null)
            {
                button.Text = _debugAdjacentMode ? "Debug Adjacent: ON" : "Debug Adjacent";
            }

            if (!_debugAdjacentMode)
            {
                ClearDebugHighlights();
            }

            _log($"Debug adjacent mode: {(_debugAdjacentMode ? "ENABLED" : "DISABLED")}");
        }

        public void ClearDebugHighlights()
        {
            foreach (var tile in _getTiles())
            {
                tile.SetHighlight(false);
            }

            _lastHoveredTile = null;
        }

        public void HandleDebugAdjacentHover(Vector2 mousePosition)
        {
            if (!_debugAdjacentMode)
            {
                return;
            }

            var tiles = _getTiles().ToList();
            var hoveredTile = tiles.FirstOrDefault(tile => _isMouseOverTile(tile, mousePosition));
            if (hoveredTile == _lastHoveredTile)
            {
                return;
            }

            ClearDebugHighlights();
            if (hoveredTile == null)
            {
                return;
            }

            hoveredTile.SetHighlight(true, HoverHighlightColor);
            var adjacentPositions = MovementValidationLogic.GetAdjacentPositions(hoveredTile.GridPosition);
            _log($"Hovering over tile at {hoveredTile.GridPosition}, adjacent tiles: {string.Join(", ", adjacentPositions)}");

            foreach (var adjacentPos in adjacentPositions)
            {
                var adjacentTile = tiles.FirstOrDefault(tile => tile.GridPosition == adjacentPos);
                adjacentTile?.SetHighlight(true, AdjacentHighlightColor);
            }

            _lastHoveredTile = hoveredTile;
        }

        public void DebugMousePosition(Vector2 mousePosition, Button nextPhaseButton, HSlider zoomSlider)
        {
            _log($"=== DEBUGGING MOUSE POSITION: {mousePosition} ===");

            if (nextPhaseButton != null)
            {
                var buttonRect = new Rect2(nextPhaseButton.GlobalPosition, nextPhaseButton.Size);
                _log($"Next Phase Button: GlobalPos={nextPhaseButton.GlobalPosition}, Size={nextPhaseButton.Size}, Over={buttonRect.HasPoint(mousePosition)}");
            }

            if (zoomSlider != null)
            {
                var zoomPanel = zoomSlider.GetParent()?.GetParent() as Panel;
                if (zoomPanel != null)
                {
                    var panelRect = new Rect2(zoomPanel.GlobalPosition, zoomPanel.Size);
                    _log($"Zoom Panel: GlobalPos={zoomPanel.GlobalPosition}, Size={zoomPanel.Size}, Over={panelRect.HasPoint(mousePosition)}");
                }
            }
        }

        public void DebugUIElements(Button nextPhaseButton, HSlider zoomSlider)
        {
            _log("=== DEBUGGING UI ELEMENTS ===");

            if (nextPhaseButton != null)
            {
                _log($"Next Phase Button: Position={nextPhaseButton.GlobalPosition}, Size={nextPhaseButton.Size}, Visible={nextPhaseButton.Visible}");
            }
            else
            {
                _log("Next Phase Button: NULL");
            }

            if (zoomSlider != null)
            {
                var zoomPanel = zoomSlider.GetParent()?.GetParent() as Panel;
                if (zoomPanel != null)
                {
                    _log($"Zoom Panel: Position={zoomPanel.GlobalPosition}, Size={zoomPanel.Size}, Visible={zoomPanel.Visible}");
                }

                _log($"Zoom Slider: Position={zoomSlider.GlobalPosition}, Size={zoomSlider.Size}, Visible={zoomSlider.Visible}");
            }
            else
            {
                _log("Zoom Slider: NULL");
            }
        }

        private static bool DefaultIsMouseOverTile(IDebugHexTile tile, Vector2 mousePosition)
        {
            return IsPointInHexagon(tile.ToLocal(mousePosition));
        }

        private static bool IsPointInHexagon(Vector2 point)
        {
            var vertices = HexGridCalculator.CreateHexagonVertices();
            bool inside = false;
            int j = vertices.Length - 1;

            for (int i = 0; i < vertices.Length; i++)
            {
                if (((vertices[i].Y > point.Y) != (vertices[j].Y > point.Y)) &&
                    (point.X < (vertices[j].X - vertices[i].X) * (point.Y - vertices[i].Y) / (vertices[j].Y - vertices[i].Y) + vertices[i].X))
                {
                    inside = !inside;
                }
                j = i;
            }

            return inside;
        }
    }
}
