using OpenTK.Mathematics;
using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class DirtBlock : IBlock
    {
        public int ID => 2;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(1, 1);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;
        public bool IsSolid => true;

        public string Name => "Dirt";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Dirt;
    }
}
