using OpenTK.Mathematics;

namespace VoxelGame.Utils
{
    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;
        public float TextureID;
        public float LightValue;
        public Vertex(Vector3 position, Vector3 normal, Vector2 texCoord, float textureId, float lightValue = 1.0f)
        {
            Position = position;
            Normal = normal;
            TexCoord = texCoord;
            TextureID = textureId;
            LightValue = lightValue;
        }
    }
}
