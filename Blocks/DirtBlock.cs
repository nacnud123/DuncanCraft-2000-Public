using DuncanCraft.Utils;
using DuncanCraft.World;
using System;

namespace DuncanCraft.Blocks
{
    public class DirtBlock : IBlock, IRandomTickable
    {
        public byte ID => 2;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(1, 1);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;

        public string Name => "Dirt";
        
        public byte LightOpacity => 15;
        
        public byte LightValue => 0;
        
        public void OnRandomTick(GameWorld world, int x, int y, int z, Random random)
        {
            if (y + 1 < GameWorld.WORLD_HEIGHT && world.GetBlock(x, y + 1, z) != BlockIDs.Air)
                return;

            bool hasGrassNeighbor = false;

            // Check North, South, East, West neighbors
            int[] dx = { 0, 0, 1, -1 };
            int[] dz = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                int neighborX = x + dx[i];
                int neighborZ = z + dz[i];

                if (world.IsPositionValid(neighborX, y, neighborZ))
                {
                    if (world.GetBlock(neighborX, y, neighborZ) == BlockIDs.Grass)
                    {
                        hasGrassNeighbor = true;
                        break;
                    }
                }
            }

            if (hasGrassNeighbor)
            {
                world.SetBlock(x, y, z, BlockIDs.Grass);
            }
        }
    }
}
