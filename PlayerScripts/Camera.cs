// This is the main camera script. It handles stuff like moving the camera and all the matrices that go along with that | DA | 8/1/25
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
        private enum CameraDirection 
        { 
            Forward = 0,
            Backward = 1,
            Left = 2,
            Right = 3,
            Up = 4,
            Down = 5
        }

        private float mFov = 90f;

        public Vector3 Position { get; set; } = new Vector3(0.0f, 100.0f, 0.0f);
        public Vector3 Front { get; private set; } = new Vector3(0.0f, 0.0f, -1.0f);
        public Vector3 Up { get; } = new Vector3(0.0f, 1.0f, 0.0f);
        public float AspectRatio { get; set; }
        public float Yaw { get; set; } = -90.0f;
        public float Pitch { get; set; } = 0.0f;
        public float Speed { get; set; } = 25.0f;
        public float Sensitivity { get; set; } = 0.1f;
        public float Fov { get => mFov; set => mFov = MathHelper.Clamp(value, 1f, 90f); }

        public Camera(Vector3 position, float aspectRatio)
        {
            Position = position;
            AspectRatio = aspectRatio;
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + Front, Up);
        }

        public Matrix4 GetProjectionMatrix() => Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(mFov), AspectRatio, .1f, 1000f);

        public void ProcessKeyboard(int direction, float deltaTime)
        {
            float velocity = Speed * deltaTime;
            Vector3 right = Vector3.Normalize(Vector3.Cross(Front, Up));

            switch (direction)
            {
                case (int)CameraDirection.Forward: 
                    Position += Front * velocity; 
                    break;
                case (int)CameraDirection.Backward:
                    Position -= Front * velocity; 
                    break;
                case (int)CameraDirection.Left:
                    Position -= right * velocity;
                    break;
                case (int)CameraDirection.Right: 
                    Position += right * velocity; 
                    break;
                case (int)CameraDirection.Up: 
                    Position += Up * velocity; 
                    break;
                case (int)CameraDirection.Down: 
                    Position -= Up * velocity; 
                    break;
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
