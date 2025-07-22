using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace VoxelGame
{
    class Program
    {
        static void Main()
        {
            var gameSettings = GameWindowSettings.Default;
            var windowSettings = new NativeWindowSettings()
            {
                Size = new Vector2i(1200, 800),
                Title = "DuncanCraft 2000 - C# Voxel Game"
            };

            using var game = new VoxelGame(gameSettings, windowSettings);
            game.Run();
        }
    }
}