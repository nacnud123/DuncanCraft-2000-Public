// This is the main chunk class, holds stuff like the blocks found in the chunk, the verticies and stuff related to OpenGL, and some lighting data. | DA | 8/25/25
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using VoxelGame.Blocks;
using VoxelGame.Utils;
using VoxelGame.Lighting;
using System.Runtime.CompilerServices;

namespace VoxelGame.World
{
    public class Chunk : IDisposable
    {
        public ChunkPos Position { get; }
        public byte[,,] Voxels { get; set; }

        public byte[,,] SunlightLevels { get; set; }
        public byte[,,] BlockLightLevels { get; set; }

        public List<Vertex>? Vertices { get; private set; }
        public List<uint>? Indices { get; private set; }

        public int VAO { get; private set; }
        public int VBO { get; private set; }
        public int EBO { get; private set; }

        private int mIndexCount = 0;
        private int mCurrentVBOSize = 0;
        private int mCurrentEBOSize = 0;
        private bool mVertexAttribsSet = false;

        public bool MeshGenerated;
        public bool MeshUploaded;
        public bool TerrainGenerated;
        public bool Modified = false;

        private bool mDisposed = false;

        public byte Biome;

        private Vector3 mChunkWorldOffset = new Vector3();

        private ChunkMeshGenerator mMeshGenerator;

        private bool mOpenGLMade = false;

        public Chunk(ChunkPos position, bool generateTerrain = true)
        {
            Position = position;
            Voxels = new byte[Constants.CHUNK_SIZE, Constants.CHUNK_HEIGHT, Constants.CHUNK_SIZE];
            SunlightLevels = new byte[Constants.CHUNK_SIZE, Constants.CHUNK_HEIGHT, Constants.CHUNK_SIZE];
            BlockLightLevels = new byte[Constants.CHUNK_SIZE, Constants.CHUNK_HEIGHT, Constants.CHUNK_SIZE];

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
            if (x >= 0 && x < Constants.CHUNK_SIZE && y >= 0 && y < Constants.CHUNK_HEIGHT && z >= 0 && z < Constants.CHUNK_SIZE)
            {
                return BlockRegistry.GetBlock(Voxels[x, y, z]).IsSolid;
            }

            // If outside Y bounds, consider as air
            if (y < 0 || y >= Constants.CHUNK_HEIGHT)
            {
                return false;
            }

            // Calculate world position for cross-chunk lookup
            Vector3i worldPos = new Vector3i(
                Position.X * Constants.CHUNK_SIZE + x,
                y,
                Position.Z * Constants.CHUNK_SIZE + z
            );

            // Get the chunk containing this world position
            ChunkPos targetChunkPos = new ChunkPos(
                (int)Math.Floor(worldPos.X / (float)Constants.CHUNK_SIZE),
                (int)Math.Floor(worldPos.Z / (float)Constants.CHUNK_SIZE)
            );

            Chunk targetChunk = chunkManager.GetChunk(targetChunkPos);
            if (targetChunk != null && targetChunk.TerrainGenerated)
            {
                Vector3i localPos = new Vector3i(
                    worldPos.X - targetChunkPos.X * Constants.CHUNK_SIZE,
                    worldPos.Y,
                    worldPos.Z - targetChunkPos.Z * Constants.CHUNK_SIZE
                );

                if (localPos.X < 0) localPos.X += Constants.CHUNK_SIZE;
                if (localPos.Z < 0) localPos.Z += Constants.CHUNK_SIZE;

                if (targetChunk.IsInBounds(localPos))
                {
                    return BlockRegistry.GetBlock(targetChunk.Voxels[localPos.X, localPos.Y, localPos.Z]).IsSolid;
                }
            }

            return false;
        }

        // Update gravity blocks when the chunk is updated. Is this function convoluted and inefficient, yes. Does it work consistently, yes. So it stays for now
        private void updateGravityBlocks(ChunkManager manager)
        {
            bool blocksChanged = false;
            bool hadChanges;

            do
            {
                hadChanges = false;

                for (int y = 1; y < Constants.CHUNK_HEIGHT; y++)
                {
                    for (int x = 0; x < Constants.CHUNK_SIZE; x++)
                    {
                        for (int z = 0; z < Constants.CHUNK_SIZE; z++)
                        {
                            byte blockType = Voxels[x, y, z];

                            if (blockType == BlockIDs.Air || !BlockRegistry.GetBlock(blockType).GravityBlock)
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
                                    // Move the block
                                    Voxels[x, y, z] = BlockIDs.Air;
                                    Voxels[x, landingY, z] = blockType;

                                    hadChanges = true;
                                    blocksChanged = true;

                                    Modified = true;
                                }
                            }
                        }
                    }
                }
            } while (hadChanges);

            if (blocksChanged)
                MeshGenerated = false;
        }

        public void GenMesh(ChunkManager chunkManager)
        {
            if (mDisposed || !TerrainGenerated)
                return;

            lock (this)
            {
                if (MeshGenerated)
                    return;

                updateGravityBlocks(chunkManager);

                // Initialize mesh generator if needed
                if (mMeshGenerator == null)
                {
                    mMeshGenerator = new ChunkMeshGenerator(chunkManager);
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

                // Generate new mesh
                (Vertices, Indices) = mMeshGenerator.GenerateMesh(this, chunkManager);
                MeshGenerated = true;
                MeshUploaded = false;
            }
        }

        public void UploadMesh(ChunkManager chunkManager)
        {
            if (mDisposed)
                return;

            List<Vertex> localVertices;
            List<uint> localIndices;

            lock (this)
            {
                if (!MeshGenerated || !TerrainGenerated || MeshUploaded ||
                    Vertices == null || Indices == null ||
                    Vertices.Count == 0 || Indices.Count == 0)
                {
                    return;
                }

                localVertices = new List<Vertex>(Vertices);
                localIndices = new List<uint>(Indices);
            }

            try
            {
                if (!mOpenGLMade)
                {
                    VAO = GL.GenVertexArray();
                    VBO = GL.GenBuffer();
                    EBO = GL.GenBuffer();
                    mOpenGLMade = true;
                }

                GL.BindVertexArray(VAO);

                var vertexArray = localVertices.ToArray();
                var indexArray = localIndices.ToArray();

                if (vertexArray.Length == 0 || indexArray.Length == 0)
                {
                    Console.WriteLine($"Warning: Empty mesh data for chunk {Position}");
                    MeshUploaded = true;
                    mIndexCount = 0;
                    return;
                }

                mIndexCount = indexArray.Length;

                int vertexDataSize = vertexArray.Length * System.Runtime.InteropServices.Marshal.SizeOf<Vertex>();
                int indexDataSize = indexArray.Length * sizeof(uint);

                // Vertex data
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
                if (vertexDataSize > mCurrentVBOSize)
                {
                    GL.BufferData(BufferTarget.ArrayBuffer, vertexDataSize, vertexArray, BufferUsageHint.DynamicDraw);
                    mCurrentVBOSize = vertexDataSize;
                }
                else
                {
                    GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertexDataSize, vertexArray);
                }

                // Index data
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
                if (indexDataSize > mCurrentEBOSize)
                {
                    GL.BufferData(BufferTarget.ElementArrayBuffer, indexDataSize, indexArray, BufferUsageHint.DynamicDraw);
                    mCurrentEBOSize = indexDataSize;
                }
                else
                {
                    GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, indexDataSize, indexArray);
                }

                // Set vertex attributes only once
                if (!mVertexAttribsSet)
                {
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

                    mVertexAttribsSet = true;
                }

                GL.BindVertexArray(0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

                lock (this)
                {
                    MeshUploaded = true;

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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading mesh for chunk {Position}: {ex.Message}");
                Console.WriteLine($"Vertex count: {localVertices?.Count ?? 0}, Index count: {localIndices?.Count ?? 0}");

                lock (this)
                {
                    MeshUploaded = false;
                }
            }
        }

        public void Render()
        {
            if (!MeshUploaded || !TerrainGenerated || mIndexCount == 0 || mDisposed) 
                return;

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
                // Clean up voxel and lighting data
                Voxels = null;
                SunlightLevels = null;
                BlockLightLevels = null;

                // Return lists to pool
                if (Vertices != null && VoxelGame.init?._ChunkManager != null)
                {
                    VoxelGame.init._ChunkManager.ReturnVertexList(Vertices);
                    Vertices = null;
                }

                if (Indices != null && VoxelGame.init?._ChunkManager != null)
                {
                    VoxelGame.init._ChunkManager.ReturnIndexList(Indices);
                    Indices = null;
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

        public bool IsMeshReadyForUpload()
        {
            lock (this)
            {
                return MeshGenerated && !MeshUploaded && TerrainGenerated && Vertices != null && Indices != null && Vertices.Count > 0 && Indices.Count > 0;
            }
        }
    }
}