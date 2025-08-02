// The main class that generates the world terrain. | DA | 8/1/25
using VoxelGame.Utils;

namespace VoxelGame.World
{
    public class TerrainGenerator : IDisposable
    {
        private readonly Noise _mHeightNoise;
        private readonly Noise _mCaveNoise;
        private readonly Noise _mBiomeNoise;
        private readonly Noise _mCoalNoise;
        private readonly Noise _mIronNoise;
        private readonly Noise _mGoldNoise;
        private readonly Noise _mDiamondNoise;
        private readonly TreeGenerator _mTreeGenerator;

        private const int MIN_CAVE_DEPTH = 0;
        private const float CAVE_THRESHOLD = 0.6f;
        private const float CAVE_SCALE = 0.1f;
        private const float TREE_DENSITY = 0.02f;

        private const float COAL_THRESHOLD = 0.85f;
        private const int COAL_MIN_HEIGHT = 5;
        private const int COAL_MAX_HEIGHT = 95;

        private const float IRON_THRESHOLD = 0.88f;
        private const int IRON_MIN_HEIGHT = 5;
        private const int IRON_MAX_HEIGHT = 70;

        private const float GOLD_THRESHOLD = 0.92f;
        private const int GOLD_MIN_HEIGHT = 5;
        private const int GOLD_MAX_HEIGHT = 35;

        private const float DIAMOND_THRESHOLD = 0.95f;
        private const int DIAMOND_MIN_HEIGHT = 5;
        private const int DIAMOND_MAX_HEIGHT = 20;

        public enum BiomeType
        {
            Desert = 1,
            Forest = 2,
            Plains = 3,
            Swamp = 4
        }

        public TerrainGenerator()
        {
            _mHeightNoise = new Noise();
            _mHeightNoise.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _mHeightNoise.SetFrequency(0.01f);

            _mCaveNoise = new Noise();
            _mCaveNoise.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _mCaveNoise.SetFrequency(0.03f);

            _mBiomeNoise = new Noise();

            _mCoalNoise = new Noise();
            _mCoalNoise.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _mCoalNoise.SetFrequency(0.08f);

            _mIronNoise = new Noise();
            _mIronNoise.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _mIronNoise.SetFrequency(0.06f);

            _mGoldNoise = new Noise();
            _mGoldNoise.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _mGoldNoise.SetFrequency(0.05f);

            _mDiamondNoise = new Noise();
            _mDiamondNoise.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _mDiamondNoise.SetFrequency(0.04f);

            _mTreeGenerator = new TreeGenerator();
        }

        public void init(int seed)
        {
            _mHeightNoise.SetSeed(seed);
            _mCaveNoise.SetSeed(seed);
            _mTreeGenerator.SetSeed(seed);
            _mBiomeNoise.SetSeed(seed);

            _mCoalNoise.SetSeed(seed + 1);
            _mIronNoise.SetSeed(seed + 2);
            _mGoldNoise.SetSeed(seed + 3);
            _mDiamondNoise.SetSeed(seed + 4);
        }

        public void GenerateTerrain(Chunk chunk)
        {
            for (int x = 0; x < Constants.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Constants.CHUNK_SIZE; z++)
                {
                    var worldX = chunk.Position.X * Constants.CHUNK_SIZE + x;
                    var worldZ = chunk.Position.Z * Constants.CHUNK_SIZE + z;

                    var height = (int)(_mHeightNoise.GetNoise(worldX, worldZ) * 8 + 100);
                    float biomeNoise = _mBiomeNoise.GetNoise(worldX, worldZ);
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
                            var caveValue = _mCaveNoise.GetNoise(worldX, y * 2, worldZ);
                            if (caveValue > CAVE_THRESHOLD && y >= MIN_CAVE_DEPTH)
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

            _mTreeGenerator.GenerateTrees(chunk);
        }

        private byte genOre(int worldX, int y, int worldZ, byte defaultBlock)
        {
            // Diamond
            if (y >= DIAMOND_MIN_HEIGHT && y <= DIAMOND_MAX_HEIGHT)
            {
                float diamondValue = _mDiamondNoise.GetNoise(worldX, y, worldZ);
                if (diamondValue > DIAMOND_THRESHOLD)
                {
                    return BlockIDs.DiamondOre;
                }
            }

            // Gold
            if (y >= GOLD_MIN_HEIGHT && y <= GOLD_MAX_HEIGHT)
            {
                float goldValue = _mGoldNoise.GetNoise(worldX, y, worldZ);
                if (goldValue > GOLD_THRESHOLD)
                {
                    return BlockIDs.GoldOre;
                }
            }

            // Iron
            if (y >= IRON_MIN_HEIGHT && y <= IRON_MAX_HEIGHT)
            {
                float ironValue = _mIronNoise.GetNoise(worldX, y, worldZ);
                if (ironValue > IRON_THRESHOLD)
                {
                    return BlockIDs.IronOre;
                }
            }

            // Coal
            if (y >= COAL_MIN_HEIGHT && y <= COAL_MAX_HEIGHT)
            {
                float coalValue = _mCoalNoise.GetNoise(worldX, y, worldZ);
                if (coalValue > COAL_THRESHOLD)
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
                        return BlockIDs.Stone;
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
            _mHeightNoise?.Dispose();
            _mCaveNoise?.Dispose();
            _mBiomeNoise?.Dispose();
            _mCoalNoise?.Dispose();
            _mIronNoise?.Dispose();
            _mGoldNoise?.Dispose();
            _mDiamondNoise?.Dispose();
            _mTreeGenerator?.Dispose();
        }
    }
}