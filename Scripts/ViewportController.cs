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
        private int _debugCounter = 0;

        public Vector2 ScrollOffset => _scrollOffset;

        public ViewportController(int mapWidth, int mapHeight, Action onViewChanged = null)
        {
            _mapWidth = mapWidth;
            _mapHeight = mapHeight;
            _onViewChanged = onViewChanged;
            
            // Initialize HexGridCalculator scroll offset
            HexGridCalculator.SetScrollOffset(_scrollOffset);
        }

        private void ApplyZoomChange(float oldZoom, float newZoom, string operation)
        {
            GD.Print($"🔍 {operation}: {oldZoom:F2}x -> {newZoom:F2}x");
            
            // Center the map when zooming out for better user experience
            if (newZoom < oldZoom)
            {
                ResetScroll();
            }
            
            NotifyViewChanged();
        }

        public void SetZoom(float zoomFactor)
        {
            var oldZoom = HexGridCalculator.ZoomFactor;
            HexGridCalculator.SetZoom(zoomFactor);
            var newZoom = HexGridCalculator.ZoomFactor;
            ApplyZoomChange(oldZoom, newZoom, "ZOOM SET");
        }

        public void ZoomIn()
        {
            var oldZoom = HexGridCalculator.ZoomFactor;
            HexGridCalculator.ZoomIn();
            var newZoom = HexGridCalculator.ZoomFactor;
            ApplyZoomChange(oldZoom, newZoom, "ZOOM IN");
        }

        public void ZoomOut()
        {
            var oldZoom = HexGridCalculator.ZoomFactor;
            HexGridCalculator.ZoomOut();
            var newZoom = HexGridCalculator.ZoomFactor;
            ApplyZoomChange(oldZoom, newZoom, "ZOOM OUT");
        }

        public void ApplyScrollDelta(Vector2 scrollDelta, Vector2 gameAreaSize)
        {
            var oldOffset = _scrollOffset;
            _scrollOffset += scrollDelta;
            
            // Calculate smart scroll bounds - stop when map edges are visible in game area
            var scrollLimits = CalculateSmartScrollLimits(gameAreaSize);
            
            // Clamp scroll offset to keep map edges visible but not scroll beyond them
            // The scroll limits are the actual min/max values for each direction
            _scrollOffset.X = Mathf.Clamp(_scrollOffset.X, scrollLimits.X, scrollLimits.Y);
            _scrollOffset.Y = Mathf.Clamp(_scrollOffset.Y, scrollLimits.Z, scrollLimits.W);
            
            // Update HexGridCalculator scroll offset
            HexGridCalculator.SetScrollOffset(_scrollOffset);
            
            // Sample debug output to avoid spam
            _debugCounter++;
            if (_debugCounter % 60 == 0) // Show every 60 calls (about once per second at 60fps)
            {
                GD.Print($"📜 SCROLL (Sample {_debugCounter}): Delta({scrollDelta.X:F1},{scrollDelta.Y:F1}) | Old({oldOffset.X:F1},{oldOffset.Y:F1}) -> New({_scrollOffset.X:F1},{_scrollOffset.Y:F1}) | GameArea({gameAreaSize.X:F0}x{gameAreaSize.Y:F0}) | Limits({scrollLimits.X:F1} to {scrollLimits.Y:F1}, {scrollLimits.Z:F1} to {scrollLimits.W:F1})");
            }
            
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

        public bool IsScrollingNeeded(Vector2 gameAreaSize)
        {
            // Only allow scrolling if the map is actually larger than the game area
            return HexGridCalculator.IsScrollingNeeded(gameAreaSize, _mapWidth, _mapHeight);
        }

        public bool HandleKeyboardInput(InputEventKey keyEvent, Vector2 gameAreaSize)
        {
            return HandleZoomInput(keyEvent) || 
                   HandleScrollInput(keyEvent, gameAreaSize);
        }

        public bool HandleZoomInput(InputEventKey keyEvent)
        {
            if (keyEvent.Keycode == Key.Equal || keyEvent.Keycode == Key.Plus)
            {
                ZoomIn();
                return true;
            }
            else if (keyEvent.Keycode == Key.Minus)
            {
                ZoomOut();
                return true;
            }
            else if (keyEvent.Keycode == Key.Key0)
            {
                SetZoom(1.0f);
                return true;
            }
            
            return false;
        }

        public bool HandleScrollInput(InputEventKey keyEvent, Vector2 gameAreaSize)
        {
            if (keyEvent.Keycode == Key.Home)
            {
                ResetScroll();
                return true;
            }
            
            if (!IsScrollingNeeded(gameAreaSize))
            {
                return false;
            }

            var scrollDelta = GetScrollDeltaForKey(keyEvent);
            if (scrollDelta != Vector2.Zero)
            {
                ApplyScrollDelta(scrollDelta, gameAreaSize);
                return true;
            }
            
            return false;
        }

        private Vector2 GetScrollDeltaForKey(InputEventKey keyEvent)
        {
            const float ScrollStep = 50.0f;

            switch (keyEvent.Keycode)
            {
                case Key.W:
                case Key.Up:
                    return new Vector2(0, -ScrollStep);
                case Key.S:
                case Key.Down:
                    return new Vector2(0, ScrollStep);
                case Key.A:
                case Key.Left:
                    return new Vector2(-ScrollStep, 0);
                case Key.D:
                case Key.Right:
                    return new Vector2(ScrollStep, 0);
                default:
                    return Vector2.Zero;
            }
        }

        public bool HandleMouseInput(InputEventMouseButton mouseEvent, Vector2 gameAreaSize)
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

        public void HandlePanGesture(InputEventPanGesture panGesture, Vector2 gameAreaSize, Func<Vector2, bool> isMouseOverUIControls, Func<Vector2, bool> isMouseOverGameArea = null)
        {
            // Only allow panning if scrolling is needed
            if (!IsScrollingNeeded(gameAreaSize))
            {
                return;
            }
            
            var mousePosition = panGesture.Position;
            
            // Check if mouse is hovering over UI controls - if so, don't scroll
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
            
            ApplyScrollDelta(scrollDelta, gameAreaSize);
        }

        public float CalculateOptimalZoom(Vector2 viewportSize)
        {
            return HexGridCalculator.CalculateOptimalZoom(viewportSize, _mapWidth, _mapHeight);
        }

        private Vector4 CalculateSmartScrollLimits(Vector2 gameAreaSize)
        {
            // Calculate the map dimensions at current zoom level (same as in CalculateHexPositionCentered)
            float mapTotalWidth = _mapWidth * HexGridCalculator.HEX_WIDTH * 0.75f * HexGridCalculator.ZoomFactor + HexGridCalculator.HEX_WIDTH * 0.25f * HexGridCalculator.ZoomFactor;
            float mapTotalHeight = _mapHeight * HexGridCalculator.HEX_HEIGHT * HexGridCalculator.ZoomFactor + HexGridCalculator.HEX_HEIGHT * 0.5f * HexGridCalculator.ZoomFactor;
            
            // Calculate the centering offset (same as in CalculateHexPositionCentered)
            float centerX = (gameAreaSize.X - mapTotalWidth) / 2;
            float centerY = (gameAreaSize.Y - mapTotalHeight) / 2;
            
            // Calculate the actual boundaries of the map including hex tile radius
            float hexSize = HexGridCalculator.HEX_SIZE * HexGridCalculator.ZoomFactor;
            
            // The map extends from the leftmost tile position to the rightmost tile position
            // Leftmost tile is at centerX, rightmost tile is at centerX + mapTotalWidth
            // But tiles extend beyond their center positions by hexSize
            float leftEdge = centerX - hexSize;
            float rightEdge = centerX + mapTotalWidth + hexSize;
            float topEdge = centerY - hexSize;
            float bottomEdge = centerY + mapTotalHeight + hexSize;
            
            // Allow scrolling beyond the map edges to show complete tile boundaries
            // Add extra space (half a hex width/height) to show black space beyond the map
            float extraSpace = hexSize * 1.0f; // Full hex radius beyond map edges
            
            // Calculate the actual scroll limits for each direction
            // Since scroll offset is subtracted from position in CalculateHexPositionCentered:
            // - Positive scroll offset moves the map left (scroll right)
            // - Negative scroll offset moves the map right (scroll left)
            
            // The scroll offset is applied to the centered map, so we need to calculate
            // how much we can scroll from the centered position (scroll offset 0,0)
            
            // To show the left edge at the left side of the game area:
            // We need scrollOffset = leftEdge - extraSpace
            float minScrollX = leftEdge - extraSpace;
            
            // To show the right edge at the right side of the game area:
            // We need scrollOffset = rightEdge + extraSpace - gameAreaSize.X
            float maxScrollX = rightEdge + extraSpace - gameAreaSize.X;
            
            // To show the top edge at the top side of the game area:
            // We need scrollOffset = topEdge - extraSpace
            float minScrollY = topEdge - extraSpace;
            
            // To show the bottom edge at the bottom side of the game area:
            // We need scrollOffset = bottomEdge + extraSpace - gameAreaSize.Y
            float maxScrollY = bottomEdge + extraSpace - gameAreaSize.Y;
            
            // Debug output to see what's happening (sampled to avoid spam)
            _debugCounter++;
            if (_debugCounter % 300 == 0) // Show every 300 calls (about every 5 seconds at 60fps)
            {
                GD.Print($"🔍 SCROLL LIMITS DEBUG (Sample {_debugCounter}):");
                GD.Print($"  Map: {_mapWidth}x{_mapHeight}, Zoom: {HexGridCalculator.ZoomFactor:F2}x");
                GD.Print($"  MapTotal: {mapTotalWidth:F1}x{mapTotalHeight:F1}");
                GD.Print($"  Center: ({centerX:F1},{centerY:F1})");
                GD.Print($"  Edges: L{leftEdge:F1} R{rightEdge:F1} T{topEdge:F1} B{bottomEdge:F1}");
                GD.Print($"  ExtraSpace: {extraSpace:F1}");
                GD.Print($"  ScrollLimits: X({minScrollX:F1} to {maxScrollX:F1}) Y({minScrollY:F1} to {maxScrollY:F1})");
                GD.Print($"  CurrentScroll: ({_scrollOffset.X:F1},{_scrollOffset.Y:F1})");
            }
            
            // Return as Vector4: (minX, maxX, minY, maxY)
            return new Vector4(minScrollX, maxScrollX, minScrollY, maxScrollY);
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
