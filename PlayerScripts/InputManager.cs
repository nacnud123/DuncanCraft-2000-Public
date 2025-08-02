// This script manages inputs from the keyboard and mouse. | DA | 8/1/25
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using VoxelGame.Utils;

namespace VoxelGame.PlayerScripts
{
    public class InputManager
    {
        private bool mSprinting = false;
        private bool mFirstMove = true;

        private Player mPlayer;

        private Vector2 mLastMousePos;

        public InputManager(Player _player)
        {
            this.mPlayer = _player;
        }

        public void KeyboardUpdate(KeyboardState keyboard, FrameEventArgs e)
        {
            Vector3 front = Vector3.Normalize(new Vector3(mPlayer._Camera.Front.X, 0.0f, mPlayer._Camera.Front.Z));
            Vector3 right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));

            if (keyboard.IsKeyDown(Keys.W))
                mPlayer.Velocity += front * (mSprinting ? Player.SPRINT_SPEED : Player.MOVE_SPEED);

            if (keyboard.IsKeyDown(Keys.S))
                mPlayer.Velocity -= front * (mSprinting ? Player.SPRINT_SPEED : Player.MOVE_SPEED);

            if (keyboard.IsKeyDown(Keys.A))
                mPlayer.Velocity -= right * (mSprinting ? Player.SPRINT_SPEED : Player.MOVE_SPEED);

            if (keyboard.IsKeyDown(Keys.D))
                mPlayer.Velocity += right * (mSprinting ? Player.SPRINT_SPEED : Player.MOVE_SPEED);

            if (keyboard.IsKeyDown(Keys.Space))
                mPlayer.Jump();

            if (keyboard.IsKeyDown(Keys.LeftShift))
                mSprinting = true;
            else
                mSprinting = false;

            if(keyboard.IsKeyDown(Keys.R))
                mPlayer.ResetPosition();

            if (keyboard.IsKeyDown(Keys.D1))
            {
                mPlayer._TerrainModifier.SetCurrentBlockByNum(0);
            }

            if (keyboard.IsKeyDown(Keys.D2))
            {
                mPlayer._TerrainModifier.SetCurrentBlockByNum(1);
            }

            if (keyboard.IsKeyDown(Keys.D3))
            {
                mPlayer._TerrainModifier.SetCurrentBlockByNum(2);
            }

            if (keyboard.IsKeyDown(Keys.D4))
            {
                mPlayer._TerrainModifier.SetCurrentBlockByNum(3);
            }

            if (keyboard.IsKeyDown(Keys.D5))
            {
                mPlayer._TerrainModifier.SetCurrentBlockByNum(4);
            }

            if (keyboard.IsKeyDown(Keys.D6))    
            {
                mPlayer._TerrainModifier.SetCurrentBlockByNum(5);
            }

            if (keyboard.IsKeyDown(Keys.D7))
            {
                mPlayer._TerrainModifier.SetCurrentBlockByNum(6);
            }

            if (keyboard.IsKeyDown(Keys.D8))
            {
                mPlayer._TerrainModifier.SetCurrentBlockByNum(7);
            }

            if (keyboard.IsKeyDown(Keys.D9))
            {
                mPlayer._TerrainModifier.SetCurrentBlockByNum(8);
            }

            if (keyboard.IsKeyDown(Keys.D0))
            {
                mPlayer._TerrainModifier.SetCurrentBlockByNum(9);
            }
        }

        public void HandleMouseScroll(MouseWheelEventArgs e)
        {
            if (e.OffsetY < 0)
            {
                mPlayer._TerrainModifier.SetCurrentBlock((int)-1);
            }
            if (e.OffsetY > 0)
            {
                mPlayer._TerrainModifier.SetCurrentBlock((int)1);
            }
        }

        public void MouseUpdate(MouseState mouse)
        {
            if (mFirstMove)
            {
                mLastMousePos = new Vector2(mouse.X, mouse.Y);
                mFirstMove = false;
            }
            else
            {
                float deltaX = mouse.X - mLastMousePos.X;
                float deltaY = mLastMousePos.Y - mouse.Y;
                mLastMousePos = new Vector2(mouse.X, mouse.Y);

                mPlayer._Camera.ProcessMouseMovement(deltaX, deltaY);
            }
        }

        public void HandleMouseDown(MouseButtonEventArgs mouse)
        {
            if (mouse.Button == MouseButton.Left)
            {
                mPlayer._TerrainModifier.BreakBlock(mPlayer._Camera.Position, mPlayer._Camera.Front);
            }
            else if (mouse.Button == MouseButton.Right)
            {
                mPlayer._TerrainModifier.PlaceBlock(mPlayer._Camera.Position, mPlayer._Camera.Front);
            }
        }

        public void OnGamePaused()
        {
            mFirstMove = true;
        }
    }
}
