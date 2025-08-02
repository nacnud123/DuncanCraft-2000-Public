// Block highlighter script, it draws a slightly bigger cube around the cube you are looking at. | DA | 8/1/25
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using VoxelGame.Utils;
using VoxelGame.World;

namespace VoxelGame.PlayerScripts
{
    public class BlockHighlighter
    {
        private int mVAO, mVBO;

        private Shader? mHighlightShader;

        private Vector3i? mHighlightedBlock;

        private const float REACH_DISTANCE = 5.0f;

        private readonly float[] _mWireframeCube =
        [
            0.0f, 0.0f, 0.0f,  1.0f, 0.0f, 0.0f,
            1.0f, 0.0f, 0.0f,  1.0f, 0.0f, 1.0f,
            1.0f, 0.0f, 1.0f,  0.0f, 0.0f, 1.0f,
            0.0f, 0.0f, 1.0f,  0.0f, 0.0f, 0.0f,
            0.0f, 1.0f, 0.0f,  1.0f, 1.0f, 0.0f,
            1.0f, 1.0f, 0.0f,  1.0f, 1.0f, 1.0f,
            1.0f, 1.0f, 1.0f,  0.0f, 1.0f, 1.0f,
            0.0f, 1.0f, 1.0f,  0.0f, 1.0f, 0.0f,
            0.0f, 0.0f, 0.0f,  0.0f, 1.0f, 0.0f,
            1.0f, 0.0f, 0.0f,  1.0f, 1.0f, 0.0f,
            1.0f, 0.0f, 1.0f,  1.0f, 1.0f, 1.0f,
            0.0f, 0.0f, 1.0f,  0.0f, 1.0f, 1.0f,
        ];

        public BlockHighlighter()
        {
            initBuffers();
            mHighlightShader = new Shader("Shaders/highlight_shader.vert", "Shaders/highlight_shader.frag");
        }

        private void initBuffers()
        {
            mVAO = GL.GenVertexArray();
            mVBO = GL.GenBuffer();

            GL.BindVertexArray(mVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, _mWireframeCube.Length * sizeof(float), _mWireframeCube, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindVertexArray(0);
        }

        public void Update(Camera camera, ChunkManager chunkManager)
        {
            mHighlightedBlock = getTargetBlock(camera, chunkManager);
        }

        private Vector3i? getTargetBlock(Camera camera, ChunkManager chunkManager)
        {
            Vector3 rayStart = camera.Position;
            Vector3 rayDirection = camera.Front;
            float step = 0.05f;
            float distance = 0.0f;

            while (distance < REACH_DISTANCE)
            {
                Vector3 currentPos = rayStart + rayDirection * distance;
                Vector3i blockPos = new Vector3i(
                    (int)Math.Floor(currentPos.X),
                    (int)Math.Floor(currentPos.Y),
                    (int)Math.Floor(currentPos.Z)
                );

                if (isBlockSolid(blockPos, chunkManager))
                {
                    return blockPos;
                }

                distance += step;
            }

            return null;
        }

        private bool isBlockSolid(Vector3i worldPos, ChunkManager chunkManager)
        {
            if (worldPos.Y < 0 || worldPos.Y >= Constants.CHUNK_HEIGHT)
            {
                return worldPos.Y < 0;
            }

            ChunkPos chunkPos = new ChunkPos(
                (int)Math.Floor(worldPos.X / (float)Constants.CHUNK_SIZE),
                (int)Math.Floor(worldPos.Z / (float)Constants.CHUNK_SIZE)
            );

            Chunk chunk = chunkManager.GetChunk(chunkPos);

            if (chunk == null)
            {
                return false;
            }

            Vector3i localPos = new Vector3i(
                worldPos.X - chunkPos.X * Constants.CHUNK_SIZE,
                worldPos.Y,
                worldPos.Z - chunkPos.Z * Constants.CHUNK_SIZE
            );

            if (chunk.IsInBounds(localPos))
            {
                return chunk.GetBlock(localPos) != BlockIDs.Air;
            }

            return false;
        }

        public void Render(Matrix4 view, Matrix4 projection)
        {
            if (mHighlightedBlock == null)
            {
                return;
            }

            bool blendEnabled = GL.IsEnabled(EnableCap.Blend);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.DepthFunc(DepthFunction.Lequal);

            GL.LineWidth(5.0f);

            mHighlightShader?.Use();

            Vector3 blockPos = new Vector3(mHighlightedBlock.Value.X, mHighlightedBlock.Value.Y, mHighlightedBlock.Value.Z);
            Matrix4 model = Matrix4.CreateScale(1.002f) * Matrix4.CreateTranslation(blockPos - new Vector3(0.001f));

            int modelLoc = GL.GetUniformLocation(mHighlightShader.Handle, "model");
            int viewLoc = GL.GetUniformLocation(mHighlightShader.Handle, "view");
            int projLoc = GL.GetUniformLocation(mHighlightShader.Handle, "projection");
            int colorLoc = GL.GetUniformLocation(mHighlightShader.Handle, "highlightColor");

            GL.UniformMatrix4(modelLoc, false, ref model);
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projLoc, false, ref projection);
            GL.Uniform3(colorLoc, new Vector3(0.0f, 0.0f, 0.0f));

            GL.BindVertexArray(mVAO);
            GL.DrawArrays(PrimitiveType.Lines, 0, _mWireframeCube.Length / 3);

            GL.BindVertexArray(0);
            GL.LineWidth(1.0f);

            GL.DepthFunc(DepthFunction.Less);
            if (!blendEnabled) GL.Disable(EnableCap.Blend);
        }

        public Vector3i? GetHighlightedBlock()
        {
            return mHighlightedBlock;
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(mVAO);
            GL.DeleteBuffer(mVBO);
            mHighlightShader?.Dispose();
        }
    }
}