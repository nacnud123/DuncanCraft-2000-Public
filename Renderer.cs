// Main renderer class, handles a lot of thing related to what the player sees and some game tick updates | DA | 9/4/25
using DuncanCraft.PlayerScript;
using DuncanCraft.UI;
using DuncanCraft.Utils;
using DuncanCraft.World;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Linq;

namespace DuncanCraft;

public class Renderer : GameWindow
{
    private readonly GameEngine _mGameEngine;
    private readonly GameWorld _mWorld;
    private Matrix4 mView;
    private Matrix4 mProjection;
    
    private uint mVao;
    private uint mVbo;
    private Shader mShaderProgram;
    public Texture WorldTexture { get; private set; }
    
    private readonly Player mPlayer;
    private readonly InputManager mInputManager;
    private readonly TerrainModifier mTerrainModifier;

    private ImGuiController mImGuiController;
    private GameHUD mUI_HUD;
    
    private readonly Dictionary<long, uint> _mChunkVBOs = new();
    private int mLastDebugOutput = 0;
    
    // FPS tracking
    private readonly Queue<double> _mFrameTimeHistory = new();
    private const int FRAME_HISTORY_SIZE = 60;
    private double mCurrentFPS = 0;
    
    public Renderer(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, GameEngine gameEngine, GameWorld world) : base(gameWindowSettings, nativeWindowSettings)
    {
        _mGameEngine = gameEngine;
        _mWorld = world;
        mPlayer = new Player(new Vector3(128, 50, 128), world);
        mInputManager = new InputManager();
        mTerrainModifier = new TerrainModifier(world);
    }
    
    protected override void OnLoad()
    {
        base.OnLoad();
        
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Back);
        GL.FrontFace(FrontFaceDirection.Ccw);
        
        // Enable alpha blending for transparency
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        GL.ClearColor(0.5f, 0.8f, 1.0f, 1.0f);

        mImGuiController = new ImGuiController(ClientSize.X, ClientSize.Y);
        mUI_HUD = new GameHUD();

        mShaderProgram = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
        SetupBuffers();

        WorldTexture = Texture.LoadFromFile("Resources/world.png");
        WorldTexture.Use(TextureUnit.Texture0);

        Console.WriteLine("Waiting for world initialization to complete...");
        while (!isWorldReady())
        {
            Thread.Sleep(100);
        }

        Thread.Sleep(1000);

        _mWorld.ChunkManager.GenAllMeshes(WorldTexture);

        CursorState = CursorState.Grabbed;
        
        Console.WriteLine("Renderer initialized");
    }
    
    private bool isWorldReady() => _mWorld.GetLoadedChunks().ToList().Count > 0;

    private void SetupBuffers()
    {
        mVao = (uint)GL.GenVertexArray();
        mVbo = (uint)GL.GenBuffer();
        
        GL.BindVertexArray(mVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, mVbo);
        
        // Position attribute
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        
        // Color attribute
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        
        // Texture coordinates attribute
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
        GL.EnableVertexAttribArray(2);
    }
    
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        updateFPS(args.Time);
        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        updateChunkMeshes();
        
        updateCamera();

        mShaderProgram.Use();

        if (mInputManager.WireframeMode)
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
        else
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

        
        var model = Matrix4.Identity;
        
        int modelLocation = GL.GetUniformLocation(mShaderProgram._Handle, "model");
        int viewLocation = GL.GetUniformLocation(mShaderProgram._Handle, "view");
        int projectionLocation = GL.GetUniformLocation(mShaderProgram._Handle, "projection");
        int textureLocation = GL.GetUniformLocation(mShaderProgram._Handle, "textureAtlas");
        
        GL.UniformMatrix4(modelLocation, false, ref model);
        GL.UniformMatrix4(viewLocation, false, ref mView);
        GL.UniformMatrix4(projectionLocation, false, ref mProjection);
        GL.Uniform1(textureLocation, 0);
        
        GL.BindVertexArray(mVao);
        renderChunks();

        var selectedBlock = BlockRegistry.GetBlock(mInputManager.SelectedBlock);
        var selectedBlockName = selectedBlock?.Name ?? "Unknown";
        
        var uiText = $"Selected Block: {selectedBlockName}\nFPS: {mCurrentFPS:F1}\nTPS: {_mGameEngine.GetCurrentTPS():F1}";
        mUI_HUD.Render(uiText);
        mImGuiController?.Render();

        SwapBuffers();
    }
    
    private void updateChunkMeshes()
    {
        var chunks = _mWorld.GetLoadedChunks().ToList();
        
        foreach (var chunk in chunks)
        {
            var mesh = chunk.GetCurrentMesh();
            var chunkKey = getChunkKey(chunk.X, chunk.Z);
            
            if (mesh != null && mesh.IsReady)
            {
                if (!_mChunkVBOs.ContainsKey(chunkKey) || chunk.NeedsRebuild())
                {
                    if (!_mChunkVBOs.ContainsKey(chunkKey))
                    {
                        _mChunkVBOs[chunkKey] = (uint)GL.GenBuffer();
                    }
                    
                    var vbo = _mChunkVBOs[chunkKey];
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

                    var vertexData = new float[mesh.Vertices.Count * 8];
                    for (int i = 0; i < mesh.Vertices.Count; i++)
                    {
                        var vertex = mesh.Vertices[i];
                        int baseIndex = i * 8;
                        
                        // Position
                        vertexData[baseIndex] = vertex.Position.X;
                        vertexData[baseIndex + 1] = vertex.Position.Y;
                        vertexData[baseIndex + 2] = vertex.Position.Z;
                        
                        // Color / light-modulated color
                        vertexData[baseIndex + 3] = vertex.Color.X;
                        vertexData[baseIndex + 4] = vertex.Color.Y;
                        vertexData[baseIndex + 5] = vertex.Color.Z;
                        
                        // Texture coordinates
                        vertexData[baseIndex + 6] = vertex.TexCoord.X;
                        vertexData[baseIndex + 7] = vertex.TexCoord.Y;
                    }
                    
                    GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), 
                                 vertexData, BufferUsageHint.DynamicDraw);
                    
                    chunk.ClearDirtyFlag();
                    
                }
            }
        }
    }

    private void renderChunks()
    {
        var chunks = _mWorld.GetLoadedChunks().ToList();
        int totalTriangles = 0;
        
        foreach (var chunk in chunks)
        {
            var chunkKey = getChunkKey(chunk.X, chunk.Z);
            var mesh = chunk.GetCurrentMesh();
            
            if (mesh != null && mesh.IsReady && _mChunkVBOs.ContainsKey(chunkKey))
            {
                var vbo = _mChunkVBOs[chunkKey];
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
                GL.EnableVertexAttribArray(0);
                
                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
                GL.EnableVertexAttribArray(1);
                
                GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
                GL.EnableVertexAttribArray(2);
                
                GL.DrawArrays(PrimitiveType.Triangles, 0, mesh.Vertices.Count);
                totalTriangles += mesh.TriangleCount;
            }
        }

        int currentTime = (int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond);
        if (currentTime - mLastDebugOutput > 5)
        {
            mLastDebugOutput = currentTime;
        }
    }

    private long getChunkKey(int x, int z)
    {
        return ((long)x << 32) | (uint)z;
    }
    
    private void updateFPS(double frameTime)
    {
        _mFrameTimeHistory.Enqueue(frameTime);
        
        while (_mFrameTimeHistory.Count > FRAME_HISTORY_SIZE)
        {
            _mFrameTimeHistory.Dequeue();
        }
        
        if (_mFrameTimeHistory.Count > 0)
        {
            double averageFrameTime = _mFrameTimeHistory.Average();
            mCurrentFPS = 1.0 / averageFrameTime;
        }
    }
    
    private void updateCamera()
    {
        mView = mPlayer.Camera.GetViewMatrix();
        mProjection = mPlayer.Camera.GetProjectionMatrix(Size.X / (float)Size.Y);
    }
    
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            _mGameEngine.Stop();
            Close();
        }

        mImGuiController.Update(this, (float)args.Time);

        mPlayer.Update((float)args.Time, KeyboardState);
        mPlayer.ProcessMouseMovement(MouseState.Delta);

        mInputManager.ProcessInput(KeyboardState, MouseState, mPlayer.Camera, mTerrainModifier, (float)args.Time);
        
    }
    
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, Size.X, Size.Y);

        mImGuiController?.WindowResized(ClientSize.X, ClientSize.Y);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        mImGuiController.PressChar((char)e.Unicode);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        mImGuiController.MouseScroll(new Vector2(e.OffsetX, e.OffsetY));
    }

    // Clean up
    protected override void OnUnload()
    {
        base.OnUnload();

        GL.DeleteVertexArray(mVao);
        GL.DeleteBuffer(mVbo);
        mShaderProgram?.Dispose();

        foreach (var vbo in _mChunkVBOs.Values)
        {
            GL.DeleteBuffer(vbo);
        }
        _mChunkVBOs.Clear();

        WorldTexture?.Dispose();

        mImGuiController?.Dispose();
        mUI_HUD?.Dispose();
    }
}