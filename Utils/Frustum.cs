// Frustum culling. Again not 100% of what it is doing, had to look up an algorithm. Not even 100% if it is working. | DA | 10/2/25
using OpenTK.Mathematics;

namespace VoxelGame.Utils
{
    public struct Plane
    {
        public Vector3 Normal;
        public float Distance;

        public Plane(Vector3 normal, float distance)
        {
            Normal = normal;
            Distance = distance;
        }

        public float DistanceToPoint(Vector3 point)
        {
            return Vector3.Dot(Normal, point) + Distance;
        }
    }

    public class Frustum
    {
        private Plane[] mPlanes = new Plane[6];

        public void Update(Matrix4 viewProjectionMatrix)
        {
            // Extract frustum planes from view-projection matrix
            // Left plane
            mPlanes[0] = new Plane(
                new Vector3(viewProjectionMatrix.M14 + viewProjectionMatrix.M11,
                           viewProjectionMatrix.M24 + viewProjectionMatrix.M21,
                           viewProjectionMatrix.M34 + viewProjectionMatrix.M31),
                viewProjectionMatrix.M44 + viewProjectionMatrix.M41);

            // Right plane
            mPlanes[1] = new Plane(
                new Vector3(viewProjectionMatrix.M14 - viewProjectionMatrix.M11,
                           viewProjectionMatrix.M24 - viewProjectionMatrix.M21,
                           viewProjectionMatrix.M34 - viewProjectionMatrix.M31),
                viewProjectionMatrix.M44 - viewProjectionMatrix.M41);

            // Bottom plane
            mPlanes[2] = new Plane(
                new Vector3(viewProjectionMatrix.M14 + viewProjectionMatrix.M12,
                           viewProjectionMatrix.M24 + viewProjectionMatrix.M22,
                           viewProjectionMatrix.M34 + viewProjectionMatrix.M32),
                viewProjectionMatrix.M44 + viewProjectionMatrix.M42);

            // Top plane
            mPlanes[3] = new Plane(
                new Vector3(viewProjectionMatrix.M14 - viewProjectionMatrix.M12,
                           viewProjectionMatrix.M24 - viewProjectionMatrix.M22,
                           viewProjectionMatrix.M34 - viewProjectionMatrix.M32),
                viewProjectionMatrix.M44 - viewProjectionMatrix.M42);

            // Near plane
            mPlanes[4] = new Plane(
                new Vector3(viewProjectionMatrix.M14 + viewProjectionMatrix.M13,
                           viewProjectionMatrix.M24 + viewProjectionMatrix.M23,
                           viewProjectionMatrix.M34 + viewProjectionMatrix.M33),
                viewProjectionMatrix.M44 + viewProjectionMatrix.M43);

            // Far plane
            mPlanes[5] = new Plane(
                new Vector3(viewProjectionMatrix.M14 - viewProjectionMatrix.M13,
                           viewProjectionMatrix.M24 - viewProjectionMatrix.M23,
                           viewProjectionMatrix.M34 - viewProjectionMatrix.M33),
                viewProjectionMatrix.M44 - viewProjectionMatrix.M43);

            // Normalize planes
            for (int i = 0; i < 6; i++)
            {
                float length = mPlanes[i].Normal.Length;
                if (length > 0)
                {
                    mPlanes[i].Normal /= length;
                    mPlanes[i].Distance /= length;
                }
            }
        }

        public bool IsChunkVisible(Vector3 chunkMin, Vector3 chunkMax)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector3 positiveVertex = new Vector3(
                    mPlanes[i].Normal.X >= 0 ? chunkMax.X : chunkMin.X,
                    mPlanes[i].Normal.Y >= 0 ? chunkMax.Y : chunkMin.Y,
                    mPlanes[i].Normal.Z >= 0 ? chunkMax.Z : chunkMin.Z
                );

                if (mPlanes[i].DistanceToPoint(positiveVertex) < 0)
                    return false;
            }
            return true;
        }

        public bool IsChunkVisible(ChunkPos chunkPos)
        {
            Vector3 chunkMin = new Vector3(
                chunkPos.X * GameConstants.CHUNK_SIZE,
                0,
                chunkPos.Z * GameConstants.CHUNK_SIZE
            );
            
            Vector3 chunkMax = new Vector3(
                (chunkPos.X + 1) * GameConstants.CHUNK_SIZE,
                GameConstants.CHUNK_HEIGHT,
                (chunkPos.Z + 1) * GameConstants.CHUNK_SIZE
            );

            return IsChunkVisible(chunkMin, chunkMax);
        }
    }
}