using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using VoxelGame.Blocks;
using VoxelGame.Utils;

namespace VoxelGame.World
{
    public class TreeGenerator : IDisposable
    {
        private readonly Random _treeRandom = new Random();

        private const float TREE_DENSITY = 0.02f;
        private const int MIN_TREE_SPACING = 8;
        private const int TREE_HEIGHT_MIN = 4;
        private const int TREE_HEIGHT_MAX = 8;
        private const int CANOPY_RADIUS = 2;

        private readonly Noise _treeNoise = new Noise();

        public TreeGenerator()
        {
            _treeNoise.SetFrequency(0.05f);
            _treeNoise.SetNoiseType(Noise.NoiseType.OpenSimplex2);
        }

        public void SetSeed(int seed)
        {
            _treeNoise.SetSeed(seed);
        }


        public void GenerateTrees(Chunk chunk)
        {
            var treePositions = new List<Vector3i>();
            
            for (int x = 0; x < Constants.CHUNK_SIZE; x += 4)
            {
                for (int z = 0; z < Constants.CHUNK_SIZE; z += 4)
                {
                    int worldX = chunk.Position.X * Constants.CHUNK_SIZE + x;
                    int worldZ = chunk.Position.Z * Constants.CHUNK_SIZE + z;
                    
                    float treeNoise = _treeNoise.GetNoise(worldX, worldZ);
                    
                    if (treeNoise > 0.6f)
                    {
                        int groundY = GetGroundLevel(chunk, x, z);

                        if (groundY != -1 && isValidTreeLocation(chunk, x, groundY, z))
                        {
                            Vector3i potentialPos = new Vector3i(x, groundY, z);
                            if (isValidSpacing(treePositions, potentialPos))
                            {
                                treePositions.Add(potentialPos);
                            }
                        }
                    }
                }
            }
            
            // Make trees
            foreach (var treePos in treePositions)
            {
                genTree(chunk, treePos.X, treePos.Y, treePos.Z);
            }
        }

        private int GetGroundLevel(Chunk chunk, int x, int z)
        {
            for (int y = Constants.CHUNK_HEIGHT - 1; y >= 0; y--)
            {
                if (chunk.Voxels[x, y, z] != BlockIDs.Air)
                {
                    return y;
                }
            }

            return -1;
        }

        private bool isValidTreeLocation(Chunk chunk, int x, int groundY, int z)
        {
            if (x < 2 || x >= Constants.CHUNK_SIZE - 2 || z < 2 || z >= Constants.CHUNK_SIZE - 2)
                return false;

            byte groundBlock = chunk.Voxels[x, groundY, z];
            if (groundBlock != BlockIDs.Grass && groundBlock != BlockIDs.Dirt)
                return false;

            int maxTreeHeight = TREE_HEIGHT_MAX + CANOPY_RADIUS;
            if (groundY + maxTreeHeight >= Constants.CHUNK_HEIGHT)
                return false;

            for (int y = groundY + 1; y <= groundY + maxTreeHeight; y++)
            {
                if (y < Constants.CHUNK_HEIGHT && chunk.Voxels[x, y, z] != BlockIDs.Air)
                    return false;
            }

            return true;
        }

        private bool isValidSpacing(List<Vector3i> existingTrees, Vector3i newPos)
        {
            foreach (var existing in existingTrees)
            {
                float distance = Vector3.Distance(
                    new Vector3(existing.X, 0, existing.Z),
                    new Vector3(newPos.X, 0, newPos.Z)
                );

                if (distance < MIN_TREE_SPACING)
                    return false;
            }

            return true;
        }

        private void genTree(Chunk chunk, int baseX, int baseY, int baseZ)
        {
            int trunkHeight = _treeRandom.Next(TREE_HEIGHT_MIN, TREE_HEIGHT_MAX + 1);
            
            genTrunk(chunk, baseX, baseY, baseZ, trunkHeight);

            genCanopy(chunk, baseX, baseY + trunkHeight, baseZ);
        }

        private void genTrunk(Chunk chunk, int x, int y, int z, int height)
        {
            for (int i = 1; i <= height; i++)
            {
                int currentY = y + i;
                if (currentY < Constants.CHUNK_HEIGHT)
                {
                    setBlock(chunk, x, currentY, z, BlockIDs.Log);
                }
            }
        }

        private void genCanopy(Chunk chunk, int centerX, int centerY, int centerZ)
        {
            for (int xi = -2; xi <= 2; xi++)
            {
                for (int yi = -2; yi <= 2; yi++)
                {
                    for (int zi = -2; zi <= 2; zi++)
                    {
                        setBlock(chunk, centerX + xi, centerY + yi, centerZ + zi, BlockIDs.Leaves);
                    }
                }
            }
        }

        private void setBlock(Chunk chunk, int x, int y, int z, byte blockType)
        {
            if (x >= 0 && x < Constants.CHUNK_SIZE &&
                y >= 0 && y < Constants.CHUNK_HEIGHT &&
                z >= 0 && z < Constants.CHUNK_SIZE)
            {
                if (chunk.Voxels[x, y, z] == BlockIDs.Air)
                {
                    chunk.Voxels[x, y, z] = blockType;
                }
            }
        }

        public void Dispose()
        {
            _treeNoise?.Dispose();
        }
    }
}