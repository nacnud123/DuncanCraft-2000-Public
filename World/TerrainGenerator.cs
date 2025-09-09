// Main terrain generation script, simple right now but will be expanded soon. | DA | 9/4/25
using DuncanCraft.Utils;

namespace DuncanCraft.World
{
    public class TerrainGenerator
    {

        public void GenerateTerrain(Chunk chunk)
        {
            for (int x = 0; x < GameConstants.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < GameConstants.CHUNK_SIZE; z++)
                {
                    int worldX = chunk.X * GameConstants.CHUNK_SIZE + x;
                    int worldZ = chunk.Z * GameConstants.CHUNK_SIZE + z;

                    int height = GenerateHeight(worldX, worldZ);

                    for (int y = 0; y < GameConstants.CHUNK_HEIGHT; y++)
                    {
                        byte blockType = GetBlockTypeForHeight(y, height);

                        chunk.SetBlock(x, y, z, blockType);
                    }
                }
            }
        }
        private int GenerateHeight(int worldX, int worldZ)
        {
            double noise = SimplexNoise(worldX * 0.01, worldZ * 0.01);
            return (int)(20 + noise * 10);
        }

        private byte GetBlockTypeForHeight(int y, int terrainHeight)
        {
            if (y < terrainHeight - 3)
                return BlockIDs.Stone;
            else if (y < terrainHeight - 1)
                return BlockIDs.Dirt;
            else if (y < terrainHeight)
                return BlockIDs.Grass;
            else
                return BlockIDs.Air;
        }

        private double SimplexNoise(double x, double z)
        {
            return Math.Sin(x * 2) * Math.Cos(z * 3) * 0.5 + 0.5;
        }
    }
}