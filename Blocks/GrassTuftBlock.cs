using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class GrassTuftBlock : IBlock
    {
        public int ID => 23;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(5, 1);

        public TextureCoords BottomTextureCoords => TopTextureCoords;

        public TextureCoords SideTextureCoords => TopTextureCoords;

        public bool IsSolid => false;

        public string Name => "Grass";

        public TextureCoords InventoryCoords => TopTextureCoords;

        public bool GravityBlock => false;

        public BlockMaterial Material => BlockMaterial.Leaves;
        public bool Transparent => true;
        public byte LightLevel => 0;
        public byte LightOpacity => 0;
        public BlockRenderingType RenderingType => BlockRenderingType.Cross;
        public bool HasCollision => false;
    }
}
