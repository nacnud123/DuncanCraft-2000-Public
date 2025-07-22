using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class SandBlock : IBlock
    {
        public int ID => 15;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(0, 3);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;
        public bool IsSolid => true;

        public string Name => "Red";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => true;
    }
}
