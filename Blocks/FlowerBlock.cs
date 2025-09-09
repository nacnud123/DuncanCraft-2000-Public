using DuncanCraft.Utils;

namespace DuncanCraft.Blocks
{
    public class FlowerBlock : IBlock
    {
        public byte ID => 7;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(4, 1);
        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;

        public string Name => "Flower";
        
        public byte LightOpacity => 0;
        
        public byte LightValue => 0;
    }
}