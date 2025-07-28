using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class GlassBlock : IBlock
    {
        public int ID => 5;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(2, 1);

        public TextureCoords BottomTextureCoords => TopTextureCoords;

        public TextureCoords SideTextureCoords => TopTextureCoords;

        public bool IsSolid => false;

        public string Name => "Glass";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Glass;
    }
}
