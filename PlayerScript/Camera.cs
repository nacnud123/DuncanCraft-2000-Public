// Main camera class | DA | 9/4/25
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace DuncanCraft.PlayerScript
{
    public class Camera
    {
        public Vector3 Position { get; set; }
        public Vector3 Front { get; private set; }
        public Vector3 Up { get; private set; }
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }

        private const float SENSITIVITY = 0.1f;

        public Camera(Vector3 position, float yaw = -90.0f, float pitch = -17.0f)
        {
            Position = position;
            Yaw = yaw;
            Pitch = pitch;
            Up = Vector3.UnitY;
            UpdateCameraVectors();
        }

        public Matrix4 GetViewMatrix() => Matrix4.LookAt(Position, Position + Front, Up);

        public Matrix4 GetProjectionMatrix(float aspectRatio, float fov = 90.0f, float near = 0.1f, float far = 1000.0f) => Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(fov), aspectRatio, near, far);

        public void ProcessMouseMovement(Vector2 mouseDelta)
        {
            Yaw += mouseDelta.X * SENSITIVITY;
            Pitch -= mouseDelta.Y * SENSITIVITY;

            Pitch = Math.Clamp(Pitch, -89.0f, 89.0f);

            UpdateCameraVectors();
        }

        private void UpdateCameraVectors()
        {
            var yawRad = MathHelper.DegreesToRadians(Yaw);
            var pitchRad = MathHelper.DegreesToRadians(Pitch);

            Front = new Vector3(
                (float)(Math.Cos(yawRad) * Math.Cos(pitchRad)),
                (float)Math.Sin(pitchRad),
                (float)(Math.Sin(yawRad) * Math.Cos(pitchRad))
            ).Normalized();
        }
    }
}