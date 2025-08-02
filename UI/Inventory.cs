// Makes the inventory UI. | DA | 8/1/25
using ImGuiNET;
using System.Diagnostics;
using System.Numerics;
using VoxelGame.Blocks;
using VoxelGame.Utils;

namespace VoxelGame.UI
{
    public class Inventory : IDisposable
    {
        private bool mIsOpen = true;
        private int mSelectedBlockType = 1;
        private int mButtonsPerRow = 5;
        private float mButtonSize = 64.0f;

        private Vector2 mWindowPadding = new Vector2(50, 80);
        private Vector2 mContentPadding = new Vector2(20, 30);

        private Texture? mBlockAtlasTexture;
        private IntPtr mBlockAtlasTexturePtr;

        private List<KeyValuePair<byte, IBlock>> mSelectableBlocks;

        public bool IsOpen => mIsOpen;
        public int SelectedBlockType => mSelectedBlockType;

        public Inventory(Texture blockAtlasTexture)
        {
            mSelectableBlocks = BlockRegistry.GetAllBocks.Where(kvp => kvp.Key != BlockIDs.Air && kvp.Key != BlockIDs.Bedrock).ToList();
            mBlockAtlasTexture = blockAtlasTexture;
            loadTextures();
        }

        private void loadTextures()
        {
            mBlockAtlasTexturePtr = new IntPtr(mBlockAtlasTexture.Handle);
        }

        public void Render()
        {
            var io = ImGui.GetIO();
            var displaySize = io.DisplaySize;

            Vector2 windowPos = new Vector2(mWindowPadding.X, mWindowPadding.Y);
            Vector2 windowSize = new Vector2(
                displaySize.X - (mWindowPadding.X * 2),
                displaySize.Y - (mWindowPadding.Y * 2)
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

            
            ImGui.Dummy(new Vector2(0, mContentPadding.Y)); // Top Padding

            float leftPadding = mContentPadding.X;

            float totalButtonWidth = mButtonsPerRow * mButtonSize + (mButtonsPerRow - 1) * ImGui.GetStyle().ItemSpacing.X;
            Vector2 contentRegion = ImGui.GetContentRegionAvail();

            contentRegion.X -= mContentPadding.X * 2;

            int numRows = (int)Math.Ceiling((double)mSelectableBlocks.Count / mButtonsPerRow);

            for (int row = 0; row < numRows; row++)
            {
                int startIndex = row * mButtonsPerRow;
                int endIndex = Math.Min(startIndex + mButtonsPerRow, mSelectableBlocks.Count);
                int buttonsInThisRow = endIndex - startIndex;

                float thisRowWidth = buttonsInThisRow * mButtonSize + (buttonsInThisRow - 1) * ImGui.GetStyle().ItemSpacing.X;

                float centerOffset = (contentRegion.X - thisRowWidth) * 0.5f;
                float totalLeftOffset = leftPadding + Math.Max(0, centerOffset);

                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + totalLeftOffset);

                // Draw buttons
                for (int i = startIndex; i < endIndex; i++)
                {
                    int col = i % mButtonsPerRow;

                    if (col > 0)
                    {
                        ImGui.SameLine();
                    }

                    var blockEntry = mSelectableBlocks[i];
                    byte blockId = blockEntry.Key;
                    var block = blockEntry.Value;

                    var texCoords = block.InventoryCoords;

                    ImGui.PushID(i);

                    if (mSelectedBlockType == blockId)
                    {
                        var drawList = ImGui.GetWindowDrawList();
                        var pos = ImGui.GetCursorScreenPos() - new Vector2(-3, -2);
                        var borderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 0.0f, 1.0f));
                        drawList.AddRect(pos - Vector2.One * 2, pos + new Vector2(mButtonSize + 4, mButtonSize + 4), borderColor, 0, 0, 3.0f);
                    }

                    bool clicked = ImGui.ImageButton(
                        $"block_{blockId}",
                        mBlockAtlasTexturePtr,
                        new Vector2(mButtonSize, mButtonSize),
                        new Vector2(texCoords.TopLeft.X, texCoords.BottomRight.Y),
                        new Vector2(texCoords.BottomRight.X, texCoords.TopLeft.Y),
                        new Vector4(0, 0, 0, 0),
                        new Vector4(1, 1, 1, 1)
                    );

                    if (clicked)
                    {
                        Console.WriteLine($"Selected block: {block.Name}");
                        mSelectedBlockType = blockId;

                        VoxelGame.init.ChangeBlockInCurrentSlot(block);

                    }

                    ImGui.PopID();
                }
            }

            ImGui.Dummy(new Vector2(0, mContentPadding.Y));// Bottom padding

            ImGui.End();
        }

        public void Dispose()
        {
            mBlockAtlasTexture?.Dispose();
        }
    }
}