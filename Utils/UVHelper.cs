using OpenTK.Mathematics;

namespace VoxelGame.Utils
{
    public static class UVHelper
    {
        private const int TileCount = 8;
        private const float TileSize = 1.0f / TileCount;

        public static TextureCoords FromTileCoords(int x, int y)
        {
            Vector2 topLeft = new Vector2(x * TileSize, y * TileSize);
            Vector2 bottomRight = new Vector2((x + 1) * TileSize, (y + 1) * TileSize);

            {
                TopLeft = topLeft,
                BottomRight = bottomRight
            };
        }
    }

}
