// The main class that generates the world terrain. | DA | 8/2/25
using VoxelGame.Utils;

namespace VoxelGame.World
{
    public class TerrainGenerator : IDisposable
    {
        // Height gene
        private readonly Noise _mHeightNoise1;    // Base terrain
        private readonly Noise _mHeightNoise2;    // Large features
        private readonly Noise _mHeightNoise3;    // Medium details
        private readonly Noise _mHeightNoise4;    // Fine details

        private readonly Noise _mCaveNoise;

        // Biome and ore gen
        private readonly Noise _mBiomeNoise;
        private readonly Noise _mCoalNoise;
        private readonly Noise _mIronNoise;
        private readonly Noise _mGoldNoise;
        private readonly Noise _mDiamondNoise;

        private readonly TreeGenerator _mTreeGenerator;

        // Terrain constants
        private const int SEA_LEVEL = 64;
        private const int MIN_HEIGHT = 8;
        private const int MAX_HEIGHT = 120;

        private const int MIN_CAVE_DEPTH = 0;
        private const float CAVE_THRESHOLD = 0.6f;
        private const float CAVE_SCALE = 0.1f;

        // Ore generation constants
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
            _mHeightNoise1 = new Noise();
            _mHeightNoise1.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _mHeightNoise1.SetFrequency(0.005f); // Large scale features

            _mHeightNoise2 = new Noise();
            _mHeightNoise2.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _mHeightNoise2.SetFrequency(0.01f);  // Medium scale features

            _mHeightNoise3 = new Noise();
            _mHeightNoise3.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _mHeightNoise3.SetFrequency(0.02f);  // Small scale features

            _mHeightNoise4 = new Noise();
            _mHeightNoise4.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _mHeightNoise4.SetFrequency(0.04f);  // Fine details

            _mCaveNoise = new Noise();
            _mCaveNoise.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _mCaveNoise.SetFrequency(0.03f);

            // Biome generation
            _mBiomeNoise = new Noise();
            _mBiomeNoise.SetNoiseType(Noise.NoiseType.OpenSimplex2);
            _mBiomeNoise.SetFrequency(0.003f);   // Large biome regions

            // Ore generation
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
            _mHeightNoise1.SetSeed(seed);
            _mHeightNoise2.SetSeed(seed + 100);
            _mHeightNoise3.SetSeed(seed + 200);
            _mHeightNoise4.SetSeed(seed + 300);

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
            int[,] heightMap = genHeightMap(chunk);

            for (int x = 0; x < GameConstants.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < GameConstants.CHUNK_SIZE; z++)
                {
                    var worldX = chunk.Position.X * GameConstants.CHUNK_SIZE + x;
                    var worldZ = chunk.Position.Z * GameConstants.CHUNK_SIZE + z;

                    int terrainHeight = heightMap[x, z];

                    float biomeNoise = _mBiomeNoise.GetNoise(worldX, worldZ);
                    biomeNoise = (biomeNoise + 1f) / 2f; // Normalize to 0-1
                    BiomeType biome = GetBiome(biomeNoise);

                    genColumn(chunk, x, z, worldX, worldZ, terrainHeight, biome);
                }
            }

            _mTreeGenerator.GenerateTrees(chunk);
        }

        private int[,] genHeightMap(Chunk chunk)
        {
            int[,] heightMap = new int[GameConstants.CHUNK_SIZE, GameConstants.CHUNK_SIZE];

            for (int x = 0; x < GameConstants.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < GameConstants.CHUNK_SIZE; z++)
                {
                    var worldX = chunk.Position.X * GameConstants.CHUNK_SIZE + x;
                    var worldZ = chunk.Position.Z * GameConstants.CHUNK_SIZE + z;

                    float height = 0f;

                    // Large scale
                    height += _mHeightNoise1.GetNoise(worldX, worldZ) * 30f;

                    // Medium scale
                    height += _mHeightNoise2.GetNoise(worldX, worldZ) * 15f;

                    // Small scale
                    height += _mHeightNoise3.GetNoise(worldX, worldZ) * 8f;

                    // Fine details
                    height += _mHeightNoise4.GetNoise(worldX, worldZ) * 4f;

                    int finalHeight = (int)(SEA_LEVEL + height);
                    finalHeight = Math.Max(MIN_HEIGHT, Math.Min(MAX_HEIGHT, finalHeight));

                    heightMap[x, z] = finalHeight;
                }
            }

            return heightMap;
        }

        private void genColumn(Chunk chunk, int x, int z, int worldX, int worldZ, int terrainHeight, BiomeType biome)
        {
            for (int y = 0; y < GameConstants.CHUNK_HEIGHT; y++)
            {
                byte blockType = BlockIDs.Air;

                if (y == 0)
                {
                    blockType = BlockIDs.Bedrock;
                }
                else if (y <= terrainHeight)
                {
                    // Caves
                    if (shouldMakeCaves(worldX, y, worldZ))
                    {
                        blockType = BlockIDs.Air;
                    }
                    else
                    {
                        // Terrain blocks
                        blockType = genTerrainBlocks(worldX, y, worldZ, terrainHeight, biome);
                    }
                }
                else if (y <= SEA_LEVEL && biome != BiomeType.Desert)
                {

                    blockType = BlockIDs.Air; // TODO: replace with water in the future
                }

                chunk.Voxels[x, y, z] = blockType;
            }
        }

        private bool shouldMakeCaves(int worldX, int y, int worldZ)
        {
            var caveValue = _mCaveNoise.GetNoise(worldX, y * 2, worldZ);
            return caveValue > CAVE_THRESHOLD && y >= MIN_CAVE_DEPTH;
        }

        private byte genTerrainBlocks(int worldX, int y, int worldZ, int terrainHeight, BiomeType biome)
        {
            // Surface
            if (y == terrainHeight)
            {
                return getSurfaceBlock(biome);
            }
            // Sub-surface
            else if (y >= terrainHeight - 3)
            {
                return getSubSurfaceBlock(biome);
            }
            // Deep underground
            else
            {
                return genOres(worldX, y, worldZ, BlockIDs.Stone);
            }
        }

        private byte getSurfaceBlock(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Desert => BlockIDs.Sand,
                BiomeType.Forest => BlockIDs.Grass,
                BiomeType.Plains => BlockIDs.Grass,
                BiomeType.Swamp => BlockIDs.Grass,
                _ => BlockIDs.Grass
            };
        }

        private byte getSubSurfaceBlock(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Desert => BlockIDs.Sand,
                BiomeType.Forest => BlockIDs.Dirt,
                BiomeType.Plains => BlockIDs.Dirt,
                BiomeType.Swamp => BlockIDs.Dirt,
                _ => BlockIDs.Dirt
            };
        }

        private byte genOres(int worldX, int y, int worldZ, byte defaultBlock)
        {
            // Gravel
            if (y >= 10 && y <= 80)
            {
                float gravelValue = _mCoalNoise.GetNoise(worldX * 1.2f, y * 0.8f, worldZ * 1.2f);
                if (gravelValue > .90f)
                    return BlockIDs.Gravel;
            }

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
            _mHeightNoise1?.Dispose();
            _mHeightNoise2?.Dispose();
            _mHeightNoise3?.Dispose();
            _mHeightNoise4?.Dispose();
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