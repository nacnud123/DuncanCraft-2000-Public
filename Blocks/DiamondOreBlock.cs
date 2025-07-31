using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoxelGame.Utils;

namespace VoxelGame.Blocks
{
    public class DiamondOreBlock : IBlock
    {
        public int ID => 20;

        public TextureCoords TopTextureCoords => UVHelper.FromTileCoords(2, 4);

        public TextureCoords BottomTextureCoords => TopTextureCoords;
        public TextureCoords SideTextureCoords => TopTextureCoords;

        public bool IsSolid => true;

        public string Name => "Diamond Ore";

        public TextureCoords InventoryCoords => TopTextureCoords;
        public bool GravityBlock => false;
        public BlockMaterial Material => BlockMaterial.Stone;
    }
}
