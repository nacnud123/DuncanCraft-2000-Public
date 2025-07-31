using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class BlackBlock : IBlock
    {
        public int ID => 13;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(3, 4);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;
        public bool IsSolid => true;

        public string Name => "Blackish";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Wool;
    }
}
