// Frustum culling class, I think it works. Again not 100% of what it is doing, had to look up an algorithm. | DA | 8/1/25
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelGame.Utils
{
    public class Frustum
    {
        private Plane[] mPlanes = new Plane[6];

        public void UpdateFromCamera(Vector3 cameraPosition, Vector3 cameraFront, Vector3 cameraUp, float fov, float aspectRatio, float near, float far)
        {
            Matrix4 view = Matrix4.LookAt(cameraPosition, cameraPosition + cameraFront, cameraUp);
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(fov), aspectRatio, near, far);
            Matrix4 viewProjection = view * projection;
            getPlanes(viewProjection);
        }

        private void getPlanes(Matrix4 m)
        {
            // Left plane
            mPlanes[0] = new Plane(
                m.M14 + m.M11,
                m.M24 + m.M21,
                m.M34 + m.M31,
                m.M44 + m.M41
            );

            // Right plane
            mPlanes[1] = new Plane(
                m.M14 - m.M11,
                m.M24 - m.M21,
                m.M34 - m.M31,
                m.M44 - m.M41
            );

            // Bottom plane
            mPlanes[2] = new Plane(
                m.M14 + m.M12,
                m.M24 + m.M22,
                m.M34 + m.M32,
                m.M44 + m.M42
            );

            // Top plane
            mPlanes[3] = new Plane(
                m.M14 - m.M12,
                m.M24 - m.M22,
                m.M34 - m.M32,
                m.M44 - m.M42
            );

            // Near plane
            mPlanes[4] = new Plane(
                m.M14 + m.M13,
                m.M24 + m.M23,
                m.M34 + m.M33,
                m.M44 + m.M43
            );

            // Far plane
            mPlanes[5] = new Plane(
                m.M14 - m.M13,
                m.M24 - m.M23,
                m.M34 - m.M33,
                m.M44 - m.M43
            );

            // Normalize all planes
            for (int i = 0; i < 6; i++)
            {
                mPlanes[i].Normalize();
            }
        }

        public bool IsBoxInFrustum(Vector3 min, Vector3 max)
        {
            for (int i = 0; i < 6; i++)
            {
                Plane plane = mPlanes[i];
                Vector3 p = min;

                if (plane.Normal.X >= 0) p.X = max.X;
                if (plane.Normal.Y >= 0) p.Y = max.Y;
                if (plane.Normal.Z >= 0) p.Z = max.Z;

                if (plane.Distance(p) < 0)
                    return false;
            }
            return true;
        }
    }

    public class Plane
    {
        public Vector3 Normal;
        public float D;

        public Plane(float a, float b, float c, float d)
        {
            Normal = new Vector3(a, b, c);
            D = d;
        }

        public void Normalize()
        {
            float length = Normal.Length;
            Normal /= length;
            D /= length;
        }

        public float Distance(Vector3 point)
        {
            return Vector3.Dot(Normal, point) + D;
        }
    }
}
