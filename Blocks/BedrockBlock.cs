using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class BedrockBlock : IBlock
    {
        public int ID => 20;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(5, 0);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;

        public bool IsSolid => true;

        public string Name => "Bedrock";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Stone;
        public bool Transparent => false;
        public byte LightLevel => 0;
        public byte LightOpacity => 15;
    }
}
