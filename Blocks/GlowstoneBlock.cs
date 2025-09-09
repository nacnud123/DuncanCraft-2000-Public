using DuncanCraft.Utils;

namespace DuncanCraft.Blocks
{
    public class GlowstoneBlock : IBlock
    {
        public byte ID => 5;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(3, 0);

        public TextureCoords BottomTextureCoords => TopTextureCoords;

        public TextureCoords SideTextureCoords => TopTextureCoords;

        public string Name => "Glowstone";
        
        public byte LightOpacity => 15;
        
        public byte LightValue => 15;
    }
}