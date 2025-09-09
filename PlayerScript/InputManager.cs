// This script manages input | DA | 9/4/25
using OpenTK.Windowing.GraphicsLibraryFramework;
using DuncanCraft.Utils;

namespace DuncanCraft.PlayerScript
{
    public class InputManager
    {
        public bool WireframeMode { get; private set; }
        public byte SelectedBlock { get; private set; } = BlockIDs.Grass;

        public void ProcessInput(KeyboardState keyboardState, MouseState mouseState, Camera camera, TerrainModifier terrainModifier, float deltaTime)
        {
            if (keyboardState.IsKeyPressed(Keys.X))
            {
                WireframeMode = !WireframeMode;
            }

            if (keyboardState.IsKeyPressed(Keys.D1))
            {
                SelectedBlock = BlockIDs.Grass;
            }
            else if (keyboardState.IsKeyPressed(Keys.D2))
            {
                SelectedBlock = BlockIDs.Dirt;
            }
            else if (keyboardState.IsKeyPressed(Keys.D3))
            {
                SelectedBlock = BlockIDs.Stone;
            }
            else if (keyboardState.IsKeyPressed(Keys.D4))
            {
                SelectedBlock = BlockIDs.Torch;
            }
            else if (keyboardState.IsKeyPressed(Keys.D5))
            {
                SelectedBlock = BlockIDs.Glowstone;
            }
            else if (keyboardState.IsKeyPressed(Keys.D6))
            {
                SelectedBlock = BlockIDs.Flower;
            }
            else if (keyboardState.IsKeyPressed(Keys.D7))
            {
                SelectedBlock = BlockIDs.Slab;
            }

            if (mouseState.IsButtonPressed(MouseButton.Left))
            {
                terrainModifier.BreakBlock(camera.Position, camera.Front);
            }

            if (mouseState.IsButtonPressed(MouseButton.Right))
            {
                terrainModifier.PlaceBlock(camera.Position, camera.Front, SelectedBlock);
            }
        }
    }
}