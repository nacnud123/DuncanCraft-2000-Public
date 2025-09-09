using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace DuncanCraft;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Minecraft Clone...");
        
        using var gameEngine = new GameEngine();
        Renderer? renderer = null;
        
        try
        {
            var nativeWindowSettings = CreateWindowSettings();
            var gameWindowSettings = createGameSettings();
            
            renderer = new Renderer(gameWindowSettings, nativeWindowSettings, gameEngine, gameEngine.World);
            
            await runGameLoop(gameEngine, renderer);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Application error: {ex.Message}");
        }
        finally
        {
            await cleanupResources(renderer);
        }
    }
    
    private static NativeWindowSettings CreateWindowSettings()
    {
        return new NativeWindowSettings
        {
            ClientSize = new Vector2i(1024, 768),
            Title = "DuncanCraft In-Dev a1.0",
            WindowState = WindowState.Normal,
            StartVisible = false,
            StartFocused = true,
            API = ContextAPI.OpenGL,
            Profile = ContextProfile.Core,
            APIVersion = new Version(3, 3)
        };
    }
    
    private static GameWindowSettings createGameSettings()
    {
        var settings = GameWindowSettings.Default;
        settings.UpdateFrequency = 60;
        return settings;
    }
    
    private static async Task runGameLoop(GameEngine gameEngine, Renderer renderer)
    {
        var engineTask = gameEngine.StartAsync();
        
        renderer.IsVisible = true;
        renderer.Run();
        
        gameEngine.Stop();
        
        try
        {
            await engineTask;
            Console.WriteLine("Game engine stopped successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping game engine: {ex.Message}");
        }
    }
    
    private static async Task cleanupResources(Renderer? renderer)
    {
        Console.WriteLine("Cleaning up resources...");
        
        try
        {
            renderer?.Dispose();
            Console.WriteLine("Renderer disposed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disposing renderer: {ex.Message}");
        }
        
        Console.WriteLine("Cleanup complete.");
    }
}