using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class SlabBlock : IBlock
    {
        public int ID => 14;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(2, 2);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;

        public bool IsSolid => false;

        public string Name => "Wood Slab";

        public TextureCoords InventoryCoords => UVHelper.FromTileCoords(4, 0);
        public bool GravityBlock => false;
    }
}
