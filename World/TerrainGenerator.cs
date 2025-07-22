using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.SoundFont;
using VoxelGame.Utils;

namespace VoxelGame.World
{
    public class TerrainGenerator
    {
        private const int MinCaveDepth = 0;
        private const float CaveThreshold = 0.5f;
        private const float CaveScale = 0.1f;
        private const float TreeDensity = 0.02f;

        private readonly FastNoiseLite noise = new FastNoiseLite();

        public void init(int seed)
        {
            noise.SetSeed(seed);
            noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            noise.SetFrequency(0.1f);
        }

        public byte[,,] GenerateTerrain(byte[,,] blocksIn, ChunkPos position)
        {
            int chunkWorldX = position.X * Constants.CHUNK_SIZE;
            int chunkWorldZ = position.Z * Constants.CHUNK_SIZE;


            for (int x = 0; x < Constants.CHUNK_SIZE; x++)
            {
                int worldX = chunkWorldX + x;

                for (int z = 0; z < Constants.CHUNK_SIZE; z++)
                {
                    int worldZ = chunkWorldZ + z;

                    int height = getNoise(worldX, worldZ, .1f, 64);
                    height = Math.Max(1, Math.Min(height, Constants.CHUNK_HEIGHT - 1));

                    for (int y = 0; y < Constants.CHUNK_HEIGHT; y++)
                    {
                        byte blockType;
                        if (y < height - 5)
                        {
                            blockType = BlockIDs.Stone;
                        }
                        else if (y < height - 1)
                        {
                            blockType = BlockIDs.Dirt;
                        }
                        else if (y < height)
                        {
                            blockType = BlockIDs.Grass;
                        }
                        else
                        {
                            blockType = BlockIDs.Air;
                        }

                        if (blockType != BlockIDs.Air && y < height - 5 && y >= MinCaveDepth)
                        {
                            float caveNoise = NoiseGenerator.CaveNoise(worldX * CaveScale, y * CaveScale, worldZ * CaveScale);

                            if (caveNoise > CaveThreshold)
                            {
                                blockType = BlockIDs.Air;
                            }
                        }

                        blocksIn[x, y, z] = blockType;
                    }
                }
            }

            return blocksIn;
        }

        private int getNoise(int x, int z, float scale, int max)
        {
            return (int)Math.Floor((noise.GetNoise(x * scale, z * scale) + 1f) * (max / 2));
        }

    }
}
