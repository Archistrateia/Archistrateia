using Godot;

namespace Archistrateia
{
    public static class HexGridCalculator
    {
        public const float HEX_SIZE = 35.0f;
        public const float HEX_WIDTH = HEX_SIZE * 2.0f;
        public const float HEX_HEIGHT = HEX_SIZE * 1.732f;

        public static Vector2 CalculateHexPosition(int x, int y)
        {
            // Flat-top hex grid positioning formula
            float xPos = x * HEX_WIDTH * 0.75f;
            float yPos = y * HEX_HEIGHT;
            
            // Offset odd columns up by half the hex height
            if (x % 2 == 1)
            {
                yPos += HEX_HEIGHT * 0.5f;
            }
            
            return new Vector2(xPos, yPos);
        }

        public static Vector2 CalculateHexPositionCentered(int x, int y, Vector2 viewportSize, int mapWidth, int mapHeight)
        {
            var basePosition = CalculateHexPosition(x, y);
            
            // Center the map in the viewport
            float mapTotalWidth = mapWidth * HEX_WIDTH * 0.75f + HEX_WIDTH * 0.25f;
            float mapTotalHeight = mapHeight * HEX_HEIGHT + HEX_HEIGHT * 0.5f;
            float centerX = (viewportSize.X - mapTotalWidth) / 2;
            float centerY = (viewportSize.Y - mapTotalHeight) / 2 + HEX_HEIGHT * 0.5f;
            
            return new Vector2(basePosition.X + centerX, basePosition.Y + centerY);
        }

        public static Vector2[] CreateHexagonVertices()
        {
            var vertices = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                var angle = i * Mathf.Pi / 3.0f;
                vertices[i] = new Vector2(
                    HEX_SIZE * Mathf.Cos(angle),
                    HEX_SIZE * Mathf.Sin(angle)
                );
            }
            return vertices;
        }
    }
}