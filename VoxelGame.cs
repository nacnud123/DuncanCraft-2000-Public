// Main game class, has stuff related to updating the game. Makes and destroys game components and has stuff related to game flow. | DA | 8/1/25
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using VoxelGame.Audio;
using VoxelGame.PlayerScripts;
using VoxelGame.UI;
using VoxelGame.Utils;
using VoxelGame.World;
using VoxelGame.Saving;
using VoxelGame.Blocks;

namespace VoxelGame
{
    public class VoxelGame : GameWindow
    {
        public static VoxelGame init;

        // Memory stuff
        private int mMemoryCheckCounter = 0;
        private long mLastMemoryUsage = 0;

        private const int MEMORY_CHECK_INTERVAL = 3600;
        private const int FORCE_GC_INTERVAL = 18000;
        // ---------

        // Components
        private Shader? mShaderProgram;
        private Texture? mWorldTexture;

        private Player mPlayer;
        private BlockHighlighter mBlockHighlighter;
        private TerrainGenerator mTerrainGen;

        public ChunkManager _ChunkManager;
        public AudioManager WorldAudioManager;
        // ---------

        // UI
        private ImGuiController mImGuiController;
        private GameUI mUI_Game;

        public TitleScreen UI_TitleScreen;
        public PauseMenu UI_PauseMenu;
        public Inventory UI_Inventory;
        public Hotbar UI_Hotbar;

        public string UIText = "Current Block: Stone";
        // ---------

        // World
        private GameState mCurrentState;
        private string mCurrentWorldName;
        private WorldSaveData mCurrentWorldData;

        public int WorldSeed = 0;
        public Vector2i CurrentChunkPosition = Vector2i.Zero;
        // ---------

        // FPS Stuff
        private int mFrameCounter = 0;
        private double mFpsUpdateTimer = 0.0;
        // ---------

        // Debug
        private bool mWireframeMode = false;
        private bool mDevMode = true;
        // ---------

        // Other
        private Matrix4 mProjection;
        // ---------

        public TerrainGenerator TerrainGen { get => mTerrainGen; }

        public VoxelGame(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            init = this;
            mCurrentState = GameState.TitleScreen;
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.6f, 1.0f);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            mImGuiController = new ImGuiController(ClientSize.X, ClientSize.Y);
            WorldAudioManager = new AudioManager();
            UI_TitleScreen = new TitleScreen();

            UI_TitleScreen.OnStartGame += startGame;
            UI_TitleScreen.OnTitleQuitGame += () => Close();

            mProjection = Matrix4.CreateOrthographicOffCenter(0, Size.X, 0, Size.Y, -1, 1);

            CursorState = CursorState.Normal;
        }

        private void startGame(string worldName)
        {
            mCurrentWorldName = worldName;
            mCurrentState = GameState.InGame;

            mTerrainGen = new TerrainGenerator();
            Serialization.s_WorldName = worldName;

            var existingWorld = Serialization.LoadWorldData(worldName);
            if (existingWorld.HasValue)
            {
                mCurrentWorldData = existingWorld.Value;
                WorldSeed = mCurrentWorldData.Seed;
            }
            else
            {
                mCurrentWorldData = Serialization.CreateWorld(worldName);
                WorldSeed = mCurrentWorldData.Seed;
            }

            mTerrainGen.init(WorldSeed);

            _ChunkManager = new ChunkManager();

            mShaderProgram = new Shader("Shaders/world_shader.vert", "Shaders/world_shader.frag");
            mWorldTexture = Texture.LoadFromFile("Resources/world.png");
            mWorldTexture.Use(TextureUnit.Texture0);

            mUI_Game = new GameUI();
            mBlockHighlighter = new BlockHighlighter();

            UI_PauseMenu = new PauseMenu();
            UI_Hotbar = new Hotbar(mWorldTexture);
            UI_Inventory = new Inventory(mWorldTexture);

            UI_PauseMenu.OnPauseQuitGame += () => QuitToMainMenu();
            UI_PauseMenu.OnResumeGame += () => ResumeGame();

            setupHotbarBlocks();

            mPlayer = new Player(_ChunkManager, Size);

            GL.ClearColor(0.5f, 0.8f, 1.0f, 1.0f);

            CursorState = CursorState.Grabbed;
        }

        // TODO: refactor this code, a little spaghetti
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            mImGuiController.Update(this, (float)args.Time);

            if (mDevMode)
            {
                mFrameCounter++;
                mFpsUpdateTimer += args.Time;
                mMemoryCheckCounter++;

                if (mFpsUpdateTimer >= 1.0)
                {
                    long currentMemory = GC.GetTotalMemory(false) / 1024 / 1024; // MB
                    long memoryDelta = currentMemory - mLastMemoryUsage;

                    Title = $"DuncanCraft 2000 - FPS: {mFrameCounter} | RAM: {currentMemory}MB ({memoryDelta:+#;-#;0}MB)";

                    mFrameCounter = 0;
                    mFpsUpdateTimer = 0.0;
                    mLastMemoryUsage = currentMemory;
                }

                // Garbage collection
                if (mMemoryCheckCounter % MEMORY_CHECK_INTERVAL == 0)
                {
                    long currentMemory = GC.GetTotalMemory(false);

                    if (currentMemory > 500 * 1024 * 1024 ||
                        mMemoryCheckCounter % FORCE_GC_INTERVAL == 0)
                    {
                        GC.Collect(0, GCCollectionMode.Optimized);
                        Console.WriteLine($"Forced GC: {currentMemory / 1024 / 1024}MB -> {GC.GetTotalMemory(false) / 1024 / 1024}MB");
                    }
                }

                if (mMemoryCheckCounter >= FORCE_GC_INTERVAL)
                {
                    mMemoryCheckCounter = 0;
                }
            }

            if (mCurrentState == GameState.TitleScreen)
            {
                if (KeyboardState.IsKeyDown(Keys.Escape))
                    Close();
            }
            else if (mCurrentState == GameState.InGame || mCurrentState == GameState.Inventory || mCurrentState == GameState.Pause)
            {
                if (KeyboardState.IsKeyPressed(Keys.Escape))
                {
                    if (mCurrentState == GameState.Pause || mCurrentState == GameState.Inventory)
                        ResumeGame();
                    else
                    {
                        CursorState = CursorState.Normal;
                        mCurrentState = GameState.Pause;
                        mPlayer._InputManager.OnGamePaused();
                        return;
                    }
                }

                if (KeyboardState.IsKeyPressed(Keys.I) && mCurrentState != GameState.Pause)
                {
                    if (mCurrentState == GameState.Inventory)
                        ResumeGame();
                    else
                    {
                        CursorState = CursorState.Normal;
                        mCurrentState = GameState.Inventory;
                        mPlayer._InputManager.OnGamePaused();
                        return;
                    }
                }

                if (KeyboardState.IsKeyPressed(Keys.X))
                {
                    mWireframeMode = !mWireframeMode;
                    if (mWireframeMode)
                    {
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    }
                    else
                    {
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    }
                }

                float deltaTime = (float)args.Time;

                mBlockHighlighter.Update(mPlayer._Camera, _ChunkManager);

                mPlayer._InputManager.KeyboardUpdate(KeyboardState, args);
                _ChunkManager.UpdateChunks(mPlayer._Camera.Position);
                _ChunkManager.UploadPendingMeshes();

                if (mCurrentState == GameState.InGame)
                    mPlayer.Update(deltaTime);
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            if (mCurrentState == GameState.InGame && mPlayer != null)
            {
                mPlayer._InputManager.MouseUpdate(MouseState);
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (mCurrentState == GameState.InGame && mPlayer != null)
            {
                mPlayer._InputManager.HandleMouseDown(e);
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            mImGuiController.MouseScroll(new Vector2(e.OffsetX, e.OffsetY));

            if (mCurrentState == GameState.InGame || mCurrentState == GameState.Inventory && mPlayer != null)
            {
                mPlayer._InputManager.HandleMouseScroll(e);
            }
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            mImGuiController.PressChar((char)e.Unicode);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (mCurrentState == GameState.TitleScreen)
            {
                UI_TitleScreen.Render();
                mImGuiController.Render();
            }
            else if (mCurrentState == GameState.InGame || mCurrentState == GameState.Pause || mCurrentState == GameState.Inventory)
            {
                mWorldTexture?.Use(TextureUnit.Texture0);
                mShaderProgram?.Use();

                Matrix4 model = Matrix4.Identity;
                Matrix4 view = mPlayer._Camera.GetViewMatrix();
                Matrix4 projection = mPlayer._Camera.GetProjectionMatrix();

                int modelLoc = GL.GetUniformLocation(mShaderProgram.Handle, "model");
                int viewLoc = GL.GetUniformLocation(mShaderProgram.Handle, "view");
                int projLoc = GL.GetUniformLocation(mShaderProgram.Handle, "projection");
                int lightDirLoc = GL.GetUniformLocation(mShaderProgram.Handle, "lightDir");
                int viewPosLoc = GL.GetUniformLocation(mShaderProgram.Handle, "viewPos");

                GL.UniformMatrix4(modelLoc, false, ref model);
                GL.UniformMatrix4(viewLoc, false, ref view);
                GL.UniformMatrix4(projLoc, false, ref projection);
                GL.Uniform3(lightDirLoc, new Vector3(0.2f, -1.0f, 0.3f));
                GL.Uniform3(viewPosLoc, mPlayer._Camera.Position);

                _ChunkManager.RenderChunks(mPlayer._Camera.Position, mPlayer._Camera.Front, mPlayer._Camera.Up, mPlayer._Camera.Fov, mPlayer._Camera.AspectRatio);

                mBlockHighlighter.Render(view, projection);

                if (mCurrentState == GameState.Pause)
                {
                    UI_PauseMenu.Render();
                }
                else if (mCurrentState == GameState.Inventory)
                {
                    UI_Inventory.Render();
                }

                if (mUI_Game != null)
                {
                    mUI_Game.Render($"World: {mCurrentWorldName} | {UIText} | {CurrentChunkPosition}");
                }

                if (UI_Hotbar != null)
                {
                    UI_Hotbar.Render(mProjection, Size);
                }

                mImGuiController.Render();
            }

            SwapBuffers();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);

            mProjection = Matrix4.CreateOrthographicOffCenter(0, ClientSize.X, 0, ClientSize.Y, -1, 1);

            mImGuiController?.WindowResized(ClientSize.X, ClientSize.Y);

            if (mPlayer != null)
            {
                mPlayer._Camera.AspectRatio = ClientSize.X / (float)ClientSize.Y;
            }
        }

        private void setupHotbarBlocks()
        {
            UI_Hotbar.SetBlockInSlot(0, BlockRegistry.GetBlock(BlockIDs.Stone));
            UI_Hotbar.SetBlockInSlot(1, BlockRegistry.GetBlock(BlockIDs.Dirt));
            UI_Hotbar.SetBlockInSlot(2, BlockRegistry.GetBlock(BlockIDs.Grass));
            UI_Hotbar.SetBlockInSlot(3, BlockRegistry.GetBlock(BlockIDs.Log));
            UI_Hotbar.SetBlockInSlot(4, BlockRegistry.GetBlock(BlockIDs.Glass));
            UI_Hotbar.SetBlockInSlot(5, BlockRegistry.GetBlock(BlockIDs.Plank));
            UI_Hotbar.SetBlockInSlot(6, BlockRegistry.GetBlock(BlockIDs.Leaves));
            UI_Hotbar.SetBlockInSlot(7, BlockRegistry.GetBlock(BlockIDs.Red));
            UI_Hotbar.SetBlockInSlot(8, BlockRegistry.GetBlock(BlockIDs.Green));
            UI_Hotbar.SetBlockInSlot(9, BlockRegistry.GetBlock(BlockIDs.Blue));
        }

        private void QuitToMainMenu()
        {
            if (_ChunkManager?._Chunks != null)
            {
                foreach (var activeChunks in _ChunkManager._Chunks)
                {
                    if (activeChunks.Value.Modified)
                    {
                        Serialization.SaveChunk(activeChunks.Value);
                    }
                }
            }

            _ChunkManager?.Dispose();
            mShaderProgram?.Dispose();
            mUI_Game?.Dispose();
            mBlockHighlighter?.Dispose();
            mTerrainGen?.Dispose();
            mWorldTexture?.Dispose();

            _ChunkManager = null;
            mShaderProgram = null;
            mUI_Game = null;
            mBlockHighlighter = null;
            mTerrainGen = null;
            mWorldTexture = null;
            mPlayer = null;
            UI_PauseMenu = null;
            UI_Hotbar = null;
            UI_Inventory = null;

            if(UI_TitleScreen == null)
            {
                UI_TitleScreen = new TitleScreen();
                UI_TitleScreen.OnStartGame += startGame;
                UI_TitleScreen.OnTitleQuitGame += () => Close();
            }

            UI_TitleScreen.RefreshWorldList();

            mCurrentState = GameState.TitleScreen;
            CursorState = CursorState.Normal;
            GL.ClearColor(0.2f, 0.3f, 0.6f, 1.0f);
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            if (mCurrentState != GameState.TitleScreen)
            {
                _ChunkManager?.Dispose();
                mShaderProgram?.Dispose();
                mUI_Game?.Dispose();
                mBlockHighlighter?.Dispose();
                WorldAudioManager?.Dispose();
                mTerrainGen?.Dispose();
                mWorldTexture?.Dispose();
            }
            else
            {
                WorldAudioManager?.Dispose();
            }

            mImGuiController?.Dispose();
        }

        public void ChangeBlockInCurrentSlot(IBlock? block)
        {
            UI_Hotbar.SetBlockInSlot(block);
            this.mPlayer._TerrainModifier.CurrentBlock = (byte)block.ID;
            UIText = $"Current Block: {block.Name}";
        }

        private void ResumeGame()
        {
            mCurrentState = GameState.InGame;
            CursorState = CursorState.Grabbed;
        }
    }
}