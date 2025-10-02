using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class GravelBlock : IBlock
    {
        public int ID => 28;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(6, 2);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;
        public bool IsSolid => true;

        public string Name => "Sand";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => true;
        public BlockMaterial Material => BlockMaterial.Sand;
        public bool Transparent => false;
        public byte LightLevel => 0;
        public byte LightOpacity => 15;
    }
}
