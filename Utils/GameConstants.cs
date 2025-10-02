// Holds the consts of the game, stuff that is important to the game functioning. | DA | 8/1/25

namespace VoxelGame.Utils
{
    public class GameConstants
    {
        public const int CHUNK_SIZE = 16;
        public const int CHUNK_HEIGHT = 256;
        public const int RENDER_DISTANCE = 6;
        public const float UI_SCALE = 1.5f;
        public const string SAVE_LOCATION = "duncanCraft2000Saves";

        public const int LOADING_TICKS_REQUIRED = 200;
        public const int TARGET_TPS = 20;
    }
}
