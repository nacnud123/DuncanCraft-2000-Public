using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class GreenBlock : IBlock
    {
        public int ID => 10;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(3, 2);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;
        public bool IsSolid => true;

        public string Name => "Green";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => false;
    }
}
