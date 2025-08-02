// Main chunk class. Holds stuff like the blocks in the chunk and has the functions that allows for chunk re-generation. Also has stuff related to OpenGL. | DA | 8/1/25
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using VoxelGame.Blocks;
using VoxelGame.Utils;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace VoxelGame.World
{
    public class Chunk : IDisposable
    {
        public ChunkPos Position { get; }
        public byte[,,] Voxels { get; set; }
        public List<Vertex>? Vertices { get; private set; }
        public List<uint>? Indices { get; private set; }

        public int VAO { get; private set; }
        public int VBO { get; private set; }
        public int EBO { get; private set; }

        private int mIndexCount = 0;

        public bool MeshGenerated;
        public bool MeshUploaded;
        public bool TerrainGenerated;
        public bool Modified = false;

        private bool mDisposed = false;

        public byte Biome;

        private Vector3 mChunkWorldOffset = new Vector3();

        private Vector3[] mFaceNormals =
        [
            new Vector3(0, 0, 1), new Vector3(0, 0, -1), new Vector3(-1, 0, 0),
            new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, -1, 0)
        ];

        private Vector3[] mFlowerNormals =
        [
            new Vector3(0.707f, 0, 0.707f),
            new Vector3(-0.707f, 0, -0.707f),
            new Vector3(-0.707f, 0, 0.707f),
            new Vector3(0.707f, 0, -0.707f)
        ];

        private bool mOpenGLMade = false;

        public Chunk(ChunkPos position, bool generateTerrain = true)
        {
            Position = position;
            Voxels = new byte[Constants.CHUNK_SIZE, Constants.CHUNK_HEIGHT, Constants.CHUNK_SIZE];

            Vertices = null;
            Indices = null;

            if (generateTerrain)
            {
                generateTerrainSync();
                TerrainGenerated = true;
            }
            else
            {
                TerrainGenerated = false;
            }

            makeOpenGLStuff();

            mChunkWorldOffset = new Vector3(
                Position.X * Constants.CHUNK_SIZE,
                0,
                Position.Z * Constants.CHUNK_SIZE
            );
        }

        private void makeOpenGLStuff()
        {
            if (!mOpenGLMade)
            {
                VAO = GL.GenVertexArray();
                VBO = GL.GenBuffer();
                EBO = GL.GenBuffer();
                mOpenGLMade = true;
            }
        }

        private void generateTerrainSync()
        {
            VoxelGame.init.TerrainGen.GenerateTerrain(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool isVoxelSolid(int x, int y, int z, ChunkManager chunkManager)
        {
            // Fast path for within chunk bounds
            if (x >= 0 && x < Constants.CHUNK_SIZE && y >= 0 && y < Constants.CHUNK_HEIGHT && z >= 0 && z < Constants.CHUNK_SIZE)
            {
                return BlockRegistry.GetBlock(Voxels[x, y, z]).IsSolid;
            }

            // If outside Y bounds, consider as air
            if (y < 0 || y >= Constants.CHUNK_HEIGHT)
            {
                return false;
            }

            ChunkPos neighborPos = Position;
            int neighborX = x;
            int neighborZ = z;

            if (x < 0)
            {
                neighborPos = new ChunkPos(Position.X - 1, Position.Z);
                neighborX = Constants.CHUNK_SIZE - 1;
            }
            else if (x >= Constants.CHUNK_SIZE)
            {
                neighborPos = new ChunkPos(Position.X + 1, Position.Z);
                neighborX = 0;
            }

            if (z < 0)
            {
                neighborPos = new ChunkPos(neighborPos.X, Position.Z - 1);
                neighborZ = Constants.CHUNK_SIZE - 1;
            }
            else if (z >= Constants.CHUNK_SIZE)
            {
                neighborPos = new ChunkPos(neighborPos.X, Position.Z + 1);
                neighborZ = 0;
            }

            Chunk? neighborChunk = chunkManager.GetChunk(neighborPos);
            if (neighborChunk != null && neighborChunk.TerrainGenerated)
            {
                return BlockRegistry.GetBlock(neighborChunk.Voxels[neighborX, y, neighborZ]).IsSolid;
            }

            return false;
        }

        // Update gravity blocks when the chunk is updated. Is this function convoluted and inefficient, yes. Does it work consistently, yes. So it stays for now
        private void updateGravityBlocks(ChunkManager manager)
        {
            bool blocksChanged = false;

            for (int y = Constants.CHUNK_HEIGHT - 1; y >= 0; y--)
            {
                for (int x = 0; x < Constants.CHUNK_SIZE; x++)
                {
                    for (int z = 0; z < Constants.CHUNK_SIZE; z++)
                    {
                        byte blockType = Voxels[x, y, z];
                        
                        if(blockType == BlockIDs.Air || !BlockRegistry.GetBlock(blockType).GravityBlock)
                            continue;

                        if (!isVoxelSolid(x, y - 1, z, manager))
                        {
                            int landingY = y;
                            for (int checkY = y - 1; checkY >= 0; checkY--)
                            {
                                if (isVoxelSolid(x, checkY, z, manager))
                                {
                                    landingY = checkY + 1;
                                    break;
                                }
                                landingY = 0;
                            }

                            if (landingY < y)
                            {
                                Voxels[x, y, z] = BlockIDs.Air;
                                Voxels[x, landingY, z] = blockType;
                                blocksChanged = true;
                                Modified = true;
                            }
                        }
                    }
                }
            }

            if (blocksChanged)
                MeshGenerated = false;
        }

        public void GenMesh(ChunkManager chunkManager)
        {
            if (mDisposed || !TerrainGenerated)
                return;
            
            updateGravityBlocks(chunkManager);

            bool gotPooledVertex = false;
            bool gotPooledIndex = false;

            var newVertices = chunkManager.GetVertexList();
            if (newVertices.Capacity > 0) gotPooledVertex = true;

            var newIndices = chunkManager.GetIndexList();
            if (newIndices.Capacity > 0) 
                gotPooledIndex = true;

            uint vertexIndex = 0;

            for (int x = 0; x < Constants.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Constants.CHUNK_HEIGHT; y++)
                {
                    for (int z = 0; z < Constants.CHUNK_SIZE; z++)
                    {
                        if (Voxels[x, y, z] == BlockIDs.Air) continue;

                        bool showFront = !isVoxelSolid(x, y, z + 1, chunkManager);
                        bool showBack = !isVoxelSolid(x, y, z - 1, chunkManager);
                        bool showLeft = !isVoxelSolid(x - 1, y, z, chunkManager);
                        bool showRight = !isVoxelSolid(x + 1, y, z, chunkManager);
                        bool showTop = !isVoxelSolid(x, y + 1, z, chunkManager);
                        bool showBottom = !isVoxelSolid(x, y - 1, z, chunkManager);

                        bool[] showFace = { showFront, showBack, showLeft, showRight, showTop, showBottom };

                        Vector3[,] blockFace = GetFace(Voxels[x, y, z]);
                        int faceCount = blockFace.GetLength(0);

                        for (int face = 0; face < faceCount; face++)
                        {
                            if (Voxels[x, y, z] != BlockIDs.YellowFlower && !showFace[face]) continue;

                            Vector2[] texCoords = getTextureCoords(Voxels[x, y, z], face);
                            Vector3 normal = (Voxels[x, y, z] == BlockIDs.YellowFlower) ? mFlowerNormals[face] : mFaceNormals[face];

                            for (int v = 0; v < 4; v++)
                            {
                                Vector3 localPosition = new Vector3(x, y, z) + blockFace[face, v];
                                Vector3 worldPosition = localPosition + mChunkWorldOffset;

                                float lightValue = (Voxels[x, y, z] == BlockIDs.YellowFlower) ? 1.0f : getFaceLighting(x, y, z, face, chunkManager);

                                Vertex vertex = new Vertex(worldPosition, normal, texCoords[v], (float)Voxels[x, y, z], lightValue);
                                newVertices.Add(vertex);
                            }

                            newIndices.Add(vertexIndex);
                            newIndices.Add(vertexIndex + 1);
                            newIndices.Add(vertexIndex + 2);
                            newIndices.Add(vertexIndex);
                            newIndices.Add(vertexIndex + 2);
                            newIndices.Add(vertexIndex + 3);

                            if (Voxels[x, y, z] == BlockIDs.YellowFlower)
                            {
                                newIndices.Add(vertexIndex);
                                newIndices.Add(vertexIndex + 3);
                                newIndices.Add(vertexIndex + 2);
                                newIndices.Add(vertexIndex);
                                newIndices.Add(vertexIndex + 2);
                                newIndices.Add(vertexIndex + 1);
                            }

                            vertexIndex += 4;
                        }
                    }
                }
            }

            // Return old lists to pool before replacing
            if (Vertices != null)
            {
                chunkManager.ReturnVertexList(Vertices);
                Vertices = null;
            }
            if (Indices != null)
            {
                chunkManager.ReturnIndexList(Indices);
                Indices = null;
            }

            Vertices = newVertices;
            Indices = newIndices;
            MeshGenerated = true;
            MeshUploaded = false;
        }

        public void UploadMesh(ChunkManager chunkManager)
        {
            if (mDisposed || !MeshGenerated || !TerrainGenerated || Vertices == null || Vertices.Count == 0)
                return;

            try
            {
                // Clean up old buffers
                if (MeshUploaded && mOpenGLMade)
                {
                    GL.DeleteVertexArray(VAO);
                    GL.DeleteBuffer(VBO);
                    GL.DeleteBuffer(EBO);
                    MeshUploaded = false;
                    mOpenGLMade = false;
                }

                // Make new buffers
                if (!mOpenGLMade)
                {
                    VAO = GL.GenVertexArray();
                    VBO = GL.GenBuffer();
                    EBO = GL.GenBuffer();
                    mOpenGLMade = true;
                }

                GL.BindVertexArray(VAO);

                var vertexArray = Vertices.ToArray();
                var indexArray = Indices.ToArray();

                // Vertex data
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
                GL.BufferData(BufferTarget.ArrayBuffer,
                    vertexArray.Length * System.Runtime.InteropServices.Marshal.SizeOf<Vertex>(),
                    vertexArray, BufferUsageHint.StaticDraw);

                // Index data
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
                GL.BufferData(BufferTarget.ElementArrayBuffer,
                    indexArray.Length * sizeof(uint),
                    indexArray, BufferUsageHint.StaticDraw);

                // Vertex attributes
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false,
                    System.Runtime.InteropServices.Marshal.SizeOf<Vertex>(), 0);
                GL.EnableVertexAttribArray(0);

                // Normal
                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false,
                    System.Runtime.InteropServices.Marshal.SizeOf<Vertex>(),
                    System.Runtime.InteropServices.Marshal.OffsetOf<Vertex>(nameof(Vertex.Normal)));
                GL.EnableVertexAttribArray(1);

                // Texture coordinates
                GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false,
                    System.Runtime.InteropServices.Marshal.SizeOf<Vertex>(),
                    System.Runtime.InteropServices.Marshal.OffsetOf<Vertex>(nameof(Vertex.TexCoord)));
                GL.EnableVertexAttribArray(2);

                // Texture ID
                GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false,
                    System.Runtime.InteropServices.Marshal.SizeOf<Vertex>(),
                    System.Runtime.InteropServices.Marshal.OffsetOf<Vertex>(nameof(Vertex.TextureID)));
                GL.EnableVertexAttribArray(3);

                // Lighting
                GL.VertexAttribPointer(4, 1, VertexAttribPointerType.Float, false,
                    System.Runtime.InteropServices.Marshal.SizeOf<Vertex>(),
                    System.Runtime.InteropServices.Marshal.OffsetOf<Vertex>(nameof(Vertex.LightValue)));
                GL.EnableVertexAttribArray(4);

                GL.BindVertexArray(0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

                MeshUploaded = true;

                mIndexCount = Indices?.Count ?? 0;

                // Clear the CPU lists
                if (Vertices != null)
                {
                    chunkManager.ReturnVertexList(Vertices);
                    Vertices = null;
                }
                if (Indices != null)
                {
                    chunkManager.ReturnIndexList(Indices);
                    Indices = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading mesh for chunk {Position}: {ex.Message}");
                MeshUploaded = false;
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

        public void Render()
        {
            if (!MeshUploaded || !TerrainGenerated || mIndexCount == 0 || mDisposed) return;

            GL.BindVertexArray(VAO);
            GL.DrawElements(PrimitiveType.Triangles, mIndexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (mDisposed)
                return;

            if (disposing)
            {
                // Return lists to pool
                if (Vertices != null && VoxelGame.init?._ChunkManager != null)
                {
                    Voxels = null;
                }

                // Clean up OpenGL resources
                if (mOpenGLMade)
                {
                    try
                    {
                        GL.DeleteBuffer(VBO);
                        GL.DeleteBuffer(EBO);
                        GL.DeleteVertexArray(VAO);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error disposing GL resources for chunk at {Position}: {ex.Message}");
                    }
                    finally
                    {
                        mOpenGLMade = false;
                        MeshUploaded = false;
                        VAO = VBO = EBO = 0;
                    }
                }

                mDisposed = true;

                mIndexCount = 0;
            }
        }

        public bool IsInBounds(Vector3i pos) =>
           pos.X >= 0 && pos.X < Constants.CHUNK_SIZE &&
           pos.Y >= 0 && pos.Y < Constants.CHUNK_HEIGHT &&
           pos.Z >= 0 && pos.Z < Constants.CHUNK_SIZE;

        public byte GetBlock(Vector3i position)
        {
            return Voxels[position.X, position.Y, position.Z];
        }

        public Vector3[,] GetFace(byte block)
        {
            if (block == BlockIDs.Slab)
            {
                return new Vector3[6, 4]
                {
                    // Front face
                    { new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, .5f, 1), new Vector3(0, .5f, 1) },
                    // Back face
                    { new Vector3(1, 0, 0), new Vector3(0, 0, 0), new Vector3(0, .5f, 0), new Vector3(1, .5f, 0) },
                    // Left face
                    { new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, .5f, 1), new Vector3(0, .5f, 0) },
                    // Right face
                    { new Vector3(1, 0, 1), new Vector3(1, 0, 0), new Vector3(1, .5f, 0), new Vector3(1, .5f, 1) },
                    // Top face
                    { new Vector3(0, .5f, 1), new Vector3(1, .5f, 1), new Vector3(1, .5f, 0), new Vector3(0, .5f, 0) },
                    // Bottom face
                    { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1) }
                };
            }
            else if (block == BlockIDs.YellowFlower)
            {
                return new Vector3[2, 4]
                {
                    // First diagonal
                    { new Vector3(0, 0, 0), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 0) },
                    // Second diagonal  
                    { new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(1, 1, 0) }
                };
            }
            else
            {
                return new Vector3[6, 4]
                {
                    // Front face
                    { new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1) },
                    // Back face
                    { new Vector3(1, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0) },
                    // Left face
                    { new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0) },
                    // Right face
                    { new Vector3(1, 0, 1), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1) },
                    // Top face
                    { new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0), new Vector3(0, 1, 0) },
                    // Bottom face
                    { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1) }
                };
            }
        }

        #region Lighting
        private float getFaceLighting(int x, int y, int z, int face, ChunkManager chunkManager)
        {
            float baseLighting = .8f;

            int checkX = x, checkY = y, checkZ = z;

            switch (face)
            {
                case 0: // Front face
                    checkZ += 1; 
                    break; 
                case 1: // Back face
                    checkZ -= 1; 
                    break; 
                case 2: // Left face
                    checkX -= 1; 
                    break; 
                case 3: // Right face
                    checkX += 1; 
                    break; 
                case 4: // Top face
                    break;              
                case 5: // Bottom face
                    checkY -= 1; 
                    break; 
            }
            return canSeeSun(checkX, checkY, checkZ, chunkManager) ? baseLighting : .4f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool canSeeSun(int x, int y, int z, ChunkManager manager)
        {

            for (int checkY = y + 1; checkY < Constants.CHUNK_HEIGHT; checkY++)
            {
                if (x >= 0 && x < Constants.CHUNK_SIZE && z >= 0 && z < Constants.CHUNK_SIZE)
                {
                    if (BlockRegistry.GetBlock(Voxels[x, checkY, z]).IsSolid)
                    {
                        return false;
                    }
                }
                else
                {
                    ChunkPos neighborPos = Position;
                    int neighborX = x;
                    int neighborZ = z;

                    if (x < 0)
                    {
                        neighborPos = new ChunkPos(Position.X - 1, Position.Z);
                        neighborX = Constants.CHUNK_SIZE - 1;
                    }
                    else if (x >= Constants.CHUNK_SIZE)
                    {
                        neighborPos = new ChunkPos(Position.X + 1, Position.Z);
                        neighborX = 0;
                    }

                    if (z < 0)
                    {
                        neighborPos = new ChunkPos(neighborPos.X, Position.Z - 1);
                        neighborZ = Constants.CHUNK_SIZE - 1;
                    }
                    else if (z >= Constants.CHUNK_SIZE)
                    {
                        neighborPos = new ChunkPos(neighborPos.X, Position.Z + 1);
                        neighborZ = 0;
                    }

                    Chunk? neighborChunk = manager.GetChunk(neighborPos);
                    if (neighborChunk != null && neighborChunk.TerrainGenerated)
                    {
                        if (BlockRegistry.GetBlock(neighborChunk.Voxels[neighborX, checkY, neighborZ]).IsSolid)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
        #endregion

        #region Frustum stuff
        public Vector3 WorldPosition
        {
            get
            {
                return new Vector3(
                    Position.X * Constants.CHUNK_SIZE,
                    0,
                    Position.Z * Constants.CHUNK_SIZE
                );
            }
        }

        public Vector3 BoundingBoxMin
        {
            get { return WorldPosition; }
        }
        public Vector3 BoundingBoxMax
        {
            get
            {
                return WorldPosition + new Vector3(
                    Constants.CHUNK_SIZE,
                    Constants.CHUNK_HEIGHT,
                    Constants.CHUNK_SIZE
                );
            }
        }

        public bool IsInFrustum(Frustum frustum)
        {
            return frustum.IsBoxInFrustum(BoundingBoxMin, BoundingBoxMax);
        }
        #endregion
    }
}