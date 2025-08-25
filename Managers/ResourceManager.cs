// This script help load in resources like shaders and textures. OpenGL stuff. | DA | 8/25/25
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using VoxelGame.Utils;
using System;

namespace VoxelGame.Managers
{
    public class ResourceManager : IDisposable
    {
        public Shader WorldShader { get; private set; }
        public Texture WorldTexture { get; private set; }

        private bool mDisposed = false;

        public void LoadGameResources()
        {
            WorldShader = new Shader("Shaders/world_shader.vert", "Shaders/world_shader.frag");
            WorldTexture = Texture.LoadFromFile("Resources/world.png");
            WorldTexture.Use(TextureUnit.Texture0);
        }

        public void UseWorldResources()
        {
            WorldTexture?.Use(TextureUnit.Texture0);
            WorldShader?.Use();
        }

        public void SetWorldShaderUniforms(Matrix4 model, Matrix4 view, Matrix4 projection, Vector3 cameraPosition)
        {
            if (WorldShader == null) return;

            int modelLoc = GL.GetUniformLocation(WorldShader.Handle, "model");
            int viewLoc = GL.GetUniformLocation(WorldShader.Handle, "view");
            int projLoc = GL.GetUniformLocation(WorldShader.Handle, "projection");
            int lightDirLoc = GL.GetUniformLocation(WorldShader.Handle, "lightDir");
            int viewPosLoc = GL.GetUniformLocation(WorldShader.Handle, "viewPos");
            int fogStartLoc = GL.GetUniformLocation(WorldShader.Handle, "fogStart");
            int fogEndLoc = GL.GetUniformLocation(WorldShader.Handle, "fogEnd");
            int fogColorLoc = GL.GetUniformLocation(WorldShader.Handle, "fogColor");

            GL.UniformMatrix4(modelLoc, false, ref model);
            GL.UniformMatrix4(viewLoc, false, ref view);
            GL.UniformMatrix4(projLoc, false, ref projection);
            GL.Uniform3(lightDirLoc, new Vector3(0.0f, -1.0f, 0.0f));
            GL.Uniform3(viewPosLoc, cameraPosition);
            
            float renderDistance = Constants.RENDER_DISTANCE * Constants.CHUNK_SIZE;
            GL.Uniform1(fogStartLoc, renderDistance * 0.7f);
            GL.Uniform1(fogEndLoc, renderDistance);
            GL.Uniform3(fogColorLoc, new Vector3(0.6f, 0.8f, 1.0f));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!mDisposed && disposing)
            {
                WorldShader?.Dispose();
                WorldTexture?.Dispose();
                mDisposed = true;
            }
        }
    }
}