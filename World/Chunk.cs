// Main chunk script, has stuff related to position and what blocks are in the chunk | DA | 9/4/25
using DuncanCraft.Blocks;
using DuncanCraft.Lighting;
using DuncanCraft.Utils;
using OpenTK.Mathematics;
using System.Collections.Concurrent;

namespace DuncanCraft.World
{
    public class ChunkMesh
    {
        public List<Vertex> Vertices { get; set; } = new();
        public bool IsReady { get; set; }
        public int TriangleCount => Vertices.Count / 3;
    }

    public class Chunk
    {

        public int X { get; }
        public int Z { get; }
        public ChunkState State { get; private set; }

        private readonly byte[,,] _mBlocks;
        private readonly object _mLockObject = new();
        private bool mIsDirty;
        private ChunkMesh? mCurrentMesh;
        private volatile bool _needsMeshRebuild;

        // Lighting data storage
        private readonly NibbleArray _mSkyLightMap;
        private readonly NibbleArray _mBlockLightMap;
        private readonly byte[] _mHeightMap;
        private int mLowestBlockHeight;

        public Chunk(int x, int z)
        {
            X = x;
            Z = z;
            
            _mBlocks = new byte[GameConstants.CHUNK_SIZE, GameConstants.CHUNK_HEIGHT, GameConstants.CHUNK_SIZE];
            State = ChunkState.Empty;
            mIsDirty = true;
            _needsMeshRebuild = true;

            // Init lighting storage
            int totalBlocks = GameConstants.CHUNK_SIZE * GameConstants.CHUNK_HEIGHT * GameConstants.CHUNK_SIZE;
            _mSkyLightMap = new NibbleArray(totalBlocks);
            _mBlockLightMap = new NibbleArray(totalBlocks);

            _mHeightMap = new byte[GameConstants.CHUNK_SIZE * GameConstants.CHUNK_SIZE];
            mLowestBlockHeight = GameConstants.CHUNK_HEIGHT;
        }

        public byte GetBlock(int x, int y, int z)
        {
            if (x < 0 || x >= GameConstants.CHUNK_SIZE || y < 0 || y >= GameConstants.CHUNK_HEIGHT || z < 0 || z >= GameConstants.CHUNK_SIZE)
                return BlockIDs.Air;

            lock (_mLockObject)
            {
                return _mBlocks[x, y, z];
            }
        }

        public void SetBlock(int x, int y, int z, byte block)
        {
            if (x < 0 || x >= GameConstants.CHUNK_SIZE || y < 0 || y >= GameConstants.CHUNK_HEIGHT || z < 0 || z >= GameConstants.CHUNK_SIZE)
                return;

            lock (_mLockObject)
            {
                if (_mBlocks[x, y, z] != block)
                {
                    _mBlocks[x, y, z] = block;
                    mIsDirty = true;
                    _needsMeshRebuild = true;
                }
            }
        }

        public async Task GenerateAsync()
        {
            if (State != ChunkState.Empty) 
                return;

            State = ChunkState.Generating;

            await Task.Run(() =>
            {
                lock (_mLockObject)
                {
                    var terrainGenerator = new TerrainGenerator();
                    terrainGenerator.GenerateTerrain(this);
                }
            });

            State = ChunkState.Generated;
            mIsDirty = true;
            _needsMeshRebuild = true;

            //// Debug: Count blocks in this chunk
            //int blockCount = 0;
            //for (int x = 0; x < GameConstants.CHUNK_SIZE; x++)
            //{
            //    for (int y = 0; y < GameConstants.CHUNK_HEIGHT; y++)
            //    {
            //        for (int z = 0; z < GameConstants.CHUNK_SIZE; z++)
            //        {
            //            if (_blocks[x, y, z] != BlockIDs.Air)
            //                blockCount++;
            //        }
            //    }
            //}

        }



        public bool NeedsRebuild() => mIsDirty && State == ChunkState.Generated;

        public void MarkForRebuild()
        {
            mIsDirty = true;
            _needsMeshRebuild = true;
        }

        public void ClearDirtyFlag()
        {
            mIsDirty = false;
        }

        private byte getNeighborBlock(int x, int y, int z, ChunkManager chunkManager)
        {
            if (x >= 0 && x < GameConstants.CHUNK_SIZE && y >= 0 && y < GameConstants.CHUNK_HEIGHT && z >= 0 && z < GameConstants.CHUNK_SIZE)
                return GetBlock(x, y, z);

            int neighborChunkX = X;
            int neighborChunkZ = Z;
            int localX = x;
            int localZ = z;

            if (x < 0)
            {
                neighborChunkX--;
                localX = GameConstants.CHUNK_SIZE - 1;
            }
            else if (x >= GameConstants.CHUNK_SIZE)
            {
                neighborChunkX++;
                localX = 0;
            }

            if (z < 0)
            {
                neighborChunkZ--;
                localZ = GameConstants.CHUNK_SIZE - 1;
            }
            else if (z >= GameConstants.CHUNK_SIZE)
            {
                neighborChunkZ++;
                localZ = 0;
            }

            if (y < 0 || y >= GameConstants.CHUNK_HEIGHT)
                return BlockIDs.Air;

            var neighborChunk = chunkManager.GetChunk(neighborChunkX, neighborChunkZ);
            return neighborChunk?.GetBlock(localX, y, localZ) ?? BlockIDs.Air;
        }


        public ChunkMesh? GetCurrentMesh() => mCurrentMesh;

        public ChunkMesh GenerateMesh(ChunkManager chunkManager, Texture textureAtlas)
        {
            var mesh = new ChunkMesh();

            lock (_mLockObject)
            {
                for (int x = 0; x < GameConstants.CHUNK_SIZE; x++)
                {
                    for (int y = 0; y < GameConstants.CHUNK_HEIGHT; y++)
                    {
                        for (int z = 0; z < GameConstants.CHUNK_SIZE; z++)
                        {
                            var blockId = _mBlocks[x, y, z];
                            if (blockId != BlockIDs.Air)
                            {
                                if (blockId == BlockIDs.Torch)
                                {
                                    addTorchMesh(mesh.Vertices, x, y, z, chunkManager);
                                }
                                else if (blockId == BlockIDs.Slab)
                                {
                                    addSlabMesh(mesh.Vertices, x, y, z, chunkManager);
                                }
                                else if (blockId == BlockIDs.Flower)
                                {
                                    addFlowerMesh(mesh.Vertices, x, y, z, chunkManager);
                                }
                                else
                                {
                                    addBlockFaces(mesh.Vertices, x, y, z, blockId, chunkManager);
                                }
                            }
                        }
                    }
                }
            }

            mesh.IsReady = true;
            return mesh;
        }

        public void SetMesh(ChunkMesh mesh)
        {
            mCurrentMesh = mesh;
            _needsMeshRebuild = false;
        }

        private void addBlockFaces(List<Vertex> vertices, int localX, int localY, int localZ, byte block, ChunkManager chunkManager)
        {
            int worldX = X * GameConstants.CHUNK_SIZE + localX;
            int worldZ = Z * GameConstants.CHUNK_SIZE + localZ;

            // Top face (Y+)
            if (isBlockTransparent(getBlockForMesh(localX, localY + 1, localZ, chunkManager)))
            {
                var texCoords = getTextureCoords(block, 4); // 4 = Top face index
                var lightLevel = getLightForMesh(localX, localY + 1, localZ, chunkManager);
                var lightValue = Math.Max(0.15f, lightLevel / 15.0f);
                addQuadVertices(vertices,
                    worldX, localY + 1, worldZ + 1,     // v1
                    worldX + 1, localY + 1, worldZ + 1, // v2
                    worldX + 1, localY + 1, worldZ,     // v3
                    worldX, localY + 1, worldZ,         // v4
                    texCoords, lightValue);
            }

            // Bottom face (Y-)
            if (isBlockTransparent(getBlockForMesh(localX, localY - 1, localZ, chunkManager)))
            {
                var texCoords = getTextureCoords(block, 5); // 5 = Bottom face index
                var lightLevel = getLightForMesh(localX, localY - 1, localZ, chunkManager);
                var lightValue = Math.Max(0.15f, lightLevel / 15.0f);
                addQuadVertices(vertices,
                    worldX, localY, worldZ,         // v1
                    worldX + 1, localY, worldZ,     // v2
                    worldX + 1, localY, worldZ + 1, // v3
                    worldX, localY, worldZ + 1,     // v4
                    texCoords, lightValue);
            }

            // Front face (Z+)
            if (isBlockTransparent(getBlockForMesh(localX, localY, localZ + 1, chunkManager)))
            {
                var texCoords = getTextureCoords(block, 0); // 0 = Side face index
                var lightLevel = getLightForMesh(localX, localY, localZ + 1, chunkManager);
                var lightValue = Math.Max(0.15f, lightLevel / 15.0f);
                addQuadVertices(vertices,
                    worldX, localY, worldZ + 1,         // v1
                    worldX + 1, localY, worldZ + 1,     // v2
                    worldX + 1, localY + 1, worldZ + 1, // v3
                    worldX, localY + 1, worldZ + 1,     // v4
                    texCoords, lightValue);
            }

            // Back face (Z-)
            if (isBlockTransparent(getBlockForMesh(localX, localY, localZ - 1, chunkManager)))
            {
                var texCoords = getTextureCoords(block, 1); // 1 = Side face index
                var lightLevel = getLightForMesh(localX, localY, localZ - 1, chunkManager);
                var lightValue = Math.Max(0.15f, lightLevel / 15.0f);
                addQuadVertices(vertices,
                    worldX + 1, localY, worldZ,     // v1
                    worldX, localY, worldZ,         // v2
                    worldX, localY + 1, worldZ,     // v3
                    worldX + 1, localY + 1, worldZ, // v4
                    texCoords, lightValue);
            }

            // Right face (X+)
            if (isBlockTransparent(getBlockForMesh(localX + 1, localY, localZ, chunkManager)))
            {
                var texCoords = getTextureCoords(block, 2); // 2 = Side face index
                var lightLevel = getLightForMesh(localX + 1, localY, localZ, chunkManager);
                var lightValue = Math.Max(0.15f, lightLevel / 15.0f);
                addQuadVertices(vertices,
                    worldX + 1, localY, worldZ + 1, // v1
                    worldX + 1, localY, worldZ,     // v2
                    worldX + 1, localY + 1, worldZ, // v3
                    worldX + 1, localY + 1, worldZ + 1, // v4
                    texCoords, lightValue);
            }

            // Left face (X-)
            if (isBlockTransparent(getBlockForMesh(localX - 1, localY, localZ, chunkManager)))
            {
                var texCoords = getTextureCoords(block, 3); // 3 = Side face index
                var lightLevel = getLightForMesh(localX - 1, localY, localZ, chunkManager);
                var lightValue = Math.Max(0.15f, lightLevel / 15.0f);
                addQuadVertices(vertices,
                    worldX, localY, worldZ,         // v1
                    worldX, localY, worldZ + 1,     // v2
                    worldX, localY + 1, worldZ + 1, // v3
                    worldX, localY + 1, worldZ,     // v4
                    texCoords, lightValue);
            }
        }

        // Helper struct for mesh generation bounds
        private struct MeshBounds
        {
            public float X1, X2, Y1, Y2, Z1, Z2;
            
            public MeshBounds(float x1, float x2, float y1, float y2, float z1, float z2)
            {
                X1 = x1; X2 = x2; Y1 = y1; Y2 = y2; Z1 = z1; Z2 = z2;
            }
        }

        // Common setup for custom mesh generation
        private (MeshBounds bounds, float lightValue, Vector2[] topCoords, Vector2[] sideCoords, Vector2[] bottomCoords) 
            setupCustomMesh(int localX, int localY, int localZ, ChunkManager chunkManager, byte blockId, 
                           float width, float height, float depth, float offsetX = 0f, float offsetZ = 0f)
        {
            int worldX = X * GameConstants.CHUNK_SIZE + localX;
            int worldZ = Z * GameConstants.CHUNK_SIZE + localZ;

            float x1 = worldX + offsetX;
            float x2 = worldX + offsetX + width;
            float y1 = localY;
            float y2 = localY + height;
            float z1 = worldZ + offsetZ;
            float z2 = worldZ + offsetZ + depth;

            var bounds = new MeshBounds(x1, x2, y1, y2, z1, z2);

            var lightLevel = getLightForMesh(localX, localY, localZ, chunkManager);
            var lightValue = Math.Max(0.15f, lightLevel / 15.0f);

            var topCoords = getTextureCoords(blockId, 4);
            var sideCoords = getTextureCoords(blockId, 0);
            var bottomCoords = getTextureCoords(blockId, 5);

            return (bounds, lightValue, topCoords, sideCoords, bottomCoords);
        }

        // Helper to add a cube mesh with specified bounds
        private void addCubeMesh(List<Vertex> vertices, MeshBounds bounds, float lightValue,
                                Vector2[] topCoords, Vector2[] sideCoords, Vector2[] bottomCoords,
                                int localX, int localY, int localZ, ChunkManager chunkManager,
                                bool alwaysShowTop = false)
        {
            // Top face
            if (alwaysShowTop || isBlockTransparent(getBlockForMesh(localX, localY + 1, localZ, chunkManager)))
            {
                addQuadVertices(vertices,
                    bounds.X1, bounds.Y2, bounds.Z2,
                    bounds.X2, bounds.Y2, bounds.Z2,
                    bounds.X2, bounds.Y2, bounds.Z1,
                    bounds.X1, bounds.Y2, bounds.Z1,
                    topCoords, lightValue);
            }

            // Bottom face
            if (isBlockTransparent(getBlockForMesh(localX, localY - 1, localZ, chunkManager)))
            {
                addQuadVertices(vertices,
                    bounds.X1, bounds.Y1, bounds.Z1,
                    bounds.X2, bounds.Y1, bounds.Z1,
                    bounds.X2, bounds.Y1, bounds.Z2,
                    bounds.X1, bounds.Y1, bounds.Z2,
                    bottomCoords, lightValue);
            }

            // Front face (Z+)
            if (isBlockTransparent(getBlockForMesh(localX, localY, localZ + 1, chunkManager)))
            {
                addQuadVertices(vertices,
                    bounds.X1, bounds.Y1, bounds.Z2,
                    bounds.X2, bounds.Y1, bounds.Z2,
                    bounds.X2, bounds.Y2, bounds.Z2,
                    bounds.X1, bounds.Y2, bounds.Z2,
                    sideCoords, lightValue);
            }

            // Back face (Z-)
            if (isBlockTransparent(getBlockForMesh(localX, localY, localZ - 1, chunkManager)))
            {
                addQuadVertices(vertices,
                    bounds.X2, bounds.Y1, bounds.Z1,
                    bounds.X1, bounds.Y1, bounds.Z1,
                    bounds.X1, bounds.Y2, bounds.Z1,
                    bounds.X2, bounds.Y2, bounds.Z1,
                    sideCoords, lightValue);
            }

            // Right face (X+)
            if (isBlockTransparent(getBlockForMesh(localX + 1, localY, localZ, chunkManager)))
            {
                addQuadVertices(vertices,
                    bounds.X2, bounds.Y1, bounds.Z2,
                    bounds.X2, bounds.Y1, bounds.Z1,
                    bounds.X2, bounds.Y2, bounds.Z1,
                    bounds.X2, bounds.Y2, bounds.Z2,
                    sideCoords, lightValue);
            }

            // Left face (X-)
            if (isBlockTransparent(getBlockForMesh(localX - 1, localY, localZ, chunkManager)))
            {
                addQuadVertices(vertices,
                    bounds.X1, bounds.Y1, bounds.Z1,
                    bounds.X1, bounds.Y1, bounds.Z2,
                    bounds.X1, bounds.Y2, bounds.Z2,
                    bounds.X1, bounds.Y2, bounds.Z1,
                    sideCoords, lightValue);
            }
        }

        private void addTorchMesh(List<Vertex> vertices, int localX, int localY, int localZ, ChunkManager chunkManager)
        {
            float torchWidth = 2.0f / 16.0f;
            float torchHeight = 9.0f / 16.0f;
            float torchDepth = 2.0f / 16.0f;
            
            float offsetX = (1.0f - torchWidth) / 2.0f;
            float offsetZ = (1.0f - torchDepth) / 2.0f;

            var (bounds, lightValue, topCoords, sideCoords, bottomCoords) = setupCustomMesh(localX, localY, localZ, chunkManager, BlockIDs.Torch, torchWidth, torchHeight, torchDepth, offsetX, offsetZ);

            addCubeMesh(vertices, bounds, lightValue, topCoords, sideCoords, bottomCoords, localX, localY, localZ, chunkManager, alwaysShowTop: true);
        }

        private void addSlabMesh(List<Vertex> vertices, int localX, int localY, int localZ, ChunkManager chunkManager)
        {
            var (bounds, lightValue, topCoords, sideCoords, bottomCoords) =  setupCustomMesh(localX, localY, localZ, chunkManager, BlockIDs.Slab, 1.0f, 0.5f, 1.0f, 0f, 0f);

            addCubeMesh(vertices, bounds, lightValue, topCoords, sideCoords, bottomCoords, localX, localY, localZ, chunkManager, alwaysShowTop: true);
        }

        private void addFlowerMesh(List<Vertex> vertices, int localX, int localY, int localZ, ChunkManager chunkManager)
        {
            int worldX = X * GameConstants.CHUNK_SIZE + localX;
            int worldZ = Z * GameConstants.CHUNK_SIZE + localZ;

            var lightLevel = getLightForMesh(localX, localY, localZ, chunkManager);
            var lightValue = Math.Max(0.15f, lightLevel / 15.0f);

            var texCoords = getTextureCoords(BlockIDs.Flower, 0);

            float centerX = worldX + 0.5f;
            float centerZ = worldZ + 0.5f;
            float y1 = localY;
            float y2 = localY + 1.0f;

            float halfSize = 0.45f;

            addFlowerCross(vertices, centerX, centerZ, y1, y2, halfSize, texCoords, lightValue);
        }

        private void addFlowerCross(List<Vertex> vertices, float centerX, float centerZ, float y1, float y2, 
                                   float halfSize, Vector2[] texCoords, float lightValue)
        {
            // NW-SE diagonal
            addQuadVertices(vertices,
                centerX - halfSize, y1, centerZ - halfSize,  // v1 (bottom NW)
                centerX + halfSize, y1, centerZ + halfSize,  // v2 (bottom SE)
                centerX + halfSize, y2, centerZ + halfSize,  // v3 (top SE)
                centerX - halfSize, y2, centerZ - halfSize,  // v4 (top NW)
                texCoords, lightValue);

            // Back side of first diagonal
            addQuadVertices(vertices,
                centerX + halfSize, y1, centerZ + halfSize,  // v1
                centerX - halfSize, y1, centerZ - halfSize,  // v2
                centerX - halfSize, y2, centerZ - halfSize,  // v3
                centerX + halfSize, y2, centerZ + halfSize,  // v4
                texCoords, lightValue);

            // NE-SW diagonal
            addQuadVertices(vertices,
                centerX + halfSize, y1, centerZ - halfSize,  // v1 (bottom NE)
                centerX - halfSize, y1, centerZ + halfSize,  // v2 (bottom SW)
                centerX - halfSize, y2, centerZ + halfSize,  // v3 (top SW)
                centerX + halfSize, y2, centerZ - halfSize,  // v4 (top NE)
                texCoords, lightValue);

            // Back side of second diagonal
            addQuadVertices(vertices,
                centerX - halfSize, y1, centerZ + halfSize,  // v1
                centerX + halfSize, y1, centerZ - halfSize,  // v2
                centerX + halfSize, y2, centerZ - halfSize,  // v3
                centerX - halfSize, y2, centerZ + halfSize,  // v4
                texCoords, lightValue);
        }

        private bool isBlockTransparent(byte blockId)
        {
            return blockId == BlockIDs.Air || blockId == BlockIDs.Torch || blockId == BlockIDs.Flower;
        }

        private byte getBlockForMesh(int localX, int localY, int localZ, ChunkManager chunkManager)
        {
            if (localY < 0 || localY >= GameConstants.CHUNK_HEIGHT)
                return BlockIDs.Air;

            if (localX >= 0 && localX < GameConstants.CHUNK_SIZE && localZ >= 0 && localZ < GameConstants.CHUNK_SIZE)
                return _mBlocks[localX, localY, localZ];

            return getNeighborBlock(localX, localY, localZ, chunkManager);
        }

        private void addQuadVertices(List<Vertex> vertices, float x1, float y1, float z1, float x2, float y2, float z2, float x3, float y3, float z3, float x4, float y4, float z4, Vector2[] texCoords, float lightValue)
        {
            var lightColor = new Vector3(lightValue, lightValue, lightValue);

            // First triangle (counter-clockwise)
            vertices.Add(new Vertex(new Vector3(x1, y1, z1), lightColor, texCoords[0]));
            vertices.Add(new Vertex(new Vector3(x2, y2, z2), lightColor, texCoords[1]));
            vertices.Add(new Vertex(new Vector3(x3, y3, z3), lightColor, texCoords[2]));

            // Second triangle (counter-clockwise)
            vertices.Add(new Vertex(new Vector3(x1, y1, z1), lightColor, texCoords[0]));
            vertices.Add(new Vertex(new Vector3(x3, y3, z3), lightColor, texCoords[2]));
            vertices.Add(new Vertex(new Vector3(x4, y4, z4), lightColor, texCoords[3]));
        }

        #region Lighting Methods
        // Gets light value for a position
        private byte getLightForMesh(int localX, int localY, int localZ, ChunkManager chunkManager)
        {
            if (localX < 0 || localX >= GameConstants.CHUNK_SIZE || localY < 0 || localY >= GameConstants.CHUNK_HEIGHT || localZ < 0 || localZ >= GameConstants.CHUNK_SIZE)
            {
                if (localY < 0)
                    return 0; // Below world = no light
                if (localY >= GameConstants.CHUNK_HEIGHT)
                    return 15; // Above world = full sky light

                int worldX = X * GameConstants.CHUNK_SIZE + localX;
                int worldZ = Z * GameConstants.CHUNK_SIZE + localZ;

                int targetChunkX = worldX >> 4;
                int targetChunkZ = worldZ >> 4;
                var targetChunk = chunkManager.GetChunk(targetChunkX, targetChunkZ);

                if (targetChunk?.State != ChunkState.Generated)
                    return 15;

                int targetLocalX = worldX & 15;
                int targetLocalZ = worldZ & 15;

                return targetChunk.GetBlockLightValue(targetLocalX, localY, targetLocalZ, 0);
            }

            return GetBlockLightValue(localX, localY, localZ, 0);
        }

        

        // Gets the stored light value for a specific light type
        public byte GetSavedLightValue(LightType lightType, int x, int y, int z)
        {
            if (x < 0 || x >= GameConstants.CHUNK_SIZE || y < 0 || y >= GameConstants.CHUNK_HEIGHT || z < 0 || z >= GameConstants.CHUNK_SIZE)
                return lightType == LightType.Sky ? (byte)15 : (byte)0;

            lock (_mLockObject)
            {
                return lightType == LightType.Sky ? _mSkyLightMap.GetNibble(x, y, z) : _mBlockLightMap.GetNibble(x, y, z);
            }
        }

        // Sets the light value for a specific light type
        public void SetLightValue(LightType lightType, int x, int y, int z, byte value)
        {
            if (x < 0 || x >= GameConstants.CHUNK_SIZE || y < 0 || y >= GameConstants.CHUNK_HEIGHT || z < 0 || z >= GameConstants.CHUNK_SIZE)
                return;

            lock (_mLockObject)
            {
                if (lightType == LightType.Sky)
                {
                    _mSkyLightMap.SetNibble(x, y, z, value);
                }
                else
                {
                    _mBlockLightMap.SetNibble(x, y, z, value);
                }
                mIsDirty = true;
                _needsMeshRebuild = true;
            }
        }

        // Gets the combined light value (sky + block light with day/night cycle)
        public byte GetBlockLightValue(int x, int y, int z, int skylightSubtracted)
        {
            if (x < 0 || x >= GameConstants.CHUNK_SIZE || y < 0 || y >= GameConstants.CHUNK_HEIGHT || z < 0 || z >= GameConstants.CHUNK_SIZE)
                return 15;

            lock (_mLockObject)
            {
                byte skyLight = _mSkyLightMap.GetNibble(x, y, z);

                skyLight = (byte)Math.Max(0, skyLight - skylightSubtracted);

                byte blockLight = _mBlockLightMap.GetNibble(x, y, z);

                return (byte)Math.Max(skyLight, (int)blockLight);
            }
        }

        // Checks if a block can see the sky
        public bool CanBlockSeeTheSky(int x, int y, int z)
        {
            if (x < 0 || x >= GameConstants.CHUNK_SIZE || z < 0 || z >= GameConstants.CHUNK_SIZE)
                return false;

            lock (_mLockObject)
            {
                int index = z * GameConstants.CHUNK_SIZE + x;
                return y >= _mHeightMap[index];
            }
        }

        // Updates the height map for sky light calculations
        public void UpdateHeightMap()
        {
            lock (_mLockObject)
            {
                int lowestHeight = GameConstants.CHUNK_HEIGHT;

                for (int x = 0; x < GameConstants.CHUNK_SIZE; x++)
                {
                    for (int z = 0; z < GameConstants.CHUNK_SIZE; z++)
                    {
                        int height = GameConstants.CHUNK_HEIGHT;

                        for (int y = GameConstants.CHUNK_HEIGHT - 1; y >= 0; y--)
                        {
                            byte blockId = _mBlocks[x, y, z];
                            var block = BlockRegistry.GetBlock(blockId);

                            if (block != null && block.LightOpacity > 0)
                            {
                                height = y + 1;
                                break;
                            }
                        }

                        int index = z * GameConstants.CHUNK_SIZE + x;
                        _mHeightMap[index] = (byte)height;

                        if (height < lowestHeight)
                            lowestHeight = height;
                    }
                }

                mLowestBlockHeight = lowestHeight;
                mIsDirty = true;
            }
        }

        // Init lighting for this chunk
        public void InitLighting()
        {
            lock (_mLockObject)
            {
                _mSkyLightMap.Clear();
                _mBlockLightMap.Clear();

                UpdateHeightMap();

                for (int x = 0; x < GameConstants.CHUNK_SIZE; x++)
                {
                    for (int z = 0; z < GameConstants.CHUNK_SIZE; z++)
                    {
                        int index = z * GameConstants.CHUNK_SIZE + x;
                        int topHeight = _mHeightMap[index];

                        for (int y = topHeight; y < GameConstants.CHUNK_HEIGHT; y++)
                        {
                            _mSkyLightMap.SetNibble(x, y, z, 15);
                        }

                        // Propagate skylight downward
                        byte currentSkyLight = 15;
                        for (int y = topHeight - 1; y >= 0; y--)
                        {
                            byte blockId = _mBlocks[x, y, z];
                            var block = BlockRegistry.GetBlock(blockId);

                            if (block != null && block.LightOpacity > 0)
                            {
                                currentSkyLight = (byte)Math.Max(0, currentSkyLight - block.LightOpacity);
                            }

                            if (currentSkyLight > 0)
                            {
                                _mSkyLightMap.SetNibble(x, y, z, currentSkyLight);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }

                // Init block lighting from light-emitting blocks
                for (int x = 0; x < GameConstants.CHUNK_SIZE; x++)
                {
                    for (int y = 0; y < GameConstants.CHUNK_HEIGHT; y++)
                    {
                        for (int z = 0; z < GameConstants.CHUNK_SIZE; z++)
                        {
                            byte blockId = _mBlocks[x, y, z];
                            var block = BlockRegistry.GetBlock(blockId);

                            if (block != null && block.LightValue > 0)
                            {
                                _mBlockLightMap.SetNibble(x, y, z, block.LightValue);
                            }
                        }
                    }
                }

                mIsDirty = true;
                _needsMeshRebuild = true;

            }
        }

        #endregion

        private Vector2[] getTextureCoords(byte voxelType, int faceIndex)
        {
            IBlock? block = BlockRegistry.GetBlock(voxelType);
            if (block == null)
            {
                var defaultCoords = UVHelper.FromTileCoords(0, 0);
                return
                [defaultCoords.TopLeft,
                new Vector2(defaultCoords.BottomRight.X, defaultCoords.TopLeft.Y),
                defaultCoords.BottomRight,
                new Vector2(defaultCoords.TopLeft.X, defaultCoords.BottomRight.Y)];
            }

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
            [coords.TopLeft,
            new Vector2(coords.BottomRight.X, coords.TopLeft.Y),
            coords.BottomRight,
            new Vector2(coords.TopLeft.X, coords.BottomRight.Y)];
        }
    }

    public enum ChunkState
    {
        Empty,
        Generating,
        Generated,
        Unloading
    }
}