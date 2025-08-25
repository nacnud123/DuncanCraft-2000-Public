using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class IronOreBlock : IBlock
    {
        public int ID => 18;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(2, 3);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;

        public bool IsSolid => true;

        public string Name => "Iron Ore";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Stone;
        public bool Transparent => false;
        public byte LightLevel => 0;
        public byte LightOpacity => 15;
    }
}
