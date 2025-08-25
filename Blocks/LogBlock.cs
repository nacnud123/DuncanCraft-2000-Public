using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class LogBlock : IBlock
    {
        public int ID => 5;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(2, 0);
        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => UVHelper.FromTileCoords(1, 0);

        public bool IsSolid => true;

        public string Name => "Logs";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Wooden;
        public bool Transparent => false;
        public byte LightLevel => 0;
        public byte LightOpacity => 15;
    }
}
