using DuncanCraft.Utils;

namespace DuncanCraft.Blocks
{
    public class GrassBlock : IBlock
    {
        public byte ID => 3;
        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(0, 2);
        public TextureCoords BottomTextureCoords => UVHelper.FromTileCoords(1, 1);
        public TextureCoords SideTextureCoords => UVHelper.FromTileCoords(0, 1);

        public string Name => "Grass";
        
        public byte LightOpacity => 15;
        
        public byte LightValue => 0;
    }
}
