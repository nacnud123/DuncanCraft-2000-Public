using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class SpongeBlock : IBlock
    {
        public int ID => 25;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(6, 1);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;
        public bool IsSolid => true;

        public string Name => "Sponge";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Dirt;
        public bool Transparent => false;
        public byte LightLevel => 0;
        public byte LightOpacity => 15;
    }
}
