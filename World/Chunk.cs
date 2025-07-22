using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using VoxelGame.Blocks;
using VoxelGame.Utils;
using System.Runtime.CompilerServices;

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

        public bool MeshGenerated { get; set; }
        public bool MeshUploaded { get; set; }

        private bool disposed = false;
        public bool Modified = false;



        public Chunk(ChunkPos position)
        {
            Position = position;
            Voxels = new byte[Constants.CHUNK_SIZE, Constants.CHUNK_HEIGHT, Constants.CHUNK_SIZE];
            Vertices = new List<Vertex>();
            Indices = new List<uint>();

            generateTerrain();

            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();
            EBO = GL.GenBuffer();
        }

        private void generateTerrain()
        {
            Voxels = VoxelGame.init.TerrainGen.GenerateTerrain(Voxels, Position);
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

            var neighborChunk = chunkManager.GetChunk(neighborPos);
            if (neighborChunk != null)
            {
                return BlockRegistry.GetBlock(neighborChunk.Voxels[neighborX, y, neighborZ]).IsSolid;
            }

            return false;
        }

        public void GenMesh(ChunkManager chunkManager)
        {
           
            var newVertices = new List<Vertex>();
            var newIndices = new List<uint>();

            Vector3 chunkWorldOffset = new Vector3(
                Position.X * Constants.CHUNK_SIZE,
                0,
                Position.Z * Constants.CHUNK_SIZE
            );

            Vector3[] faceNormals = new Vector3[]
            {
                new Vector3(0, 0, 1), new Vector3(0, 0, -1), new Vector3(-1, 0, 0),
                new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, -1, 0)
            };

            Vector3[] flowerNormals = new Vector3[]
            {
                new Vector3(0.707f, 0, 0.707f),
                new Vector3(-0.707f, 0, -0.707f),
                new Vector3(-0.707f, 0, 0.707f),
                new Vector3(0.707f, 0, -0.707f)
            };

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

                                Vertex vertex = new Vertex(worldPosition, normal, texCoords[v], (float)Voxels[x, y, z]);
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

            Vertices = newVertices;
            Indices = newIndices;
            MeshGenerated = true;
            MeshUploaded = false;
        }

        public void UploadMesh()
        {
            if (!MeshGenerated || Vertices.Count == 0) return;

            GL.BindVertexArray(VAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Count * System.Runtime.InteropServices.Marshal.SizeOf<Vertex>(),
                         Vertices.ToArray(), BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Count * sizeof(uint),
                         Indices.ToArray(), BufferUsageHint.StaticDraw);

            // Position
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, System.Runtime.InteropServices.Marshal.SizeOf<Vertex>(), 0);
            GL.EnableVertexAttribArray(0);

            // Normal
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, System.Runtime.InteropServices.Marshal.SizeOf<Vertex>(),
                                  System.Runtime.InteropServices.Marshal.OffsetOf<Vertex>(nameof(Vertex.Normal)));
            GL.EnableVertexAttribArray(1);

            // Texture coordinate
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, System.Runtime.InteropServices.Marshal.SizeOf<Vertex>(),
                                  System.Runtime.InteropServices.Marshal.OffsetOf<Vertex>(nameof(Vertex.TexCoord)));
            GL.EnableVertexAttribArray(2);

            // Texture ID
            GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, System.Runtime.InteropServices.Marshal.SizeOf<Vertex>(),
                                  System.Runtime.InteropServices.Marshal.OffsetOf<Vertex>(nameof(Vertex.TextureID)));
            GL.EnableVertexAttribArray(3);

            GL.BindVertexArray(0);

            MeshUploaded = true;
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
            if (!MeshUploaded || Indices.Count == 0) return;

            GL.BindVertexArray(VAO);
            GL.DrawElements(PrimitiveType.Triangles, Indices.Count, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (MeshUploaded)
                {
                    GL.DeleteVertexArray(VAO);
                    GL.DeleteBuffer(VBO);
                    GL.DeleteBuffer(EBO);
                }
                disposed = true;
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
    }
}