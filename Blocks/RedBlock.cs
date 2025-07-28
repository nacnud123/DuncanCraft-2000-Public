using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class RedBlock : IBlock
    {
        public int ID => 9;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(3, 1);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;
        public bool IsSolid => true;

        public string Name => "Red";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Wool;
    }
}
