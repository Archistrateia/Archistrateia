using Godot;
using System;

namespace Archistrateia
{
    public sealed class MainUIHitTestController
    {
        private readonly Func<HSlider> _getZoomSlider;
        private readonly Func<Button> _getNextPhaseButton;
        private readonly Func<Button> _getDebugAdjacentButton;
        private readonly Func<OptionButton> _getPurchaseUnitSelector;
        private readonly Func<Button> _getPurchaseBuyButton;
        private readonly Func<Button> _getPurchaseCancelButton;
        private readonly Func<Control> _getGameArea;

        public MainUIHitTestController(
            Func<HSlider> getZoomSlider,
            Func<Button> getNextPhaseButton,
            Func<Button> getDebugAdjacentButton,
            Func<OptionButton> getPurchaseUnitSelector,
            Func<Button> getPurchaseBuyButton,
            Func<Button> getPurchaseCancelButton,
            Func<Control> getGameArea)
        {
            _getZoomSlider = getZoomSlider ?? throw new ArgumentNullException(nameof(getZoomSlider));
            _getNextPhaseButton = getNextPhaseButton ?? throw new ArgumentNullException(nameof(getNextPhaseButton));
            _getDebugAdjacentButton = getDebugAdjacentButton ?? throw new ArgumentNullException(nameof(getDebugAdjacentButton));
            _getPurchaseUnitSelector = getPurchaseUnitSelector ?? throw new ArgumentNullException(nameof(getPurchaseUnitSelector));
            _getPurchaseBuyButton = getPurchaseBuyButton ?? throw new ArgumentNullException(nameof(getPurchaseBuyButton));
            _getPurchaseCancelButton = getPurchaseCancelButton ?? throw new ArgumentNullException(nameof(getPurchaseCancelButton));
            _getGameArea = getGameArea ?? throw new ArgumentNullException(nameof(getGameArea));
        }

        public bool IsMouseOverGameArea(Vector2 mousePosition)
        {
            return !IsMouseOverUIControls(mousePosition);
        }

        public bool IsMouseOverUIControls(Vector2 mousePosition)
        {
            var zoomSlider = _getZoomSlider();
            if (zoomSlider != null)
            {
                var zoomPanel = zoomSlider.GetParent()?.GetParent() as Panel;
                if (IsPointInsideControl(zoomPanel, mousePosition))
                {
                    return true;
                }
            }

            if (IsPointInsideControl(_getNextPhaseButton(), mousePosition))
            {
                return true;
            }

            if (IsPointInsideControl(_getDebugAdjacentButton(), mousePosition))
            {
                return true;
            }

            if (IsPointInsideControl(_getPurchaseUnitSelector(), mousePosition))
            {
                return true;
            }

            if (IsPointInsideControl(_getPurchaseBuyButton(), mousePosition))
            {
                return true;
            }

            if (IsPointInsideControl(_getPurchaseCancelButton(), mousePosition))
            {
                return true;
            }

            return false;
        }

        public bool IsMouseWithinGameArea(Vector2 mousePosition)
        {
            var gameArea = _getGameArea();
            if (gameArea == null)
            {
                return true;
            }

            var gameAreaRect = new Rect2(gameArea.GlobalPosition, gameArea.Size);
            return gameAreaRect.HasPoint(mousePosition);
        }

        private static bool IsPointInsideControl(Control control, Vector2 mousePosition)
        {
            if (control == null)
            {
                return false;
            }

            var controlRect = new Rect2(control.GlobalPosition, control.Size);
            return controlRect.HasPoint(mousePosition);
        }
    }
}
