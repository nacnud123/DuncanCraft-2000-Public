// Used to help UVs of block textures. | DA | 8/1/25
using OpenTK.Mathematics;

namespace VoxelGame.Utils
{
    public static class UVHelper
    {
        private const int TILE_COUNT = 8;
        private const float TILE_SIZE = 1.0f / TILE_COUNT;

        public static TextureCoords FromTileCoords(int x, int y)
        {
            Vector2 topLeft = new Vector2(x * TILE_SIZE, y * TILE_SIZE);
            Vector2 bottomRight = new Vector2((x + 1) * TILE_SIZE, (y + 1) * TILE_SIZE);

            return new TextureCoords
            {
                TopLeft = topLeft,
                BottomRight = bottomRight
            };
        }
    }

}
