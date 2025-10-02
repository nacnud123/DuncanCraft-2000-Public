using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class SlabBlock : IBlock
    {
        public int ID => 15;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(2, 2);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;

        public bool IsSolid => false;

        public string Name => "Wood Slab";

        public TextureCoords InventoryCoords => UVHelper.FromTileCoords(4, 0);
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Wooden;
        public bool Transparent => false;
        public byte LightLevel => 0;
        public byte LightOpacity => 15;
        public BlockRenderingType RenderingType => BlockRenderingType.Half;
    }
}
