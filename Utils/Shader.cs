// Main Shader script, used to load in, compile, and destroy shaders
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace DuncanCraft.Utils
{
    public class Shader
    {
        public readonly int _Handle;
        private readonly Dictionary<string, int> _mUniformLocations;

        public Shader(string vertPath, string fragPath)
        {
            var shaderSource = File.ReadAllText(vertPath);
            var vertexShader = GL.CreateShader(ShaderType.VertexShader);

            GL.ShaderSource(vertexShader, shaderSource);

            compileShader(vertexShader);

            shaderSource = File.ReadAllText(fragPath);
            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, shaderSource);
            compileShader(fragmentShader);

            _Handle = GL.CreateProgram();

            GL.AttachShader(_Handle, vertexShader);
            GL.AttachShader(_Handle, fragmentShader);

            linkProgram(_Handle);

            GL.DetachShader(_Handle, vertexShader);
            GL.DetachShader(_Handle, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);

            GL.GetProgram(_Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

            _mUniformLocations = new Dictionary<string, int>();

            for (var i = 0; i < numberOfUniforms; i++)
            {
                var key = GL.GetActiveUniform(_Handle, i, out _, out _);
                var location = GL.GetUniformLocation(_Handle, key);

                _mUniformLocations.Add(key, location);
            }
        }

        private static void compileShader(int shader)
        {
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
            if (code != (int)All.True)
            {
                var infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}");
            }
        }

        private static void linkProgram(int program)
        {
            GL.LinkProgram(program);

            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
            if (code != (int)All.True)
            {
                throw new Exception($"Error occurred whilst linking Program({program})");
            }
        }

        public void Use()
        {
            GL.UseProgram(_Handle);
        }

        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(_Handle, attribName);
        }

        public void SetInt(string name, int data)
        {
            GL.UseProgram(_Handle);
            GL.Uniform1(_mUniformLocations[name], data);
        }

        public void SetFloat(string name, float data)
        {
            GL.UseProgram(_Handle);
            GL.Uniform1(_mUniformLocations[name], data);
        }

        public void SetMatrix4(string name, Matrix4 data)
        {
            GL.UseProgram(_Handle);
            GL.UniformMatrix4(_mUniformLocations[name], true, ref data);
        }

        public void SetVector3(string name, Vector3 data)
        {
            GL.UseProgram(_Handle);
            GL.Uniform3(_mUniformLocations[name], data);
        }

        public void SetVector2(string name, Vector2 data)
        {
            GL.UseProgram(_Handle);
            GL.Uniform2(_mUniformLocations[name], data);
        }

        public void Dispose()
        {
            GL.DeleteProgram(_Handle);
        }
    }
}
