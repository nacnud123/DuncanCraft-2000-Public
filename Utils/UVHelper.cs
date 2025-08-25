// Used to help UVs of block textures. Also can be used to get like half textures | DA | 8/25/25
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

        public static TextureCoords FromPartialTile(int tileX, int tileY, float startPixelX, float startPixelY, float widthPixels, float heightPixels)
        {
            float tilePixelSize = TILE_SIZE / 16f;
            
            float baseX = tileX * TILE_SIZE;
            float baseY = tileY * TILE_SIZE;
            
            Vector2 topLeft = new Vector2(
                baseX + startPixelX * tilePixelSize,
                baseY + startPixelY * tilePixelSize
            );
            
            Vector2 bottomRight = new Vector2(
                baseX + (startPixelX + widthPixels) * tilePixelSize,
                baseY + (startPixelY + heightPixels) * tilePixelSize
            );

            return new TextureCoords
            {
                TopLeft = topLeft,
                BottomRight = bottomRight
            };
        }
    }

}
