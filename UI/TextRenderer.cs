// TODO: Get rid of, using ImGuiNET is probably a better idea than this mess.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using VoxelGame.Utils;


namespace VoxelGame.UI
{
    public class TextRenderer : IDisposable
    {
        private Shader _shaderProgram;
        private int _vao, _vbo;
        private Dictionary<char, Character> _characters;
        private bool _disposed = false;

        public struct Character
        {
            public int TextureId;
            public Vector2 Size;
            public Vector2 Bearing;
            public int Advance;
        }

        public TextRenderer()
        {
            InitializeShaders();
            InitializeBuffers();
            LoadFont();
        }

        private void InitializeShaders()
        {
            _shaderProgram = new Shader("Shaders/text_shader.vert", "Shaders/text_shader.frag");
        }

        private void InitializeBuffers()
        {
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * 6 * 4, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private void LoadFont()
        {
            _characters = new Dictionary<char, Character>();

            // Create a simple bitmap font using System.Drawing
            Font font = new Font("Arial", 24, FontStyle.Regular);

            for (char c = (char)32; c < (char)127; c++)
            {
                // Measure character size
                SizeF charSize;
                using (var tempBitmap = new Bitmap(1, 1))
                using (var g = Graphics.FromImage(tempBitmap))
                {
                    charSize = g.MeasureString(c.ToString(), font);
                }

                int width = (int)Math.Ceiling(charSize.Width);
                int height = (int)Math.Ceiling(charSize.Height);

                // Create bitmap for character
                using (var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Transparent);
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    g.DrawString(c.ToString(), font, Brushes.White, 0, 0);

                    // Generate texture
                    int texture = GL.GenTexture();
                    GL.BindTexture(TextureTarget.Texture2D, texture);

                    BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height),
                        ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                        width, height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                    bitmap.UnlockBits(data);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                    Character character = new Character
                    {
                        TextureId = texture,
                        Size = new Vector2(width, height),
                        Bearing = new Vector2(0, height - font.Height),
                        Advance = width
                    };

                    _characters[c] = character;
                }
            }

            font.Dispose();
        }

        public void RenderText(string text, float x, float y, float scale, Vector3 color, Matrix4 projection)
        {
            _shaderProgram.Use();
            GL.UniformMatrix4(GL.GetUniformLocation(_shaderProgram.Handle, "projection"), false, ref projection);
            GL.Uniform3(GL.GetUniformLocation(_shaderProgram.Handle, "textColor"), color);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindVertexArray(_vao);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            float startX = x;

            foreach (char c in text)
            {
                if (c == '\n')
                {
                    y -= 48 * scale; // Line height
                    x = startX;
                    continue;
                }

                if (!_characters.ContainsKey(c))
                    continue;

                Character ch = _characters[c];

                float xpos = x + ch.Bearing.X * scale;
                float ypos = y - (ch.Size.Y - ch.Bearing.Y) * scale;
                float w = ch.Size.X * scale;
                float h = ch.Size.Y * scale;

                float[] vertices = {
                    xpos,     ypos + h,   0.0f, 0.0f,
                    xpos,     ypos,       0.0f, 1.0f,
                    xpos + w, ypos,       1.0f, 1.0f,

                    xpos,     ypos + h,   0.0f, 0.0f,
                    xpos + w, ypos,       1.0f, 1.0f,
                    xpos + w, ypos + h,   1.0f, 0.0f
                };

                GL.BindTexture(TextureTarget.Texture2D, ch.TextureId);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertices.Length * sizeof(float), vertices);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

                x += ch.Advance * scale;
            }

            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Disable(EnableCap.Blend);
        }

        public Vector2 MeasureText(string text, float scale)
        {
            float width = 0;
            float height = 0;
            float lineWidth = 0;
            int lines = 1;

            foreach (char c in text)
            {
                if (c == '\n')
                {
                    lines++;
                    width = Math.Max(width, lineWidth);
                    lineWidth = 0;
                    continue;
                }

                if (_characters.ContainsKey(c))
                {
                    lineWidth += _characters[c].Advance * scale;
                    height = Math.Max(height, _characters[c].Size.Y * scale);
                }
            }

            width = Math.Max(width, lineWidth);
            height *= lines;

            return new Vector2(width, height);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var character in _characters.Values)
                {
                    GL.DeleteTexture(character.TextureId);
                }

                GL.DeleteVertexArray(_vao);
                GL.DeleteBuffer(_vbo);
                _shaderProgram.Dispose();
                _disposed = true;
            }
        }
    }
}
