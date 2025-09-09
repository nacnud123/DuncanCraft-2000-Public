using DuncanCraft.Utils;

namespace DuncanCraft.Blocks
{
    public class TorchBlock : IBlock
    {
        public byte ID => 4;

        public TextureCoords TopTextureCoords => UVHelper.FromPartialTile(6, 0, 7, 7, 2, 2); // Top of torch - 2x2 square
        public TextureCoords BottomTextureCoords => UVHelper.FromPartialTile(6, 0, 7, 0, 2, 2); // Bottom of torch - 2x2 square
        public TextureCoords SideTextureCoords => UVHelper.FromPartialTile(6, 0, 7, 0, 2, 9); // Side of torch - 2x9 rectangle

        public string Name => "Torch";
        
        public byte LightOpacity => 0;
        
        public byte LightValue => 14;
    }
}