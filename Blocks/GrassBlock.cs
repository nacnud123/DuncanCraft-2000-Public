using OpenTK.Mathematics;
using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class GrassBlock : IBlock
    {
        public int ID => 3;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(0, 2);
        public TextureCoords BottomTextureCoords => UVHelper.FromTileCoords(1, 1);
        public TextureCoords SideTextureCoords => UVHelper.FromTileCoords(0, 1);

        public bool IsSolid => true;

        public string Name => "Grass";

        public TextureCoords InventoryCoords => SideTextureCoords;
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Dirt;
    }
}
