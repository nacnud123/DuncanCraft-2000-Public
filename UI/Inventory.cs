using ImGuiNET;
using System.Diagnostics;
using System.Numerics;
using VoxelGame.Blocks;
using VoxelGame.Utils;

namespace VoxelGame.UI
{
    public class Inventory : IDisposable
    {
        private bool _isOpen = true;
        private int _selectedBlockType = 1;
        private int _buttonsPerRow = 5;
        private float _buttonSize = 64.0f;

        private Vector2 _windowPadding = new Vector2(50, 80);
        private Vector2 _contentPadding = new Vector2(20, 30);

        private Texture? _blockAtlasTexture;
        private IntPtr _blockAtlasTexturePtr;

        public bool IsOpen => _isOpen;
        public int SelectedBlockType => _selectedBlockType;
        List<KeyValuePair<byte, IBlock>> selectableBlocks;

        public void ToggleMenu() => _isOpen = !_isOpen;
        public void CloseMenu() => _isOpen = false;

        public Inventory(Texture blockAtlasTexture)
        {
            selectableBlocks = BlockRegistry.GetAllBocks.Where(kvp => kvp.Key != BlockIDs.Air).ToList();
            _blockAtlasTexture = blockAtlasTexture;
            loadTextures();
        }

        private void loadTextures()
        {
            _blockAtlasTexturePtr = new IntPtr(_blockAtlasTexture.Handle);
        }

        public void Render()
        {
            var io = ImGui.GetIO();
            var displaySize = io.DisplaySize;

            Vector2 windowPos = new Vector2(_windowPadding.X, _windowPadding.Y);
            Vector2 windowSize = new Vector2(
                displaySize.X - (_windowPadding.X * 2),
                displaySize.Y - (_windowPadding.Y * 2)
            );

            ImGui.SetNextWindowPos(windowPos);
            ImGui.SetNextWindowSize(windowSize);

            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoTitleBar |
                                             ImGuiWindowFlags.NoResize |
                                             ImGuiWindowFlags.NoMove |
                                             ImGuiWindowFlags.NoCollapse |
                                             ImGuiWindowFlags.NoBringToFrontOnFocus |
                                             ImGuiWindowFlags.NoFocusOnAppearing;

            ImGui.Begin("Block Selection Menu", windowFlags);

            
            ImGui.Dummy(new Vector2(0, _contentPadding.Y)); // Top Padding

            float leftPadding = _contentPadding.X;

            float totalButtonWidth = _buttonsPerRow * _buttonSize + (_buttonsPerRow - 1) * ImGui.GetStyle().ItemSpacing.X;
            Vector2 contentRegion = ImGui.GetContentRegionAvail();

            contentRegion.X -= _contentPadding.X * 2;

            int numRows = (int)Math.Ceiling((double)selectableBlocks.Count / _buttonsPerRow);

            for (int row = 0; row < numRows; row++)
            {
                int startIndex = row * _buttonsPerRow;
                int endIndex = Math.Min(startIndex + _buttonsPerRow, selectableBlocks.Count);
                int buttonsInThisRow = endIndex - startIndex;

                float thisRowWidth = buttonsInThisRow * _buttonSize + (buttonsInThisRow - 1) * ImGui.GetStyle().ItemSpacing.X;

                float centerOffset = (contentRegion.X - thisRowWidth) * 0.5f;
                float totalLeftOffset = leftPadding + Math.Max(0, centerOffset);

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + totalLeftOffset);

                // Draw buttons
                for (int i = startIndex; i < endIndex; i++)
                {
                    int col = i % _buttonsPerRow;

                    if (col > 0)
                    {
                        ImGui.SameLine();
                    }

                    var blockEntry = selectableBlocks[i];
                    byte blockId = blockEntry.Key;
                    var block = blockEntry.Value;

                    var texCoords = block.InventoryCoords;

                    ImGui.PushID(i);

                    if (_selectedBlockType == blockId)
                    {
                        var drawList = ImGui.GetWindowDrawList();
                        var pos = ImGui.GetCursorScreenPos() - new Vector2(-3, -2);
                        var borderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 0.0f, 1.0f));
                        drawList.AddRect(pos - Vector2.One * 2, pos + new Vector2(_buttonSize + 4, _buttonSize + 4), borderColor, 0, 0, 3.0f);
                    }

                    bool clicked = ImGui.ImageButton(
                        $"block_{blockId}",
                        _blockAtlasTexturePtr,
                        new Vector2(_buttonSize, _buttonSize),
                        new Vector2(texCoords.TopLeft.X, texCoords.BottomRight.Y),
                        new Vector2(texCoords.BottomRight.X, texCoords.TopLeft.Y),
                        new Vector4(0, 0, 0, 0),
                        new Vector4(1, 1, 1, 1)
                    );

                    if (clicked)
                    {
                        Console.WriteLine($"Selected block: {block.Name}");
                        _selectedBlockType = blockId;

                        VoxelGame.init.ChangeBlockInCurrentSlot(block);

                    }

                    ImGui.PopID();
                }
            }

            ImGui.Dummy(new Vector2(0, _contentPadding.Y));// Bottom padding

            ImGui.End();
        }

        public void Dispose()
        {
            _blockAtlasTexture?.Dispose();
        }
    }
}