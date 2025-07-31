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
        public List<Vertex> Vertices { get; private set; }
        public List<uint> Indices { get; private set; }

        public int VAO { get; private set; }
        public int VBO { get; private set; }
        public int EBO { get; private set; }

        private int indexCount = 0;

        public bool MeshGenerated { get; set; }
        public bool MeshUploaded { get; set; }

        private bool disposed = false;
        public bool Modified = false;

        public byte biome;

        Vector3 chunkWorldOffset = new Vector3();

        Vector3[] faceNormals =
        [
            new Vector3(0, 0, 1), new Vector3(0, 0, -1), new Vector3(-1, 0, 0),
            new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, -1, 0)
        ];

        Vector3[] flowerNormals =
        [
            new Vector3(0.707f, 0, 0.707f),
            new Vector3(-0.707f, 0, -0.707f),
            new Vector3(-0.707f, 0, 0.707f),
            new Vector3(0.707f, 0, -0.707f)
        ];

        private bool openGLMade = false;

        public Chunk(ChunkPos position)
        {
            Position = position;
            Voxels = new byte[Constants.CHUNK_SIZE, Constants.CHUNK_HEIGHT, Constants.CHUNK_SIZE];

            Vertices = null;
            Indices = null;

            generateTerrain();
            makeOpenGLStuff();

            chunkWorldOffset = new Vector3(
                Position.X * Constants.CHUNK_SIZE,
                0,
                Position.Z * Constants.CHUNK_SIZE
            );
        }

        private void makeOpenGLStuff()
        {
            if (!openGLMade)
            {
                VAO = GL.GenVertexArray();
                VBO = GL.GenBuffer();
                EBO = GL.GenBuffer();
                openGLMade = true;
            }
        }

        private void generateTerrain()
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
            if (neighborChunk != null)
            {
                return BlockRegistry.GetBlock(neighborChunk.Voxels[neighborX, y, neighborZ]).IsSolid;
            }

            return false;
        }

        public void GenMesh(ChunkManager chunkManager)
        {
            if (disposed)
                return;

            bool gotPooledVertex = false;
            bool gotPooledIndex = false;

            var newVertices = chunkManager.GetVertexList();
            if (newVertices.Capacity > 0) gotPooledVertex = true;

            var newIndices = chunkManager.GetIndexList();
            if (newIndices.Capacity > 0) gotPooledIndex = true;

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

                        Vector3[,] blockFace = getFace(Voxels[x, y, z]);
                        int faceCount = blockFace.GetLength(0);

                        for (int face = 0; face < faceCount; face++)
                        {
                            if (Voxels[x, y, z] != BlockIDs.YellowFlower && !showFace[face]) continue;

                            Vector2[] texCoords = GetTextureCoords(Voxels[x, y, z], face);
                            Vector3 normal = (Voxels[x, y, z] == BlockIDs.YellowFlower) ? flowerNormals[face] : faceNormals[face];

                            for (int v = 0; v < 4; v++)
                            {
                                Vector3 localPosition = new Vector3(x, y, z) + blockFace[face, v];
                                Vector3 worldPosition = localPosition + chunkWorldOffset;

                                float lightValue = (Voxels[x, y, z] == BlockIDs.YellowFlower) ? 1.0f : GetFaceLightValue(x, y, z, face, chunkManager);

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
            if (disposed || !MeshGenerated || Vertices == null || Vertices.Count == 0)
                return;

            try
            {
                // Clean up old buffers
                if (MeshUploaded && openGLMade)
                {
                    GL.DeleteVertexArray(VAO);
                    GL.DeleteBuffer(VBO);
                    GL.DeleteBuffer(EBO);
                    MeshUploaded = false;
                    openGLMade = false;
                }

                // Make new buffers
                if (!openGLMade)
                {
                    VAO = GL.GenVertexArray();
                    VBO = GL.GenBuffer();
                    EBO = GL.GenBuffer();
                    openGLMade = true;
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

                GL.VertexAttribPointer(4, 1, VertexAttribPointerType.Float, false,
                    System.Runtime.InteropServices.Marshal.SizeOf<Vertex>(),
                    System.Runtime.InteropServices.Marshal.OffsetOf<Vertex>(nameof(Vertex.LightValue)));
                GL.EnableVertexAttribArray(4);

                GL.BindVertexArray(0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

                MeshUploaded = true;

                indexCount = Indices?.Count ?? 0;

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

        private Vector2[] GetTextureCoords(byte voxelType, int faceIndex)
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

            return new Vector2[]
            {
                coords.TopLeft,
                new Vector2(coords.BottomRight.X, coords.TopLeft.Y),
                coords.BottomRight,
                new Vector2(coords.TopLeft.X, coords.BottomRight.Y)
            };
        }

        public void Render()
        {
            if (!MeshUploaded || indexCount == 0 || disposed) return;

            GL.BindVertexArray(VAO);
            GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Return lists to pool
                if (Vertices != null && VoxelGame.init?.chunkManager != null)
                {
                    VoxelGame.init.chunkManager.ReturnVertexList(Vertices);
                    Vertices = null;
                }

                if (Indices != null && VoxelGame.init?.chunkManager != null)
                {
                    VoxelGame.init.chunkManager.ReturnIndexList(Indices);
                    Indices = null;
                }

                Voxels = null;
            }

            // Clean up OpenGL resources
            if (openGLMade)
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
                    openGLMade = false;
                    MeshUploaded = false;
                    VAO = VBO = EBO = 0;
                }
            }

            disposed = true;

            indexCount = 0;
        }

        public bool IsInBounds(Vector3i pos) =>
           pos.X >= 0 && pos.X < Constants.CHUNK_SIZE &&
           pos.Y >= 0 && pos.Y < Constants.CHUNK_HEIGHT &&
           pos.Z >= 0 && pos.Z < Constants.CHUNK_SIZE;

        public byte GetBlock(Vector3i position)
        {
            return Voxels[position.X, position.Y, position.Z];
        }

        public Vector3[,] getFace(byte block)
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
        private float GetFaceLightValue(int x, int y, int z, int face, ChunkManager chunkManager)
        {
            float baseLighting = .8f;

            int checkX = x, checkY = y, checkZ = z;

            switch (face)
            {
                case 0: checkZ += 1; break; // Front face
                case 1: checkZ -= 1; break; // Back face
                case 2: checkX -= 1; break; // Left face
                case 3: checkX += 1; break; // Right face
                case 4: break;              // Top face
                case 5: checkY -= 1; break; // Bottom face
            }
            return CanSeeSunFast(checkX, checkY, checkZ, chunkManager) ? baseLighting : .4f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanSeeSunFast(int x, int y, int z, ChunkManager manager)
        {

            for (int checkY = y + 1; checkY < Constants.CHUNK_HEIGHT; checkY++)
            {
                if( x >= 0 && x < Constants.CHUNK_SIZE && z >= 0 && z < Constants.CHUNK_SIZE)
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
                    if (neighborChunk != null)
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