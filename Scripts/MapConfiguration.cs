using Godot;

namespace Archistrateia
{
    /// <summary>
    /// Single source of truth for all map-related configuration.
    /// This prevents dimension mismatches between visual and logical maps.
    /// </summary>
    public static class MapConfiguration
    {
        /// <summary>
        /// Number of hex tiles horizontally across the map
        /// </summary>
        public const int MAP_WIDTH = 20;
        
        /// <summary>
        /// Number of hex tiles vertically down the map  
        /// </summary>
        public const int MAP_HEIGHT = 10;
        
        /// <summary>
        /// Total number of tiles in the map
        /// </summary>
        public const int TOTAL_TILES = MAP_WIDTH * MAP_HEIGHT;
        
        /// <summary>
        /// Validate that the map dimensions are reasonable
        /// </summary>
        public static bool IsValidMapSize()
        {
            return MAP_WIDTH > 0 && MAP_HEIGHT > 0 && TOTAL_TILES <= 1000;
        }
    }
} 