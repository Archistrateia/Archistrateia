using Godot;

namespace Archistrateia
{
    public static class HexGridCalculator
    {
        public const float HEX_SIZE = 35.0f;
        public const float HEX_WIDTH = HEX_SIZE * 2.0f;
        public const float HEX_HEIGHT = HEX_SIZE * 1.732f;
        
        private static float _zoomFactor = 1.0f;
        public static float ZoomFactor 
        { 
            get => _zoomFactor; 
            set => _zoomFactor = Mathf.Clamp(value, 0.1f, 3.0f); 
        }

        private static Vector2 _scrollOffset = Vector2.Zero;
        public static Vector2 ScrollOffset
        {
            get => _scrollOffset;
            set => _scrollOffset = value;
        }

        public static Vector2 CalculateHexPosition(int x, int y)
        {
            float xPos = x * HEX_WIDTH * 0.75f * _zoomFactor;
            float yPos = y * HEX_HEIGHT * _zoomFactor;
            
            // Offset odd columns down by half the hex height (for pointy-top hexes)
            if (x % 2 == 1)
            {
                yPos += HEX_HEIGHT * 0.5f * _zoomFactor;
            }
            
            return new Vector2(xPos, yPos);
        }

        public static Vector2 CalculateHexPositionCentered(int x, int y, Vector2 viewportSize, int mapWidth, int mapHeight)
        {
            var basePosition = CalculateHexPosition(x, y);
            
            // Center the map in the viewport with zoom scaling
            float mapTotalWidth = mapWidth * HEX_WIDTH * 0.75f * _zoomFactor + HEX_WIDTH * 0.25f * _zoomFactor;
            float mapTotalHeight = mapHeight * HEX_HEIGHT * _zoomFactor + HEX_HEIGHT * 0.5f * _zoomFactor;
            float centerX = (viewportSize.X - mapTotalWidth) / 2;
            float centerY = (viewportSize.Y - mapTotalHeight) / 2;
            
            // Apply scroll offset (inverted to match expected edge scrolling behavior)
            return new Vector2(basePosition.X + centerX - _scrollOffset.X, basePosition.Y + centerY - _scrollOffset.Y);
        }

        public static Vector2[] CreateHexagonVertices()
        {
            var vertices = new Vector2[6];
            float scaledHexSize = HEX_SIZE * _zoomFactor;
            
            for (int i = 0; i < 6; i++)
            {
                var angle = i * Mathf.Pi / 3.0f;
                vertices[i] = new Vector2(
                    scaledHexSize * Mathf.Cos(angle),
                    scaledHexSize * Mathf.Sin(angle)
                );
            }
            return vertices;
        }
        
        public static void SetZoom(float zoomFactor)
        {
            ZoomFactor = zoomFactor;
        }
        
        public static void ZoomIn()
        {
            ZoomFactor *= 1.2f;
        }
        
        public static void ZoomOut()
        {
            ZoomFactor /= 1.2f;
        }

        public static void SetScrollOffset(Vector2 offset)
        {
            ScrollOffset = offset;
        }

        public static void AddScrollOffset(Vector2 delta)
        {
            ScrollOffset += delta;
        }

        public static Vector2 CalculateScrollBounds(Vector2 viewportSize, int mapWidth, int mapHeight)
        {
            // Calculate the map dimensions at current zoom level
            float mapTotalWidth = mapWidth * HEX_WIDTH * 0.75f * _zoomFactor + HEX_WIDTH * 0.25f * _zoomFactor;
            float mapTotalHeight = mapHeight * HEX_HEIGHT * _zoomFactor + HEX_HEIGHT * 0.5f * _zoomFactor;
            
            // Calculate how much the map extends beyond the viewport
            // This determines the maximum scroll offset needed to show the complete edges
            float maxScrollX = Mathf.Max(0, (mapTotalWidth - viewportSize.X) / 2);
            float maxScrollY = Mathf.Max(0, (mapTotalHeight - viewportSize.Y) / 2);
            
            // Account for hex tile size to ensure tiles are fully visible
            // Hex tiles extend beyond their calculated positions by the hex size
            float hexSize = HEX_SIZE * _zoomFactor;
            maxScrollX += hexSize;
            maxScrollY += hexSize;
            
            return new Vector2(maxScrollX, maxScrollY);
        }

        public static float CalculateOptimalZoom(Vector2 viewportSize, int mapWidth, int mapHeight)
        {
            // Calculate the map dimensions at zoom level 1.0
            float mapTotalWidth = mapWidth * HEX_WIDTH * 0.75f + HEX_WIDTH * 0.25f;
            float mapTotalHeight = mapHeight * HEX_HEIGHT + HEX_HEIGHT * 0.5f;
            
            // Calculate zoom factors needed to fit the map in the viewport
            // Leave some margin (10% of viewport size) for UI elements and visual comfort
            float marginX = viewportSize.X * 0.1f;
            float marginY = viewportSize.Y * 0.1f;
            
            float availableWidth = viewportSize.X - marginX;
            float availableHeight = viewportSize.Y - marginY;
            
            float zoomX = availableWidth / mapTotalWidth;
            float zoomY = availableHeight / mapTotalHeight;
            
            // Use the smaller zoom factor to ensure the map fits in both dimensions
            float optimalZoom = Mathf.Min(zoomX, zoomY);
            
            // Clamp to minimum zoom of 1.0 (never zoom out below 1.0)
            return Mathf.Max(optimalZoom, 1.0f);
        }

        public static bool IsScrollingNeeded(Vector2 viewportSize, int mapWidth, int mapHeight)
        {
            // Calculate the map dimensions at current zoom level
            float mapTotalWidth = mapWidth * HEX_WIDTH * 0.75f * _zoomFactor + HEX_WIDTH * 0.25f * _zoomFactor;
            float mapTotalHeight = mapHeight * HEX_HEIGHT * _zoomFactor + HEX_HEIGHT * 0.5f * _zoomFactor;
            
            // Check if the map is larger than the viewport in either dimension
            bool needsHorizontalScroll = mapTotalWidth > viewportSize.X;
            bool needsVerticalScroll = mapTotalHeight > viewportSize.Y;
            
            return needsHorizontalScroll || needsVerticalScroll;
        }
    }
}