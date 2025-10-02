using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class DuncanBlock : IBlock
    {
        public int ID => 24;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(7, 1);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;
        public bool IsSolid => true;

        public string Name => "Duncan";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Glass;
        public bool Transparent => false;
        public byte LightLevel => 0;
        public byte LightOpacity => 15;
    }
}
