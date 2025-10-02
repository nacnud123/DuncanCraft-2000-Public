using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class TorchBlock : IBlock
    {
        public int ID => 21;

        public TextureCoords TopTextureCoords => UVHelper.FromPartialTile(6, 0, 7, 7, 2, 2); // Top of torch - 2x2 square

        public TextureCoords BottomTextureCoords => UVHelper.FromPartialTile(6, 0, 7, 0, 2, 2); // Bottom of torch - 2x2 square
        public TextureCoords SideTextureCoords => UVHelper.FromPartialTile(6, 0, 7, 0, 2, 9); // Side of torch - 2x9 rectangle
        public bool IsSolid => false;

        public string Name => "Torch";

        public TextureCoords InventoryCoords => UVHelper.FromTileCoords(6, 0);
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Wooden;
        public bool Transparent => true;
        public byte LightLevel => 14;
        public byte LightOpacity => 0;
        public BlockRenderingType RenderingType => BlockRenderingType.Torch;
        public bool HasCollision => false;
    }
}