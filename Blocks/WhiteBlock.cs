using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class WhiteBlock : IBlock
    {
        public int ID => 9;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(3, 0);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;
        public bool IsSolid => true;

        public string Name => "White";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Wool;
        public bool Transparent => false;
        public byte LightLevel => 0;
        public byte LightOpacity => 15;
    }
}
