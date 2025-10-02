// Makes the hotbar UI. Also manages the current block used. | DA | 8/1/25
using OpenTK.Mathematics;
using ImGuiNET;
using System.Numerics;
using VoxelGame.Utils;
using VoxelGame.Blocks;
using Vector2 = System.Numerics.Vector2;

namespace VoxelGame.UI
{
    public class Hotbar : IDisposable
    {
        private Texture? mHotbarTexture;
        private Texture? mSelectorTexture;
        private Texture? mBlockAtlasTexture;

        private IntPtr mHotbarTexturePtr;
        private IntPtr mSelectorTexturePtr;
        private IntPtr mBlockAtlasTexturePtr;

        private const int HOTBAR_SLOTS = 10;
        private int mSelectedSlot = 0;

        private IBlock?[] mHotbarBlocks = new IBlock?[HOTBAR_SLOTS];

        // Hotbar dimensions and positioning
        private const float HOTBAR_WIDTH = 320f * GameConstants.UI_SCALE;
        private const float HOTBAR_HEIGHT = 32f * GameConstants.UI_SCALE;
        private const float SELECTOR_WIDTH = 32f * GameConstants.UI_SCALE;
        private const float SELECTOR_HEIGHT = 32f * GameConstants.UI_SCALE;
        private const float SLOT_WIDTH = 32f * GameConstants.UI_SCALE;
        private const float SLOT_SPACING = 0f;
        private const float ITEM_SIZE = 24f * GameConstants.UI_SCALE;
        private const float ITEM_OFFSET = 4f * GameConstants.UI_SCALE;

        public Hotbar(Texture blockAtlasTexture)
        {
            mBlockAtlasTexture = blockAtlasTexture;
            loadTextures();
        }

        private void loadTextures()
        {
            mHotbarTexture = Texture.LoadFromFile("Resources/hotbar.png");
            mSelectorTexture = Texture.LoadFromFile("Resources/selector.png");

            mHotbarTexturePtr = new IntPtr(mHotbarTexture.Handle);
            mSelectorTexturePtr = new IntPtr(mSelectorTexture.Handle);
            mBlockAtlasTexturePtr = new IntPtr(mBlockAtlasTexture.Handle);
        }

        public void SetHotbarSlot(int slot)
        {
            mSelectedSlot = Math.Clamp(slot, 0, HOTBAR_SLOTS - 1);
        }

        public int MoveHotbarSlot(int amount)
        {
            mSelectedSlot = Math.Clamp(mSelectedSlot + amount, 0, HOTBAR_SLOTS - 1);
            return GetBlockInSlot(mSelectedSlot).ID;
        }

        public void SetBlockInSlot(IBlock? block) => SetBlockInSlot(mSelectedSlot, block);

        public void SetBlockInSlot(int slot, IBlock? block)
        {
            if (slot >= 0 && slot < HOTBAR_SLOTS)
            {
                mHotbarBlocks[slot] = block;
            }
        }

        public IBlock? GetBlockInSlot(int slot)
        {
            if (slot >= 0 && slot < HOTBAR_SLOTS)
            {
                return mHotbarBlocks[slot];
            }
            return null;
        }

        public IBlock? GetSelectedBlock()
        {
            return GetBlockInSlot(mSelectedSlot);
        }

        // Unused
        public void ClearSlot(int slot)
        {
            SetBlockInSlot(slot, null);
        }

        public void ClearAllSlots()
        {
            for (int i = 0; i < HOTBAR_SLOTS; i++)
            {
                mHotbarBlocks[i] = null;
            }
        }
        //---------------

        public void Render(Matrix4 projection, OpenTK.Mathematics.Vector2 screenSize)
        {
            var drawList = ImGui.GetBackgroundDrawList();

            float hotbarX = (screenSize.X - HOTBAR_WIDTH) / 2f;
            float hotbarY = screenSize.Y - HOTBAR_HEIGHT - 50f;

            // Draw hotbar background
            drawList.AddImage(
                mHotbarTexturePtr,
                new Vector2(hotbarX, hotbarY),
                new Vector2(hotbarX + HOTBAR_WIDTH, hotbarY + HOTBAR_HEIGHT),
                new Vector2(0, 1),
                new Vector2(1, 0)
            );

            // Draw selection highlight
            float selectorX = hotbarX + (mSelectedSlot * (SLOT_WIDTH + SLOT_SPACING));
            drawList.AddImage(
                mSelectorTexturePtr,
                new Vector2(selectorX, hotbarY),
                new Vector2(selectorX + SELECTOR_WIDTH, hotbarY + SELECTOR_HEIGHT),
                new Vector2(0, 1),
                new Vector2(1, 0)
            );

            // Draw block icons
            for (int i = 0; i < HOTBAR_SLOTS; i++)
            {
                var block = mHotbarBlocks[i];
                if (block != null)
                {
                    float slotX = hotbarX + (i * (SLOT_WIDTH + SLOT_SPACING));
                    float itemX = slotX + ITEM_OFFSET;
                    float itemY = hotbarY + ITEM_OFFSET;

                    var texCoords = block.InventoryCoords;

                    // Draw block texture with proper UV mapping
                    drawList.AddImage(
                        mBlockAtlasTexturePtr,
                        new Vector2(itemX, itemY),
                        new Vector2(itemX + ITEM_SIZE, itemY + ITEM_SIZE),
                        new Vector2(texCoords.TopLeft.X, texCoords.BottomRight.Y),
                        new Vector2(texCoords.BottomRight.X, texCoords.TopLeft.Y)
                    );
                }
            }
        }

        public void Dispose()
        {
            mHotbarTexture?.Dispose();
            mSelectorTexture?.Dispose();
            mBlockAtlasTexture?.Dispose();
        }
    }
}