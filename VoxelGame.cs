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

        // Memory management stuff
        private int mMemoryCheckCounter = 0;
        private long mLastMemoryUsage = 0;

        private const int MEMORY_CHECK_INTERVAL = 3600;
        private const int FORCE_GC_INTERVAL = 18000;
        private const long MEMORY_THRESHOLD_MB = 800;

        // Core managers
        private GameStateManager mStateManager;
        private ResourceManager mResourceManager;

        // Game components
        private Player mPlayer;
        private BlockHighlighter mBlockHighlighter;
        private TerrainGenerator mTerrainGen;

        public ChunkManager _ChunkManager;
        public AudioManager WorldAudioManager;

        // UI components
        private ImGuiController mImGuiController;
        private GameUI mUI_Game;

        public TitleScreen UI_TitleScreen;
        public PauseMenu UI_PauseMenu;
        public Inventory UI_Inventory;
        public Hotbar UI_Hotbar;
        public LoadingScreen UI_LoadingScrene;

        public string UIText = "Current Block: Stone";

        // World state
        private string mCurrentWorldName;
        private WorldSaveData mCurrentWorldData;

        public int WorldSeed = 0;
        public Vector2i CurrentChunkPosition = Vector2i.Zero;

        // Performance tracking
        private int mFrameCounter = 0;
        private double mFpsUpdateTimer = 0.0;

        // Debug settings
        private bool mWireframeMode = false;
        private bool mDevMode = true;

        // Graphics
        private Matrix4 mProjection;

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

            initializeOpenGL();
            initializeCoreComponents();
            setupTitleScreen();
            setupInitialState();
        }

        private void initializeOpenGL()
        {
            GL.ClearColor(0.6f, 0.8f, 1.0f, 1.0f);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
        }

        private void initializeCoreComponents()
        {
            mImGuiController = new ImGuiController(ClientSize.X, ClientSize.Y);
            WorldAudioManager = new AudioManager();
        }

        private void setupTitleScreen()
        {
            UI_TitleScreen = new TitleScreen();
            UI_TitleScreen.OnStartGame += startGame;
            UI_TitleScreen.OnTitleQuitGame += () => Close();
        }

        private void setupInitialState()
        {
            mProjection = Matrix4.CreateOrthographicOffCenter(0, Size.X, 0, Size.Y, -1, 1);
            CursorState = CursorState.Normal;
        }

        private void startGame(string worldName)
        {
            mCurrentWorldName = worldName;
            mStateManager.ChangeState(GameState.InGame);

            Serialization.UpdateLastPlayed(worldName);

            initWorld(worldName);
            initGameComponents();
            initUI();

            setGameplayState();
        }

        private void setGameplayState()
        {
            GL.ClearColor(0.5f, 0.8f, 1.0f, 1.0f);
            //CursorState = CursorState.Grabbed;
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
            // Recreate ResourceManager to ensure fresh GPU resources
            if (mResourceManager != null)
            {
                mResourceManager.Dispose();
                mResourceManager = null;
            }
            mResourceManager = new ResourceManager();
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
            UI_LoadingScrene = new LoadingScreen();

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
            updateCursorAndState();
        }

        private void updateCursorAndState()
        {
            // Keep cursor visible during loading, grab it once world is ready
            if (mStateManager.IsWorldLoading())
            {
                CursorState = CursorState.Normal;
            }
            else if (mStateManager.IsWorldReady() && CursorState != CursorState.Grabbed)
            {
                CursorState = CursorState.Grabbed;
            }
        }

        private void updateDebugInfo(FrameEventArgs args)
        {
            if (!mDevMode) 
                return;

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
            long currentMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
            string tickStats = _ChunkManager?.GetTickSystemStats() ?? "";
            
            Title = $"DuncanCraft 2000 - | {tickStats} | FPS: {mFrameCounter}";

            resetFPSCounters();
            mLastMemoryUsage = currentMemoryMB;
        }

        private void resetFPSCounters()
        {
            mFrameCounter = 0;
            mFpsUpdateTimer = 0.0;
        }

        private void performOptimizedGarbageCollection(long currentMemory)
        {
            long currentMB = currentMemory / 1024 / 1024;

            if (currentMB > MEMORY_THRESHOLD_MB * 1.5)
            {
                GC.Collect(1, GCCollectionMode.Optimized, false, false);
            }
            else
            {
                GC.Collect(0, GCCollectionMode.Optimized, false, false);
            }

            long afterGC = GC.GetTotalMemory(false);
            long freedMB = (currentMemory - afterGC) / 1024 / 1024;
            long afterMB = afterGC / 1024 / 1024;

            Console.WriteLine($"GC: {currentMB}MB -> {afterMB}MB (Freed: {freedMB}MB)");
        }

        private void memoryManagement()
        {
            if (mMemoryCheckCounter % MEMORY_CHECK_INTERVAL == 0)
            {
                long currentMemory = GC.GetTotalMemory(false);
                long memoryThresholdBytes = MEMORY_THRESHOLD_MB * 1024 * 1024;

                if (currentMemory > memoryThresholdBytes || mMemoryCheckCounter % FORCE_GC_INTERVAL == 0)
                {
                    performOptimizedGarbageCollection(currentMemory);
                }
            }

            if (mMemoryCheckCounter >= FORCE_GC_INTERVAL)
            {
                mMemoryCheckCounter = 0;
            }
        }

        private void handleInput()
        {
            handleGameStateKeys();
            handleDebugKeys();
            handleTitleScreenKeys();
        }

        private void handleGameStateKeys()
        {
            if (KeyboardState.IsKeyPressed(Keys.Escape))
            {
                mStateManager.HandleEscapeKey(this);
            }

            if (KeyboardState.IsKeyPressed(Keys.I))
            {
                mStateManager.HandleInventoryKey(this);
            }
        }

        private void handleDebugKeys()
        {
            if (KeyboardState.IsKeyPressed(Keys.X) && mStateManager.ShouldHandleInput())
            {
                toggleWireframeMode();
            }
        }

        private void handleTitleScreenKeys()
        {
            if (KeyboardState.IsKeyPressed(Keys.Delete) && mStateManager.CurrentState == GameState.TitleScreen)
            {
                UI_TitleScreen?.HandleDeleteKey();
            }
        }

        private void toggleWireframeMode()
        {
            mWireframeMode = !mWireframeMode;
            GL.PolygonMode(MaterialFace.FrontAndBack, mWireframeMode ? PolygonMode.Line : PolygonMode.Fill);
        }

        private void updateGameLogic(FrameEventArgs args)
        {
            if (!mStateManager.ShouldHandleInput()) 
                return;

            float deltaTime = (float)args.Time;

            updateGameComponents(args, deltaTime);
            updateWorld();
        }

        private void updateGameComponents(FrameEventArgs args, float deltaTime)
        {
            mBlockHighlighter?.Update(mPlayer._Camera, _ChunkManager);
            mPlayer?._InputManager.KeyboardUpdate(KeyboardState, args);

            if (mStateManager.ShouldUpdatePlayer() && mStateManager.IsWorldReady())
            {
                mPlayer?.Update(deltaTime);
            }
        }

        private void updateWorld()
        {
            _ChunkManager?.UpdateChunks(mPlayer._Camera.Position);
            _ChunkManager?.UploadPendingMeshes();
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            if (mStateManager.IsInGame() && mPlayer != null && mStateManager.IsWorldReady())
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

            if ((mStateManager.CurrentState == GameState.InGame || mStateManager.CurrentState == GameState.Inventory) && mPlayer != null )
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
            
            switch (mStateManager.CurrentState)
            {
                case GameState.TitleScreen:
                    renderTitleScreen();
                    break;
                default:
                    if (mStateManager.IsGameplayState())
                    {
                        renderGameplay();
                    }
                    break;
            }

            SwapBuffers();
        }

        private void renderTitleScreen()
        {
            GL.ClearColor(0.2f, 0.3f, 0.6f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            UI_TitleScreen?.Render();
            mImGuiController?.Render();
        }

        private void renderGameplay()
        {
            GL.ClearColor(0.6f, 0.8f, 1.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            renderWorld();
            renderUI();
            mImGuiController?.Render();
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
            if (mStateManager.IsWorldLoading())
            {
                UI_LoadingScrene?.Render();
                return;
            }

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
            mPlayer = null;
            mBlockHighlighter?.Dispose();
            mBlockHighlighter = null;
            mUI_Game?.Dispose();
            mUI_Game = null;
            mResourceManager?.Dispose();
            mResourceManager = null;
            mTerrainGen?.Dispose();
            mTerrainGen = null;

            // Clear UI references
            UI_PauseMenu = null;
            UI_Hotbar = null;
            UI_Inventory = null;
            UI_LoadingScrene = null;

            _ChunkManager = null;

            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);

            Console.WriteLine($"World cleanup complete. Memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB");
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