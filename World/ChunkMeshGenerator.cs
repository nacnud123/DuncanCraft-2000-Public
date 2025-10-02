// This script generates the chunk's mesh. I moved it from the main Chunk file to it's own. | DA | 8/25/25
using OpenTK.Mathematics;
using VoxelGame.Blocks;
using VoxelGame.Lighting;
using VoxelGame.Utils;

namespace VoxelGame.World
{
    public class ChunkMeshGenerator
    {
        private readonly Vector3[] mFaceNormals =
        [
            new Vector3(0, 0, 1), new Vector3(0, 0, -1), new Vector3(-1, 0, 0),
            new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, -1, 0)
        ];

        private readonly Vector3[] mFlowerNormals =
        [
            new Vector3(0.707f, 0, 0.707f),
            new Vector3(-0.707f, 0, -0.707f),
            new Vector3(-0.707f, 0, 0.707f),
            new Vector3(0.707f, 0, -0.707f)
        ];

        public ChunkMeshGenerator()
        {
        }

        public (List<Vertex> vertices, List<uint> indices) GenerateMesh(Chunk chunk, ChunkManager chunkManager, LightingEngine lightingEngine)
        {
            var vertices = chunkManager.GetVertexList();
            var indices = chunkManager.GetIndexList();
            uint vertexIndex = 0;

            Vector3 chunkWorldOffset = new Vector3(
                chunk.Position.X * GameConstants.CHUNK_SIZE,
                0,
                chunk.Position.Z * GameConstants.CHUNK_SIZE
            );

            if (vertices.Capacity < 16000)
                vertices.Capacity = 16000;

            if (indices.Capacity < 24000)
                indices.Capacity = 24000;

            for (int x = 0; x < GameConstants.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < GameConstants.CHUNK_SIZE; z++)
                {
                    int maxY = GameConstants.CHUNK_HEIGHT - 1;
                    while (maxY >= 0 && chunk.Voxels[x, maxY, z] == BlockIDs.Air)
                    {
                        maxY--;
                    }

                    for (int y = 0; y <= maxY; y++)
                    {
                        byte blockType = chunk.Voxels[x, y, z];
                        if (blockType == BlockIDs.Air) 
                            continue;

                        vertexIndex = generateBlockMesh(chunk, chunkManager, lightingEngine, x, y, z, blockType, vertices, indices, vertexIndex, chunkWorldOffset);
                    }
                }
            }

            return (vertices, indices);
        }

        private uint generateBlockMesh(Chunk chunk, ChunkManager chunkManager, LightingEngine lightingEngine, int x, int y, int z, byte blockType, List<Vertex> vertices, List<uint> indices, uint vertexIndex, Vector3 chunkWorldOffset)
        {
            bool[] visibleFaces = getVisibleFaces(chunk, chunkManager, x, y, z, blockType);
            Vector3[,] blockFaces = getBlockFaces(blockType);
            int faceCount = blockFaces.GetLength(0);

            for (int face = 0; face < faceCount; face++)
            {
                if (blockType != BlockIDs.YellowFlower && blockType != BlockIDs.GrassTuff && blockType != BlockIDs.Torch && blockType != BlockIDs.RedMushroom && blockType != BlockIDs.BrownMushroom && !visibleFaces[face])
                    continue;

                vertexIndex = genFace(chunk, chunkManager, lightingEngine, x, y, z, blockType, face,
                    blockFaces, vertices, indices, vertexIndex, chunkWorldOffset);
            }

            ArrayPool.ReturnBoolArray(visibleFaces);
            return vertexIndex;
        }

        private bool[] getVisibleFaces(Chunk chunk, ChunkManager chunkManager, int x, int y, int z, byte blockType)
        {
            bool[] visible = ArrayPool.RentBoolArray(6);

            // Fast path for internal blocks - check chunk bounds first
            if (x > 0 && x < GameConstants.CHUNK_SIZE - 1 && y > 0 && y < GameConstants.CHUNK_HEIGHT - 1 && z > 0 && z < GameConstants.CHUNK_SIZE - 1)
            {
                // Cache block lookups to avoid redundant registry calls
                byte frontBlock = chunk.Voxels[x, y, z + 1];
                byte backBlock = chunk.Voxels[x, y, z - 1];
                byte leftBlock = chunk.Voxels[x - 1, y, z];
                byte rightBlock = chunk.Voxels[x + 1, y, z];
                byte topBlock = chunk.Voxels[x, y + 1, z];
                byte bottomBlock = chunk.Voxels[x, y - 1, z];

                visible[0] = frontBlock == BlockIDs.Air || !BlockRegistry.GetBlock(frontBlock).IsSolid;
                visible[1] = backBlock == BlockIDs.Air || !BlockRegistry.GetBlock(backBlock).IsSolid;
                visible[2] = leftBlock == BlockIDs.Air || !BlockRegistry.GetBlock(leftBlock).IsSolid;
                visible[3] = rightBlock == BlockIDs.Air || !BlockRegistry.GetBlock(rightBlock).IsSolid;
                visible[4] = topBlock == BlockIDs.Air || !BlockRegistry.GetBlock(topBlock).IsSolid;
                visible[5] = bottomBlock == BlockIDs.Air || !BlockRegistry.GetBlock(bottomBlock).IsSolid;
            }
            else
            {
                // Slower path for edge blocks that might need cross-chunk lookups
                visible[0] = !isVoxelSolid(chunk, chunkManager, x, y, z + 1); // Front
                visible[1] = !isVoxelSolid(chunk, chunkManager, x, y, z - 1); // Back
                visible[2] = !isVoxelSolid(chunk, chunkManager, x - 1, y, z); // Left
                visible[3] = !isVoxelSolid(chunk, chunkManager, x + 1, y, z); // Right
                visible[4] = !isVoxelSolid(chunk, chunkManager, x, y + 1, z); // Top
                visible[5] = !isVoxelSolid(chunk, chunkManager, x, y - 1, z); // Bottom
            }

            return visible;
        }

        private uint genFace(Chunk chunk, ChunkManager chunkManager, LightingEngine lightingEngine, int x, int y, int z, byte blockType, int face, Vector3[,] blockFaces, List<Vertex> vertices, List<uint> indices, uint vertexIndex, Vector3 chunkWorldOffset)
        {
            Vector2[] texCoords = getTextureCoords(blockType, face);

            Vector3 normal;
            float lightValue;

            var block = BlockRegistry.GetBlock(blockType);

            if (block.RenderingType == BlockRenderingType.Cross)
            {
                normal = mFlowerNormals[face];
                lightValue = 1.0f;
            }
            else if (block.RenderingType == BlockRenderingType.Torch)
            {
                normal = mFaceNormals[Math.Min(face, mFaceNormals.Length - 1)];
                lightValue = 1.0f;
            }
            else
            {
                normal = mFaceNormals[face];
                lightValue = lightingEngine.GetFaceLightVal(chunk, x, y, z, face);
            }

            for (int v = 0; v < 4; v++)
            {
                Vector3 localPosition = new Vector3(x, y, z) + blockFaces[face, v];
                Vector3 worldPosition = localPosition + chunkWorldOffset;

                Vertex vertex = new Vertex(worldPosition, normal, texCoords[v], (float)blockType, lightValue);
                vertices.Add(vertex);
            }

            addFaceIndices(indices, vertexIndex, block.RenderingType == BlockRenderingType.Cross);
            return vertexIndex + 4;
        }

        private void addFaceIndices(List<uint> indices, uint vertexIndex, bool isFlower)
        {
            // Standard face indices
            indices.Add(vertexIndex);
            indices.Add(vertexIndex + 1);
            indices.Add(vertexIndex + 2);
            indices.Add(vertexIndex);
            indices.Add(vertexIndex + 2);
            indices.Add(vertexIndex + 3);

            // Double-sided for flowers
            if (isFlower)
            {
                indices.Add(vertexIndex);
                indices.Add(vertexIndex + 3);
                indices.Add(vertexIndex + 2);
                indices.Add(vertexIndex);
                indices.Add(vertexIndex + 2);
                indices.Add(vertexIndex + 1);
            }
        }

        private bool isVoxelSolid(Chunk chunk, ChunkManager chunkManager, int x, int y, int z)
        {
            if (x >= 0 && x < GameConstants.CHUNK_SIZE && y >= 0 && y < GameConstants.CHUNK_HEIGHT && z >= 0 && z < GameConstants.CHUNK_SIZE)
            {
                byte blockType = chunk.Voxels[x, y, z];
                return blockType != BlockIDs.Air && BlockRegistry.GetBlock(blockType).IsSolid;
            }

            if (y < 0 || y >= GameConstants.CHUNK_HEIGHT)
            {
                return false;
            }

            Vector3i worldPos = new Vector3i(
                chunk.Position.X * GameConstants.CHUNK_SIZE + x,
                y,
                chunk.Position.Z * GameConstants.CHUNK_SIZE + z
            );

            var (targetChunk, localPos) = WorldPositionHelper.GetChunkAndLocalPos(worldPos, chunkManager);

            if (targetChunk != null && targetChunk.TerrainGenerated && WorldPositionHelper.IsInChunkBounds(localPos))
            {
                byte blockType = targetChunk.Voxels[localPos.X, localPos.Y, localPos.Z];
                return blockType != BlockIDs.Air && BlockRegistry.GetBlock(blockType).IsSolid;
            }

            return false;
        }

        private Vector3[,] getBlockFaces(byte blockType)
        {
            var block = BlockRegistry.GetBlock(blockType);

            if (block.RenderingType == BlockRenderingType.Half)
            {
                return new Vector3[6, 4]
                {
                    { new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, .5f, 1), new Vector3(0, .5f, 1) },
                    { new Vector3(1, 0, 0), new Vector3(0, 0, 0), new Vector3(0, .5f, 0), new Vector3(1, .5f, 0) },
                    { new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, .5f, 1), new Vector3(0, .5f, 0) },
                    { new Vector3(1, 0, 1), new Vector3(1, 0, 0), new Vector3(1, .5f, 0), new Vector3(1, .5f, 1) },
                    { new Vector3(0, .5f, 1), new Vector3(1, .5f, 1), new Vector3(1, .5f, 0), new Vector3(0, .5f, 0) },
                    { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1) }
                };
            }
            else if (block.RenderingType == BlockRenderingType.Cross)
            {
                float inset = 0.1464f;
                return new Vector3[2, 4]
                {
                    { new Vector3(inset, 0, inset), new Vector3(1 - inset, 0, 1 - inset), new Vector3(1 - inset, 1, 1 - inset), new Vector3(inset, 1, inset) },
                    { new Vector3(1 - inset, 0, inset), new Vector3(inset, 0, 1 - inset), new Vector3(inset, 1, 1 - inset), new Vector3(1 - inset, 1, inset) }
                };
            }
            else if (block.RenderingType == BlockRenderingType.Torch)
            {
                float center = 0.5f;
                float halfThick = 0.0625f;
                float height = 0.625f;

                return new Vector3[6, 4]
                {
                    // Front face
                    { new Vector3(center - halfThick, 0, center + halfThick), new Vector3(center + halfThick, 0, center + halfThick),
                      new Vector3(center + halfThick, height, center + halfThick), new Vector3(center - halfThick, height, center + halfThick) },
                    // Back face
                    { new Vector3(center + halfThick, 0, center - halfThick), new Vector3(center - halfThick, 0, center - halfThick),
                      new Vector3(center - halfThick, height, center - halfThick), new Vector3(center + halfThick, height, center - halfThick) },
                    // Left face
                    { new Vector3(center - halfThick, 0, center - halfThick), new Vector3(center - halfThick, 0, center + halfThick),
                      new Vector3(center - halfThick, height, center + halfThick), new Vector3(center - halfThick, height, center - halfThick) },
                    // Right face
                    { new Vector3(center + halfThick, 0, center + halfThick), new Vector3(center + halfThick, 0, center - halfThick),
                      new Vector3(center + halfThick, height, center - halfThick), new Vector3(center + halfThick, height, center + halfThick) },
                    // Top face
                    { new Vector3(center - halfThick, height, center + halfThick), new Vector3(center + halfThick, height, center + halfThick),
                      new Vector3(center + halfThick, height, center - halfThick), new Vector3(center - halfThick, height, center - halfThick) },
                    // Bottom face
                    { new Vector3(center - halfThick, 0, center - halfThick), new Vector3(center + halfThick, 0, center - halfThick),
                      new Vector3(center + halfThick, 0, center + halfThick), new Vector3(center - halfThick, 0, center + halfThick) }
                };
            }
            else
            {
                return new Vector3[6, 4]
                {
                    { new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1) },
                    { new Vector3(1, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0) },
                    { new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0) },
                    { new Vector3(1, 0, 1), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1) },
                    { new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0), new Vector3(0, 1, 0) },
                    { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1) }
                };
            }
        }

        private Vector2[] getTextureCoords(byte voxelType, int faceIndex)
        {
            IBlock block = BlockRegistry.GetBlock(voxelType);

            TextureCoords coords;
            switch (faceIndex)
            {
                case 4: // Top
                    coords = block.TopTextureCoords;
                    break;
                case 5: // Bottom
                    coords = block.BottomTextureCoords;
                    break;
                default: // Side
                    coords = block.SideTextureCoords;
                    break;
            }

            return
            [
                coords.TopLeft,
                new Vector2(coords.BottomRight.X, coords.TopLeft.Y),
                coords.BottomRight,
                new Vector2(coords.TopLeft.X, coords.BottomRight.Y)
            ];
        }
    }
}