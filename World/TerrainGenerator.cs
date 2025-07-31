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
        private readonly Noise _biomeNoise;
        private readonly Noise _coalNoise;
        private readonly Noise _ironNoise;
        private readonly Noise _goldNoise;
        private readonly Noise _diamondNoise;
        private readonly TreeGenerator _treeGenerator;

        private const int MinCaveDepth = 0;
        private const float CaveThreshold = 0.6f;
        private const float CaveScale = 0.1f;
        private const float TreeDensity = 0.02f;

        private const float CoalThreshold = 0.85f;
        private const int CoalMinHeight = 5;
        private const int CoalMaxHeight = 95;

        private const float IronThreshold = 0.88f;
        private const int IronMinHeight = 5;
        private const int IronMaxHeight = 70;

        private const float GoldThreshold = 0.92f;
        private const int GoldMinHeight = 5;
        private const int GoldMaxHeight = 35;

        private const float DiamondThreshold = 0.95f;
        private const int DiamondMinHeight = 5;
        private const int DiamondMaxHeight = 20;

        public enum BiomeType
        {
            Desert = 1,
            Forest = 2,
            Plains = 3,
            Swamp = 4
        }

        public TerrainGenerator()
        {
            _heightNoise = new Noise();
            _heightNoise.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _heightNoise.SetFrequency(0.01f);

            _caveNoise = new Noise();
            _caveNoise.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _caveNoise.SetFrequency(0.03f);

            _biomeNoise = new Noise();

            _coalNoise = new Noise();
            _coalNoise.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _coalNoise.SetFrequency(0.08f);

            _ironNoise = new Noise();
            _ironNoise.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _ironNoise.SetFrequency(0.06f);

            _goldNoise = new Noise();
            _goldNoise.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _goldNoise.SetFrequency(0.05f);

            _diamondNoise = new Noise();
            _diamondNoise.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _diamondNoise.SetFrequency(0.04f);

            _treeGenerator = new TreeGenerator();
        }

        public void init(int seed)
        {
            _heightNoise.SetSeed(seed);
            _caveNoise.SetSeed(seed);
            _treeGenerator.SetSeed(seed);
            _biomeNoise.SetSeed(seed);

            _coalNoise.SetSeed(seed + 1);
            _ironNoise.SetSeed(seed + 2);
            _goldNoise.SetSeed(seed + 3);
            _diamondNoise.SetSeed(seed + 4);
        }

        public void GenerateTerrain(Chunk chunk)
        {
            for (int x = 0; x < Constants.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Constants.CHUNK_SIZE; z++)
                {
                    var worldX = chunk.Position.X * Constants.CHUNK_SIZE + x;
                    var worldZ = chunk.Position.Z * Constants.CHUNK_SIZE + z;

                    var height = (int)(_heightNoise.GetNoise(worldX, worldZ) * 8 + 100);
                    float biomeNoise = _biomeNoise.GetNoise(worldX, worldZ);
                    biomeNoise = (biomeNoise + 1f) / 2f;

                    BiomeType biome = GetBiome(biomeNoise);

                    for (int y = 0; y < Constants.CHUNK_HEIGHT; y++)
                    {
                        byte blockType = BlockIDs.Air;

                        if (y == 0)
                        {
                            blockType = BlockIDs.Bedrock;
                        }
                        else
                        {
                            var caveValue = _caveNoise.GetNoise(worldX, y * 2, worldZ);
                            if (caveValue > CaveThreshold && y >= MinCaveDepth)
                            {
                                blockType = BlockIDs.Air;
                            }
                            else if (y < height - 4)
                            {
                                blockType = genOre(worldX, y, worldZ, BlockIDs.Stone);
                            }
                            else
                            {
                                blockType = genSurfaceBlock(biome, y, height);
                            }
                        }

                        chunk.Voxels[x, y, z] = blockType;
                    }
                }
            }

            _treeGenerator.GenerateTrees(chunk);
        }

        private byte genOre(int worldX, int y, int worldZ, byte defaultBlock)
        {
            // Diamond
            if (y >= DiamondMinHeight && y <= DiamondMaxHeight)
            {
                float diamondValue = _diamondNoise.GetNoise(worldX, y, worldZ);
                if (diamondValue > DiamondThreshold)
                {
                    return BlockIDs.DiamondOre;
                }
            }

            // Gold
            if (y >= GoldMinHeight && y <= GoldMaxHeight)
            {
                float goldValue = _goldNoise.GetNoise(worldX, y, worldZ);
                if (goldValue > GoldThreshold)
                {
                    return BlockIDs.GoldOre;
                }
            }

            // Iron
            if (y >= IronMinHeight && y <= IronMaxHeight)
            {
                float ironValue = _ironNoise.GetNoise(worldX, y, worldZ);
                if (ironValue > IronThreshold)
                {
                    return BlockIDs.IronOre;
                }
            }

            // Coal
            if (y >= CoalMinHeight && y <= CoalMaxHeight)
            {
                float coalValue = _coalNoise.GetNoise(worldX, y, worldZ);
                if (coalValue > CoalThreshold)
                {
                    return BlockIDs.CoalOre;
                }
            }

            return defaultBlock;
        }

        private byte genSurfaceBlock(BiomeType biome, int y, int height)
        {
            switch (biome)
            {
                case BiomeType.Desert:
                    if (y < height - 1)
                        return BlockIDs.Sand;
                    else if (y < height)
                        return BlockIDs.Sand;
                    break;

                case BiomeType.Forest:
                case BiomeType.Plains:
                case BiomeType.Swamp:
                    if (y < height - 1)
                        return BlockIDs.Dirt;
                    else if (y < height)
                        return BlockIDs.Grass;
                    break;
            }

            return BlockIDs.Air;
        }

        public BiomeType GetBiome(float biomeNoise)
        {
            if (biomeNoise < 0.25f)
                return BiomeType.Desert;
            else if (biomeNoise < 0.5f)
                return BiomeType.Plains;
            else if (biomeNoise < 0.75f)
                return BiomeType.Forest;
            else
                return BiomeType.Swamp;
        }

        public void Dispose()
        {
            _heightNoise?.Dispose();
            _caveNoise?.Dispose();
            _biomeNoise?.Dispose();
            _coalNoise?.Dispose();
            _ironNoise?.Dispose();
            _goldNoise?.Dispose();
            _diamondNoise?.Dispose();
            _treeGenerator?.Dispose();
        }
    }
}