using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoxelGame.Utils;

namespace VoxelGame.World
{
    public class TerrainGenerator : IDisposable
    {
        private readonly Noise _heightNoise;
        private readonly Noise _caveNoise;
        private readonly TreeGenerator _treeGenerator;

        private const int MinCaveDepth = 0;
        private const float CaveThreshold = 0.5f;
        private const float CaveScale = 0.1f;
        private const float TreeDensity = 0.02f;


        public TerrainGenerator()
        {
            _heightNoise = new Noise();
            _heightNoise.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _heightNoise.SetFrequency(0.01f);

            _caveNoise = new Noise();
            _caveNoise.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _caveNoise.SetFrequency(0.03f);

            _treeGenerator = new TreeGenerator();
        }

        public void init(int seed)
        {
            _heightNoise.SetSeed(seed);
            _caveNoise.SetSeed(seed);
            _treeGenerator.SetSeed(seed);
        }

        public void GenerateTerrain(Chunk chunk)
        {
            for (int x = 0; x < Constants.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Constants.CHUNK_SIZE; z++)
                {
                    var worldX = chunk.Position.X * Constants.CHUNK_SIZE + x;
                    var worldZ = chunk.Position.Z * Constants.CHUNK_SIZE + z;

                    var height = (int)(_heightNoise.GetNoise(worldX, worldZ) * 30 + 64);
                    //var height = (int)64;

                    for (int y = 0; y < Constants.CHUNK_HEIGHT; y++)
                    {
                        byte blockType = BlockIDs.Air;

                        if (y < height - 4)
                        {
                            // Check for caves
                            var caveValue = _caveNoise.GetNoise(worldX, y * 2, worldZ);
                            if (caveValue < 0.3f)
                            {
                                blockType = BlockIDs.Stone;
                            }
                        }
                        else if (y < height - 1)
                        {
                            blockType = BlockIDs.Dirt;
                        }
                        else if (y < height)
                        {
                            blockType = BlockIDs.Grass;
                        }

                        chunk.Voxels[x, y, z] = blockType;
                    }
                }
            }

            _treeGenerator.GenerateTrees(chunk);
        }

        public void Dispose()
        {
            _heightNoise?.Dispose();
            _caveNoise?.Dispose();
            _treeGenerator?.Dispose();
        }
    }
}
