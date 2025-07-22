using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class LeavesBlock : IBlock
    {
        public int ID => 7;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(1, 2);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;
        public bool IsSolid => false;

        public string Name => "Leaves";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => false;
    }
}
