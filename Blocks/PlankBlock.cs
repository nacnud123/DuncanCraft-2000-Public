using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class PlankBlock : IBlock
    {
        public int ID => 7;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(2, 2);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;
        public bool IsSolid => true;

        public string Name => "Planks";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Wooden;
        public bool Transparent => false;
        public byte LightLevel => 0;
        public byte LightOpacity => 15;
    }
}
