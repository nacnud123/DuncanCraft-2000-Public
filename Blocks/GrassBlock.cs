using OpenTK.Mathematics;
using VoxelGame.Utils;
using VoxelGame.Ticking;
using VoxelGame.World;

namespace VoxelGame.Blocks
{
    public class GrassBlock : IBlock
    {
        public int ID => 3;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(0, 2);
        public TextureCoords BottomTextureCoords => UVHelper.FromTileCoords(1, 1);
        public TextureCoords SideTextureCoords => UVHelper.FromTileCoords(0, 1);

        public bool IsSolid => true;

        public string Name => "Grass Block";

        public TextureCoords InventoryCoords => SideTextureCoords;
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Dirt;
        public bool Transparent => false;
        public byte LightLevel => 0;
        public byte LightOpacity => 15;
    }
}
