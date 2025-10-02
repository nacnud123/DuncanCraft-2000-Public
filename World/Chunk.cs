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

        private bool mOpenGLMade = false;

        public Chunk(ChunkPos position, bool generateTerrain = true)
        {
            Position = position;
            Voxels = new byte[GameConstants.CHUNK_SIZE, GameConstants.CHUNK_HEIGHT, GameConstants.CHUNK_SIZE];
            SunlightLevels = new byte[GameConstants.CHUNK_SIZE, GameConstants.CHUNK_HEIGHT, GameConstants.CHUNK_SIZE];
            BlockLightLevels = new byte[GameConstants.CHUNK_SIZE, GameConstants.CHUNK_HEIGHT, GameConstants.CHUNK_SIZE];

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
                Position.X * GameConstants.CHUNK_SIZE,
                0,
                Position.Z * GameConstants.CHUNK_SIZE
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
            if (x >= 0 && x < GameConstants.CHUNK_SIZE && y >= 0 && y < GameConstants.CHUNK_HEIGHT && z >= 0 && z < GameConstants.CHUNK_SIZE)
            {
                return BlockRegistry.GetBlock(Voxels[x, y, z]).IsSolid;
            }

            // If outside Y bounds, consider as air
            if (y < 0 || y >= GameConstants.CHUNK_HEIGHT)
            {
                return false;
            }

            // Calculate world position for cross-chunk lookup
            Vector3i worldPos = new Vector3i(
                Position.X * GameConstants.CHUNK_SIZE + x,
                y,
                Position.Z * GameConstants.CHUNK_SIZE + z
            );

            // Get the chunk containing this world position
            ChunkPos targetChunkPos = new ChunkPos(
                (int)Math.Floor(worldPos.X / (float)GameConstants.CHUNK_SIZE),
                (int)Math.Floor(worldPos.Z / (float)GameConstants.CHUNK_SIZE)
            );

            Chunk targetChunk = chunkManager.GetChunk(targetChunkPos);
            if (targetChunk != null && targetChunk.TerrainGenerated)
            {
                Vector3i localPos = new Vector3i(
                    worldPos.X - targetChunkPos.X * GameConstants.CHUNK_SIZE,
                    worldPos.Y,
                    worldPos.Z - targetChunkPos.Z * GameConstants.CHUNK_SIZE
                );

                if (localPos.X < 0) localPos.X += GameConstants.CHUNK_SIZE;
                if (localPos.Z < 0) localPos.Z += GameConstants.CHUNK_SIZE;

                if (targetChunk.IsInBounds(localPos))
                {
                    return BlockRegistry.GetBlock(targetChunk.Voxels[localPos.X, localPos.Y, localPos.Z]).IsSolid;
                }
            }

            return false;
        }

        private bool mGravityProcessed = false;
        
        private void updateGravityBlocks(ChunkManager manager)
        {
            if (mGravityProcessed && !Modified) 
                return;
            
            bool blocksChanged = processGravitySimulation(manager);
            
            updateGravityProcessingState(blocksChanged);
        }

        private bool processGravitySimulation(ChunkManager manager)
        {
            bool blocksChanged = false;
            bool hadChanges;
            int iterations = 0;
            const int maxIterations = 10;

            do
            {
                hadChanges = simulateGravityIteration(manager, ref blocksChanged);
                iterations++;
            } while (hadChanges && iterations < maxIterations);

            return blocksChanged;
        }

        private bool simulateGravityIteration(ChunkManager manager, ref bool blocksChanged)
        {
            bool hadChanges = false;
            
            for (int y = 1; y < GameConstants.CHUNK_HEIGHT; y++)
            {
                for (int x = 0; x < GameConstants.CHUNK_SIZE; x++)
                {
                    for (int z = 0; z < GameConstants.CHUNK_SIZE; z++)
                    {
                        if (processGravityForBlock(x, y, z, manager))
                        {
                            hadChanges = true;
                            blocksChanged = true;
                            Modified = true;
                        }
                    }
                }
            }
            
            return hadChanges;
        }

        private bool processGravityForBlock(int x, int y, int z, ChunkManager manager)
        {
            byte blockType = Voxels[x, y, z];

            if (blockType == BlockIDs.Air || !BlockRegistry.GetBlock(blockType).GravityBlock)
                return false;

            if (isBlockSupported(x, y, z))
                return false;

            int landingY = findLandingPosition(x, y, z, manager);
            
            if (landingY < y)
            {
                moveBlock(x, y, z, landingY, blockType);
                return true;
            }
            
            return false;
        }

        private bool isBlockSupported(int x, int y, int z)
        {
            return y > 0 && Voxels[x, y - 1, z] != BlockIDs.Air && 
                   BlockRegistry.GetBlock(Voxels[x, y - 1, z]).IsSolid;
        }

        private int findLandingPosition(int x, int y, int z, ChunkManager manager)
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
            return landingY;
        }

        private void moveBlock(int x, int y, int z, int landingY, byte blockType)
        {
            Voxels[x, y, z] = BlockIDs.Air;
            Voxels[x, landingY, z] = blockType;
        }

        private void updateGravityProcessingState(bool blocksChanged)
        {
            if (blocksChanged)
            {
                MeshGenerated = false;
                mGravityProcessed = false;
            }
            else
            {
                mGravityProcessed = true;
            }
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

                var meshGenerator = new ChunkMeshGenerator();
                (Vertices, Indices) = meshGenerator.GenerateMesh(this, chunkManager, chunkManager.LightingEngine);
                MeshGenerated = true;
                MeshUploaded = false;
            }
        }

        public void UploadMesh(ChunkManager chunkManager)
        {
            if (mDisposed)
                return;

            var meshData = prepareMeshData();
            if (!meshData.HasValue)
                return;

            try
            {
                uploadMeshToGPU(meshData.Value, chunkManager);
            }
            catch (Exception ex)
            {
                handleMeshUploadError(ex, meshData.Value);
            }
        }

        private (List<Vertex> vertices, List<uint> indices)? prepareMeshData()
        {
            lock (this)
            {
                if (!MeshGenerated || !TerrainGenerated || MeshUploaded ||
                    Vertices == null || Indices == null ||
                    Vertices.Count == 0 || Indices.Count == 0)
                {
                    return null;
                }

                return (new List<Vertex>(Vertices), new List<uint>(Indices));
            }
        }

        private void uploadMeshToGPU((List<Vertex> vertices, List<uint> indices) meshData, ChunkManager chunkManager)
        {
            ensureOpenGLResources();
            
            GL.BindVertexArray(VAO);

            var vertexArray = meshData.vertices.ToArray();
            var indexArray = meshData.indices.ToArray();

            if (vertexArray.Length == 0 || indexArray.Length == 0)
            {
                handleEmptyMesh();
                return;
            }

            uploadVertexData(vertexArray);
            uploadIndexData(indexArray);
            setupVertexAttributes();
            
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            finalizeUpload(chunkManager);
        }

        private void ensureOpenGLResources()
        {
            if (!mOpenGLMade)
            {
                VAO = GL.GenVertexArray();
                VBO = GL.GenBuffer();
                EBO = GL.GenBuffer();
                mOpenGLMade = true;
            }
        }

        private void handleEmptyMesh()
        {
            Console.WriteLine($"Warning: Empty mesh data for chunk {Position}");
            MeshUploaded = true;
            mIndexCount = 0;
        }

        private void uploadVertexData(Vertex[] vertexArray)
        {
            int vertexDataSize = vertexArray.Length * System.Runtime.InteropServices.Marshal.SizeOf<Vertex>();
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

            int minBufferSize = Math.Max(vertexDataSize, 65536); // 64KB minimum
            if (vertexDataSize > mCurrentVBOSize || mCurrentVBOSize == 0)
            {
                GL.BufferData(BufferTarget.ArrayBuffer, minBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                mCurrentVBOSize = minBufferSize;
            }
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertexDataSize, vertexArray);
        }

        private void uploadIndexData(uint[] indexArray)
        {
            mIndexCount = indexArray.Length;
            int indexDataSize = indexArray.Length * sizeof(uint);
            
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);

            int minBufferSize = Math.Max(indexDataSize, 32768); // 32KB minimum for indices
            if (indexDataSize > mCurrentEBOSize || mCurrentEBOSize == 0)
            {
                GL.BufferData(BufferTarget.ElementArrayBuffer, minBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                mCurrentEBOSize = minBufferSize;
            }
            GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, indexDataSize, indexArray);
        }

        private void setupVertexAttributes()
        {
            if (mVertexAttribsSet)
                return;

            int vertexSize = System.Runtime.InteropServices.Marshal.SizeOf<Vertex>();

            // Position
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, 0);
            GL.EnableVertexAttribArray(0);

            // Normal
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize,
                System.Runtime.InteropServices.Marshal.OffsetOf<Vertex>(nameof(Vertex.Normal)));
            GL.EnableVertexAttribArray(1);

            // Texture coordinates
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, vertexSize,
                System.Runtime.InteropServices.Marshal.OffsetOf<Vertex>(nameof(Vertex.TexCoord)));
            GL.EnableVertexAttribArray(2);

            // Texture ID
            GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, vertexSize,
                System.Runtime.InteropServices.Marshal.OffsetOf<Vertex>(nameof(Vertex.TextureID)));
            GL.EnableVertexAttribArray(3);

            // Lighting
            GL.VertexAttribPointer(4, 1, VertexAttribPointerType.Float, false, vertexSize,
                System.Runtime.InteropServices.Marshal.OffsetOf<Vertex>(nameof(Vertex.LightValue)));
            GL.EnableVertexAttribArray(4);

            mVertexAttribsSet = true;
        }

        private void finalizeUpload(ChunkManager chunkManager)
        {
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

        private void handleMeshUploadError(Exception ex, (List<Vertex> vertices, List<uint> indices) meshData)
        {
            Console.WriteLine($"Error uploading mesh for chunk {Position}: {ex.Message}");
            Console.WriteLine($"Vertex count: {meshData.vertices?.Count ?? 0}, Index count: {meshData.indices?.Count ?? 0}");

            lock (this)
            {
                MeshUploaded = false;
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
                if (Voxels != null)
                {
                    Array.Clear(Voxels, 0, Voxels.Length);
                    Voxels = null;
                }

                if (SunlightLevels != null)
                {
                    Array.Clear(SunlightLevels, 0, SunlightLevels.Length);
                    SunlightLevels = null;
                }

                if (BlockLightLevels != null)
                {
                    Array.Clear(BlockLightLevels, 0, BlockLightLevels.Length);
                    BlockLightLevels = null;
                }

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
           pos.X >= 0 && pos.X < GameConstants.CHUNK_SIZE &&
           pos.Y >= 0 && pos.Y < GameConstants.CHUNK_HEIGHT &&
           pos.Z >= 0 && pos.Z < GameConstants.CHUNK_SIZE;

        public byte GetBlock(Vector3i position)
        {
            return Voxels[position.X, position.Y, position.Z];
        }


        public bool IsMeshReadyForUpload()
        {
            lock (this)
            {
                return MeshGenerated && !MeshUploaded && TerrainGenerated && Vertices != null && Indices != null && Vertices.Count > 0 && Indices.Count > 0;
            }
        }
    }
}