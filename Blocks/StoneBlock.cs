using OpenTK.Mathematics;
using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class StoneBlock : IBlock
    {
        public int ID => 1;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(0, 0);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;
        public bool IsSolid => true;

        public string Name => "Stone";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Stone;
    }
}
