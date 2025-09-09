using DuncanCraft.Utils;
using System;

namespace DuncanCraft.Blocks
{
    public class AirBlock : IBlock
    {
        public byte ID => 0;

        public TextureCoords TopTextureCoords => throw new NotImplementedException();

        public TextureCoords BottomTextureCoords => throw new NotImplementedException();

        public TextureCoords SideTextureCoords => throw new NotImplementedException();

        public string Name => "AIR";
        
        public byte LightOpacity => 0;
        
        public byte LightValue => 0;
    }
}
