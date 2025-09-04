using Godot;
using System;

namespace Archistrateia
{
    public class ViewportController
    {
        private Vector2 _scrollOffset = Vector2.Zero;
        private int _mapWidth;
        private int _mapHeight;
        private readonly Action _onViewChanged;

        public Vector2 ScrollOffset => _scrollOffset;

        public ViewportController(int mapWidth, int mapHeight, Action onViewChanged = null)
        {
            _mapWidth = mapWidth;
            _mapHeight = mapHeight;
            _onViewChanged = onViewChanged;
            
            // Initialize HexGridCalculator scroll offset
            HexGridCalculator.SetScrollOffset(_scrollOffset);
        }

        public void SetZoom(float zoomFactor)
        {
            HexGridCalculator.SetZoom(zoomFactor);
            GD.Print($"🔍 ZOOM SET: {zoomFactor:F2}x");
            NotifyViewChanged();
        }

        public void ZoomIn()
        {
            var oldZoom = HexGridCalculator.ZoomFactor;
            HexGridCalculator.ZoomIn();
            var newZoom = HexGridCalculator.ZoomFactor;
            GD.Print($"🔍 ZOOM IN: {oldZoom:F2}x -> {newZoom:F2}x");
            NotifyViewChanged();
        }

        public void ZoomOut()
        {
            var oldZoom = HexGridCalculator.ZoomFactor;
            HexGridCalculator.ZoomOut();
            var newZoom = HexGridCalculator.ZoomFactor;
            GD.Print($"🔍 ZOOM OUT: {oldZoom:F2}x -> {newZoom:F2}x");
            NotifyViewChanged();
        }

        public void ApplyScrollDelta(Vector2 scrollDelta, Vector2 viewportSize)
        {
            var oldOffset = _scrollOffset;
            _scrollOffset += scrollDelta;
            
            // Calculate dynamic scroll bounds based on current zoom and viewport
            var scrollBounds = HexGridCalculator.CalculateScrollBounds(viewportSize, _mapWidth, _mapHeight);
            
            // Clamp scroll offset to keep grid on screen
            _scrollOffset.X = Mathf.Clamp(_scrollOffset.X, -scrollBounds.X, scrollBounds.X);
            _scrollOffset.Y = Mathf.Clamp(_scrollOffset.Y, -scrollBounds.Y, scrollBounds.Y);
            
            // Update HexGridCalculator scroll offset
            HexGridCalculator.SetScrollOffset(_scrollOffset);
            
            GD.Print($"📜 SCROLL: Delta({scrollDelta.X:F1},{scrollDelta.Y:F1}) | Old({oldOffset.X:F1},{oldOffset.Y:F1}) -> New({_scrollOffset.X:F1},{_scrollOffset.Y:F1}) | Bounds(±{scrollBounds.X:F1},±{scrollBounds.Y:F1})");
            
            NotifyViewChanged();
        }

        public void ResetScroll()
        {
            var oldOffset = _scrollOffset;
            _scrollOffset = Vector2.Zero;
            HexGridCalculator.SetScrollOffset(_scrollOffset);
            GD.Print($"📜 SCROLL RESET: ({oldOffset.X:F1},{oldOffset.Y:F1}) -> (0,0)");
            NotifyViewChanged();
        }

        public bool IsScrollingNeeded(Vector2 viewportSize)
        {
            return HexGridCalculator.IsScrollingNeeded(viewportSize, _mapWidth, _mapHeight);
        }

        public bool HandleKeyboardInput(InputEventKey keyEvent, Vector2 viewportSize)
        {
            bool handled = false;
            
            // Handle zoom input
            if (keyEvent.Keycode == Key.Equal || keyEvent.Keycode == Key.Plus)
            {
                ZoomIn();
                handled = true;
            }
            else if (keyEvent.Keycode == Key.Minus)
            {
                ZoomOut();
                handled = true;
            }
            else if (keyEvent.Keycode == Key.Key0)
            {
                SetZoom(1.0f);
                handled = true;
            }
            
            // Handle Home key (reset scroll)
            if (keyEvent.Keycode == Key.Home)
            {
                ResetScroll();
                handled = true;
            }
            
            // Handle directional scrolling (only if scrolling is needed)
            if (IsScrollingNeeded(viewportSize))
            {
                var scrollDelta = Vector2.Zero;
                const float ScrollStep = 50.0f;

                if (keyEvent.Keycode == Key.W || keyEvent.Keycode == Key.Up)
                {
                    scrollDelta.Y = -ScrollStep;
                    handled = true;
                }
                else if (keyEvent.Keycode == Key.S || keyEvent.Keycode == Key.Down)
                {
                    scrollDelta.Y = ScrollStep;
                    handled = true;
                }
                else if (keyEvent.Keycode == Key.A || keyEvent.Keycode == Key.Left)
                {
                    scrollDelta.X = -ScrollStep;
                    handled = true;
                }
                else if (keyEvent.Keycode == Key.D || keyEvent.Keycode == Key.Right)
                {
                    scrollDelta.X = ScrollStep;
                    handled = true;
                }

                if (scrollDelta != Vector2.Zero)
                {
                    ApplyScrollDelta(scrollDelta, viewportSize);
                }
            }
            
            return handled;
        }

        public bool HandleMouseInput(InputEventMouseButton mouseEvent, Vector2 viewportSize)
        {
            bool handled = false;
            
            if (mouseEvent.Pressed)
            {
                if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
                {
                    ZoomIn();
                    handled = true;
                }
                else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
                {
                    ZoomOut();
                    handled = true;
                }
            }
            
            return handled;
        }

        public void HandlePanGesture(InputEventPanGesture panGesture, Vector2 viewportSize, Func<Vector2, bool> isMouseOverUIControls)
        {
            // Only allow scrolling if the grid extends beyond the viewport
            if (!IsScrollingNeeded(viewportSize))
            {
                return;
            }
            
            // Check if mouse is hovering over UI controls - if so, don't scroll
            var mousePosition = panGesture.Position;
            if (isMouseOverUIControls != null && isMouseOverUIControls(mousePosition))
            {
                return;
            }
            
            // Convert pan gesture delta to scroll delta
            // Pan gesture delta is typically smaller, so we scale it up for responsive scrolling
            const float PAN_SCROLL_MULTIPLIER = 4.0f;
            var scrollDelta = new Vector2(
                panGesture.Delta.X * PAN_SCROLL_MULTIPLIER,
                panGesture.Delta.Y * PAN_SCROLL_MULTIPLIER
            );
            
            ApplyScrollDelta(scrollDelta, viewportSize);
        }

        public float CalculateOptimalZoom(Vector2 viewportSize)
        {
            return HexGridCalculator.CalculateOptimalZoom(viewportSize, _mapWidth, _mapHeight);
        }

        private void NotifyViewChanged()
        {
            _onViewChanged?.Invoke();
        }

        public void UpdateMapSize(int mapWidth, int mapHeight)
        {
            _mapWidth = mapWidth;
            _mapHeight = mapHeight;
            GD.Print($"🗺️ VIEWPORT: Map size updated to {mapWidth}x{mapHeight}");
        }
    }
}
