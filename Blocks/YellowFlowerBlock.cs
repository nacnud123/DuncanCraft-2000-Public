using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    internal class YellowFlowerBlock : IBlock
    {
        public int ID => 14;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(4, 1);

        public TextureCoords BottomTextureCoords => TopTextureCoords;

        public TextureCoords SideTextureCoords => TopTextureCoords;

        public bool IsSolid => false;

        public string Name => "Yellow Flower";

        public TextureCoords InventoryCoords => TopTextureCoords;

        public bool GravityBlock => false;

        public BlockMaterial Material => BlockMaterial.Leaves;
        public bool Transparent => true;
        public byte LightLevel => 0;
        public byte LightOpacity => 0;
    }
}