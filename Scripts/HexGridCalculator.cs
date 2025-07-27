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

        public static Vector2 CalculateHexPosition(int x, int y)
        {
            // Flat-top hex grid positioning formula with zoom scaling
            float xPos = x * HEX_WIDTH * 0.75f * _zoomFactor;
            float yPos = y * HEX_HEIGHT * _zoomFactor;
            
            // Offset odd columns up by half the hex height
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
            float centerY = (viewportSize.Y - mapTotalHeight) / 2 + HEX_HEIGHT * 0.5f * _zoomFactor;
            
            return new Vector2(basePosition.X + centerX, basePosition.Y + centerY);
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
    }
}