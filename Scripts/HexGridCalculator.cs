using Godot;

namespace Archistrateia
{
    public static class HexGridCalculator
    {
        public const float HEX_SIZE = 35.0f;
        public const float HEX_WIDTH = HEX_SIZE * 2.0f;
        public const float HEX_HEIGHT = HEX_SIZE * 1.732f;

        private static readonly HexGridViewState DefaultViewState = new();

        private static HexGridViewState ResolveViewState(HexGridViewState viewState)
        {
            return viewState ?? DefaultViewState;
        }

        public static Vector2 CalculateHexPosition(int x, int y, HexGridViewState viewState = null)
        {
            var state = ResolveViewState(viewState);
            float xPos = x * HEX_WIDTH * 0.75f * state.ZoomFactor;
            float yPos = y * HEX_HEIGHT * state.ZoomFactor;
            
            // Offset odd columns down by half the hex height (for pointy-top hexes)
            if (x % 2 == 1)
            {
                yPos += HEX_HEIGHT * 0.5f * state.ZoomFactor;
            }
            
            return new Vector2(xPos, yPos);
        }

        public static Vector2 CalculateHexPositionCentered(
            int x,
            int y,
            Vector2 viewportSize,
            int mapWidth,
            int mapHeight,
            HexGridViewState viewState = null)
        {
            var state = ResolveViewState(viewState);
            var basePosition = CalculateHexPosition(x, y, state);
            
            // Center the map in the viewport with zoom scaling
            float mapTotalWidth = mapWidth * HEX_WIDTH * 0.75f * state.ZoomFactor + HEX_WIDTH * 0.25f * state.ZoomFactor;
            float mapTotalHeight = mapHeight * HEX_HEIGHT * state.ZoomFactor + HEX_HEIGHT * 0.5f * state.ZoomFactor;
            float centerX = (viewportSize.X - mapTotalWidth) / 2;
            float centerY = (viewportSize.Y - mapTotalHeight) / 2;
            
            // Apply scroll offset (inverted to match expected edge scrolling behavior)
            return new Vector2(basePosition.X + centerX - state.ScrollOffset.X, basePosition.Y + centerY - state.ScrollOffset.Y);
        }

        public static Vector2[] CreateHexagonVertices(HexGridViewState viewState = null)
        {
            var state = ResolveViewState(viewState);
            var vertices = new Vector2[6];
            float scaledHexSize = HEX_SIZE * state.ZoomFactor;
            
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
        
        public static void SetZoom(float zoomFactor, HexGridViewState viewState = null)
        {
            ResolveViewState(viewState).ZoomFactor = zoomFactor;
        }
        
        public static void ZoomIn(HexGridViewState viewState = null)
        {
            var state = ResolveViewState(viewState);
            state.ZoomFactor *= 1.2f;
        }
        
        public static void ZoomOut(HexGridViewState viewState = null)
        {
            var state = ResolveViewState(viewState);
            state.ZoomFactor /= 1.2f;
        }

        public static void SetScrollOffset(Vector2 offset, HexGridViewState viewState = null)
        {
            ResolveViewState(viewState).ScrollOffset = offset;
        }

        public static void AddScrollOffset(Vector2 delta, HexGridViewState viewState = null)
        {
            var state = ResolveViewState(viewState);
            state.ScrollOffset += delta;
        }

        public static Vector2 CalculateScrollBounds(Vector2 viewportSize, int mapWidth, int mapHeight, HexGridViewState viewState = null)
        {
            var state = ResolveViewState(viewState);
            // Calculate the map dimensions at current zoom level
            float mapTotalWidth = mapWidth * HEX_WIDTH * 0.75f * state.ZoomFactor + HEX_WIDTH * 0.25f * state.ZoomFactor;
            float mapTotalHeight = mapHeight * HEX_HEIGHT * state.ZoomFactor + HEX_HEIGHT * 0.5f * state.ZoomFactor;
            
            // Calculate how much the map extends beyond the viewport
            // This determines the maximum scroll offset needed to show the complete edges
            float maxScrollX = Mathf.Max(0, (mapTotalWidth - viewportSize.X) / 2);
            float maxScrollY = Mathf.Max(0, (mapTotalHeight - viewportSize.Y) / 2);
            
            // Account for hex tile size to ensure tiles are fully visible
            // Hex tiles extend beyond their calculated positions by the hex size
            float hexSize = HEX_SIZE * state.ZoomFactor;
            maxScrollX += hexSize;
            maxScrollY += hexSize;
            
            return new Vector2(maxScrollX, maxScrollY);
        }

        public static float CalculateOptimalZoom(Vector2 viewportSize, int mapWidth, int mapHeight, HexGridViewState viewState = null)
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

        public static bool IsScrollingNeeded(Vector2 viewportSize, int mapWidth, int mapHeight, HexGridViewState viewState = null)
        {
            var state = ResolveViewState(viewState);
            // Calculate the map dimensions at current zoom level
            float mapTotalWidth = mapWidth * HEX_WIDTH * 0.75f * state.ZoomFactor + HEX_WIDTH * 0.25f * state.ZoomFactor;
            float mapTotalHeight = mapHeight * HEX_HEIGHT * state.ZoomFactor + HEX_HEIGHT * 0.5f * state.ZoomFactor;
            
            // Check if the map is larger than the viewport in either dimension
            bool needsHorizontalScroll = mapTotalWidth > viewportSize.X;
            bool needsVerticalScroll = mapTotalHeight > viewportSize.Y;
            
            return needsHorizontalScroll || needsVerticalScroll;
        }
    }
}
