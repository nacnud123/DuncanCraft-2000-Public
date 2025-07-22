using OpenTK.Mathematics;
using ImGuiNET;
using System.Numerics; // Add this namespace
using VoxelGame.Utils;
using VoxelGame.Blocks;

namespace VoxelGame.UI
{
    public class Hotbar : IDisposable
    {
        private Texture? _hotbarTexture;
        private Texture? _selectorTexture;
        private Texture? _blockAtlasTexture;

        private IntPtr _hotbarTexturePtr;
        private IntPtr _selectorTexturePtr;
        private IntPtr _blockAtlasTexturePtr;

        private const int HOTBAR_SLOTS = 10;
        private int _selectedSlot = 0;

        private IBlock?[] _hotbarBlocks = new IBlock?[HOTBAR_SLOTS];

        // Hotbar dimensions and positioning
        private const float HOTBAR_WIDTH = 320f * Constants.UI_SCALE;
        private const float HOTBAR_HEIGHT = 32f * Constants.UI_SCALE;
        private const float SELECTOR_WIDTH = 32f * Constants.UI_SCALE;
        private const float SELECTOR_HEIGHT = 32f * Constants.UI_SCALE;
        private const float SLOT_WIDTH = 32f * Constants.UI_SCALE;
        private const float SLOT_SPACING = 0f;
        private const float ITEM_SIZE = 24f * Constants.UI_SCALE;
        private const float ITEM_OFFSET = 4f * Constants.UI_SCALE;

        public Hotbar(Texture blockAtlasTexture)
        {
            _blockAtlasTexture = blockAtlasTexture;
            loadTextures();
        }

        private void loadTextures()
        {
            _hotbarTexture = Texture.LoadFromFile("Resources/hotbar.png");
            _selectorTexture = Texture.LoadFromFile("Resources/selector.png");

            _hotbarTexturePtr = new IntPtr(_hotbarTexture.Handle);
            _selectorTexturePtr = new IntPtr(_selectorTexture.Handle);
            _blockAtlasTexturePtr = new IntPtr(_blockAtlasTexture.Handle);
        }

        public void SetHotbarSlot(int slot)
        {
            _selectedSlot = Math.Clamp(slot, 0, HOTBAR_SLOTS - 1);
        }

        public int MoveHotbarSlot(int amount)
        {
            _selectedSlot = Math.Clamp(_selectedSlot + amount, 0, HOTBAR_SLOTS - 1);
            return GetBlockInSlot(_selectedSlot).ID;
        }

        public void SetBlockInSlot(IBlock? block) => SetBlockInSlot(_selectedSlot, block);

        public void SetBlockInSlot(int slot, IBlock? block)
        {
            if (slot >= 0 && slot < HOTBAR_SLOTS)
            {
                _hotbarBlocks[slot] = block;
            }
        }

        public IBlock? GetBlockInSlot(int slot)
        {
            if (slot >= 0 && slot < HOTBAR_SLOTS)
            {
                return _hotbarBlocks[slot];
            }
            return null;
        }

        public IBlock? GetSelectedBlock()
        {
            return GetBlockInSlot(_selectedSlot);
        }

        public void ClearSlot(int slot)
        {
            SetBlockInSlot(slot, null);
        }

        public void ClearAllSlots()
        {
            for (int i = 0; i < HOTBAR_SLOTS; i++)
            {
                _hotbarBlocks[i] = null;
            }
        }

        public void Render(Matrix4 projection, OpenTK.Mathematics.Vector2 screenSize)
        {
            var drawList = ImGui.GetBackgroundDrawList();

            float hotbarX = (screenSize.X - HOTBAR_WIDTH) / 2f;
            float hotbarY = screenSize.Y - HOTBAR_HEIGHT - 50f;

            // Draw hotbar
            drawList.AddImage(
                _hotbarTexturePtr,
                new System.Numerics.Vector2(hotbarX, hotbarY),
                new System.Numerics.Vector2(hotbarX + HOTBAR_WIDTH, hotbarY + HOTBAR_HEIGHT),
                new System.Numerics.Vector2(0, 1),
                new System.Numerics.Vector2(1, 0)
            );

            // Draw selector
            float selectorX = hotbarX + (_selectedSlot * (SLOT_WIDTH + SLOT_SPACING));
            drawList.AddImage(
                _selectorTexturePtr,
                new System.Numerics.Vector2(selectorX, hotbarY),
                new System.Numerics.Vector2(selectorX + SELECTOR_WIDTH, hotbarY + SELECTOR_HEIGHT),
                new System.Numerics.Vector2(0, 1),
                new System.Numerics.Vector2(1, 0)
            );

            for (int i = 0; i < HOTBAR_SLOTS; i++)
            {
                var block = _hotbarBlocks[i];
                if (block != null)
                {
                    // Slot's position
                    float slotX = hotbarX + (i * (SLOT_WIDTH + SLOT_SPACING));
                    float itemX = slotX + ITEM_OFFSET;
                    float itemY = hotbarY + ITEM_OFFSET;

                    var texCoords = block.InventoryCoords;

                    // Draw block texture
                    drawList.AddImage(
                        _blockAtlasTexturePtr,
                        new System.Numerics.Vector2(itemX, itemY),
                        new System.Numerics.Vector2(itemX + ITEM_SIZE, itemY + ITEM_SIZE),
                        new System.Numerics.Vector2(texCoords.TopLeft.X, texCoords.BottomRight.Y),
                        new System.Numerics.Vector2(texCoords.BottomRight.X, texCoords.TopLeft.Y)
                    );
                }
            }
        }

        public void Dispose()
        {
            _hotbarTexture?.Dispose();
            _selectorTexture?.Dispose();
            _blockAtlasTexture?.Dispose();
        }
    }
}