using DuncanCraft.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuncanCraft.Blocks
{
    public interface IBlock
    {
        public byte ID { get; }
        public TextureCoords TopTextureCoords { get; }
        public TextureCoords BottomTextureCoords { get; }
        public TextureCoords SideTextureCoords { get; }
        public string Name { get; }
        public byte LightOpacity { get; }
        public byte LightValue { get; }
    }
}
