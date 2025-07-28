using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoxelGame.Utils;

namespace VoxelGame.PlayerScripts
{
    public class Camera
    {
        public Vector3 Position { get; set; } = new Vector3(0.0f, 100.0f, 0.0f);
        public Vector3 Front { get; private set; } = new Vector3(0.0f, 0.0f, -1.0f);
        public Vector3 Up { get; } = new Vector3(0.0f, 1.0f, 0.0f);
        private float fov = 90f;
        public float AspectRatio { get; set; }

        public float Yaw { get; set; } = -90.0f;
        public float Pitch { get; set; } = 0.0f;
        public float Speed { get; set; } = 25.0f;
        public float Sensitivity { get; set; } = 0.1f;

        public float Fov { get => fov; set => fov = MathHelper.Clamp(value, 1f, 90f); }


        public Camera(Vector3 position, float aspectRatio)
        {
            Position = position;
            AspectRatio = aspectRatio;
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + Front, Up);
        }

        public Matrix4 GetProjectionMatrix() => Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(fov), AspectRatio, .1f, 1000f);

        public void ProcessKeyboard(int direction, float deltaTime)
        {
            float velocity = Speed * deltaTime;
            Vector3 right = Vector3.Normalize(Vector3.Cross(Front, Up));

            switch (direction)
            {
                case 0: Position += Front * velocity; break;
                case 1: Position -= Front * velocity; break;
                case 2: Position -= right * velocity; break;
                case 3: Position += right * velocity; break;
                case 4: Position += Up * velocity; break;
                case 5: Position -= Up * velocity; break;
            }
        }

        public void ProcessMouseMovement(float xOffset, float yOffset)
        {
            xOffset *= Sensitivity;
            yOffset *= Sensitivity;

            Yaw += xOffset;
            Pitch += yOffset;

            if (Pitch > 89.0f) Pitch = 89.0f;
            if (Pitch < -89.0f) Pitch = -89.0f;

            Vector3 direction = new Vector3(
                MathF.Cos(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch)),
                MathF.Sin(MathHelper.DegreesToRadians(Pitch)),
                MathF.Sin(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch))
            );
            Front = Vector3.Normalize(direction);
        }


    }
}
