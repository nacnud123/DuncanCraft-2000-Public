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
using VoxelGame.Managers;

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

        // Managers
        private GameStateManager mStateManager;
        private ResourceManager mResourceManager;
        // ---------

        // Components
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
            mStateManager = new GameStateManager();
            mResourceManager = new ResourceManager();
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.6f, 0.8f, 1.0f, 1.0f);
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
            mStateManager.ChangeState(GameState.InGame);

            // Update the last played timestamp for this world
            Serialization.UpdateLastPlayed(worldName);

            initWorld(worldName);
            initGameComponents();
            initUI();

            GL.ClearColor(0.5f, 0.8f, 1.0f, 1.0f);
            CursorState = CursorState.Grabbed;
        }

        private void initWorld(string worldName)
        {
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
        }

        private void initGameComponents()
        {
            mResourceManager.LoadGameResources();
            mUI_Game = new GameUI();
            mBlockHighlighter = new BlockHighlighter();
            mPlayer = new Player(_ChunkManager, Size);
        }

        private void initUI()
        {
            UI_PauseMenu = new PauseMenu();
            UI_Hotbar = new Hotbar(mResourceManager.WorldTexture);
            UI_Inventory = new Inventory(mResourceManager.WorldTexture);

            UI_PauseMenu.OnPauseQuitGame += () => quitToMainMenu();
            UI_PauseMenu.OnResumeGame += () => ResumeGame();

            setupHotbarBlocks();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            mImGuiController.Update(this, (float)args.Time);

            updateDebugInfo(args);
            handleInput();
            updateGameLogic(args);
        }

        private void updateDebugInfo(FrameEventArgs args)
        {
            if (!mDevMode) return;

            mFrameCounter++;
            mFpsUpdateTimer += args.Time;
            mMemoryCheckCounter++;

            if (mFpsUpdateTimer >= 1.0)
            {
                updateFPSDisplay();
            }

            memoryManagement();
        }

        private void updateFPSDisplay()
        {
            long currentMemory = GC.GetTotalMemory(false) / 1024 / 1024;
            long memoryDelta = currentMemory - mLastMemoryUsage;
            string tickStats = _ChunkManager?.GetTickSystemStats() ?? "";
            
            Title = $"DuncanCraft 2000 - | {tickStats} | FPS: {mFrameCounter}";

            mFrameCounter = 0;
            mFpsUpdateTimer = 0.0;
            mLastMemoryUsage = currentMemory;
        }

        private void memoryManagement()
        {
            if (mMemoryCheckCounter % MEMORY_CHECK_INTERVAL == 0)
            {
                long currentMemory = GC.GetTotalMemory(false);
                long memoryThreshold = 800 * 1024 * 1024;

                if (currentMemory > memoryThreshold || mMemoryCheckCounter % FORCE_GC_INTERVAL == 0)
                {
                    GC.Collect(2, GCCollectionMode.Forced, true, true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Forced, true, true);
                    
                    long afterGC = GC.GetTotalMemory(false);
                    Console.WriteLine($"GC: {currentMemory / 1024 / 1024}MB -> {afterGC / 1024 / 1024}MB (Freed: {(currentMemory - afterGC) / 1024 / 1024}MB)");
                }
            }

            if (mMemoryCheckCounter >= FORCE_GC_INTERVAL)
            {
                mMemoryCheckCounter = 0;
            }
        }

        private void handleInput()
        {
            if (KeyboardState.IsKeyPressed(Keys.Escape))
            {
                mStateManager.HandleEscapeKey(this);
            }

            if (KeyboardState.IsKeyPressed(Keys.I))
            {
                mStateManager.HandleInventoryKey(this);
            }

            if (KeyboardState.IsKeyPressed(Keys.X) && mStateManager.ShouldHandleInput())
            {
                mWireframeMode = !mWireframeMode;
                GL.PolygonMode(MaterialFace.FrontAndBack, mWireframeMode ? PolygonMode.Line : PolygonMode.Fill);
            }

            if (KeyboardState.IsKeyPressed(Keys.Delete) && mStateManager.CurrentState == GameState.TitleScreen)
            {
                UI_TitleScreen?.HandleDeleteKey();
            }
        }

        private void updateGameLogic(FrameEventArgs args)
        {
            if (!mStateManager.ShouldHandleInput()) return;

            float deltaTime = (float)args.Time;

            mBlockHighlighter?.Update(mPlayer._Camera, _ChunkManager);
            mPlayer?._InputManager.KeyboardUpdate(KeyboardState, args);
            _ChunkManager?.UpdateChunks(mPlayer._Camera.Position);
            _ChunkManager?.UploadPendingMeshes();

            if (mStateManager.ShouldUpdatePlayer())
            {
                mPlayer?.Update(deltaTime);
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            if (mStateManager.IsInGame() && mPlayer != null)
            {
                mPlayer._InputManager.MouseUpdate(MouseState);
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (mStateManager.IsInGame() && mPlayer != null)
            {
                mPlayer._InputManager.HandleMouseDown(e);
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            mImGuiController.MouseScroll(new Vector2(e.OffsetX, e.OffsetY));

            if ((mStateManager.CurrentState == GameState.InGame || mStateManager.CurrentState == GameState.Inventory) && mPlayer != null)
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
            
            if (mStateManager.CurrentState == GameState.TitleScreen)
            {
                GL.ClearColor(0.2f, 0.3f, 0.6f, 1.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                UI_TitleScreen?.Render();
                mImGuiController?.Render();
            }
            else if (mStateManager.IsGameplayState())
            {
                GL.ClearColor(0.6f, 0.8f, 1.0f, 1.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                renderWorld();
                renderUI();
                mImGuiController?.Render();
            }

            SwapBuffers();
        }

        private void renderWorld()
        {
            mResourceManager.UseWorldResources();

            Matrix4 model = Matrix4.Identity;
            Matrix4 view = mPlayer._Camera.GetViewMatrix();
            Matrix4 projection = mPlayer._Camera.GetProjectionMatrix();

            mResourceManager.SetWorldShaderUniforms(model, view, projection, mPlayer._Camera.Position);

            _ChunkManager?.RenderChunks(mPlayer._Camera.Position, mPlayer._Camera.Front, mPlayer._Camera.Up, mPlayer._Camera.Fov, mPlayer._Camera.AspectRatio);

            mBlockHighlighter?.Render(view, projection);
        }

        private void renderUI()
        {
            switch (mStateManager.CurrentState)
            {
                case GameState.Pause:
                    UI_PauseMenu?.Render();
                    break;
                case GameState.Inventory:
                    UI_Inventory?.Render();
                    break;
            }

            mUI_Game?.Render($"World: {mCurrentWorldName} | {UIText} | {CurrentChunkPosition}");
            UI_Hotbar?.Render(mProjection, Size);
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

        private void quitToMainMenu()
        {
            saveActiveChunks();
            cleanup();
            gotoTitleScreen();
        }

        private void saveActiveChunks()
        {
            if (_ChunkManager?._Chunks != null)
            {
                foreach (var activeChunk in _ChunkManager._Chunks)
                {
                    if (activeChunk.Value.Modified)
                    {
                        Serialization.SaveChunk(activeChunk.Value);
                    }
                }
            }
        }

        private void cleanup()
        {
            _ChunkManager?.Dispose();
            mResourceManager?.Dispose();
            mUI_Game?.Dispose();
            mBlockHighlighter?.Dispose();
            mTerrainGen?.Dispose();

            _ChunkManager = null;
            mUI_Game = null;
            mBlockHighlighter = null;
            mTerrainGen = null;
            mPlayer = null;
            UI_PauseMenu = null;
            UI_Hotbar = null;
            UI_Inventory = null;
        }

        private void gotoTitleScreen()
        {
            if (UI_TitleScreen == null)
            {
                UI_TitleScreen = new TitleScreen();
                UI_TitleScreen.OnStartGame += startGame;
                UI_TitleScreen.OnTitleQuitGame += () => Close();
            }

            UI_TitleScreen.RefreshWorldList();
            mStateManager.ChangeState(GameState.TitleScreen);
            CursorState = CursorState.Normal;
            GL.ClearColor(0.2f, 0.3f, 0.6f, 1.0f);
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            if (mStateManager.CurrentState != GameState.TitleScreen)
            {
                cleanup();
            }
            
            WorldAudioManager?.Dispose();
            mImGuiController?.Dispose();
        }

        public void ChangeBlockInCurrentSlot(IBlock? block)
        {
            UI_Hotbar.SetBlockInSlot(block);
            this.mPlayer._TerrainModifier.CurrentBlock = (byte)block.ID;
            UIText = $"Current Block: {block.Name}";
        }

        public void ResumeGame()
        {
            mStateManager.ChangeState(GameState.InGame);
            CursorState = CursorState.Grabbed;
        }

        public void PauseGame()
        {
            CursorState = CursorState.Normal;
            mStateManager.ChangeState(GameState.Pause);
            mPlayer._InputManager.OnGamePaused();
        }

        public void OpenInventory()
        {
            CursorState = CursorState.Normal;
            mStateManager.ChangeState(GameState.Inventory);
            mPlayer._InputManager.OnGamePaused();
        }
    }
}