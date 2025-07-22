using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ImGuiNET;

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

        private Shader? shaderProgram;
        private Texture? WorldTexture;

        private ChunkManager chunkManager;
        private Player player;
        private BlockHighlighter blockHighlighter;
        private TerrainGenerator terrainGen;

        private TextRenderer _textRenderer;
        private ImGuiController _imGuiController;

        public TitleScreen _titleScreen;
        public PauseMenu _pauseMenu;
        public Inventory _inventory;
        public Hotbar _hotbar;

        private GameState _currentState;
        private string _currentWorldName;

        private AudioManager audioManager;

        private bool wireframeMode = false;
        private Matrix4 _projection;

        public string UIText = "Current Block: Stone";

        public int WorldSeed = 0;

        public Vector2i currentChunkPosition = Vector2i.Zero;

        public TerrainGenerator TerrainGen { get => terrainGen; }

        public VoxelGame(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            init = this;
            _currentState = GameState.TitleScreen;
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.6f, 1.0f);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            _imGuiController = new ImGuiController(Size.X, Size.Y);
            audioManager = new AudioManager();
            _titleScreen = new TitleScreen();


            _titleScreen.OnStartGame += StartGame;
            _titleScreen.OnTitleQuitGame += () => Close();

            _projection = Matrix4.CreateOrthographicOffCenter(0, Size.X, 0, Size.Y, -1, 1);

            CursorState = CursorState.Normal;
        }

        private void StartGame(string worldName)
        {
            NoiseGenerator.SetRandomSeed();

            _currentWorldName = worldName;
            _currentState = GameState.InGame;

            terrainGen = new TerrainGenerator();
            Serialization.WorldName = worldName;

            string path = Constants.SAVE_LOCATION + "/" + worldName + "/";
            try
            {
                WorldSeed = Serialization.readSead(path);
            }
            catch
            {
                WorldSeed = Serialization.writeSeed(path, worldName);
            }
            terrainGen.init(WorldSeed);

            chunkManager = new ChunkManager();

            shaderProgram = new Shader("Shaders/world_shader.vert", "Shaders/world_shader.frag");
            WorldTexture = Texture.LoadFromFile("Resources/world.png");
            WorldTexture.Use(TextureUnit.Texture0);

            _textRenderer = new TextRenderer();
            blockHighlighter = new BlockHighlighter();

            _pauseMenu = new PauseMenu();
            _hotbar = new Hotbar(WorldTexture);
            _inventory = new Inventory(WorldTexture);

            _pauseMenu.OnPauseQuitGame += () => CloseGame();
            _pauseMenu.OnResumeGame += () => ResumeGame();


            SetupHotbarBlocks();

            player = new Player(chunkManager, Size);

            GL.ClearColor(0.5f, 0.8f, 1.0f, 1.0f);

            CursorState = CursorState.Grabbed;
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            _imGuiController.Update(this, (float)args.Time);


            if (_currentState == GameState.TitleScreen)
            {
                if (KeyboardState.IsKeyDown(Keys.Escape))
                    Close();
            }
            else if (_currentState == GameState.InGame || _currentState == GameState.Inventory || _currentState == GameState.Pause)
            {
                if (KeyboardState.IsKeyPressed(Keys.Escape))
                {
                    if (_currentState == GameState.Pause || _currentState == GameState.Inventory)
                        ResumeGame();
                    else
                    {
                        CursorState = CursorState.Normal;
                        _currentState = GameState.Pause;
                        player.inputManager.OnGamePaused();
                        return;
                    }
                }

                if (KeyboardState.IsKeyPressed(Keys.I) && _currentState != GameState.Pause)
                {
                    if (_currentState == GameState.Inventory)
                        ResumeGame();
                    else
                    {
                        CursorState = CursorState.Normal;
                        _currentState = GameState.Inventory;
                        player.inputManager.OnGamePaused();
                        return;
                    }
                }

                if (KeyboardState.IsKeyPressed(Keys.X))
                {
                    wireframeMode = !wireframeMode;
                    if (wireframeMode)
                    {
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    }
                    else
                    {
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    }
                }

                float deltaTime = (float)args.Time;

                player.inputManager.KeyboardUpdate(KeyboardState, args);
                chunkManager.UpdateChunks(player.Camera.Position);
                chunkManager.UploadPendingMeshes();

                if (_currentState == GameState.InGame)
                    player.Update(deltaTime);
            }
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            if (_currentState == GameState.InGame && player != null)
            {
                player.inputManager.MouseUpdate(MouseState);
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (_currentState == GameState.InGame && player != null)
            {
                player.inputManager.HandleMouseDown(e);
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            _imGuiController.MouseScroll(new Vector2(e.OffsetX, e.OffsetY));

            if (_currentState == GameState.InGame || _currentState == GameState.Inventory && player != null)
            {
                player.inputManager.HandleMouseScroll(e);
            }
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            _imGuiController.PressChar((char)e.Unicode);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (_currentState == GameState.TitleScreen)
            {
                _titleScreen.Render();
                _imGuiController.Render();
            }
            else if (_currentState == GameState.InGame || _currentState == GameState.Pause || _currentState == GameState.Inventory)
            {
                WorldTexture?.Use(TextureUnit.Texture0);
                shaderProgram?.Use();

                Matrix4 model = Matrix4.Identity;
                Matrix4 view = player.Camera.GetViewMatrix();
                Matrix4 projection = player.Camera.GetProjectionMatrix();

                int modelLoc = GL.GetUniformLocation(shaderProgram.Handle, "model");
                int viewLoc = GL.GetUniformLocation(shaderProgram.Handle, "view");
                int projLoc = GL.GetUniformLocation(shaderProgram.Handle, "projection");
                int lightPosLoc = GL.GetUniformLocation(shaderProgram.Handle, "lightPos");
                int viewPosLoc = GL.GetUniformLocation(shaderProgram.Handle, "viewPos");

                GL.UniformMatrix4(modelLoc, false, ref model);
                GL.UniformMatrix4(viewLoc, false, ref view);
                GL.UniformMatrix4(projLoc, false, ref projection);
                GL.Uniform3(lightPosLoc, new Vector3(0, 200, 0));
                GL.Uniform3(viewPosLoc, player.Camera.Position);


                chunkManager.RenderChunks();

                if (_currentState == GameState.Pause)
                {
                    _pauseMenu.Render();
                }
                else if (_currentState == GameState.Inventory)
                {
                    _inventory.Render();
                }

                _textRenderer.RenderText($"World: {_currentWorldName} | {UIText} | {currentChunkPosition}", 50f, Size.Y - 100f, .5f, new Vector3(1.0f, 1.0f, 1.0f), _projection);
                _textRenderer.RenderText("O", Size.X / 2, Size.Y / 2, .5f, new Vector3(1.0f, 1.0f, 1.0f), _projection);

                if (_hotbar != null)
                {
                    _hotbar.Render(_projection, Size);
                }

                _imGuiController.Render();
            }

            SwapBuffers();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);

            _projection = Matrix4.CreateOrthographicOffCenter(0, Size.X, 0, Size.Y, -1, 1);

            _imGuiController?.WindowResized(e.Width, e.Height);

            if (player != null)
            {
                player.Camera.AspectRatio = e.Width / (float)e.Height;
            }
        }

        private void SetupHotbarBlocks()
        {
            _hotbar.SetBlockInSlot(0, BlockRegistry.GetBlock(BlockIDs.Stone));
            _hotbar.SetBlockInSlot(1, BlockRegistry.GetBlock(BlockIDs.Dirt));
            _hotbar.SetBlockInSlot(2, BlockRegistry.GetBlock(BlockIDs.Grass));
            _hotbar.SetBlockInSlot(3, BlockRegistry.GetBlock(BlockIDs.Log));
            _hotbar.SetBlockInSlot(4, BlockRegistry.GetBlock(BlockIDs.Glass));
            _hotbar.SetBlockInSlot(5, BlockRegistry.GetBlock(BlockIDs.Plank));
            _hotbar.SetBlockInSlot(6, BlockRegistry.GetBlock(BlockIDs.Leaves));
            _hotbar.SetBlockInSlot(7, BlockRegistry.GetBlock(BlockIDs.Red));
            _hotbar.SetBlockInSlot(8, BlockRegistry.GetBlock(BlockIDs.Green));
            _hotbar.SetBlockInSlot(9, BlockRegistry.GetBlock(BlockIDs.Blue));
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            chunkManager?.Dispose();
            shaderProgram?.Dispose();
            _textRenderer?.Dispose();
            blockHighlighter?.Dispose();
            _imGuiController?.Dispose();

            WorldTexture?.Dispose();
        }

        private void CloseGame()
        {
            foreach (var activeChunks in chunkManager._chunks)
            {
                if (activeChunks.Value.Modified)
                {
                    Serialization.SaveChunk(activeChunks.Value);
                }
            }

            Close();
        }

        public void ChangeBlockInCurrentSlot(IBlock? block)
        {
            _hotbar.SetBlockInSlot(block);
            this.player.terrainModifier.CurrentBlock = (byte)block.ID;
            UIText = $"Current Block: {block.Name}";
        }

        private void ResumeGame()
        {
            _currentState = GameState.InGame;
            CursorState = CursorState.Grabbed;
        }
    }
}