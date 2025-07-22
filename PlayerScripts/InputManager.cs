using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelGame.Utils;

namespace VoxelGame.PlayerScripts
{
    public class InputManager
    {
        private bool sprinting = false;
        Player player;
        private bool firstMove = true;

        private Vector2 lastMousePos;

        public InputManager(Player _player)
        {
            this.player = _player;
        }

        public void KeyboardUpdate(KeyboardState keyboard, FrameEventArgs e)
        {
            Vector3 front = Vector3.Normalize(new Vector3(player.Camera.Front.X, 0.0f, player.Camera.Front.Z));
            Vector3 right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));

            if (keyboard.IsKeyDown(Keys.W))
                player.Velocity += front * (sprinting ? Player.SPRINT_SPEED : Player.MOVE_SPEED);

            if (keyboard.IsKeyDown(Keys.S))
                player.Velocity -= front * (sprinting ? Player.SPRINT_SPEED : Player.MOVE_SPEED);

            if (keyboard.IsKeyDown(Keys.A))
                player.Velocity -= right * (sprinting ? Player.SPRINT_SPEED : Player.MOVE_SPEED);

            if (keyboard.IsKeyDown(Keys.D))
                player.Velocity += right * (sprinting ? Player.SPRINT_SPEED : Player.MOVE_SPEED);

            if (keyboard.IsKeyDown(Keys.Space))
                player.Jump();

            if (keyboard.IsKeyDown(Keys.LeftShift))
                sprinting = true;
            else
                sprinting = false;

            if(keyboard.IsKeyDown(Keys.R))
                player.ResetPosition();

            if (keyboard.IsKeyDown(Keys.D1))
            {
                player.terrainModifier.SetCurrentBlockByNum(0);
            }

            if (keyboard.IsKeyDown(Keys.D2))
            {
                player.terrainModifier.SetCurrentBlockByNum(1);
            }

            if (keyboard.IsKeyDown(Keys.D3))
            {
                player.terrainModifier.SetCurrentBlockByNum(2);
            }

            if (keyboard.IsKeyDown(Keys.D4))
            {
                player.terrainModifier.SetCurrentBlockByNum(3);
            }

            if (keyboard.IsKeyDown(Keys.D5))
            {
                player.terrainModifier.SetCurrentBlockByNum(4);
            }

            if (keyboard.IsKeyDown(Keys.D6))    
            {
                player.terrainModifier.SetCurrentBlockByNum(5);
            }

            if (keyboard.IsKeyDown(Keys.D7))
            {
                player.terrainModifier.SetCurrentBlockByNum(6);
            }

            if (keyboard.IsKeyDown(Keys.D8))
            {
                player.terrainModifier.SetCurrentBlockByNum(7);
            }

            if (keyboard.IsKeyDown(Keys.D9))
            {
                player.terrainModifier.SetCurrentBlockByNum(8);
            }

            if (keyboard.IsKeyDown(Keys.D0))
            {
                player.terrainModifier.SetCurrentBlockByNum(9);
            }
        }

        public void HandleMouseScroll(MouseWheelEventArgs e)
        {
            if (e.OffsetY < 0)
            {
                player.terrainModifier.SetCurrentBlock((int)-1);
            }
            if (e.OffsetY > 0)
            {
                player.terrainModifier.SetCurrentBlock((int)1);
            }
        }

        public void MouseUpdate(MouseState mouse)
        {
            if (firstMove)
            {
                lastMousePos = new Vector2(mouse.X, mouse.Y);
                firstMove = false;
            }
            else
            {
                float deltaX = mouse.X - lastMousePos.X;
                float deltaY = lastMousePos.Y - mouse.Y;
                lastMousePos = new Vector2(mouse.X, mouse.Y);

                player.Camera.ProcessMouseMovement(deltaX, deltaY);
            }
        }

        public void HandleMouseDown(MouseButtonEventArgs mouse)
        {
            if (mouse.Button == MouseButton.Left)
            {
                player.terrainModifier.BreakBlock(player.Camera.Position, player.Camera.Front);
            }
            else if (mouse.Button == MouseButton.Right)
            {
                player.terrainModifier.PlaceBlock(player.Camera.Position, player.Camera.Front, BlockIDs.Stone);
            }
        }

        public void OnGamePaused()
        {
            firstMove = true;
        }
    }
}
