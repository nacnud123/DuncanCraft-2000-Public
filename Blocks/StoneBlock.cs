using DuncanCraft.Utils;

namespace DuncanCraft.Blocks
{
    public class StoneBlock : IBlock
    {
        public byte ID => 1;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(0, 0);

        public TextureCoords BottomTextureCoords => TopTextureCoords;

        public TextureCoords SideTextureCoords => TopTextureCoords;

        public string Name => "Stone";
        
        public byte LightOpacity => 15;
        
        public byte LightValue => 0;
    }
}
